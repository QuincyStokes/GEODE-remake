using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ChunkManager : NetworkBehaviour
{
    public static ChunkManager Instance;
    public int chunkSize = 16;


    public Dictionary<Vector2Int, List<GameObject>> chunkMap = new Dictionary<Vector2Int, List<GameObject>>();
    public Dictionary<Vector2Int, ulong> chunkPlayers = new Dictionary<Vector2Int, ulong>();

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


}
