using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class MeshSettings : UpdatableData
{
    public bool usingFlatShading;

    // mesh uniform scale (both x and y axis affected)
    public float infiniteTerrainScale = 10.0f;

    public const int numOfSupportedLevelsOfDetail = 5;
    // This is the length of the array below
    public const int numSupportedChunkSizes = 9;
    public const int numSupportedFlatShadedChunkSizes = 3;
    public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

    [Range(0, numSupportedChunkSizes - 1)]
    public int chunkSizeIndex;
    [Range(0, numSupportedFlatShadedChunkSizes - 1)]
    public int flatShadedChunkSizeIndex;


    // Set the chunk size of the map
    // The number of vertices per line of a mesh rendered at the highest resolution (level of detail = 0), includes the 2 extra vertices created for calculating normals at borders
    public int CHUNK_SIZE
    {
        get
        {
            return supportedChunkSizes[(usingFlatShading) ? flatShadedChunkSizeIndex : chunkSizeIndex] + 1;
        }
    }

    public float meshWorldSize
    {
        get
        { // - 3, because of 2 extra vertices at each side and the 1 as getting the amount of lines between all vertices and not all vertices
            return (CHUNK_SIZE - 3) * infiniteTerrainScale;
        }
    }
}
