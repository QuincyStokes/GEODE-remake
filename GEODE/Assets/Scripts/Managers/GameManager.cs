using System;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance; 
    private int seed;
    private AudioListener audioListener;
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

        audioListener = Camera.main.gameObject.GetComponent<AudioListener>();
        audioListener.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();     
        if(!IsServer)
        {
            Debug.Log("NOT THE SERVER. Disabling GameManager");
            enabled = false;
        }
        
        StartCoroutine(GenerateWorld());
   
    }

    
    private IEnumerator GenerateWorld()
    {
        Debug.Log("Generating world from GameManager!");
        seed = UnityEngine.Random.Range(0, 1000000);  

        yield return StartCoroutine(WorldGenManager.Instance.InitializeWorldGen(seed));  
        EnemySpawningManager.Instance.activated = true;
        ConnectionManager.Instance.OnWorldReady();
        audioListener.enabled = true;
    }

}
