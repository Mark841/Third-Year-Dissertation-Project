using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu()]
public class TextureData : UpdatableData
{
    const int TEXTURE_SIZE = 512;
    const TextureFormat TEXTURE_FORMAT = TextureFormat.RGB565;

    public Layer[] layers;

    [Range(0, 1)]
    public float glossiness;
    [Range(0, 1)]
    public float metallic;

    public bool applyTextures;

    float savedMinHeight;
    float savedMaxHeight;

    public void ApplyToMaterial(Material material)
    {
        material.SetInt("layerCount", layers.Length);
        material.SetColorArray("baseColours", layers.Select(x => x.tint).ToArray());
        material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
        material.SetFloatArray("baseColourStrengths", layers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());
        material.SetFloat("glossiness", glossiness);
        material.SetFloat("metallic", metallic);

        Texture2DArray texturesArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
        material.SetTexture("baseTextures", texturesArray);

        if (applyTextures)
        {
            material.SetInt("useTextures", 1);
        }
        else
        {
            material.SetInt("useTextures", 0);
        }

        UpdateMeshHeights(material,savedMinHeight, savedMaxHeight);
    }

    public void UpdateMeshHeights(Material material, float minMeshHeight, float maxMeshHeight)
    {
        savedMinHeight = minMeshHeight;
        savedMaxHeight = maxMeshHeight;

        material.SetFloat("minMeshHeight", minMeshHeight);
        material.SetFloat("maxMeshHeight", maxMeshHeight);
    }

    Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        Texture2DArray textureArray = new Texture2DArray(TEXTURE_SIZE, TEXTURE_SIZE, textures.Length, TEXTURE_FORMAT, true);
        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        textureArray.Apply();
        return textureArray;
    }

    [System.Serializable]
    public class Layer
    {
        public Texture2D texture;
        public Color tint;
        [Range(0, 1)]
        public float tintStrength;
        [Range(0, 1)]
        public float startHeight;
        [Range(0, 1)]
        public float blendStrength;
        public float textureScale;
    }
}
