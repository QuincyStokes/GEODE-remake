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
       
        Debug.Log("World is ready!");
        isWorldReady = true;

        
        //Spawn the player's for each client in the waiting list
        foreach(ulong clientId in waitingClientIds)
        {
            DoClientConnectedThings(clientId);
        }
        waitingClientIds.Clear();

        DoClientConnectedThings(NetworkManager.Singleton.LocalClientId);

        
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


        LoadLoadingSceneClientRpc(new ClientRpcParams 
        {
            Send = new ClientRpcSendParams 
            {
                TargetClientIds = new[] {clientId}
            }
        });

        
        if(isWorldReady)
        {
            Debug.Log($"Doing Client Connected Things for {clientId}");
            DoClientConnectedThings(clientId);
        }
        else
        {
            Debug.Log($"Adding client {clientId} to waiting list.");
            waitingClientIds.Add(clientId);
        }
    }

    private void DoClientConnectedThings(ulong clientId)
    {


        WorldGenManager.Instance.InitializeBiomeTilesSeededClientRpc(0, 5, new Vector2(10, 10), new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        });
        //the last ting we do for the client is unload the loading screen, this should make a clean transition into game.
        UnloadLoadingSceneClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        });
        SpawnPlayerForClient(clientId);
    }

    [ClientRpc]
    private void LoadLoadingSceneClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log(
        $"LoadLoadingSceneClientRpc on client {NetworkManager.Singleton.LocalClientId}, " +
        $"filter = [{string.Join(",", clientRpcParams.Send.TargetClientIds)}]"
        );
        SceneManager.LoadScene("Loading", LoadSceneMode.Additive);
    }

    [ClientRpc]
    private void UnloadLoadingSceneClientRpc(ClientRpcParams clientRpcParams = default)
    {
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
