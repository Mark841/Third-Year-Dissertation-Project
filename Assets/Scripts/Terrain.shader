Shader "Custom/Terrain"
{
    Properties
    {
        testTexture("Texture", 2D) = "white"{}
        testScale("Scale", Float) = 1

        glossiness("Smoothness", Range(0,1)) = 0.5
        metallic("Metallic", Range(0,1)) = 0.0
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        const static int maxLayerCount = 8;
        const static float epsilon = 1E-4;

        // Variable to keep track of how many things are in arrays below
        int layerCount;
        float3 baseColours[maxLayerCount];
        float baseStartHeights[maxLayerCount];
        float baseBlends[maxLayerCount];
        float baseColourStrengths[maxLayerCount];
        float baseTextureScales[maxLayerCount];

        float minMeshHeight;
        float maxMeshHeight;

        float glossiness;
        float metallic;

        bool useTextures;

        sampler2D testTexture;
        float testScale;

        UNITY_DECLARE_TEX2DARRAY(baseTextures);

        struct Input
        {
            // World height of a surface at a point
            float3 worldPos;
            // World normal of a surface at a point
            float3 worldNormal;
        };

        float inverseLerp(float minValue, float maxValue, float currentValue)
        { // Return difference between current value and minimum value / difference between max value and minimum value. Saturate is used to clamp vlaue between 0 and 1
            return saturate((currentValue - minValue) / (maxValue - minValue));
        }

        float3 triplanar(float3 worldPos, float scale, float3 blendAxis, int textureIndex)
        {
            // Blend between 3 projections of textures for normal of mesh at each point so dont have texture strecthing on hills or gound (triplanar mapping)
            float3 scaledWorldPos = worldPos / scale;
            // Also weight the positions according to the mesh normal at that point on the mesh
            float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxis.x;
            float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxis.y;
            float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxis.z;
            return xProjection + yProjection + zProjection;
        }

        // Method is called for every pixel that is visible
        void surf (Input IN, inout SurfaceOutputStandard o)
        {       
            float heightPercent = inverseLerp(minMeshHeight, maxMeshHeight, IN.worldPos.y);
            for (int i = 0; i < layerCount; i++)
            {
                // Variable is 0 if below the heightPercent or 1 if its above it
                float drawStrength = inverseLerp((-baseBlends[i] / 2) - epsilon, baseBlends[i] / 2, heightPercent - baseStartHeights[i]);
                if (useTextures)
                { // If using textures on the terrain
                    float3 blendAxis = abs(IN.worldNormal);
                    // Don't want to make the texture brighter by accidentally raising the RGB values when projecting
                    blendAxis /= blendAxis.x + blendAxis.y + blendAxis.z;
                    float3 baseColour = baseColours[i] * baseColourStrengths[i];
                    float3 textureColour = triplanar(IN.worldPos, baseTextureScales[i], blendAxis, i) * (1 - baseColourStrengths[i]);
                    o.Albedo = o.Albedo * (1 - drawStrength) + (baseColour + textureColour) * drawStrength;
                }
                else
                { // If only wanting colours not textures
                    // Avoids an overriding the albedo value if drawStrength is 0
                    o.Albedo = o.Albedo * (1 - drawStrength) + baseColours[i] * drawStrength;
                }
            }

            // Set the glossiness and metallicness of the terrain
            o.Metallic = metallic;
            o.Smoothness = glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
