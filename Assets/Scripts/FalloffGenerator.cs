using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class FalloffGenerator
{
    public static float[,] GenerateFalloffMap(int size, float falloffSize, float falloffDistToEdge)
    {
        float[,] map = new float[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                // Give value in range -1 to 1
                float x = i / (float)size * 2 - 1;
                float y = j / (float)size * 2 - 1;

                // Find out if the x or y is closest to the edge
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                map[i, j] = Evaluate(value, falloffSize, falloffDistToEdge);
            }
        }

        return map;
    }

    static float Evaluate(float value, float falloffSize, float falloffDistToEdge)
    {
        return Mathf.Pow(value, falloffSize) / (Mathf.Pow(value, falloffSize) + Mathf.Pow(falloffDistToEdge - (falloffDistToEdge * value), falloffSize));
    }
}
