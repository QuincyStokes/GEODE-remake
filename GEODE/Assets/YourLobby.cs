using System;
using System.Threading.Tasks;
using NUnit.Compatibility;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class YourLobby : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject playerLobbyCardPrefab;
    [SerializeField] private TMP_Text lobbyCode;
    private Lobby lobby;
    public Lobby Lobby {
        get=>lobby;
        private set => lobby = value;
    }

    private void Start()
    {
        LobbyHandler.onLobbyUpdated += UpdatePlayerList;
    }

    public void SetLobby(Lobby lobby)
    {   
        Lobby = lobby;
        lobbyCode.text = Lobby.LobbyCode;
    }
    public void UpdatePlayerList(Lobby lobby)
    {   
        Debug.Log("Updating Player List for Lobby "+ lobby.Name);
        Lobby = lobby;

        foreach (Transform transform in contentParent)
        {
            Destroy(transform.gameObject);
        }


        if(lobby != null)
        {
            Debug.Log("Players in Lobby " + lobby.Name + " " + lobby.Players);
            foreach (Player player in lobby.Players)
            {
                Debug.Log(player.Data["PlayerName"].Value);
                GameObject playerLobbyCard = Instantiate(playerLobbyCardPrefab);
                PlayerLobbyCard plc = playerLobbyCard.GetComponent<PlayerLobbyCard>();
                plc.InitializePlayerLobbyCard(player);
                plc.transform.SetParent(contentParent, false);
            }    
        }
    }


}
