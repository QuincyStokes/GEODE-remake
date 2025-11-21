using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ConnectionManager : NetworkBehaviour
{
    public static ConnectionManager Instance;


    [HideInInspector] public string LobbyCode;
    [HideInInspector] public string RelayCode;

    [HideInInspector] public string PlayerID;
    [HideInInspector] public string PlayerName;




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
