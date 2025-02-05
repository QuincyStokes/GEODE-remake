using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class JoinLobbyScreen : MonoBehaviour
{
    [SerializeField] private GameObject lobbyCardPrefab;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField lobbyCodeField;
    [SerializeField] private TMP_InputField playerName;

    
    void Start()
    {
        joinButton.onClick.AddListener(JoinLobby);
        
    }

    private async void JoinLobby()
    {
        try
        {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions 
            {
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
            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCodeField.text, joinLobbyByCodeOptions);
            Debug.Log("Joined lobby " + lobby.Name);
            LobbyHandler.Instance.SetJoinedLobby(lobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
        
    }
}
