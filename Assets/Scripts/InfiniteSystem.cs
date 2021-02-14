using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfiniteSystem : MonoBehaviour
{
    public const float MAX_VIEW_DIST = 450.0f;
    public Transform viewer;

    public static Vector2 viewerPos;
    int chunkSize;
    int chunkVisibleInViewDist;

    Dictionary<Vector2, TerrainChunk> terrainChunkDict = new Dictionary<Vector2, TerrainChunk>();
    List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
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
                    terrainChunkDict.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, transform));
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

        public TerrainChunk(Vector2 coord, int size, Transform parent)
        {
            pos = coord * size;
            bounds = new Bounds(pos, Vector2.one * size);
            Vector3 posInWorld = new Vector3(pos.x, 0, pos.y);

            // Create a plane to put the mesh onto
            meshObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            // Set the position of the chunk in the game world
            meshObject.transform.position = posInWorld;
            // Set the scale of it to appear larger, divide by 10 as thats the planes default state
            meshObject.transform.localScale = Vector3.one * size / 10.0f;
            // Attach the chunk to the parent object (MapGenerator in Unity) so it doesnt fill up the heirarchy
            meshObject.transform.parent = parent;
            // Make the chunk invisible
            SetVisible(false);
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