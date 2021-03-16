using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    // These are the list of settings the user can change in the Unity inspector that will affect the terrain
    public enum DrawMode { NoiseMap, ColourMap, Mesh };
    public DrawMode drawMode;

<<<<<<< Updated upstream
    // Must be 241 or below in size otherwise the LevelOfDetail functionality breaks as the list index goes out of bounds
    public const int CHUNK_SIZE = 241;
=======
    public NoiseGenerator.NormaliseMode normaliseMode;

    public bool usingFlatShading;

>>>>>>> Stashed changes
    // The higher the level of detail goes the smaller the chunk size must be, 241 is largest it can be for LoD 6 if more LoD then chunk size must be decreased
    [Range(0, 6)]
    public int levelOfDetail;
    [Range(10.0f, 1000.0f)]
    public float noiseScale;

    [Range(1, 10)]
    public int octaves;
    [Range(0, 1)]
    public float persistence;
    [Range(1.0f, 60.0f)]
    public float lacunarity;
    const float DISTORT_STRENGTH = 1.25f;
    [Range(0.0f, 15.0f)]
    public float roughness = 2.0f;
    public Vector2 xWarpOffset = new Vector2(50.0f, 50.0f);
    public Vector2 yWarpOffset = new Vector2(20.0f, 30.0f);

    public int seed;
    public Vector2 offset;

    // These affect the heights of the mesh and how smooth it appears
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    // Normalise affects whether the nosie will be normalised or not
    public bool normalise;
    // This determines if the terrain will update when a value is changed or only when the update button is pressed
    public bool autoUpdate;

    public TerrainType[] regions;
    static MapGenerator instance;

    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

<<<<<<< Updated upstream
=======
    private void Awake()
    {
        falloffMap = FalloffGenerator.GenerateFalloffMap(3 * (CHUNK_SIZE + 2), falloffSize, falloffDistToEdge);
        falloffMapPerChunk = FalloffGenerator.GenerateFalloffMap(CHUNK_SIZE + 2, falloffSize, falloffDistToEdge);
    }


    // Set the chunk size of the map
    public static int CHUNK_SIZE
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<MapGenerator>();
            }
            // Chose this number as divisible by all even numbers up to 12 so can have flexibility with LOD slider, has +1 added later which is why it doesnt look divisible at the moment
            if (instance.usingFlatShading)
            { // if using flatshading use a smaller chunksize, isn't divisible by 10 so cant use a LOD of 5
                return 95;
            }
            else
            {
                return 239;
            }
        }
    }

>>>>>>> Stashed changes
    public void DrawMapInEditor()
    {
        // Make the noise and height maps
        MapData mapData = GenerateMapData();
        // Find the object in unity that is using the MapDisplay script
        MapDisplay display = FindObjectOfType<MapDisplay>();

        if (drawMode == DrawMode.NoiseMap)
        { // If the selected mode is to draw the noise map, then display that
            display.drawTexture(TextureGenerator.TextureFromHeightMap(mapData.noiseMap));
        }
        else if (drawMode == DrawMode.ColourMap)
        { // If the selected mode is to draw the coloured noise map, then display that
            display.drawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, CHUNK_SIZE, CHUNK_SIZE));
        }
        else if (drawMode == DrawMode.Mesh)
        { // If the selected mode is to draw the terrain map, then display that
<<<<<<< Updated upstream
            display.drawMesh(MeshGenerator.GenerateTerrainMesh(mapData.noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColourMap(mapData.colourMap, CHUNK_SIZE, CHUNK_SIZE));
=======
            display.drawMesh(MeshGenerator.GenerateTerrainMesh(mapData.noiseMap, meshHeightMultiplier, meshHeightCurve, editorLevelOfDetail, usingFlatShading), TextureGenerator.TextureFromColourMap(mapData.colourMap, CHUNK_SIZE, CHUNK_SIZE));
        }
        else if (drawMode == DrawMode.FalloffMap)
        { // If the selected mode is to draw the falloff map, then display that
            if (useFalloffMapPerChunk)
            {
                display.drawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(CHUNK_SIZE + 2, falloffSize, falloffDistToEdge)));
            }
            else
            {
                display.drawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(3 * (CHUNK_SIZE + 2), falloffSize, falloffDistToEdge)));
            }
>>>>>>> Stashed changes
        }
    }

    MapData GenerateMapData()
    {
        float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(CHUNK_SIZE, CHUNK_SIZE, seed, noiseScale, octaves, persistence, lacunarity, DISTORT_STRENGTH, roughness, offset, xWarpOffset, yWarpOffset, normalise);

        Color[] colourMap = new Color[CHUNK_SIZE * CHUNK_SIZE];

        for (int y = 0; y < CHUNK_SIZE; y++)
        {
            for (int x = 0; x < CHUNK_SIZE; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i=0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colourMap[y * CHUNK_SIZE + x] = regions[i].colour;
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colourMap);
    }

    // This method starts other threads for MapData type
    public void RequestMapData(Action<MapData> callback)
    {
        ThreadStart threadStart = delegate { MapDataThread(callback); };
        new Thread(threadStart).Start();
    }
    // This method will be run on different threads, it generates the terrain for each of the chunks for MapData type
    void MapDataThread(Action<MapData> callback)
    {
        MapData mapData = GenerateMapData();

        // Dont want the queue to be accessed at multiple times by mutliple threads so lock the queue until these lines have been run
        lock (mapDataThreadInfoQueue)
        {
            // Add the chunk to be processed onto a queue as only the main unity thread can process mesh info
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    // This method starts other threads for MeshData type
    public void RequestMeshData(MapData mapData, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate { MeshDataThread(mapData, callback); };
        new Thread(threadStart).Start();
    }
    // This method will be run on different threads, it generates the terrain for each of the chunks for MapData type
    void MeshDataThread(MapData mapData, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail, usingFlatShading);

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
        if (levelOfDetail < 0)
        {
            levelOfDetail = 0;
        }
        if (noiseScale < 10)
        {
            noiseScale = 10.0f;
        }
        if (roughness < 0)
        {
            roughness = 0.0f;
        }
        if (lacunarity < 1)
        {
            lacunarity = 1.0f;
        }
        if (octaves < 0)
        {
            octaves = 0;
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

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}

public struct MapData
{
    // Once created we dont want to change the values of the variables
    public readonly float[,] noiseMap;
    public readonly Color[] colourMap;

    public MapData(float[,] noiseMap, Color[] colourMap)
    {
        this.noiseMap = noiseMap;
        this.colourMap = colourMap;
    }
}