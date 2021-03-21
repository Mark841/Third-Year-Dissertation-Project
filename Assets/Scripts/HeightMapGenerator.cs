using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class HeightMapGenerator
{
    public static HeightMap GenerateHeightMap(int chunkSize, HeightMapSettings settings, Vector2 sampleCentre, MeshSettings meshSettings)
    {
        float[,] noiseMap = NoiseGenerator.GenerateNoiseMap(chunkSize, chunkSize, settings.noiseSettings, sampleCentre);
        //noiseMap = NoiseGenerator.ApplyFalloffMap(noiseMap, chunkSize, settings, sampleCentre, meshSettings);

        // Have to create a local copy because of multiple simultaneous access using threads
        AnimationCurve hieghtCurveThreadSafe = new AnimationCurve(settings.heightCurve.keys);
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        for (int i = 0; i < chunkSize; i++)
        {
            for (int j = 0; j < chunkSize; j++)
            {
                noiseMap[i, j] *= hieghtCurveThreadSafe.Evaluate(noiseMap[i, j]) * settings.heightMultiplier;

                if (noiseMap[i,j] > maxValue)
                {
                    maxValue = noiseMap[i, j];
                }
                if (noiseMap[i,j] < minValue)
                {
                    minValue = noiseMap[i, j];
                }
            }
        }

        return new HeightMap(noiseMap, minValue, maxValue);
    }
}
public struct HeightMap
{
    // Once created we dont want to change the values of the variables
    public readonly float[,] noiseMap;
    public readonly float minValue;
    public readonly float maxValue;

    public HeightMap(float[,] noiseMap, float minValue, float maxValue)
    {
        this.noiseMap = noiseMap;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}
