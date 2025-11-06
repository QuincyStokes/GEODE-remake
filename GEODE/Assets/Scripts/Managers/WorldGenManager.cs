using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;


public class WorldGenManager : NetworkBehaviour
{
    //* --------------- Public Instance ------------- */
    public static WorldGenManager Instance;


    //* ---------------  World Settings ------------- */
    [Header("World Settings")]
    [SerializeField] private const int worldSizeX = 300;
    [SerializeField] private const int worldSizeY = 250;


    //* --------------- Specials ------------- */
    [Header("Glades")]
    [SerializeField] private int numGlades;
    [SerializeField] private float gladeRadius;
    private List<Vector2> gladePositions = new List<Vector2>();

    [Header("Points of Interest")]
    public List<PointOfInterest> pois;


    //* --------------- Tiles ------------- */
    [Header("Tiles")]
    [SerializeField] Tile worldBoundaryTile;
    [SerializeField] Tilemap backgroundTilemap;
    [SerializeField] Tilemap worldBoundaryTilemap;


    //* --------------- Biome Data ------------- */
    [Header("Biome Data")]
    [SerializeField] private List<BiomeData> biomeDatas;
    private Dictionary<BiomeType, BiomeData> biomeTypeToDataMap = new();
    
    
    //WEIRD THING, just going to create a field to the ItemDatabase so it loads.. kindof a hack but it works?
    //* --------------- Databases ------------- */
    [SerializeField]private ItemDatabase itemDatabase;
    [SerializeField]private EnemyDatabase enemyDatabase;

    //* --------------- Events ------------- */
    public event Action OnWorldGenerated;



