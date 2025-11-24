using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance; 
    public WorldGenParams worldParams;

    private AudioListener audioListener;

    public static bool IsReady = false;
    
    // Player spawning - Single Entry Point
    [SerializeField] private GameObject playerPrefab;
    private HashSet<ulong> spawnedClients = new HashSet<ulong>();
    private List<ulong> waitingClientIds = new List<ulong>();

    public static event Action OnPlayerSpawned;

    private void Awake()
    {
        if (Instance == null)
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

    private void Start()
    {
        Debug.Log("Checking whether GAMEMANAGER is server");  
        if(!IsServer)
        {
            Debug.Log("NOT THE SERVER. Disabling GameManager");
            enabled = false;
            return;
        }
        StartCoroutine(GenerateWorld());
       
        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            Debug.Log("Initializing WorldGenParams.");
            worldParams = new WorldGenParams
            {
                seed = UnityEngine.Random.Range(0, 1000000),
                noiseScale = 50,
                offset = new Vector2(UnityEngine.Random.Range(0, 10000), UnityEngine.Random.Range(1,10000)),
                difficulty = RunSettings.Instance.worldDifficulty,
                size = RunSettings.Instance.worldSize,
            };
        }
        

        //Once this scene is fully loaded, set it to be the active scene
            //This is so newly created gameobjects belong to Game, not Loading
        Scene gameplay = SceneManager.GetSceneByName("Game");
        SceneManager.SetActiveScene(gameplay);
        // Note: IsReady will be set to true AFTER world generation completes

    }


    public IEnumerator GenerateWorld()
    {
        Debug.Log("Generating world from GameManager!");

        yield return StartCoroutine(WorldGenManager.Instance.InitializeWorldGen(worldParams));
        EnemySpawningManager.Instance.SetDifficulty(worldParams.difficulty);
        EnemySpawningManager.Instance.activated = true;

        audioListener.enabled = true;
        
        // World is now ready - set flag and process waiting clients
        IsReady = true;
        Debug.Log("[GameManager] World generation complete! IsReady = true");
        
        ProcessWaitingClients();
        
        // Also ensure host local client is spawned (if not already)
        if (NetworkManager.Singleton.IsHost)
        {
            ulong hostClientId = NetworkManager.Singleton.LocalClientId;
            if (!spawnedClients.Contains(hostClientId))
            {
                Debug.Log($"[GameManager] Host local client {hostClientId} not yet spawned. Spawning now.");
                HandlePlayerSpawnRequest(hostClientId);
            }
        }
    }

    public WorldGenParams GetWorldGenParams()
    {
        return worldParams;
    }

    /// <summary>
    /// Single Entry Point for player spawning. All player spawn requests go through here.
    /// </summary>
    public void HandlePlayerSpawnRequest(ulong clientId)
    {
        if (!IsServer)
        {
            Debug.LogWarning($"[GameManager] HandlePlayerSpawnRequest called on non-server for client {clientId}");
            return;
        }

        // Check if already spawned
        if (spawnedClients.Contains(clientId))
        {
            Debug.LogWarning($"[GameManager] Client {clientId} already has a player spawned. Skipping duplicate spawn.");
            return;
        }

        // Check if client already has a player object (additional safety check)
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
        {
            Debug.LogWarning($"[GameManager] Client {clientId} already has a PlayerObject. Skipping duplicate spawn.");
            spawnedClients.Add(clientId); // Track it to prevent future attempts
            return;
        }

        // If world is not ready, queue the client
        if (!IsReady)
        {
            if (!waitingClientIds.Contains(clientId))
            {
                Debug.Log($"[GameManager] World not ready, queueing client {clientId}");
                waitingClientIds.Add(clientId);
            }
            return;
        }

        // World is ready, spawn the player
        StartCoroutine(SpawnPlayerForClientCoroutine(clientId));
    }

    private void ProcessWaitingClients()
    {
        Debug.Log($"[GameManager] Processing {waitingClientIds.Count} waiting clients");
        
        // Process all waiting clients
        foreach (ulong clientId in waitingClientIds)
        {
            HandlePlayerSpawnRequest(clientId);
        }
        waitingClientIds.Clear();
    }

    private IEnumerator SpawnPlayerForClientCoroutine(ulong clientId)
    {
        // Wait a frame to ensure everything is initialized
        yield return null;

        if (spawnedClients.Contains(clientId))
        {
            Debug.LogWarning($"[GameManager] Client {clientId} was already spawned during coroutine wait. Skipping.");
            yield break;
        }

        Debug.Log($"[GameManager] Spawning player for client {clientId}");

        // Send world generation parameters to client
        WorldGenManager.Instance.InitializeBiomeTilesSeededClientRpc(worldParams.seed, worldParams.noiseScale, worldParams.offset, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        });

        // Spawn the player object
        SpawnPlayerForClient(clientId);
    }

    private void SpawnPlayerForClient(ulong clientId)
    {
        Debug.Log($"[GameManager] Instantiating and spawning player for client {clientId}");

        if (playerPrefab == null)
        {
            Debug.LogError("[GameManager] PlayerPrefab is null! Cannot spawn player.");
            return;
        }

        GameObject playerInstance = Instantiate(playerPrefab);

        // Spawn at world center
        int centerX = WorldGenManager.Instance.WorldSizeX / 2;
        int centerY = WorldGenManager.Instance.WorldSizeY / 2;
        playerInstance.transform.position = new Vector3(centerX, centerY, 0);

        NetworkObject netObj = playerInstance.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("[GameManager] PlayerPrefab does not have a NetworkObject component!");
            Destroy(playerInstance);
            return;
        }

        netObj.SpawnAsPlayerObject(clientId, destroyWithScene: false);
        OnPlayerSpawned?.Invoke();
        // Track spawned client
        spawnedClients.Add(clientId);

        Debug.Log($"[GameManager] Successfully spawned player for client {clientId}");
        
        // Notify that a player was spawned
        //ConnectionManager.Instance?.OnPlayerSpawned?.Invoke();
        
        // Tell the specific client to unload their loading scene
        UnloadLoadingSceneClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        });
    }

    [ClientRpc]
    private void UnloadLoadingSceneClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"[GameManager] Client {NetworkManager.Singleton.LocalClientId} received UnloadLoadingSceneClientRpc - unloading loading scene");
        StartCoroutine(UnloadLoadingSceneCoroutine());
    }

    private IEnumerator UnloadLoadingSceneCoroutine()
    {
        yield return SceneManager.UnloadSceneAsync("Loading");
        Debug.Log($"[GameManager] Loading scene unloaded for client {NetworkManager.Singleton.LocalClientId} - game revealed!");
    }

    

}

[System.Serializable]
public struct WorldGenParams
{
    public int seed;
    public float noiseScale;
    public Vector2 offset;
    public Difficulty difficulty;
    public Size size;
}
