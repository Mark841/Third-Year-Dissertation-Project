using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteSystem : MonoBehaviour
{
    const float VIEWER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE = 25.0f;
    const float SQUARE_VIEWER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE = VIEWER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE * VIEWER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE;
    Vector2 viewerPosOld;

    public levelOfDetailInfo[] detailLevels;
    public static float maxViewDist = 450.0f;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPos;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunkVisibleInViewDist;

    Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        maxViewDist = detailLevels[detailLevels.Length - 1].viewerDistThreshold;
        chunkSize = MapGenerator.CHUNK_SIZE - 1;
        chunkVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);
        // On start load update teh chunks as it won't go through the if statement in the Update method on the start of the run
        UpdateVisibleChunks();
    }

    // Method called on each frame
    private void Update()
    {
        viewerPos = new Vector2(viewer.position.x, viewer.position.z);

        // This if statement makes it so the chunks dont updsate every frame but only when the vieweer has moved past a certain threshold amount
        // To have it update the chunks every frame remove the if and just have the "UpdateVisibleChunks();" line
        if ((viewerPosOld - viewerPos).sqrMagnitude > SQUARE_VIEWER_MOVE_THRESHOLD_FOR_CHUNK_UPDATE)
        {
            viewerPosOld = viewerPos;
            UpdateVisibleChunks();
        }
    }

    public void UpdateVisibleChunks()
    {
        // Go through all chunks that were visible last update and make them invisible
        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        // Empty the list to remove all chunks in it, so no chunks should be visible at this point in time
        terrainChunksVisibleLastUpdate.Clear();

        // Make an easy to read grid for the chunk coordinates so they go up in 1's not 240's (as thats the chunk size)
        int currentChunkCoordX = Mathf.RoundToInt(viewerPos.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(viewerPos.y / chunkSize);

        // Loop through all the surrounding chunks of the viewer
        for (int xOffset = -chunkVisibleInViewDist; xOffset <= chunkVisibleInViewDist; xOffset++)
        {
            for (int yOffset = -chunkVisibleInViewDist; yOffset <= chunkVisibleInViewDist; yOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDict.ContainsKey(viewedChunkCoord))
                { // If the chunk already exists in the key, update that chunk
                    terrainChunkDict[viewedChunkCoord].UpdateTerrainChunk();
                    if (terrainChunkDict[viewedChunkCoord].IsVisible())
                    {
                        terrainChunksVisibleLastUpdate.Add(terrainChunkDict[viewedChunkCoord]);
                    }
                }
                else
                { // If the chunk doesn't exist yet add it to the dictionary
                    terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }

    // This class represents each terrain chunk object
    public class TerrainChunk
    {
        GameObject meshObject;
        Vector2 pos;
        Bounds bounds;

        // MapData mapData;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;

        levelOfDetailInfo[] detailLevels;
        LODMesh[] lodMeshes;

        MapData mapData;
        bool mapDataReceived;

        int prevLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, levelOfDetailInfo[] detailLevels, Transform parent, Material mapMaterial)
        {
            this.detailLevels = detailLevels;

            pos = coord * size;
            bounds = new Bounds(pos, Vector2.one * size);
            Vector3 posInWorld = new Vector3(pos.x, 0, pos.y);

            // Create a game object to put the mesh onto
            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = mapMaterial;
            meshFilter = meshObject.AddComponent<MeshFilter>();
            // Set the position of the chunk in the game world
            meshObject.transform.position = posInWorld;
            // Attach the chunk to the parent object (MapGenerator in Unity) so it doesnt fill up the heirarchy
            meshObject.transform.parent = parent;
            // Make the chunk invisible
            SetVisible(false);

            // Set teh level of detail for each chunk
            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].levelOfDetail, UpdateTerrainChunk);
            }

            mapGenerator.RequestMapData(pos, OnMapDataReceived);
        }

        // Cant get mesh data directly and avoid this method, as by doing it this way we can only affect the level of detail of a chunk when its needed to be and not every time the viewer moves
        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;
            
            // Set the texture of the mesh to be the colour map
            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, MapGenerator.CHUNK_SIZE, MapGenerator.CHUNK_SIZE);
            meshRenderer.material.mainTexture = texture;

            UpdateTerrainChunk();
        }

        // Find the point on the chunks perimeter that is closest to the viewers position and find the distance between that point and the viewer, and if thats < than then MAX_VIEW_DIST then it'll make sure that the meshObject is enabled otherwise disable it
        public void UpdateTerrainChunk()
        { 
            if (mapDataReceived)
            {
                float viewDistFromClosestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPos));
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
                }

                SetVisible(visible);
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
        System.Action updateCallback;

        // The constructor takes an integer for the level of detail of that chunks mesh and the method to update that mesh
        public LODMesh(int levelOfDetail, System.Action updateCallback)
        {
            this.levelOfDetail = levelOfDetail;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            // Call the method to update the chunk with level of detail
            updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, levelOfDetail, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct levelOfDetailInfo
    {
        public int levelOfDetail;
        // If the viewer is outside of this threshold decrease the level of detail
        public float viewerDistThreshold;
    }
}