    //* --------------- Public Access ------------- */
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
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        SeedBiomeTypeDataMap();
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }


    public IEnumerator InitializeWorldGen(int newseed, float noiseScale, Vector2 offset)
    {   
        GenerateGladeLocations();
        
        // Server must also generate its own tiles!
        Debug.Log("[WorldGenManager] Server generating tiles...");
        yield return StartCoroutine(InitializeBiomeTiles(newseed, noiseScale, offset));

        yield return StartCoroutine(GeneratePOIs());
        Debug.Log("[WorldGenManager] Server PoI generation complete.");
        
        Debug.Log("[WorldGenManager] Server spawning environment objects...");
        yield return StartCoroutine(SpawnEnvironmentFluff());

        

        Debug.Log("[WorldGenManager] Server world generation complete!");
        OnWorldGenerated?.Invoke();
    }

    [ClientRpc]
    public void InitializeBiomeTilesSeededClientRpc(int seed, float noiseScale, Vector2 offset, ClientRpcParams clientRpcParams = default)
    {
        if (IsHost) return;
        Debug.Log($"[WorldGenManager] Client received world gen parameters: seed={seed}, scale={noiseScale}, offset={offset}");
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

        int chunkSize = 3000; // Reduced chunk size for more frequent updates
        int totalTiles = WorldSizeX * WorldSizeY;
        int processedCount = 0;

        Debug.Log($"[WorldGen] Starting world generation with seed {newseed}");

        //Tally up all of the biome weights
        float weight = 0;
        foreach (BiomeData bd in biomeDatas)
        {
            weight += bd.weight;
        }
        Debug.Log($"WorldGenManager | {weight}");
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
                float noiseValue = Mathf.PerlinNoise(sampleX, sampleY); // [0,1]


                //Decide what tiles to draw based on biome weights.
                float curr = 0f;
                Tile tileToPlace = null;
                foreach (BiomeData bd in biomeDatas)
                {
                    float w = bd.weight / weight;
                    //This means we've reached a success point, I think.
                    curr += w;
                    if (noiseValue <= curr)
                    {
                        int tileIndex = Mathf.FloorToInt(noiseValue * bd.tiles.Length) % bd.tiles.Length;
                        tileToPlace = bd.tiles[tileIndex];
                        break;
                    }
                }

                //Fallback
                if (tileToPlace == null && biomeDatas.Count > 0)
                    tileToPlace = biomeDatas[biomeDatas.Count - 1].tiles[0];

                backgroundTilemap.SetTile(new Vector3Int(x, y), tileToPlace);

                processedCount++;

                if (processedCount % chunkSize == 0)
                {
                    float progress = (float)processedCount / (float)totalTiles;
                    Debug.Log($"[WorldGen] Generation Progress: {progress:P2}");

                    yield return null; // More frequent yields for smoother loading
                }

            }
        }
        IsWorldGenerating = false;
        Debug.Log("[WorldGen] World generation complete!");
    }
    
    private IEnumerator GeneratePOIs()
    {
        foreach (PointOfInterest poi in pois)
        {
            //First, choose a location for this POI based on biome
                //With this vector3Int location, can then simply add poi.position to it, the math will just work. 
            for(int i = 0; i < poi.numSpawns; i++)
            {
                //Spawn a certain number of these POIs
                Vector3Int position = GetPositionInBiome(poi.biomeType);
                foreach(PoIObject obj in poi.poiObjects)
                {

                    Vector3Int finalPos = position + obj.position;
                    RemoveObjectsAtGridPosition(finalPos);
                    GridManager.Instance.PlaceObjectOnGridServerRpc(obj.itemId, finalPos);
                }
            }
        }
        yield return null;
    }

    //! BROKEN DUE TO COMMENTED LINE.
    [ContextMenu("Test-fire table 10 000×")]
    public void MonteCarlo()
    {
        const int shots = 10_000;
        Dictionary<BaseItem,int> tally = new();
        for (int i = 0; i < shots; i++)
        {
            int id = GetRandomSpawn(biomeDatas[0]);             // assumes in same SO
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

    public Vector3Int GetRandomPositionInWorld()
    {
        return new Vector3Int(UnityEngine.Random.Range(0, WorldSizeX), UnityEngine.Random.Range(0, WorldSizeY));
    }

    public Vector3Int GetPositionInBiome(BiomeType biomeType)
    {
        //! For now this is a really shitty way to do this, not scalable with lots of biomes.
            //* In the future, maybe can pre-store the different biome tiles, so we can choose a random tile and just get its position from a given list.
    
        Vector3Int randomWorldPos = GetRandomPositionInWorld();
        while(GetBiomeAtPosition(randomWorldPos) != biomeType)
        {
            randomWorldPos = GetRandomPositionInWorld();
        } 
        return randomWorldPos;
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

                toSpawn = GetRandomSpawn(biomeTypeToDataMap[currType]);

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


    private int GetRandomSpawn(BiomeData bd)
    {
        if (bd == null || bd.spawnEntries == null || bd.spawnEntries.Count == 0)
        {
            Debug.Log($"Error | Biome Entry Table for {bd.name} is null");
            return -1;
        }
        float randomValue = UnityEngine.Random.Range(0, bd.totalWeight);
        //Debug.Log($"Total Weight On Spawn: {bst.totalWeight}");
        foreach (var entry in bd.spawnEntries)
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
        if (structureItem != null)
        {
            //Vector3 placePos = new Vector3(position.x-structureItem.width/2, position.y-structureItem.height/2, 0);
            //GameObject newObject = Instantiate(structureItem.prefab, placePos, Quaternion.identity);
            if (structureItem.prefab != null)
            {
                GameObject newObject = Instantiate(structureItem.prefab, position, Quaternion.identity);

                FlowFieldManager.Instance.CalculateFlowField();
                newObject.GetComponent<NetworkObject>().Spawn(destroyWithScene: false);

                BaseObject bo = newObject.GetComponent<BaseObject>();
                if (bo != null)
                {
                    bo.InitializeItemId(itemId);
                }

            }
        }
        else
        {
            Debug.Log("Error. Item is not a structure.");
        }
    }

    private void SeedBiomeTypeDataMap()
    {
        foreach (BiomeData bd in biomeDatas)
        {
            biomeTypeToDataMap.Add(bd.biomeType, bd);
        }
    }

    private void RemoveObjectsAtGridPosition(Vector3Int position)
    {
        //Raycast a square on this grid position, destroy any gameobjects we hit.
        Collider2D hit;
        hit = Physics2D.OverlapBox(new Vector2(position.x+.5f, position.y+.5f), new Vector2(.4f, .4f), 0f);

        if (hit != null)
        {
            Debug.Log($"Removing objject {hit}");
            //Deregister it from the network
            hit.GetComponentInParent<NetworkObject>().Despawn(true);
        }
        else
        {
            Debug.Log("Tried to remove object, but didn't find anything.");
        }


    }
    
    


}
