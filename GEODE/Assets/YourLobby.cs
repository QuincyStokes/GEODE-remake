using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class YourLobby : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private Button startButton;
    [SerializeField] private GameObject playerLobbyCardPrefab;
    [SerializeField] private Button refreshButton;
    private Lobby lobby;
    public Lobby Lobby {
        get=>lobby;
        private set => lobby = value;
    }

    private void Start()
    {
        UpdatePlayerList();
        refreshButton.onClick.AddListener(UpdatePlayerList);
    }

    public void SetLobby(Lobby lobby)
    {   
        Lobby = lobby;
    }
    public void UpdatePlayerList()
    {
        if(lobby != null)
        {
            Debug.Log("Players in Lobby " + lobby.Name);
            foreach (Player player in lobby.Players)
            {
                GameObject playerLobbyCard = Instantiate(playerLobbyCardPrefab);
                PlayerLobbyCard plc = playerLobbyCard.GetComponent<PlayerLobbyCard>();
                plc.InitializePlayerLobbyCard(player);
                plc.transform.SetParent(contentParent, false);
            }    
        }
    }


}
