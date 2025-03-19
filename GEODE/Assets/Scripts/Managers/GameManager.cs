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
    public int seed;
    [SerializeField] float noiseScale = 5f;
    [SerializeField] Vector2 offset = new Vector2(10, 10);

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
        seed = UnityEngine.Random.Range(0, 1000000);
    }

    private void Start()
    {
        //since this script is loaded locally, OnNetworkSpawn() WILL NOT be called/
        Debug.Log("Checking whether GAMEMANAGER is server");  
        // if(!IsServer)
        // {
        //     Debug.Log("NOT THE SERVER. Disabling GameManager");
        //     enabled = false;
        //     return;
        // }
        Debug.Log("GameManager calling OnWorldReady!");
        ConnectionManager.Instance.OnWorldReady();
        //StartCoroutine(GenerateWorld());
        
   
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();   
        
    }

    
    public IEnumerator GenerateWorld(ulong clientId)
    {
        Debug.Log("Generating world from GameManager!");

        yield return StartCoroutine(WorldGenManager.Instance.InitializeWorldGen(seed, noiseScale, offset, clientId));  
        EnemySpawningManager.Instance.activated = true;
        
        audioListener.enabled = true;

        //when we're here, world is done generating
        Scene gameplayScene = SceneManager.GetSceneByName("GameplayTest");
        SceneManager.SetActiveScene(gameplayScene);
        SceneManager.UnloadSceneAsync("LoadingScreen");
    }

}
