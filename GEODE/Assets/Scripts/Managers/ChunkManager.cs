using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChunkManager : NetworkBehaviour
{
    public static ChunkManager Instance;
    public int chunkSize = 16;


    public Dictionary<Vector2Int, List<GameObject>> chunkMap = new Dictionary<Vector2Int, List<GameObject>>();
    public Dictionary<Vector2Int, int> chunkPlayers = new Dictionary<Vector2Int, int>();

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //it is up to the object to register itself
    public void RegisterObject(GameObject obj)
    {
        Vector2Int chunk = GetChunkCoords(obj.transform.position);

        if(!chunkMap.ContainsKey(chunk))
        {
            chunkMap[chunk] = new List<GameObject>();
        }
        chunkMap[chunk].Add(obj);
    }

    public Vector2Int GetChunkCoords(Vector3 worldPos)
    {
        int chunkX = Mathf.FloorToInt(worldPos.x / chunkSize);
        int chunkY = Mathf.FloorToInt(worldPos.y / chunkSize);
        return new Vector2Int(chunkX, chunkY);
    }

    /// <summary>
    /// This function should be called from the player's ChunkCuller function inside the chunk update. 
    /// </summary>
    /// <param name="chunk">Vector2Int chunk coordinates</param>
    /// <param name="playerId">Player's ulong id</param>
    public void PlayerEntersChunk(Vector2Int chunk)
    {
        //if the chunk has never been loaded, add it to the chunk dictionary
        if(!chunkPlayers.ContainsKey(chunk))
        {
            chunkPlayers[chunk] = 0;
        }

        //if the chunk does not have any players attatched, we know there was no players in it previously,
        //  which means we need to load it
        bool wasEmpty = chunkPlayers[chunk] == 0;
        chunkPlayers[chunk]++;

        if(wasEmpty)
        {
            SetChunkActive(chunk, true);
        }
    }

    /// <summary>
    /// This function should be called when a player wants to unload a chunk from chunkCuller
    /// </summary>
    /// <param name="chunk">Vector2Int chunk to unload</param>
    /// <param name="playerId">ulong playerId</param>
    public void PlayerLeavesChunk(Vector2Int chunk)
    {
        //if the chunk is not in the dictionary, return
        //if this happens, it means we are calling it in the wrong context, or something is going wrong
        if(!chunkPlayers.ContainsKey(chunk))
        {
            return;
        }

        //remove the playerId from the list, just returns false if the player isn't in the list, wont crash
        //max function is just to prevent it from going negative
        chunkPlayers[chunk] = Mathf.Max(0, chunkPlayers[chunk] - 1);

        //now that we've removed the player, if the hashset is empty, there are no more players loading this chunk, so we can deactivate it. 
        if(chunkPlayers[chunk] == 0)
        {
            SetChunkActive(chunk, false);
        }
    }


    private void SetChunkActive(Vector2Int chunkCoord, bool active)
    {
        if(Instance.chunkMap.TryGetValue(chunkCoord, out var objList))
        {
            foreach(GameObject go in objList)
            {
                //for now fully disabling objects not in the chunks, this may need to change later down the road if things need to update. 
                go.SetActive(active);
            }
        }
    }


}
