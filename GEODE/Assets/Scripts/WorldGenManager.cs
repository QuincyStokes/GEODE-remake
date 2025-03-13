using System;
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

    [SerializeField] private const int worldSizeX = 100;
    [SerializeField] private const int worldSizeY = 100;
    [SerializeField] float noiseScale = 20f;
    [SerializeField] Vector2 offset = new Vector2(10, 10);
    [SerializeField] Tilemap backgroundTilemap;

    [Header("Tiles")]
    [SerializeField] Tile[] desertTiles;
    [SerializeField] Tile[] forestTiles;

    [Header("Biome Objects")]
    [SerializeField] private List<GameObject> ForestObjects;
    [SerializeField] private List<GameObject> ForestShrubbies;
    
    
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
        if(!IsServer)
        {
            enabled = false;
        }
        //here can continue to do magical spawn things

    }

    private void Start()
    {
        //always at the end 
       
    }

    public void InitializeWorldGen(int newseed)
    {
        InitializeBiomeTiles(newseed);
        SpawnEnvironmentFluff();
    }

    public void InitializeBiomeTiles(int newseed)
    {
        IsWorldGenerating = true;
        UnityEngine.Random.InitState(newseed);
        if(noiseScale <= 0f)
        {
            noiseScale = .0001f; //this will just prevent division by zero error
        }
       
        //now the fun part
        for (int x = 0; x < worldSizeX; x++)
        {
            for (int y = 0; y < worldSizeY; y++)
            {
                float sampleX = (x + offset.x) / noiseScale;
                float sampleY = (y + offset.y) / noiseScale;

                float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);

                Tile tileToPlace;
                if(noiseValue <= .5f)
                {
                    tileToPlace = desertTiles[UnityEngine.Random.Range(0, desertTiles.Length)];
                }
                else //in the future this will be more else ifs for different biomes
                {
                    tileToPlace = forestTiles[UnityEngine.Random.Range(0, forestTiles.Length)];
                }
                backgroundTilemap.SetTile(new Vector3Int(x, y), tileToPlace);

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

    public void SpawnEnvironmentFluff()
    {
        List<Vector3> positionsToBlock = new List<Vector3>();;

        for (int x = 0; x < worldSizeX; x++)
        {
            for (int y = 0; y < worldSizeY; y++)
            {
                float sampleX = (x + offset.x) / noiseScale;
                float sampleY = (y + offset.y) / noiseScale;

                float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
                if(noiseValue <= .2f)
                {
                    BaseItem baseItem = ItemDatabase.Instance.GetItem(8);
                    StructureItem structureItem = baseItem as StructureItem;
                    if(structureItem != null)
                    {
                        for(int i = 0; x < structureItem.width; x++)
                        {
                            for(int j = 0; j < structureItem.height; j++)
                            {
                               if(GridManager.Instance.IsPositionOccupied(new Vector3Int((int)x+(1*i), (int)y +(1*j), 0)))
                                {
                                    Debug.Log($"Collided with something at {x} + {1*i}, {y} + {1*j}");
                                    break;
                                }
                                positionsToBlock.Add(new Vector3(x+1*i, y+1*j)); 
                            }
                        }
                        PlaceObjectOffGridServerRpc(8, new Vector3(x+UnityEngine.Random.Range(.3f , .8f), y+UnityEngine.Random.Range(.3f, .8f), 0), positionsToBlock.ToArray());
                    }
                    else
                    {
                        Debug.Log("Cannot place tree, structureitem is null");
                    }
                }
                
            }
            
        }
        
    }

    
    public void SpawnEnvironmentInteractables()
    {

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
            GameObject newObject = Instantiate(structureItem.prefab, position, Quaternion.identity);
            foreach (Vector3 pos in positionsToBlock)
            {
                FlowFieldManager.Instance.SetWalkable(pos, false);
            } 
            FlowFieldManager.Instance.CalculateFlowField();
            newObject.GetComponent<NetworkObject>().Spawn();
            
        }
        else
        {
            Debug.Log("Error. Item is not a structure.");
        }
    }


}
