﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve heightCurvature, int levelOfDetail, bool usingFlatShading)
    {
        // Have to create a new height curve object as otherwise because of threading multiple chunks it doesnt like to evaluate teh same object mutliple times and heavily distorts the chunks
        AnimationCurve heightCurve = new AnimationCurve(heightCurvature.keys);
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        float topLeftX = (width - 1) / -2.0f;
        float topLeftZ = (height - 1) / 2.0f;

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

<<<<<<< Updated upstream
        for (int y = 0; y < height; y += meshSimplificationIncrement)
=======
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
>>>>>>> Stashed changes
        {
            for (int x = 0; x < width; x += meshSimplificationIncrement)
            {
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, topLeftZ - y);
                meshData.uvs[vertexIndex] = new Vector2(x / (float) width, y / (float) height);

                // Ignore the right and bottom edge of the map
                if ((x < width - 1) && (y < height - 1))
                {
                    meshData.AddTriangle(vertexIndex, (vertexIndex + verticesPerLine + 1), (vertexIndex + verticesPerLine));
                    meshData.AddTriangle((vertexIndex + verticesPerLine + 1), vertexIndex, (vertexIndex + 1));
                }

                vertexIndex++;
            }
        }
<<<<<<< Updated upstream
<<<<<<< Updated upstream

=======
        meshData.Finalise();
>>>>>>> Stashed changes
=======
        meshData.Finalise();
>>>>>>> Stashed changes
        return meshData;
    }
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    int triangleIndex;

<<<<<<< Updated upstream
    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
=======
    bool usingFlatShading;

<<<<<<< Updated upstream
=======
    bool usingFlatShading;

>>>>>>> Stashed changes
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
>>>>>>> Stashed changes
    }

    public void AddTriangle(int a, int b, int c)
    {
<<<<<<< Updated upstream
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
=======
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
>>>>>>> Stashed changes
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
<<<<<<< Updated upstream
<<<<<<< Updated upstream
        mesh.RecalculateNormals();
=======
=======
>>>>>>> Stashed changes
        if (usingFlatShading)
        {
            mesh.RecalculateNormals();
        }
        else
        {
            mesh.normals = bakedNormals;
        }
<<<<<<< Updated upstream
>>>>>>> Stashed changes
=======
>>>>>>> Stashed changes
        return mesh;
    }
}