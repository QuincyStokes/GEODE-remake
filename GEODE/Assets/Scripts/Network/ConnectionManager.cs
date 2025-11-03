using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConnectionManager : NetworkBehaviour
{
    public static ConnectionManager Instance;


    [HideInInspector] public string LobbyCode;
    [HideInInspector] public string RelayCode;

    [HideInInspector] public string PlayerID;
    [HideInInspector] public string PlayerName;
    [SerializeField] private GameObject playerPrefab;

    private List<ulong> waitingClientIds = new List<ulong>();

    private bool isWorldReady = false;
    public event Action OnPlayerSpawned;



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    public override void OnNetworkSpawn()
    {
        if(IsServer)
        {
            base.OnNetworkSpawn();
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }   
        
        
    }   

    public override void OnNetworkDespawn()
    {
        if(IsServer)
        {
            base.OnNetworkDespawn();
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }   
        
    }   

    public void OnWorldReady()
    {
       
        Debug.Log($"[ConnectionManager] World is ready! Processing {waitingClientIds.Count} waiting clients.");
        isWorldReady = true;

        
        //Spawn the player's for each client in the waiting list
        foreach(ulong clientId in waitingClientIds)
        {
            Debug.Log($"[ConnectionManager] Processing waiting client {clientId}");
            StartCoroutine(WaitForGameManagerThenConnect(clientId));
        }
        waitingClientIds.Clear();

        // Only process local client if we're the host
        if (NetworkManager.Singleton.IsHost)
        {
            Debug.Log("[ConnectionManager] Processing host local client");
            StartCoroutine(WaitForGameManagerThenConnect(NetworkManager.Singleton.LocalClientId));
        }

        // Failsafe: Check if there are any connected clients that weren't processed
        StartCoroutine(FailsafeCheckForUnprocessedClients());
    }

    private System.Collections.IEnumerator FailsafeCheckForUnprocessedClients()
    {
        yield return new WaitForSeconds(2f); // Wait a bit for any late connections
        
        if (!NetworkManager.Singleton.IsServer) yield break;

        Debug.Log($"[ConnectionManager] Failsafe check: {NetworkManager.Singleton.ConnectedClientsList.Count} connected clients");
        
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null && client.ClientId != NetworkManager.Singleton.LocalClientId)
            {
                Debug.LogWarning($"[ConnectionManager] Failsafe: Client {client.ClientId} connected but has no player object! Processing now.");
                StartCoroutine(WaitForGameManagerThenConnect(client.ClientId));
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if(!NetworkManager.Singleton.IsServer) 
        {
            return;
        }

        if (clientId == NetworkManager.Singleton.LocalClientId && NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"[ConnectionManager] OnClientConnected: Host local client {clientId}, skipping RPC calls.");
            return;
        }

        Debug.Log($"[ConnectionManager] Client {clientId} connected. World ready: {isWorldReady}");

        // Don't send loading screen RPC - client should already have it from lobby detection
        // Just ensure they have the correct world generation parameters
        
        if(isWorldReady)
        {
            Debug.Log($"[ConnectionManager] World is ready, immediately processing client {clientId}");
            StartCoroutine(WaitForGameManagerThenConnect(clientId));
        }
        else
        {
            Debug.Log($"[ConnectionManager] World not ready, adding client {clientId} to waiting list. Current waiting list size: {waitingClientIds.Count}");
            waitingClientIds.Add(clientId);
            Debug.Log($"[ConnectionManager] Client {clientId} added to waiting list. New size: {waitingClientIds.Count}");
        }
    }

    private void DoClientConnectedThings(ulong clientId)
    {
        Debug.Log($"[ConnectionManager] DoClientConnectedThings for client {clientId}");

        // Send the official world generation parameters (client may have started with temporary ones)
        GameManager.WorldGenParams worldGenParams = GameManager.Instance.GetWorldGenParams();
        WorldGenManager.Instance.InitializeBiomeTilesSeededClientRpc(worldGenParams.seed, worldGenParams.noiseScale, worldGenParams.offset, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        });
        
        // Unload the loading screen - this should make a clean transition into game
        Debug.Log($"[ConnectionManager] Sending UnloadLoadingSceneClientRpc to client {clientId}");
        UnloadLoadingSceneClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        });
        
        SpawnPlayerForClient(clientId);
    }

    private IEnumerator WaitForGameManagerThenConnect(ulong clientId)
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (!GameManager.IsReady && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }

        if (!GameManager.IsReady)
        {
            Debug.LogError("[ConnectionManager] Timed out waiting for GameManager to initialize.");
            yield break;
        }

        DoClientConnectedThings(clientId);
    }

    [ClientRpc]
    private void UnloadLoadingSceneClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"[ConnectionManager] UnloadLoadingSceneClientRpc called on client {NetworkManager.Singleton.LocalClientId}");
        SceneManager.UnloadSceneAsync("Loading");
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        Debug.Log($"Spawning player for {clientId}");
        //THIS WILL NEED TO BE REPLACED WHEN WE DO CHARACTER CUSTOMIZATION I THINK
        GameObject playerInstance = Instantiate(playerPrefab);

        //can assume worldgenmanager.instance exists because this will only trigger
        //after recieving a message from it
        int centerX = WorldGenManager.Instance.WorldSizeX / 2;
        int centerY = WorldGenManager.Instance.WorldSizeY / 2;

        playerInstance.transform.position = new Vector3(centerX, centerY, 0);

        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(clientId, destroyWithScene: false);
        OnPlayerSpawned?.Invoke();
    }

    public void ResetData()
    {
        LobbyCode = null;
        RelayCode = null;
        PlayerID = null;
        PlayerName = null;
    }
}
