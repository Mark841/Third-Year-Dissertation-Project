using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NoiseGenerator
{
    public enum NormaliseMode { Local, Global };

    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, float distortStrength, float roughness, Vector2 offset, Vector2 xWarpOffset, Vector2 yWarpOffset, bool normalise, NormaliseMode normaliseMode)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // Pseudo random number generator to get same thing for each seed
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        // Offset the noise for that seed
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossHeight += amplitude;
            amplitude *= persistence;
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

                for (int i = 0; i < octaves; i++)
                {
                    // Don't want the terrain to change when changing the offsets, just want it to move
                    float xSample = (x - mapWidth + octaveOffsets[i].x) / scale * frequency;
                    float ySample = (y - mapHeight + octaveOffsets[i].y) / scale * frequency;

                    float xWarping = distortStrength * Mathf.PerlinNoise((xSample + xWarpOffset.x) * roughness, (ySample + xWarpOffset.y) * roughness);
                    float yWarping = distortStrength * Mathf.PerlinNoise((xSample + yWarpOffset.x) * roughness, (ySample + yWarpOffset.y) * roughness);

                    float noiseValue = Mathf.PerlinNoise(xSample + xWarping, ySample + yWarping) * 2 - 1;
                    noiseHeight += (noiseValue * amplitude);

                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight)
                {
                    // Set the highest point of the terrain
                    maxLocalNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minLocalNoiseHeight)
                {
                    // Set the lowest point of the terrain
                    minLocalNoiseHeight = noiseHeight;
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
                    // If terrain is just on one chunk can do this
                    if (NormaliseMode.Local == normaliseMode)
                    {
                        // normalises the noise map using the highest and lowest heights of terrain
                        noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                    }
                    // If the terrain is endless don't want seams between chunks
                    else
                    {
                        float normalisedHeight = (noiseMap[x, y] + 1) / (2.0f * maxPossHeight / 1.4f);
                        noiseMap[x, y] = Mathf.Clamp(normalisedHeight, 0, int.MaxValue);
                    }
                }
            }
        }

        return noiseMap;
    }
}
