using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance;


    [HideInInspector] public string LobbyCode;
    [HideInInspector] public string RelayCode;
    [HideInInspector] public bool IsHost;

    [HideInInspector] public string PlayerID;
    [HideInInspector] public string PlayerName;
    [SerializeField] private GameObject playerPrefab;

    private List<ulong> waitingClientIds = new List<ulong>();

    private bool isWorldReady = false;



    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        
    }

    private void OnDestroy()
    {
        if(NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void Start()
    {
        IsHost = NetworkManager.Singleton.IsHost;
    }

    public void OnWorldReady()
    {
        Debug.Log("World is ready!");
        isWorldReady = true;
        foreach(ulong clientId in waitingClientIds)
        {
            SpawnPlayerForClient(clientId);
        }
        waitingClientIds.Clear();
        SceneManager.UnloadSceneAsync("LoadingScreen");
    }

    private void OnClientConnected(ulong clientId)
    {
        if(!NetworkManager.Singleton.IsServer) 
        {
            return;
        }

        if(isWorldReady)
        {
            SpawnPlayerForClient(clientId);
        }
        else
        {
            waitingClientIds.Add(clientId);
        }
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        Debug.Log($"Spawning player for {clientId}");
        //THIS WILL NEED TO BE REPLACED WHEN WE DO CHARACTER CUSTOMIZATION I THINK
        GameObject playerInstance = Instantiate(playerPrefab);
        
        //can assume worldgenmanager.instance exists because this will only trigger
        //after recieving a message from it
        int centerX = WorldGenManager.Instance.WorldSizeX/2;
        int centerY = WorldGenManager.Instance.WorldSizeY/2;

        playerInstance.transform.position = new Vector3(centerX, centerY, 0);

        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId, destroyWithScene: false);
    }

    public void ResetData()
    {
        LobbyCode = null;
        RelayCode = null;
        PlayerID = null;
        PlayerName = null;
    }
}
