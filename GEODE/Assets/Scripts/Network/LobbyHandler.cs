using System;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyHandler : MonoBehaviour
{

    [SerializeField] private TMP_InputField lobbyNameField;
    [SerializeField] private Slider maxPlayersSlider;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private TMP_Text maxPlayersText;

    private async void Start()
    {
        //initialize the unity services
        await UnityServices.InitializeAsync();

        //sign in the current user anonymously, no need to authenticate them for now.
        //eventually, will need this to be replaced with some sort of steam authentification?
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        createLobbyButton.onClick.AddListener(CreateLobby);
        maxPlayersSlider.onValueChanged.AddListener(delegate {UpdateMaxPlayersText();});
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

            //create the lobby!
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);
            Debug.Log("Created lobby " + lobby.Name + " with max players: " + lobby.MaxPlayers);
        
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
}
