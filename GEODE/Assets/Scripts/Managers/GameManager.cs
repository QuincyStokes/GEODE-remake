using System;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance; 
    private int seed;
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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();     
        if(!IsServer)
        {
            Debug.Log("NOT THE SERVER. Disabling GameManager");
            enabled = false;
        }
        
        GenerateWorld();
   
    }

    
    private void GenerateWorld()
    {
        Debug.Log("Generating world from GameManager!");
        seed = UnityEngine.Random.Range(0, 1000000);  

        WorldGenManager.Instance.InitializeWorldGen(seed);  
        EnemySpawningManager.Instance.activated = true;
        ConnectionManager.Instance.OnWorldReady();
    }

}
