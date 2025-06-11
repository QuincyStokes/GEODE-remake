using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Netcode;

using UnityEngine;
using UnityEngine.Tilemaps;


public class WorldGenManager : NetworkBehaviour
{
    public static WorldGenManager Instance;
    [SerializeField] private GameObject playerPrefab;

    [SerializeField] private const int worldSizeX = 300;
    [SerializeField] private const int worldSizeY = 250;
    [Header("Glades")]
    [SerializeField] private int numGlades;
    [SerializeField] private float gladeRadius;
    private List<Vector2> gladePositions = new List<Vector2>();
    [SerializeField] private LayerMask objectLayer;
   
    [SerializeField] Tilemap backgroundTilemap;
    [SerializeField] Tilemap worldBoundaryTilemap;
    

    [Header("Tiles")]
    [SerializeField] Tile[] desertTiles;
    [SerializeField] Tile[] forestTiles;
    [SerializeField] Tile worldBoundaryTile;
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


    public IEnumerator InitializeWorldGen(int newseed, float noiseScale, Vector2 offset)
    {
        InitializeBiomeTilesSeededClientRpc(newseed, noiseScale, offset, new ClientRpcParams { });
        GenerateGladeLocations();
        //yield return StartCoroutine(InitializeBiomeTiles(newseed, noiseScale, offset));
        yield return StartCoroutine(SpawnEnvironmentFluff());

        
    }

    [ClientRpc]
    public void InitializeBiomeTilesSeededClientRpc(int seed, float noiseScale, Vector2 offset, ClientRpcParams clientRpcParams = default)
    {
        StartCoroutine(InitializeBiomeTiles(seed, noiseScale, offset));
    }

    public IEnumerator InitializeBiomeTiles(int newseed, float noiseScale, Vector2 offset)
    {
        IsWorldGenerating = true;
        UnityEngine.Random.InitState(newseed);
        if (noiseScale <= 0f)
        {
            noiseScale = .0001f; //this will just prevent division by zero error
        }

        int chunkSize = 5000;
        int totalTiles = WorldSizeX * WorldSizeY;
        int processedCount = 0;

        //now the fun part
        for (int x = -1; x < worldSizeX + 1; x++)
        {
            for (int y = -1; y < worldSizeY + 1; y++)
            {
                if (x == -1 || y == -1 || x == worldSizeX || y == worldSizeY)
                {
                    worldBoundaryTilemap.SetTile(new Vector3Int(x, y), worldBoundaryTile);
                }
                float sampleX = (x + offset.x) / noiseScale;
                float sampleY = (y + offset.y) / noiseScale;

                float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);

                Tile tileToPlace;
                if (noiseValue <= .5f) //CHANGED TO FOREST ONLY TEMP
                {
                    tileToPlace = forestTiles[UnityEngine.Random.Range(0, forestTiles.Length)];
                }
                else //in the future this will be more else ifs for different biomes
                {
                    tileToPlace = forestTiles[UnityEngine.Random.Range(0, forestTiles.Length)];
                }
                backgroundTilemap.SetTile(new Vector3Int(x, y), tileToPlace);

                processedCount++;

                if (processedCount % chunkSize == 0)
                {
                    float progress = (float)processedCount / (float)totalTiles;
                    //Debug.Log($"Generation Progress: {progress:P2}");

                    yield return null;
                }

            }
        }
        IsWorldGenerating = false;
    }
    
    [ContextMenu("Test-fire table 10 000×")]
    public void MonteCarlo()
    {
        const int shots = 10_000;
        Dictionary<BaseItem,int> tally = new();
        for (int i = 0; i < shots; i++)
        {
            int id = GetRandomSpawn(forestSpawnTable);             // assumes in same SO
            var item = ItemDatabase.Instance.GetItem(id);
            if (!tally.TryAdd(item, 1))
                tally[item]++;
        }
        foreach (var kv in tally.OrderBy(k => k.Value))
            Debug.Log($"{kv.Key.name}: {(float)kv.Value / shots:P2}");
    }

    public BiomeType GetBiomeAtPosition(Vector3Int pos)
    {
        TileBase tile = backgroundTilemap.GetTile(pos);
        if (tile != null)
        {
            BiomeTile biomeTile = tile as BiomeTile;
            if (biomeTile != null)
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

        for (int x = 0; x < WorldSizeX; x++)
        {
            for (int y = 0; y < WorldSizeY; y++)
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

                if (!GridManager.Instance.IsPositionOccupied(currentPos) && toSpawn != -1)
                {
                    bool place = true;
                    foreach (Vector2 vec in gladePositions)
                    {
                        Vector2 vec2 = new Vector2(currentPos.x, currentPos.y);
                        if (Vector2.Distance(vec2, vec) < gladeRadius)
                        {
                            place = false;
                        }
                    }
                    if (place)
                    {
                        BaseItem item = ItemDatabase.Instance.GetItem(toSpawn);
                        StructureItem structItem = item as StructureItem;

                        Vector3 newPos = new Vector3(x + UnityEngine.Random.Range(-.4f, 0.4f), y + UnityEngine.Random.Range(-.4f, .4f), 0);
                        if (structItem != null)
                        {
                            structItem.Use(newPos, false, true);
                            
                        }
                    }
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

    public void GenerateGladeLocations()
    {
        for (int i = 0; i < numGlades; ++i)
        {
            //choose a random world position
            float x = UnityEngine.Random.Range(0 + gladeRadius, WorldSizeX - gladeRadius);
            float y = UnityEngine.Random.Range(0 + gladeRadius, WorldSizeY - gladeRadius);
            Vector2 gladePos = new Vector2(x, y);
            gladePositions.Add(gladePos);
            Debug.Log($"Glade position: {gladePos}");
        }
    }


    private int GetRandomSpawn(BiomeSpawnTable bst)
    {
        if (bst == null || bst.spawnEntries == null || bst.spawnEntries.Count == 0)
        {
            Debug.Log($"Error | Biome Entry Table for {bst.name} is null");
            return -1;
        }
        float randomValue = UnityEngine.Random.Range(0, bst.totalWeight);
        //Debug.Log($"Total Weight On Spawn: {bst.totalWeight}");
        foreach (var entry in bst.spawnEntries)
        {
            if (randomValue < entry.weight)
            {
                if (entry.baseItem != null)
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
    public void PlaceObjectOffGridServerRpc(int itemId, Vector3 position)
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
                
                FlowFieldManager.Instance.CalculateFlowField();
                newObject.GetComponent<NetworkObject>().Spawn(destroyWithScene:false);

                BaseObject bo = newObject.GetComponent<BaseObject>();
                if (bo != null)
                {
                    Debug.Log($"Initializing from WorldGenManager with ItemID {itemId}");
                    bo.InitializeItemId(itemId);
                    bo.InitializeDescriptionAndSpriteClientRpc(itemId);
                }
                
            }
        }
        else
        {
            Debug.Log("Error. Item is not a structure.");
        }
    }


}
