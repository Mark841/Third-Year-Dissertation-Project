using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    // These are the list of settings the user can change in the Unity inspector that will affect the terrain
    public enum DrawMode { NoiseMap, Mesh, FalloffMap };
    public DrawMode drawMode;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureData textureData;

    public Material terrainMaterial;


    // The higher the level of detail goes the smaller the chunk size must be, 241 is largest it can be for LoD 6 if more LoD then chunk size must be decreased
    [Range(0, MeshSettings.numOfSupportedLevelsOfDetail - 1)]
    public int editorLevelOfDetail;

    // This determines if the terrain will update when a value is changed or only when the update button is pressed
    public bool autoUpdate;

    Queue<MapThreadInfo<HeightMap>> heightMapThreadInfoQueue = new Queue<MapThreadInfo<HeightMap>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    private void Start()
    {
        textureData.ApplyToMaterial(terrainMaterial);
        // Should be called here because of the effect of calling it on seperate threads so should be done before
        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minMeshHeight, heightMapSettings.maxMeshHeight);
    }

    void OnValuesUpdated()
    {
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    void OnTextureValuesUpdated()
    {
        textureData.ApplyToMaterial(terrainMaterial);
    }


    public void DrawMapInEditor()
    {

        textureData.UpdateMeshHeights(terrainMaterial, heightMapSettings.minMeshHeight, heightMapSettings.maxMeshHeight);
        // Make the noise and height maps
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.CHUNK_SIZE, heightMapSettings, Vector2.zero, meshSettings);
        // Find the object in unity that is using the MapDisplay script
        MapDisplay display = FindObjectOfType<MapDisplay>();

        if (drawMode == DrawMode.NoiseMap)
        { // If the selected mode is to draw the noise map, then display that
            display.drawTexture(TextureGenerator.TextureFromHeightMap(heightMap.noiseMap));
        }
        else if (drawMode == DrawMode.Mesh)
        { // If the selected mode is to draw the terrain map, then display that
            display.drawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.noiseMap, meshSettings, editorLevelOfDetail));
        }
        else if (drawMode == DrawMode.FalloffMap)
        { // If the selected mode is to draw the falloff map, then display that
            if (heightMapSettings.useFalloffMapPerChunk)
            {
                display.drawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.CHUNK_SIZE, heightMapSettings.falloffSize, heightMapSettings.falloffDistToEdge)));
            }
            else
            {
                display.drawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(3 * (meshSettings.CHUNK_SIZE), heightMapSettings.falloffSize, heightMapSettings.falloffDistToEdge)));
            }
        }
    }

    // This method starts other threads for HeightMap type
    public void RequestHeightMap(Vector2 centre, Action<HeightMap> callback)
    {
        ThreadStart threadStart = delegate { HeightMapThread(centre, callback); };
        new Thread(threadStart).Start();
    }
    // This method will be run on different threads, it generates the terrain for each of the chunks for HeightMap type
    void HeightMapThread(Vector2 centre, Action<HeightMap> callback)
    {
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.CHUNK_SIZE, heightMapSettings, centre, meshSettings);

        // Dont want the queue to be accessed at multiple times by mutliple threads so lock the queue until these lines have been run
        lock (heightMapThreadInfoQueue)
        {
            // Add the chunk to be processed onto a queue as only the main unity thread can process mesh info
            heightMapThreadInfoQueue.Enqueue(new MapThreadInfo<HeightMap>(callback, heightMap));
        }
    }

    // This method starts other threads for MeshData type
    public void RequestMeshData(HeightMap heightMap, int levelOfDetail, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate { MeshDataThread(heightMap, levelOfDetail, callback); };
        new Thread(threadStart).Start();
    }
    // This method will be run on different threads, it generates the terrain for each of the chunks for HeightMap type
    void MeshDataThread(HeightMap heightMap, int levelOfDetail, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap.noiseMap, meshSettings, editorLevelOfDetail);

        // Dont want the queue to be accessed at multiple times by mutliple threads so lock the queue until these lines have been run
        lock (meshDataThreadInfoQueue)
        {
            // Add the chunk to be processed onto a queue as only the main unity thread can process mesh info
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    private void Update()
    {
        // If the queue has any items in it
        if (heightMapThreadInfoQueue.Count > 0)
        { // Loop through all the items in the queue
            for (int i = 0; i < heightMapThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<HeightMap> threadInfo = heightMapThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }

        // If the queue has any items in it
        if (meshDataThreadInfoQueue.Count > 0)
        { // Loop through all the items in the queue
            for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    private void OnValidate()
    {
        if (editorLevelOfDetail < 0)
        {
            editorLevelOfDetail = 0;
        }

        if (meshSettings != null)
        {
            // Dont want to constantly add to it so if already added subtract first and then add again
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (heightMapSettings != null)
        {
            // Dont want to constantly add to it so if already added subtract first and then add again
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        if (textureData != null)
        {
            // Dont want to constantly add to it so if already added subtract first and then add again
            textureData.OnValuesUpdated -= OnTextureValuesUpdated;
            textureData.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }

    // Struct is generic to handle both Map and Mesh data
    struct MapThreadInfo<T>
    {
        // Once created we dont want to change the values of the variables
        public readonly Action<T> callback;
        public readonly T parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}

