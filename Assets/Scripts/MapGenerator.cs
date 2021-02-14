using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    // These are the list of settings the user can change in the Unity inspector that will affect the terrain
    public enum DrawMode { NoiseMap, ColourMap, Mesh };
    public DrawMode drawMode;

    // Must be 241 or below in size otherwise the LevelOfDetail functionality breaks as the list index goes out of bounds
    public const int CHUNK_SIZE = 241;
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

    public void GenerateMap()
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

        MapDisplay display = FindObjectOfType<MapDisplay>();

        if (drawMode == DrawMode.NoiseMap)
        {
            display.drawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            display.drawTexture(TextureGenerator.TextureFromColourMap(colourMap, CHUNK_SIZE, CHUNK_SIZE));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.drawMesh(MeshGenerator.GenerateTerrainMesh(noiseMap, meshHeightMultiplier, meshHeightCurve, levelOfDetail), TextureGenerator.TextureFromColourMap(colourMap, CHUNK_SIZE, CHUNK_SIZE));
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
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}
