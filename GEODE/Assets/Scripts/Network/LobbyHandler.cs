using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyHandler : MonoBehaviour
{
    public static LobbyHandler Instance;

    [SerializeField] private YourLobby yourLobby;
    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartbeatTimer;
    private float lobbyUpdateTimer;
    public static event Action<Lobby> onLobbyUpdated;
    const string KEY_START_GAME = "StartGame_RelayCode";

    //* ------- Events ---------- */
    public event Action OnGameStarted; 

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async void Start()
    {
        //initialize the unity services
        //dont need this since we're claing it in the menu before this now.
        //await UnityServices.InitializeAsync();

        //sign in the current user anonymously, no need to authenticate them for now.
        //eventually, will need this to be replaced with some sort of steam authentification?
        
        // Check if UnityServices is already initialized before initializing again
        // This prevents issues when returning to the main menu after disconnection
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            Debug.Log("[LobbyHandler] UnityServices not initialized, initializing...");
            await UnityServices.InitializeAsync();
        }
        else
        {
            Debug.Log("[LobbyHandler] UnityServices already initialized");
        }

        // Check if user is already signed in before attempting to sign in again
        // This prevents issues when returning to the main menu after disconnection
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("[LobbyHandler] User not signed in, signing in anonymously...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        else
        {
            Debug.Log($"[LobbyHandler] User already signed in with PlayerId: {AuthenticationService.Instance.PlayerId}");
        }
    }

    private void Update()
    {

        HeartbeatTimer();
        HandleLobbyUpdatePoll();

    }
    /// <summary>
    /// Create a lobby.
    /// For now just following along CodeMonkey's video, but this will later be made into a button and whatnot.
    /// </summary>
    public async void CreateLobby(string name)
    {
        //important to have this in a try/catch because it the await call can fail.
        try
        {
            //string lobbyName = lobbyNameField.text; //lobby name, will let the user change this 
            //int maxPlayers = (int)maxPlayersSlider.value; //this will be a setting maybe

            //here we will create the options that the player has chosen for this lobby
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions
            {
                //IsPrivate = privateToggle.isOn,
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        //Set the player's name! visibility = member means only other members of the server can see the player's name
                        //this also now means we can access the players in the lobby. :eyes:
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, name) }
                        
                        //I believe here is where we would store other player data that we want to define, unsure of what exactly to put here for now.
                    }
                },
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "0")}
                }
            };

            //create the lobby!
            // if (lobbyNameField.text != "")
            // {
            //Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
            //     Debug.Log("Created lobby " + lobby.Name + " with max players: " + lobby.MaxPlayers + "CODE: " + lobby.LobbyCode);
            //hostLobby = lobby;
                
            // }
            // else
            // {
            //     Debug.Log("Error, cannot have a blank lobby name.");
            // }
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync("lobbyName", 8, createLobbyOptions);
            hostLobby = lobby;
            PrintCurrentPlayers();
            yourLobby.SetLobby(hostLobby);
            onLobbyUpdated += yourLobby.HandleLobbyUpdate;
            joinedLobby = hostLobby;

            if(SteamManager.Initialized)
            {
                SteamPresence.SetJoinableWrapper(lobby.Id);
            }  
            //yourLobby.UpdatePlayerList();
        }
        catch (LobbyServiceException e)
        {
            //print out any errors if there were any.
            Debug.Log(e);
        }
    }


    /// <summary>
    /// sends a heartbeat to the host lobby every 15 seconds to keep it alive
    /// </summary>
    private async void HeartbeatTimer()
    {
        if (hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 15;
                heartbeatTimer = heartbeatTimerMax;
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
                Debug.Log("Sent heartbeat to " + hostLobby.Name);
            }
        }

    }

    private async void HandleLobbyUpdatePoll()
    {
        if (joinedLobby == null || this == null || gameObject == null) return;

        lobbyUpdateTimer -= Time.deltaTime;
        if (lobbyUpdateTimer >= 0f) return;

        const float updateLobbyMaxTime = 2f;
        lobbyUpdateTimer = updateLobbyMaxTime;

        // Pull the latest state from the Lobby service so all clients stay in sync.
        if(joinedLobby == null) return;
        //Lobby latestLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
        //if (latestLobby == null) return;
        Lobby latestLobby = null;
        try
        {
            latestLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            if(e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                Debug.LogWarning("[LobbyHandler] Lobby was deleted by host.");
                joinedLobby = null;
                onLobbyUpdated?.Invoke(null);

                //HandleLobbyClosedByHost()
                return;
            }
        }

        joinedLobby = latestLobby;
        if (IsLobbyHost())
        {
            hostLobby = latestLobby; // keep heartbeat & start-game data current
        }

        Debug.Log($"Updating {joinedLobby.Name}; players: {joinedLobby.Players.Count}");
        onLobbyUpdated?.Invoke(joinedLobby);

        // Check if the host has signalled game start
        if (joinedLobby.Data[KEY_START_GAME].Value != "0")
        {
            if (!IsLobbyHost())
            {
                // Start loading immediately when game start is detected
                Debug.Log("Game start detected! Beginning client loading process...");
                StartClientGameLoadingProcess(joinedLobby.Data[KEY_START_GAME].Value);
            }
            joinedLobby = null; // stop polling once game is launching
        }
    }

    private void StartClientGameLoadingProcess(string relayCode)
    {
        Debug.Log("[LobbyHandler] Starting client game loading process");
        
        // Stop lobby polling to prevent accessing destroyed objects
        StopAllCoroutines();
        joinedLobby = null;
        
        // Show loading screen immediately, then connect after a small delay
        SceneManager.LoadScene("Loading", LoadSceneMode.Additive);
        StartCoroutine(DelayedRelayConnection(relayCode));
    }

    private System.Collections.IEnumerator DelayedRelayConnection(string relayCode)
    {
        // Wait a frame to ensure loading screen is fully loaded
        yield return null;
        yield return null; // Extra frame for safety
        
        Debug.Log("[LobbyHandler] Loading screen established, connecting to relay");
        RelayHandler.Instance.JoinRelay(relayCode);
    }

    private void PrintCurrentPlayers()
    {
        if (hostLobby != null)
        {
            Debug.Log("Players in Lobby " + hostLobby.Name + " Players( " + hostLobby.Players.Count + " )");
            foreach (Player player in hostLobby.Players)
            {
                Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);
            }
        }

    }

    public async void StartGame()
    {
        try
        {
            OnGameStarted?.Invoke();
            Debug.Log("Game Started!");
            string relayCode = await RelayHandler.Instance.CreateRelay(hostLobby.MaxPlayers - 1);

            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode)}
                }
            });
            //load the gameplay scene!


            ConnectionManager.Instance.LobbyCode = joinedLobby.LobbyCode;
            ConnectionManager.Instance.PlayerID = AuthenticationService.Instance.PlayerId;
            ConnectionManager.Instance.PlayerName = AuthenticationService.Instance.PlayerName;

            // RelayHandler.CreateRelay already starts the host; start only if not already listening
            if (!NetworkManager.Singleton.IsHost && !NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.StartHost();
            }


            //Load the game in the background as the primary scene, we need to set this as the active scene
            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);

            //Then, load the Loading Screen to hide loading
            SceneManager.LoadScene("Loading", LoadSceneMode.Additive);



            // could maybe Load the loading screen here for clients?
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }



    private bool IsLobbyHost()
    {
        if(joinedLobby == null) return false;
        return joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    public void SetJoinedLobby(Lobby lobby)
    {
        joinedLobby = lobby;
        yourLobby.SetLobby(joinedLobby);
        onLobbyUpdated += yourLobby.HandleLobbyUpdate;

        if(SteamManager.Initialized)
        {
            SteamPresence.SetJoinableWrapper(lobby.Id);
        }
    }

    public async void LeaveLobby()
    {

        try
        {
            if (IsLobbyHost())
            {
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
                
                //Fire an event here to let joined clients know the lobby no longer exists?
            }
            else
            {
                string playerId = AuthenticationService.Instance.PlayerId;
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"Lobby leave failed: {e}");
        }

        if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        if(SteamManager.Initialized)
        {
            SteamPresence.ClearJoinable();
        }

        // Check if the object still exists before calling StopAllCoroutines
        if (this != null && gameObject != null)
        {
            StopAllCoroutines();
        }

        onLobbyUpdated?.Invoke(null);
        hostLobby = null;
        joinedLobby = null;
    }

    private void OnApplicationQuit()
    {
        LeaveLobby();
    }
}
