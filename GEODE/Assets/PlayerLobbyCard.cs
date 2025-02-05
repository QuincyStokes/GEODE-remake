using UnityEngine;
using TMPro;
using Unity.Services.Lobbies.Models;
public class PlayerLobbyCard : MonoBehaviour
{
    [SerializeField] private TMP_Text playerName;
    private Player player;
    public Player Player {
        get => player;
        private set => player = value;
    }

    public void InitializePlayerLobbyCard(Player player)
    {
        Player = player;
        playerName.text = player.Data["PlayerName"].Value;
    }

}
