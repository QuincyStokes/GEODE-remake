using System.Collections;
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

        
        //Spawn the player's for each client in the waiting list
        foreach(ulong clientId in waitingClientIds)
        {
            StartCoroutine(DoClientConnectedThings(clientId));
        }
        waitingClientIds.Clear();

       StartCoroutine(DoClientConnectedThings(NetworkManager.Singleton.LocalClientId));

        
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
            StartCoroutine(DoClientConnectedThings(clientId));
        }
        else
        {
            Debug.Log($"Adding client {clientId} to waiting list.");
            waitingClientIds.Add(clientId);
        }
    }

    private IEnumerator DoClientConnectedThings(ulong clientId)
    {
       
        yield return StartCoroutine(GameManager.Instance.GenerateWorld(clientId));
        SpawnPlayerForClient(clientId);

        //the last ting we do for the client is unload the loading screen, this should make a clean transition into game.
        //SceneManager.UnloadSceneAsync("LoadingScreen");
        UnloadLoadingSceneClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] {clientId}
            }
        });
    }

    [ClientRpc]
    private void LoadLoadingSceneClientRpc(ClientRpcParams clientRpcParams = default)
    {
        SceneManager.LoadScene("Loading", LoadSceneMode.Additive);
    }

    [ClientRpc]
    private void UnloadLoadingSceneClientRpc(ClientRpcParams clientRpcParams = default)
    {
        SceneManager.UnloadSceneAsync("Loading");
    }

    private void SpawnPlayerForHost()
    {
        GameObject playerInstance = Instantiate(playerPrefab);
        
        //can assume worldgenmanager.instance exists because this will only trigger
        //after recieving a message from it
        int centerX = WorldGenManager.Instance.WorldSizeX/2;
        int centerY = WorldGenManager.Instance.WorldSizeY/2;

        playerInstance.transform.position = new Vector3(centerX, centerY, 0);

        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
        netObj.SpawnAsPlayerObject(NetworkManager.Singleton.LocalClientId, destroyWithScene: false);
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
