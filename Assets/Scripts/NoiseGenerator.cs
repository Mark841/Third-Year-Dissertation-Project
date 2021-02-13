using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NoiseGenerator
{
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, float DISTORT_STRENGTH, float roughness, Vector2 offset, Vector2 xWarpOffset, Vector2 yWarpOffset, bool normalise)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // Pseudo random number generator to get same thing for each seed
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        // Offset the noise for that seed
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        // Variables to keep track of the maximum and minimum height of the mesh
        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {

                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float xSample = (x - mapWidth) / scale * frequency + octaveOffsets[i].x;
                    float ySample = (y - mapHeight) / scale * frequency + octaveOffsets[i].y;

                    float xWarping = DISTORT_STRENGTH * Mathf.PerlinNoise((xSample + xWarpOffset.x) * roughness, (ySample + xWarpOffset.y) * roughness);
                    float yWarping = DISTORT_STRENGTH * Mathf.PerlinNoise((xSample + yWarpOffset.x) * roughness, (ySample + yWarpOffset.y) * roughness);

                    float noiseValue = Mathf.PerlinNoise(xSample + xWarping, ySample + yWarping) * 2 - 1;
                    noiseHeight += (noiseValue * amplitude);

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                {
                    // Set the highest point of the terrain
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    // Set the lowest point of the terrain
                    minNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }

        // If the mesh has been set to normalise the noise
        if (normalise)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    // normalises the noise map using the highest and lowest heights of terrain
                    noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
                }
            }
        }

        return noiseMap;
    }
}
