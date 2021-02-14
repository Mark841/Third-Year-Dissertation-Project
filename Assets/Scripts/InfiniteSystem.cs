using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteSystem : MonoBehaviour
{
    public const float MAX_VIEW_DIST = 450.0f;
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
        chunkSize = MapGenerator.CHUNK_SIZE - 1;
        chunkVisibleInViewDist = Mathf.RoundToInt(MAX_VIEW_DIST / chunkSize);
    }
    // Method called on each frame
    private void Update()
    {
        viewerPos = new Vector2(viewer.position.x, viewer.position.z);
        UpdateVisibleChunks();
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
                    terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform, mapMaterial));
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

        public TerrainChunk(Vector2 coord, int size, Transform parent, Material mapMaterial)
        {
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

            mapGenerator.RequestMapData(OnMapDataReceived);
        }

        // Cant get mesh data directly and avoid this method, as by doing it this way we can only affect the level of detail of a chunk when its needed to be and not every time the viewer moves
        void OnMapDataReceived(MapData mapData)
        {
            mapGenerator.RequestMeshData(mapData, OnMeshDataReceived);
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            meshFilter.mesh = meshData.CreateMesh();
        }

        // Find the point on the chunks perimeter that is closest to the viewers position and find the distance between that point and the viewer, and if thats < than then MAX_VIEW_DIST then it'll make sure that the meshObject is enabled otherwise disable it
        public void UpdateTerrainChunk()
        { 
            float viewDistFromClosestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPos));
            bool visible = (viewDistFromClosestEdge <= MAX_VIEW_DIST);
            SetVisible(visible);
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
}