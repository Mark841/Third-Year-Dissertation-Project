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

    public TerrainData terrainData;
    public NoiseData noiseData;
    public TextureData textureData;

    public Material terrainMaterial;

    // The higher the level of detail goes the smaller the chunk size must be, 241 is largest it can be for LoD 6 if more LoD then chunk size must be decreased
    [Range(0, 6)]
    public int editorLevelOfDetail;

    // This determines if the terrain will update when a value is changed or only when the update button is pressed
    public bool autoUpdate;

    float[,] falloffMapPerChunk;
    float[,] falloffMap;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();


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

    // Set the chunk size of the map
    public int CHUNK_SIZE
    {
        get
        {
            // Chose this number as divisible by all even numbers up to 12 so can have flexibility with LOD slider, has +1 added later which is why it doesnt look divisible at the moment
            if (terrainData.usingFlatShading)
            { // if using flatshading use a smaller chunksize, isn't divisible by 10 so cant use a LOD of 5
                return 95;
            }
            else
            {
                return 239;
            }
        }
    }

    public void DrawMapInEditor()
    {
        // Make the noise and height maps
        MapData mapData = GenerateMapData(Vector2.zero);
        // Find the object in unity that is using the MapDisplay script
        MapDisplay display = FindObjectOfType<MapDisplay>();

        if (drawMode == DrawMode.NoiseMap)
        { // If the selected mode is to draw the noise map, then display that
            display.drawTexture(TextureGenerator.TextureFromHeightMap(mapData.noiseMap));
        }
        else if (drawMode == DrawMode.Mesh)
        { // If the selected mode is to draw the terrain map, then display that
            display.drawMesh(MeshGenerator.GenerateTerrainMesh(mapData.noiseMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, editorLevelOfDetail, terrainData.usingFlatShading));
        }
        else if (drawMode == DrawMode.FalloffMap)
        { // If the selected mode is to draw the falloff map, then display that
            if (terrainData.useFalloffMapPerChunk)
            {
                display.drawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(CHUNK_SIZE + 2, terrainData.falloffSize, terrainData.falloffDistToEdge)));
            }
            else
            {
                display.drawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(3 * (CHUNK_SIZE + 2), terrainData.falloffSize, terrainData.falloffDistToEdge)));
            }
        }
    }

    MapData GenerateMapData(Vector2 centre)
    {
        float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(CHUNK_SIZE + 2, CHUNK_SIZE + 2, noiseData.seed, noiseData.noiseScale, noiseData.octaves, noiseData.persistence, noiseData.lacunarity, noiseData.distortStrength, noiseData.roughness, centre + noiseData.offset, noiseData.xWarpOffset, noiseData.yWarpOffset, noiseData.normalise, noiseData.normaliseMode);

        if (falloffMap == null)
        {
            falloffMap = FalloffGenerator.GenerateFalloffMap(3 * (CHUNK_SIZE + 2), terrainData.falloffSize, terrainData.falloffDistToEdge);
        }
        if (falloffMapPerChunk == null)
        {
            falloffMapPerChunk = FalloffGenerator.GenerateFalloffMap(CHUNK_SIZE + 2, terrainData.falloffSize, terrainData.falloffDistToEdge);
        }

        if (terrainData.useFalloffMapPerChunk || terrainData.useFalloffMapPer9Chunks)
        {
            for (int y = 0; y < CHUNK_SIZE + 2; y++)
            {
                for (int x = 0; x < CHUNK_SIZE + 2; x++)
                {
                    if (terrainData.useFalloffMapPerChunk && !terrainData.useFalloffMapPer9Chunks)
                    {
                        noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMapPerChunk[x, y]);
                    }
                    if (terrainData.useFalloffMapPer9Chunks && !terrainData.useFalloffMapPerChunk)
                    {
                        int modulus = 3;
                        int chunkCoordInterval = 238;
                        // Centre of the falloff
                        if ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 0 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 0)
                        {
                            noiseMap[x, y] = noiseMap[x, y];
                        }
                        // Right chunk
                        else if (((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 0) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 0))
                        {
                            noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMap[(2 * CHUNK_SIZE) + x, CHUNK_SIZE + y]);
                        }
                        // Bottom right chunk
                        else if (((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1))
                        {
                            noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMap[(2 * CHUNK_SIZE) + x, (2 * CHUNK_SIZE) + y]);
                        }
                        // Bottom chunk
                        else if (((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 0 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 0 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2))
                        {
                            noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMap[CHUNK_SIZE + x, (2 * CHUNK_SIZE) + y]);
                        }
                        // Bottom left chunk
                        else if (((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1))
                        {
                            noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMap[x, (2 * CHUNK_SIZE) + y]);
                        }
                        // Left chunk
                        else if (((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 0) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 0))
                        {
                            noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMap[x, CHUNK_SIZE + y]);
                        }
                        // Top left chunk
                        else if (((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2))
                        {
                            noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMap[x, y]);
                        }
                        // Top chunk
                        else if (((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 0 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 0 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2))
                        {
                            noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMap[CHUNK_SIZE + x, y]);
                        }
                        // Top right chunk
                        else if (((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2))
                        {
                            noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMap[(2 * CHUNK_SIZE) + x, y]);
                        }
                    }
                }
            }
        }

        return new MapData(noiseMap);
    }

    // This method starts other threads for MapData type
    public void RequestMapData(Vector2 centre, Action<MapData> callback)
    {
        ThreadStart threadStart = delegate { MapDataThread(centre, callback); };
        new Thread(threadStart).Start();
    }
    // This method will be run on different threads, it generates the terrain for each of the chunks for MapData type
    void MapDataThread(Vector2 centre, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(centre);

        // Dont want the queue to be accessed at multiple times by mutliple threads so lock the queue until these lines have been run
        lock (mapDataThreadInfoQueue)
        {
            // Add the chunk to be processed onto a queue as only the main unity thread can process mesh info
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    // This method starts other threads for MeshData type
    public void RequestMeshData(MapData mapData, int levelOfDetail, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate { MeshDataThread(mapData, levelOfDetail, callback); };
        new Thread(threadStart).Start();
    }
    // This method will be run on different threads, it generates the terrain for each of the chunks for MapData type
    void MeshDataThread(MapData mapData, int levelOfDetail, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.noiseMap, terrainData.meshHeightMultiplier, terrainData.meshHeightCurve, levelOfDetail, terrainData.usingFlatShading);

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
        if (mapDataThreadInfoQueue.Count > 0)
        { // Loop through all the items in the queue
            for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
            {
                MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
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

        if (terrainData != null)
        {
            // Dont want to constantly add to it so if already added subtract first and then add again
            terrainData.OnValuesUpdated -= OnValuesUpdated;
            terrainData.OnValuesUpdated += OnValuesUpdated;
        }
        if (noiseData != null)
        {
            // Dont want to constantly add to it so if already added subtract first and then add again
            noiseData.OnValuesUpdated -= OnValuesUpdated;
            noiseData.OnValuesUpdated += OnValuesUpdated;
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

public struct MapData
{
    // Once created we dont want to change the values of the variables
    public readonly float[,] noiseMap;

    public MapData(float[,] noiseMap)
    {
        this.noiseMap = noiseMap;
    }
}