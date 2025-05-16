using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class WorldGenManager : NetworkBehaviour
{
    public static WorldGenManager Instance;
    [SerializeField] private GameObject playerPrefab;

    [SerializeField] private const int worldSizeX = 300;
    [SerializeField] private const int worldSizeY = 250
    ;
   
    [SerializeField] Tilemap backgroundTilemap;

    [Header("Tiles")]
    [SerializeField] Tile[] desertTiles;
    [SerializeField] Tile[] forestTiles;

    [Header("Biome Objects")]
    [SerializeField] private BiomeSpawnTable forestSpawnTable;
    private int totalWeight;
    //[SerializeField] private BiomeSpawnTable desertSpawnTable;
    
    
    //WEIRD THING, just going to create a field to the ItemDatabase so it loads.. kindof a hack but it works?
    [SerializeField]private ItemDatabase itemDatabase;
    [SerializeField]private EnemyDatabase enemyDatabase;

    public event Action OnWorldGenerated;

    public bool IsWorldGenerating;

    public int WorldSizeX
    {
        get => worldSizeX;
    }
    public int WorldSizeY
    {
        get => worldSizeY;
    }


    private void Awake()
    {   
        IsWorldGenerating = true;
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }


    public IEnumerator InitializeWorldGen(int newseed, float noiseScale, Vector2 offset, ulong clientId)
    {
        InitializeBiomeTilesSeededClientRpc(newseed, noiseScale, offset, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new [] { clientId }
            }
        });
        //yield return StartCoroutine(InitializeBiomeTiles(newseed, noiseScale, offset));
        yield return StartCoroutine(SpawnEnvironmentFluff());

        
    }

    [ClientRpc]
    private void InitializeBiomeTilesSeededClientRpc(int seed, float noiseScale, Vector2 offset, ClientRpcParams clientRpcParams = default)
    {
        StartCoroutine(InitializeBiomeTiles(seed, noiseScale, offset));
    }

    private IEnumerator InitializeBiomeTiles(int newseed, float noiseScale, Vector2 offset)
    {
        IsWorldGenerating = true;
        UnityEngine.Random.InitState(newseed);
        if(noiseScale <= 0f)
        {
            noiseScale = .0001f; //this will just prevent division by zero error
        }

        int chunkSize = 5000;
        int totalTiles = WorldSizeX * WorldSizeY;
        int processedCount = 0;

        //now the fun part
        for (int x = 0; x < worldSizeX; x++)
        {
            for (int y = 0; y < worldSizeY; y++)
            {
                float sampleX = (x + offset.x) / noiseScale;
                float sampleY = (y + offset.y) / noiseScale;

                float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);

                Tile tileToPlace;
                if(noiseValue <= .5f) //CHANGED TO FOREST ONLY TEMP
                {
                    tileToPlace = forestTiles[UnityEngine.Random.Range(0, forestTiles.Length)];
                }
                else //in the future this will be more else ifs for different biomes
                {
                    tileToPlace = forestTiles[UnityEngine.Random.Range(0, forestTiles.Length)];
                }
                backgroundTilemap.SetTile(new Vector3Int(x, y), tileToPlace);

                processedCount++;

                if(processedCount % chunkSize == 0)
                {
                    float progress = (float)processedCount / (float)totalTiles;
                    Debug.Log($"Generation Progress: {progress:P2}");

                    yield return null;
                }

            }
        }
        IsWorldGenerating = false;

    }
    
    public BiomeType GetBiomeAtPosition(Vector3Int pos)
    {
        TileBase tile = backgroundTilemap.GetTile(pos);
        if (tile != null){
            BiomeTile biomeTile = tile as BiomeTile;
            if(biomeTile != null)
            {
                return biomeTile.biomeType;
            }
            else
            {
                return BiomeType.None;
            }
        } 
        else
        {
            return BiomeType.None;
        }
    }

    private IEnumerator SpawnEnvironmentFluff()
    {
        int chunkSize = 5000;
        int totalTiles = WorldSizeX * WorldSizeY;
        int processedCount = 0;

        for (int x = 0; x < worldSizeX; x++)
        {
            for (int y = 0; y < worldSizeY; y++)
            {
                Vector3Int currentPos = new Vector3Int(x, y);
                BiomeType currType = GetBiomeAtPosition(currentPos);
                int toSpawn = -1;
                switch(currType)
                {
                    case BiomeType.Forest:
                        toSpawn = GetRandomSpawn(forestSpawnTable);
                        break;

                    default:
                        break;
                }
                    
                if(!GridManager.Instance.IsPositionOccupied(currentPos) && toSpawn != -1)
                {
                    BaseItem item = ItemDatabase.Instance.GetItem(toSpawn);
                    StructureItem structItem = item as StructureItem;

                    Vector3 newPos = new Vector3(x+UnityEngine.Random.Range(-.2f , .2f), y+UnityEngine.Random.Range(-.2f, .2f), 0);
                    if(structItem != null)
                    {
                        structItem.Use(newPos, false, true);
                    }
                    else
                    {
                        Debug.Log("Cannot place item, structItem not found");
                    }
                    
                    //PlaceObjectOffGridServerRpc(toSpawn, new Vector3(x+UnityEngine.Random.Range(-.2f , .2f), y+UnityEngine.Random.Range(-.2f, .2f), 0), positionsToBlock.ToArray());
                    //PlaceObjectOffGridServerRpc(toSpawn, new Vector3(x, y, 0), positionsToBlock.ToArray());
                    
                }
                processedCount++;

                if(processedCount % chunkSize == 0)
                {
                    float progress = (float)processedCount / (float)totalTiles;
                    Debug.Log($"Generation Progress: {progress:P2}");

                    yield return null;
                }
            }
            
        }
        
    }

    private int GetRandomSpawn(BiomeSpawnTable bst)
    {
        if(bst == null || bst.spawnEntries == null || bst.spawnEntries.Count == 0)
        {
            Debug.Log($"Error | Biome Entry Table for {bst.name} is null");
            return -1;
        }
        float randomValue = UnityEngine.Random.value * bst.totalWeight;

        foreach(var entry in bst.spawnEntries)
        {
            if(randomValue < entry.weight)
            {
                if(entry.baseItem != null)
                {
                    return entry.baseItem.Id;
                }
                else
                {
                    return -1;
                }
                
            }
            else
            {
                randomValue -= entry.weight;
            }
        }
        //should never get here, but gotta put this
        return -1;
    }


    [ServerRpc(RequireOwnership = false)]
    public void PlaceObjectOffGridServerRpc(int itemId, Vector3 position, Vector3[] positionsToBlock)
    {
        
        BaseItem baseItem = ItemDatabase.Instance.GetItem(itemId);
        StructureItem structureItem = baseItem as StructureItem;
        if(structureItem != null)
        {
            //Vector3 placePos = new Vector3(position.x-structureItem.width/2, position.y-structureItem.height/2, 0);
            //GameObject newObject = Instantiate(structureItem.prefab, placePos, Quaternion.identity);
            if(structureItem.prefab != null)
            {
                GameObject newObject = Instantiate(structureItem.prefab, position, Quaternion.identity);
                foreach (Vector3 pos in positionsToBlock)
                {
                    FlowFieldManager.Instance.SetWalkable(pos, false);
                } 
                FlowFieldManager.Instance.CalculateFlowField();
                newObject.GetComponent<NetworkObject>().Spawn(destroyWithScene:false);
            }
        }
        else
        {
            Debug.Log("Error. Item is not a structure.");
        }
    }


}
