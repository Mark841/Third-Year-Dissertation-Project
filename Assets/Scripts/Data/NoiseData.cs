using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class NoiseData : UpdatableData
{
    public NoiseGenerator.NormaliseMode normaliseMode;

    [Range(10.0f, 1000.0f)]
    public float noiseScale;

    [Range(1, 10)]
    public int octaves;
    [Range(0, 1)]
    public float persistence;
    [Range(1.0f, 60.0f)]
    public float lacunarity;
    public float distortStrength = 1.25f;
    [Range(0.0f, 15.0f)]
    public float roughness = 2.0f;
    public Vector2 xWarpOffset = new Vector2(50.0f, 50.0f);
    public Vector2 yWarpOffset = new Vector2(20.0f, 30.0f);

    public int seed;
    public Vector2 offset;

    // Normalise affects whether the nosie will be normalised or not
    public bool normalise;


    // Only compile the following code if its inside the untiy editor
    #if UNITY_EDITOR

    protected override void OnValidate()
    {
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

        base.OnValidate();
    }
    #endif
}
