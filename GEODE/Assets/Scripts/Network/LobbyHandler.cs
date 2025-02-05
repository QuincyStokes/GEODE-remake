using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class LobbyHandler : MonoBehaviour
{

    [SerializeField] private TMP_InputField lobbyNameField;
    [SerializeField] private Slider maxPlayersSlider;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private TMP_Text maxPlayersText;
    [SerializeField] private Toggle privateToggle;
    [SerializeField] private TMP_InputField playerName;
    [SerializeField] private GameObject createALobbyScreen;
    [SerializeField] private GameObject customizeLobbyScreen;
    [SerializeField] private GameObject yourLobbyScreen;
    [SerializeField] private YourLobby yourLobby;
    private Lobby hostLobby;
    private float heartbeatTimer;

    private async void Start()
    {
        //initialize the unity services
        //dont need this since we're claing it in the menu before this now.
        //await UnityServices.InitializeAsync();

        //sign in the current user anonymously, no need to authenticate them for now.
        //eventually, will need this to be replaced with some sort of steam authentification?
        
        createLobbyButton.onClick.AddListener(CreateLobby);
        maxPlayersSlider.onValueChanged.AddListener(delegate {UpdateMaxPlayersText();});
        maxPlayersSlider.value = maxPlayersSlider.minValue;

        createALobbyScreen.SetActive(true);
        customizeLobbyScreen.SetActive(true);
        yourLobbyScreen.SetActive(false);
    }

    private void Update()
    {
        if(hostLobby != null)
        {
            HeartbeatTimer();
        }        
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
            int maxPlayers = (int)maxPlayersSlider.value; //this will be a setting maybe

            //here we will create the options that the player has chosen for this lobby
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions 
            {
                IsPrivate = privateToggle.isOn,
                Player = new Player
                {
                    Data = new Dictionary<string, PlayerDataObject> 
                    {
                        //Set the player's name! visibility = member means only other members of the server can see the player's name
                        //this also now means we can access the players in the lobby. :eyes:
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName.text) }
                        //I believe here is where we would store other player data that we want to define, unsure of what exactly to put here for now.
                    }
                }
            };

            //create the lobby!
            if(lobbyNameField.text != "")
            {
                Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);
                Debug.Log("Created lobby " + lobby.Name + " with max players: " + lobby.MaxPlayers);
                hostLobby = lobby;
            }
            else
            {
                Debug.Log("Error, cannot have a blank lobby name.");
            }
           
            PrintCurrentPlayers();
            yourLobby.SetLobby(hostLobby);
            yourLobby.UpdatePlayerList();
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
        heartbeatTimer -= Time.deltaTime;
        if(heartbeatTimer < 0f)
        {
            float heartbeatTimerMax = 15;
            heartbeatTimer = heartbeatTimerMax;
            await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
        }
    
    }

    private void PrintCurrentPlayers()
    {
        if(hostLobby != null)
        {
            Debug.Log("Players in Lobby " + hostLobby.Name);
            foreach (Player player in hostLobby.Players)
            {
                Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);
            }    
        }
        
    }
}
