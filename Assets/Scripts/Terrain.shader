Shader "Custom/Terrain"
{
    Properties
    {
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
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

        const static int maxColourCount = 8;

        // Variable to keep track of how many things are in arrays below
        int baseColourCount;
        float3 baseColours[maxColourCount];
        float baseStartHeights[maxColourCount];

        float minMeshHeight;
        float maxMeshHeight;

        //half _Glossiness;
        //half _Metallic;

        struct Input
        {
            // World height of a surface at a point
            float3 worldPos;
        };

        float inverseLerp(float minValue, float maxValue, float currentValue)
        { // Return difference between current value and minimum value / difference between max value and minimum value. Saturate is used to clamp vlaue between 0 and 1
            return saturate((currentValue - minValue) / (maxValue - minValue));
        }

        // Method is called for every pixel that is visible
        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float heightPercent = inverseLerp(minMeshHeight, maxMeshHeight, IN.worldPos.y);
            for (int i = 0; i < baseColourCount; i++)
            {
                // Variable is 0 if below the heightPercent or 1 if its above it
                float drawStrength = saturate(sign(heightPercent - baseStartHeights[i]));
                // Avoids an overriding the albedo value if drawStrength is 0
                o.Albedo = o.Albedo * (1 - drawStrength) +  baseColours[i] * drawStrength;
            }
            // Metallic and smoothness come from slider variables
            //o.Metallic = _Metallic;
           // o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
