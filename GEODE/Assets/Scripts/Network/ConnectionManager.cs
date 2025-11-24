using System;
using System.Collections;
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

    private bool hasSubscribedToDisconnect = false;




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

    private void Start()
    {
        // Subscribe to disconnect callbacks when NetworkManager is available
        SubscribeToDisconnectCallbacks();
    }

    private void SubscribeToDisconnectCallbacks()
    {
        if (hasSubscribedToDisconnect) return;
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        hasSubscribedToDisconnect = true;
        Debug.Log("[ConnectionManager] Subscribed to disconnect callbacks");
    }

    private void UnsubscribeFromDisconnectCallbacks()
    {
        if (!hasSubscribedToDisconnect) return;
        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        hasSubscribedToDisconnect = false;
        Debug.Log("[ConnectionManager] Unsubscribed from disconnect callbacks");
    }

    public override void OnNetworkSpawn()
    {
        SubscribeToDisconnectCallbacks();
        
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
        
        UnsubscribeFromDisconnectCallbacks();
    }

    private new void OnDestroy()
    {
        UnsubscribeFromDisconnectCallbacks();
        // Note: NetworkBehaviour has its own OnDestroy, but Unity message methods
        // are called automatically. This ensures cleanup happens regardless.
    }

    private void OnClientDisconnected(ulong clientId)
    {
        // Check if this is a client (not host/server) that got disconnected
        if (NetworkManager.Singleton == null) return;

        // If we're a client (not host) and we got disconnected, redirect to main menu
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        {
            Debug.Log($"[ConnectionManager] Client {clientId} disconnected from server. Redirecting to main menu.");
            HandleClientDisconnect();
        }
        // If we're the host and a client disconnected, we don't need to do anything special
        else if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"[ConnectionManager] Client {clientId} disconnected from server (host perspective).");
        }
    }

    private void HandleClientDisconnect()
    {
        // Only redirect if we're in the Game scene
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name == "Game")
        {
            Debug.Log("[ConnectionManager] Host disconnected. Returning to main menu.");
            
            // Shutdown the network connection
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }



            // Reset connection data
            ResetData();

            foreach(var no in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
            {
                if (no != NetworkManager.Singleton)
                    Destroy(no.gameObject);
            }

            // Load the main menu (Lobby scene)
            SceneManager.LoadScene("Lobby", LoadSceneMode.Single);
        }
    }   

    public void OnWorldReady()
    {
        // This method is called by GameManager when world generation completes
        // The actual processing of waiting clients is now handled by GameManager
        Debug.Log("[ConnectionManager] OnWorldReady called - GameManager will handle waiting clients");
        
        // Failsafe: Check if there are any connected clients that weren't processed
        StartCoroutine(FailsafeCheckForUnprocessedClients());
    }

    private System.Collections.IEnumerator FailsafeCheckForUnprocessedClients()
    {
        yield return new WaitForSeconds(2f); // Wait a bit for any late connections
        
        if (!NetworkManager.Singleton.IsServer) yield break;
        if (GameManager.Instance == null) yield break;

        Debug.Log($"[ConnectionManager] Failsafe check: {NetworkManager.Singleton.ConnectedClientsList.Count} connected clients");
        
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                Debug.LogWarning($"[ConnectionManager] Failsafe: Client {client.ClientId} connected but has no player object! Processing now.");
                GameManager.Instance.HandlePlayerSpawnRequest(client.ClientId);
            }
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if(!NetworkManager.Singleton.IsServer) 
        {
            return;
        }

        Debug.Log($"[ConnectionManager] Client {clientId} connected. Delegating to GameManager.");

        // Single Entry Point: All player spawning goes through GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.HandlePlayerSpawnRequest(clientId);
        }
        else
        {
            Debug.LogWarning($"[ConnectionManager] GameManager.Instance is null! Cannot spawn player for client {clientId}");
        }
    }

    public void ResetData()
    {
        LobbyCode = null;
        RelayCode = null;
        PlayerID = null;
        PlayerName = null;
    }
}
