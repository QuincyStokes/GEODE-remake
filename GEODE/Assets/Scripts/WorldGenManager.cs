using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class WorldGenManager : NetworkBehaviour
{
    ///all things dealing with world generation (tiles, obstacles, ..idk) SHALL be generated through this script.
    ///
    public static WorldGenManager Instance;
    [SerializeField] private GameObject playerPrefab;

    [SerializeField] private const int worldSizeX = 100;
    [SerializeField] private const int worldSizeY = 100;
    [SerializeField] float noiseScale = 20f;
    [SerializeField] Vector2 offset = new Vector2(10, 10);
    [SerializeField] Tilemap backgroundTilemap;
    [SerializeField] Tile[] desertTiles;
    [SerializeField] Tile[] forestTiles;

    //[Header("Environment Fluff")]
    
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

    }

    public void SpawnEnvironmentInteractables()
    {

    }


}
