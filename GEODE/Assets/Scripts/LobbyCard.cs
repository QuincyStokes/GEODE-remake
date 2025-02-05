using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCard : MonoBehaviour
{
    [SerializeField] private TMP_Text lobbyName;
    [SerializeField] private TMP_Text currentPlayers;
    [SerializeField] private Button joinButton;
    [SerializeField] private Slider maxPlayerSlider;
    private Lobby lobby;
    public Lobby Lobby{
        get => lobby; 
        private set => lobby = value;
    }

    private void Start()
    {
        joinButton.onClick.AddListener(JoinLobby);
    }

    public void InitializeLobbyCard(Lobby newLobby)
    {
        lobby = newLobby;
        lobbyName.text = lobby.Name;
        currentPlayers.text = lobby.Players.Count.ToString() + " / " + lobby.MaxPlayers.ToString();
    }

    private async void JoinLobby()
    {
        //this will work fine for now, but eventually im not sure I want lobbies to show publically in the first place
        //will design further later.
        await LobbyService.Instance.JoinLobbyByIdAsync(Lobby.Id);
        Debug.Log("Joined lobby " + Lobby.Id);
    }

}
