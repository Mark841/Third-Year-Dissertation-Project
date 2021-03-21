using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NoiseGenerator
{
    public enum NormaliseMode { Local, Global };

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCentre)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // Pseudo random number generator to get same thing for each seed
        System.Random prng = new System.Random(settings.seed);
        Vector2[] octaveOffsets = new Vector2[settings.octaves];

        float maxPossHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        // Offset the noise for that seed
        for (int i = 0; i < settings.octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + settings.offset.x + sampleCentre.x;
            float offsetY = prng.Next(-100000, 100000) - settings.offset.y - sampleCentre.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossHeight += amplitude;
            amplitude *= settings.persistence;
        }

        // Variables to keep track of the maximum and minimum height of the mesh
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < settings.octaves; i++)
                {
                    // Don't want the terrain to change when changing the offsets, just want it to move
                    float xSample = (x - mapWidth + octaveOffsets[i].x) / settings.scale * frequency;
                    float ySample = (y - mapHeight + octaveOffsets[i].y) / settings.scale * frequency;

                    float xWarping = settings.distortStrength * Mathf.PerlinNoise((xSample + settings.xWarpOffset.x) * settings.roughness, (ySample + settings.xWarpOffset.y) * settings.roughness);
                    float yWarping = settings.distortStrength * Mathf.PerlinNoise((xSample + settings.yWarpOffset.x) * settings.roughness, (ySample + settings.yWarpOffset.y) * settings.roughness);

                    float noiseValue = Mathf.PerlinNoise(xSample + xWarping, ySample + yWarping) * 2 - 1;
                    noiseHeight += (noiseValue * amplitude);

                    amplitude *= settings.persistence;
                    frequency *= settings.lacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight)
                {
                    // Set the highest point of the terrain
                    maxLocalNoiseHeight = noiseHeight;
                }
                if (noiseHeight < minLocalNoiseHeight)
                {
                    // Set the lowest point of the terrain
                    minLocalNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;

                // If the terrain is endless don't want seams between chunks
                if (settings.normalise && settings.normaliseMode == NormaliseMode.Global)
                {
                    float normalisedHeight = (noiseMap[x, y] + 1) / (2.0f * maxPossHeight / 1.4f);
                    noiseMap[x, y] = Mathf.Clamp(normalisedHeight, 0, int.MaxValue);
                }
            }
        }

        // If the mesh has been set to normalise the noise and if terrain is just on one chunk can do this
        if (settings.normalise && settings.normaliseMode == NormaliseMode.Local)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    // normalises the noise map using the highest and lowest heights of terrain
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
            }
        }

        return noiseMap;
    }

    public static float[,] ApplyFalloffMap(float[,] noiseMap, int CHUNK_SIZE, HeightMapSettings settings, Vector2 centre, MeshSettings terrainData)
    {
        float[,] falloffMapPerChunk = FalloffGenerator.GenerateFalloffMap(CHUNK_SIZE, settings.falloffSize, settings.falloffDistToEdge);
        float[,] falloffMap = FalloffGenerator.GenerateFalloffMap(3 * (CHUNK_SIZE), settings.falloffSize, settings.falloffDistToEdge);       

        if (settings.useFalloffMapPerChunk || settings.useFalloffMapPer9Chunks)
        {
            for (int y = 0; y < CHUNK_SIZE; y++)
            {
                for (int x = 0; x < CHUNK_SIZE; x++)
                {
                    if (settings.useFalloffMapPerChunk && !settings.useFalloffMapPer9Chunks)
                    {
                        noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMapPerChunk[x, y]);
                    }
                    if (settings.useFalloffMapPer9Chunks && !settings.useFalloffMapPerChunk)
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
                            noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMap[(2 * (CHUNK_SIZE - 2)) + x, (CHUNK_SIZE - 2) + y]);
                        }
                        // Bottom right chunk
                        else if (((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1))
                        {
                            noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMap[(2 * (CHUNK_SIZE - 2)) + x, (2 * (CHUNK_SIZE - 2)) + y]);
                        }
                        // Bottom chunk
                        else if (((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 0 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 0 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2))
                        {
                            noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMap[(CHUNK_SIZE - 2) + x, (2 * (CHUNK_SIZE - 2)) + y]);
                        }
                        // Bottom left chunk
                        else if (((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1))
                        {
                            noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMap[x, (2 * (CHUNK_SIZE - 2)) + y]);
                        }
                        // Left chunk
                        else if (((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 0) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 0))
                        {
                            noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMap[x, (CHUNK_SIZE - 2) + y]);
                        }
                        // Top left chunk
                        else if (((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2))
                        {
                            noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMap[x, y]);
                        }
                        // Top chunk
                        else if (((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 0 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 0 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2))
                        {
                            noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMap[(CHUNK_SIZE - 2) + x, y]);
                        }
                        // Top right chunk
                        else if (((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2) || ((centre.x / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == 1 && (centre.y / chunkCoordInterval * terrainData.infiniteTerrainScale) % modulus == -2))
                        {
                            noiseMap[x, y] = noiseMap[x, y] * (1 - falloffMap[(2 * (CHUNK_SIZE - 2)) + x, y]);
                        }
                    }
                }
            }
        }

        return noiseMap;
    }
}

[System.Serializable]
public class NoiseSettings
{
    public NoiseGenerator.NormaliseMode normaliseMode;

    [Range(10.0f, 1000.0f)]
    public float scale = 50;

    [Range(1, 10)]
    public int octaves = 6;
    [Range(0, 1)]
    public float persistence = 0.6f;
    [Range(1.0f, 60.0f)]
    public float lacunarity = 2.0f;
    public float distortStrength = 1.25f;
    [Range(0.0f, 15.0f)]
    public float roughness = 2.0f;
    public Vector2 xWarpOffset = new Vector2(50.0f, 50.0f);
    public Vector2 yWarpOffset = new Vector2(20.0f, 30.0f);

    public int seed;
    public Vector2 offset;

    // Normalise affects whether the nosie will be normalised or not
    public bool normalise;

    public void ValidateValues()
    {
        scale = Mathf.Max(scale, 0.0001f);
        octaves = Mathf.Max(octaves, 1);
        lacunarity = Mathf.Max(lacunarity, 1.0f);
        persistence = Mathf.Clamp01(persistence);
    }
}
