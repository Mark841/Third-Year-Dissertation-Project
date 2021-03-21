using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class HeightMapSettings : UpdatableData
{
    public NoiseSettings noiseSettings;


    // These affect the heights of the mesh and how smooth it appears (y axis)
    public float heightMultiplier;
    public AnimationCurve heightCurve;

    // Use the falloff map per chunk
    public bool useFalloffMapPerChunk;
    // Use the falloff map to create clusters of islands across a 3x3 chunk grid or not
    public bool useFalloffMapPer9Chunks;
    // Variable to use in the falloff maps equation to control how big a falloff to have
    [Range(1.0f, 10.0f)]
    public float falloffSize = 3.0f;
    // Variable to use in the falloff maps equation to control how strong a falloff to have
    [Range(1.0f, 10.0f)]
    public float falloffDistToEdge = 2.2f;

    public float minMeshHeight
    {
        get
        {
            return heightMultiplier * heightCurve.Evaluate(0);
        }
    }
    public float maxMeshHeight
    {
        get
        {
            return heightMultiplier * heightCurve.Evaluate(1);
        }
    }

    // Only compile the following code if its inside the untiy editor
#if UNITY_EDITOR

    protected override void OnValidate()
    {
        noiseSettings.ValidateValues();
        base.OnValidate();
    }
    #endif
}
