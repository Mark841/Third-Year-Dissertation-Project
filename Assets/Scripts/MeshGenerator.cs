using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurvature, int levelOfDetail, bool usingFlatShading)
    {
        // Have to create a new height curve object as otherwise because of threading multiple chunks it doesnt like to evaluate the same object multiple times and heavily distorts the chunks
        AnimationCurve heightCurve = new AnimationCurve(heightCurvature.keys);

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

        int meshWithBorderSize = heightMap.GetLength(0);
        int meshSize = meshWithBorderSize - (2 * meshSimplificationIncrement);
        int meshSizeUnsimplified = meshWithBorderSize - 2;

        float topLeftX = (meshSizeUnsimplified - 1) / -2.0f;
        float topLeftZ = (meshSizeUnsimplified - 1) / 2.0f;

        int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, usingFlatShading);
        int[,] vertexIndicesMap = new int[meshWithBorderSize, meshWithBorderSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        // This section needs its own loop as the next section accesses values that are being set here but in further along indexes, so cant get them and then access them in same loop as tries accessing before they're set
        for (int x = 0; x < meshWithBorderSize; x += meshSimplificationIncrement)
        {
            for (int y = 0; y < meshWithBorderSize; y += meshSimplificationIncrement)
            {
                bool isBorderVertex = (y == 0) || (y == meshWithBorderSize - 1) || (x == 0) || (x == meshWithBorderSize - 1);
                if (isBorderVertex)
                {
                    vertexIndicesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int x = 0; x < meshWithBorderSize; x += meshSimplificationIncrement)
        {
            for (int y = 0; y < meshWithBorderSize; y += meshSimplificationIncrement)
            {
                int vertexIndex = vertexIndicesMap[x, y];

                // - meshSimplificationIncrement to make sure uvs are still properly centered
                Vector2 percent = new Vector2((x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);
                float height = heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                Vector3 vertexPosition = new Vector3(topLeftX + (percent.x * meshSizeUnsimplified), height, topLeftZ - (percent.y * meshSizeUnsimplified));

                meshData.AddVertex(vertexPosition, percent, vertexIndex);

                // Ignore the right and bottom edge of the map
                if ((x < meshWithBorderSize - 1) && (y < meshWithBorderSize - 1))
                {
                    int a = vertexIndicesMap[x, y];
                    int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];
                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }
            }
        }
        meshData.Finalise();
        return meshData;
    }
}

public class MeshData
{
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;
    Vector3[] bakedNormals;

    Vector3[] borderVertices;
    int[] borderTriangles;

    int triIndex;
    int borderTriIndex;

    bool usingFlatShading;

    public MeshData(int verticesPerLine, bool usingFlatShading)
    {
        this.usingFlatShading = usingFlatShading;
        vertices = new Vector3[verticesPerLine * verticesPerLine];
        uvs = new Vector2[verticesPerLine * verticesPerLine];
        triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];
        // Has this size because it goes round the outside of the mesh so has 4 of its edges and then 4 corners
        borderVertices = new Vector3[(verticesPerLine * 4) + 4];
        // Its 4 * verticesPerLine but also * 6 as there two triangles bering stored at each which gives 6 vertexs
        borderTriangles = new int[24 * verticesPerLine];
    }

    public void AddVertex(Vector3 vertexPos, Vector2 verticesUV, int vertexIndex)
    {
        // Check if the vertex is part of the border triangles or the mesh triangles
        if (vertexIndex < 0)
        {
            // Add the point to the border triangle data, these have - indexes to distinguish from the mesh array
            borderVertices[-vertexIndex - 1] = vertexPos;
        }
        else
        {
            // Add the points to the mesh triangles data
            vertices[vertexIndex] = vertexPos;
            uvs[vertexIndex] = verticesUV;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriIndex] = a;
            borderTriangles[borderTriIndex + 1] = b;
            borderTriangles[borderTriIndex + 2] = c;
            borderTriIndex += 3;
        }
        else
        {
            triangles[triIndex] = a;
            triangles[triIndex + 1] = b;
            triangles[triIndex + 2] = c;
            triIndex += 3;
        }
    }

    Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[vertices.Length];
        // Divide by 3 as that stores each vertex of a triangle but we want amount of them
        int triangleAmount = triangles.Length / 3;
        for (int i = 0; i < triangleAmount; i++)
        {
            int normalTriIndex = i * 3;
            int vertexIndexA = triangles[normalTriIndex];
            int vertexIndexB = triangles[normalTriIndex + 1];
            int vertexIndexC = triangles[normalTriIndex + 2];

            Vector3 triNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triNormal;
            vertexNormals[vertexIndexB] += triNormal;
            vertexNormals[vertexIndexC] += triNormal;
        }

        // Divide by 3 as that stores each vertex of a triangle but we want amount of them
        int borderTriAmount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriAmount; i++)
        {
            int normalTriIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriIndex];
            int vertexIndexB = borderTriangles[normalTriIndex + 1];
            int vertexIndexC = borderTriangles[normalTriIndex + 2];

            Vector3 triNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            if (vertexIndexA >= 0)
            {
                vertexNormals[vertexIndexA] += triNormal;
            }
            if (vertexIndexB >= 0)
            {
                vertexNormals[vertexIndexB] += triNormal;
            }
            if (vertexIndexC >= 0)
            {
                vertexNormals[vertexIndexC] += triNormal;
            }
        }

        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void Finalise()
    {
        if (usingFlatShading)
        {
            FlatShading();
        }
        else
        {
            BakeNormals();
        }
    }

    private void BakeNormals()
    {
        bakedNormals = CalculateNormals();
    }

    private void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[triangles.Length];
        Vector2[] flatShadedUVs = new Vector2[triangles.Length];

        for (int i = 0; i < triangles.Length; i++)
        {
            // Get vertex and uv from vertices array for current triangle
            flatShadedVertices[i] = vertices[triangles[i]];
            flatShadedUVs[i] = uvs[triangles[i]];
            // Update triangles index to refer to index of flatshaded vertex and uvs
            triangles[i] = i;
        }

        vertices = flatShadedVertices;
        uvs = flatShadedUVs;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        if (usingFlatShading)
        {
            mesh.RecalculateNormals();
        }
        else
        {
            mesh.normals = bakedNormals;
        }
        return mesh;
    }
}