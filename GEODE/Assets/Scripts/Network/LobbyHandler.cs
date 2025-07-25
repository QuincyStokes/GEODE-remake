using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyHandler : MonoBehaviour
{
    public static LobbyHandler Instance;

    [SerializeField] private TMP_InputField lobbyNameField;
    [SerializeField] private Slider maxPlayersSlider;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private TMP_Text maxPlayersText;
    [SerializeField] private Toggle privateToggle;
    [SerializeField] private TMP_InputField playerName;
    [SerializeField] private GameObject createALobbyScreen;
    [SerializeField] private GameObject customizeLobbyScreen;
    [SerializeField] private GameObject yourLobbyScreen;
    [SerializeField] private GameObject hostOrJoinButtons;
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

    void OnEnable()
    {
        hostOrJoinButtons.SetActive(true);
    }
    private void Start()
    {
        //initialize the unity services
        //dont need this since we're claing it in the menu before this now.
        //await UnityServices.InitializeAsync();

        //sign in the current user anonymously, no need to authenticate them for now.
        //eventually, will need this to be replaced with some sort of steam authentification?

        maxPlayersSlider.onValueChanged.AddListener(delegate { UpdateMaxPlayersText(); });
        maxPlayersSlider.value = maxPlayersSlider.minValue + 1;

        createALobbyScreen.SetActive(true);
        //customizeLobbyScreen.SetActive(true);
        yourLobbyScreen.SetActive(false);
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
    public async void CreateLobby()
    {
        //important to have this in a try/catch because it the await call can fail.
        try
        {
            string lobbyName = lobbyNameField.text; //lobby name, will let the user change this 
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
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName.text) }
                        
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
            joinedLobby = hostLobby;


            //yourLobby.UpdatePlayerList();
        }
        catch (LobbyServiceException e)
        {
            //print out any errors if there were any.
            Debug.Log(e);
        }
    }

    ///Helper functions
    ///
    public void UpdateMaxPlayersText()
    {
        maxPlayersText.text = "Maximum Players: " + maxPlayersSlider.value;
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
        if (joinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0f)
            {
                float updateLobbyMaxTime = 2;
                lobbyUpdateTimer = updateLobbyMaxTime;
                await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                Debug.Log("Updating " + joinedLobby.Name);
                onLobbyUpdated?.Invoke(joinedLobby);

                if (joinedLobby.Data[KEY_START_GAME].Value != "0")
                {
                    //start game!
                    if (!IsLobbyHost())
                    {
                        RelayHandler.Instance.JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);
                    }
                    joinedLobby = null;
                    //here invoke some event to start the game.

                }
            }
        }

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

            NetworkManager.Singleton.StartHost();




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
        return joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    public void SetJoinedLobby(Lobby lobby)
    {
        joinedLobby = lobby;
    }

    public async void LeaveLobby()
    {
        if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        try
        {
            if (IsLobbyHost())
            {
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Name);
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

        StopAllCoroutines();
        joinedLobby = null;
        hostOrJoinButtons.SetActive(true);
        customizeLobbyScreen.SetActive(false);
        yourLobbyScreen.SetActive(false);
    }

    public void CreateALobbyButton()
    {
        // if (lobbyNameField.text == "")
        // {
        //     LobbyErrorMessages.Instance.SetError("Please enter a lobby name.");
        //     return;
        // }

        if (playerName.text == "")
        {
            LobbyErrorMessages.Instance.SetError("Please enter a player name.");
            return;
        }

        CreateLobby();
        yourLobbyScreen.SetActive(true);
        customizeLobbyScreen.SetActive(true);
        createALobbyScreen.SetActive(false);
        
    }


    private void OnApplicationQuit()
    {
        LeaveLobby();
    }
}
