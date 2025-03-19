using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor.EditorTools;
using UnityEngine;

public class ChunkCuller : MonoBehaviour
{
    //very reliant on ChunkManager existing

 
    [Header("Chunk Settings")]
    [SerializeField]private int renderDistance = 1; // Like minecraft, the radius for distance in chunks that we will load. Ideally the player can change this?
    
    [Tooltip("Update chunks every X frames")]
    [SerializeField]private float chunkUpdateFrequency;
    //hash set because it's fast lookup, and we dont want duplicate chunk positions
    private HashSet<Vector2Int> currentlyActiveChunks = new HashSet<Vector2Int>();


    //PRIVATE
    private float chunkUpdateCounter = 0f;



    private void Update()
    {
        ChunkUpdate();
    }

    private void ChunkUpdate()
    {
        chunkUpdateCounter++;
        if(chunkUpdateCounter >= chunkUpdateFrequency)
        {
            //do the chunk update
            Debug.Log("Culling chunks");
            
            //we have the position the player is at, beacuse this script is attatched to the player!
            Vector2Int playerChunk = ChunkManager.Instance.GetChunkCoords(transform.position);
            //First, collect all of the chunks that we want to update;
            HashSet<Vector2Int> chunksToActivate = new HashSet<Vector2Int>();

            for(int x = -renderDistance; x <= renderDistance; x++)
            {
                for(int y = -renderDistance; y <= renderDistance; y++)
                {
                    //loop through each chunk around the player and add it to the list of chunks to activate
                    Vector2Int neighbor = new Vector2Int(playerChunk.x + x, playerChunk.y + y);
                    chunksToActivate.Add(neighbor);
                }
            }

            //Deactivate chunks that are currently active, but are not in the list of ones we need to update
            foreach(Vector2Int chunk in currentlyActiveChunks)
            {
                if(!chunksToActivate.Contains(chunk))
                {
                    ChunkManager.Instance.PlayerLeavesChunk(chunk);
                    //SetChunkActive(chunk, false);
                }
            }

            //activate the chunks in chunksToActivate, unless its already active
            foreach(Vector2Int chunk in chunksToActivate)
            {
                if(!currentlyActiveChunks.Contains(chunk))
                {
                    ChunkManager.Instance.PlayerEntersChunk(chunk);
                    //SetChunkActive(chunk, true);
                }
            }

            //lastly, update which chunks are actually currently active.
            currentlyActiveChunks = chunksToActivate;
            chunkUpdateCounter = 0;
        }
    }

    private void OnDisable()
    {
        foreach(Vector2Int chunk in currentlyActiveChunks)
        {
            ChunkManager.Instance.PlayerLeavesChunk(chunk);
        }
    }
}
