using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TextureData : UpdatableData
{
    public Color[] baseColours;
    [Range(0, 1)]
    public float[] baseStartHeights;

    float savedMinHeight;
    float savedMaxHeight;

    public void ApplyToMaterial(Material material)
    {
        material.SetInt("baseColourCount", baseColours.Length);
        material.SetColorArray("baseColours", baseColours);
        material.SetFloatArray("baseStartHeights", baseStartHeights);

        UpdateMeshHeights(material,savedMinHeight, savedMaxHeight);
    }

    public void UpdateMeshHeights(Material material, float minMeshHeight, float maxMeshHeight)
    {
        savedMinHeight = minMeshHeight;
        savedMaxHeight = maxMeshHeight;

        material.SetFloat("minMeshHeight", minMeshHeight);
        material.SetFloat("maxMeshHeight", maxMeshHeight);
    }
}
