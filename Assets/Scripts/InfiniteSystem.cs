﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteSystem : MonoBehaviour
{
    const float VIEWER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE = 25.0f;
    const float SQUARE_VIEWER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE = VIEWER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE * VIEWER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE;
    // Distance from player to edge of chunk before a collider should be generated for that chunk
    const float colliderGenerationDistanceThreshold = 5.0f;

    public int colliderLevelOfDetailIndex;
    public levelOfDetailInfo[] detailLevels;
    public static float maxViewDist = 450.0f;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPos;
    Vector2 viewerPosOld;
    static MapGenerator mapGenerator;
    float meshWorldSize;
    int chunkVisibleInViewDist;

    Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> visibleTerrainChunks = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        maxViewDist = detailLevels[detailLevels.Length - 1].viewerDistThreshold;
        meshWorldSize = mapGenerator.meshSettings.meshWorldSize;
        chunkVisibleInViewDist = Mathf.RoundToInt(maxViewDist / meshWorldSize);
        // On start load update teh chunks as it won't go through the if statement in the Update method on the start of the run
        UpdateVisibleChunks();
    }

    // Method called on each frame
    private void Update()
    {
        viewerPos = new Vector2(viewer.position.x, viewer.position.z);

        // Call every frame so long as the viewer has moved
        if (viewerPos != viewerPosOld)
        {
            foreach (TerrainChunk chunk in visibleTerrainChunks)
            {
                chunk.UpdateCollisionMesh();
            }
        }

        // This if statement makes it so the chunks dont update every frame but only when the viewer has moved past a certain threshold amount
        // To have it update the chunks every frame remove the if and just have the "UpdateVisibleChunks();" line
        if ((viewerPosOld - viewerPos).sqrMagnitude > SQUARE_VIEWER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE)
        {
            viewerPosOld = viewerPos;
            UpdateVisibleChunks();
        }
    }

    public void UpdateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        // Go through all chunks that were visible and update them which will either make them visible or invisible
        for (int i = visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoords.Add(visibleTerrainChunks[i].coord);
            visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        // Make an easy to read grid for the chunk coordinates so they go up in 1's not 240's (as thats the chunk size)
        int currentChunkCoordX = Mathf.RoundToInt(viewerPos.x / meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPos.y / meshWorldSize);

        // Loop through all the surrounding chunks of the viewer
        for (int xOffset = -chunkVisibleInViewDist; xOffset <= chunkVisibleInViewDist; xOffset++)
        {
            for (int yOffset = -chunkVisibleInViewDist; yOffset <= chunkVisibleInViewDist; yOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                // If the chunk hasn't already been updated at the start of the method
                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {
                    if (terrainChunkDict.ContainsKey(viewedChunkCoord))
                    { // If the chunk already exists in the key, update that chunk
                        terrainChunkDict[viewedChunkCoord].UpdateTerrainChunk();
                    }
                    else
                    { // If the chunk doesn't exist yet add it to the dictionary
                        terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, meshWorldSize, detailLevels, colliderLevelOfDetailIndex, transform, mapMaterial));
                    }
                }
            }
        }
    }

    // This class represents each terrain chunk object
    public class TerrainChunk
    {
        public Vector2 coord;

        GameObject meshObject;
        Vector2 sampleCentre;
        Bounds bounds;

        // MapData mapData;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        levelOfDetailInfo[] detailLevels;
        LODMesh[] lodMeshes;
        int colliderLevelOfDetailIndex;

        HeightMap mapData;
        bool mapDataReceived;

        int prevLODIndex = -1;
        bool hasSetCollider;

        public TerrainChunk(Vector2 coord, float meshWorldSize, levelOfDetailInfo[] detailLevels, int colliderLevelOfDetailIndex, Transform parent, Material mapMaterial)
        {
            this.coord = coord;
            this.detailLevels = detailLevels;
            this.colliderLevelOfDetailIndex = colliderLevelOfDetailIndex;

            sampleCentre = coord * meshWorldSize / mapGenerator.meshSettings.infiniteTerrainScale;
            Vector2 pos = coord * meshWorldSize;
            bounds = new Bounds(pos, Vector2.one * meshWorldSize);

            // Create a game object to put the mesh onto
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = mapMaterial;
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            // Set the position of the chunk in the game world
            meshObject.transform.position = new Vector3(pos.x, 0, pos.y);
            // Attach the chunk to the parent object (MapGenerator in Unity) so it doesnt fill up the heirarchy
            meshObject.transform.parent = parent;
            // Make the chunk invisible
            SetVisible(false);

            // Set the level of detail for each chunk
            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                // Updates the chunks mesh to be using this level of detail
                lodMeshes[i] = new LODMesh(detailLevels[i].levelOfDetail);
                lodMeshes[i].updateCallback += UpdateTerrainChunk;
                // If the indexes are the same update the collision mesh for that chunk
                if (i == colliderLevelOfDetailIndex)
                {
                    lodMeshes[i].updateCallback += UpdateCollisionMesh;
                }
            }

            mapGenerator.RequestHeightMap(sampleCentre, OnMapDataReceived);
        }

        // Cant get mesh data directly and avoid this method, as by doing it this way we can only affect the level of detail of a chunk when its needed to be and not every time the viewer moves
        void OnMapDataReceived(HeightMap mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;
            
            UpdateTerrainChunk();
        }

        // Find the point on the chunks perimeter that is closest to the viewers position and find the distance between that point and the viewer, and if thats < than then MAX_VIEW_DIST then it'll make sure that the meshObject is enabled otherwise disable it
        public void UpdateTerrainChunk()
        { 
            if (mapDataReceived)
            {
                float viewDistFromClosestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPos));

                bool wasVisible = IsVisible();
                bool visible = (viewDistFromClosestEdge <= maxViewDist);

                if (visible)
                {
                    int lodIndex = 0;
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    { // Dont have to look at the last value as the visible bool would be false then anyway
                        if (viewDistFromClosestEdge > detailLevels[i].viewerDistThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Check to see if the level of detail for that chunk has changed or not so whether the mesh needs to be recaclulated
                    if (lodIndex != prevLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            prevLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }

                    // Update the chunks visible last update list here to avoid having some chunks stay visible even if they aren't near the viewer
                    visibleTerrainChunks.Add(this);
                }

                if (wasVisible != visible)
                {
                    if (visible)
                    {
                        visibleTerrainChunks.Add(this);
                    }
                    else
                    {
                        visibleTerrainChunks.Remove(this);
                    }
                }

                SetVisible(visible);
            }
        }

        // 
        public void UpdateCollisionMesh()
        {
            if (!hasSetCollider)
            {
                float sqrDistFromViewerToEdge = bounds.SqrDistance(viewerPos);

                // If the mesh has not been made for that chunk yet request it to be
                if (sqrDistFromViewerToEdge < detailLevels[colliderLevelOfDetailIndex].sqrVisibleDistanceThreshold)
                {
                    if (!lodMeshes[colliderLevelOfDetailIndex].hasRequestedMesh)
                    {
                        lodMeshes[colliderLevelOfDetailIndex].RequestMesh(mapData);
                    }
                }

                if (sqrDistFromViewerToEdge < colliderGenerationDistanceThreshold * colliderGenerationDistanceThreshold)
                {
                    // Check to make sure that index has a mesh
                    if (lodMeshes[colliderLevelOfDetailIndex].hasMesh)
                    {
                        meshCollider.sharedMesh = lodMeshes[colliderLevelOfDetailIndex].mesh;
                        hasSetCollider = true;
                    }
                }
            }
        }

        // Method to determine whether the chunk should be visible in the world or not
        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        // Returns if the chunk is visible or not
        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

    // Class is responsible for fetching its own mesh from the MapGenerator and applying the level of detail for that mesh
    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int levelOfDetail;
        public event System.Action updateCallback;

        // The constructor takes an integer for the level of detail of that chunks mesh and the method to update that mesh
        public LODMesh(int levelOfDetail)
        {
            this.levelOfDetail = levelOfDetail;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            // Call the method to update the chunk with level of detail
            updateCallback();
        }

        public void RequestMesh(HeightMap mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, levelOfDetail, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct levelOfDetailInfo
    {
        [Range(0, MeshSettings.numOfSupportedLevelsOfDetail - 1)]
        public int levelOfDetail;
        // If the viewer is outside of this threshold decrease the level of detail
        public float viewerDistThreshold;

        public float sqrVisibleDistanceThreshold
        {
            get
            {
                return viewerDistThreshold * viewerDistThreshold;
            }
        }
    }
}