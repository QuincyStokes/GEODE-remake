using System;
using System.Threading.Tasks;
using NUnit.Compatibility;
using TMPro;
using Unity.Services.Authentication;
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
    public Lobby Lobby
    {
        get => lobby;
        private set => lobby = value;
    }

    private void Awake()
    {


        startButton.gameObject.SetActive(true);
        startButton.onClick.AddListener(() =>
        {
            LobbyHandler.Instance.StartGame();
        });


    }

    private void Start()
    {
        LobbyHandler.onLobbyUpdated += UpdatePlayerList;
    }

    private void OnDisable()
    {
        LobbyHandler.onLobbyUpdated -= UpdatePlayerList;
        Lobby = null;
        lobbyCode.text = "";
    }

    public void SetLobby(Lobby lobby)
    {
        Lobby = lobby;
        lobbyCode.text = Lobby.LobbyCode;
        if (!IsLobbyHost())
        {
            startButton.gameObject.SetActive(false);
        }
        else
        {
            startButton.gameObject.SetActive(true);
        }
    }
    public void UpdatePlayerList(Lobby lobby)
    {
        if(lobby == null) return;
        Debug.Log("Updating Player List for Lobby " + lobby.Name);
        SetLobby(lobby);



        foreach (Transform transform in contentParent)
        {
            Destroy(transform.gameObject);
        }


        if (lobby != null)
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

    private bool IsLobbyHost()
    {
        Debug.Log("Checking whether this is the host" + Lobby.HostId == AuthenticationService.Instance.PlayerId);
        return Lobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    public void LeaveLobby()
    {
        LobbyHandler.Instance.LeaveLobby();
        MainMenuController.Instance.ShowPanel("MultiplayerPanel");
    }

    public void HandleLobbyUpdate(Lobby l)
    {
        if(l == null)
        {
            ClearUI();
            MainMenuController.Instance.ShowPanel("MultiplayerPanel");
            LobbyErrorMessages.Instance.SetError("Host left lobby. Returning to Main Menu.");
        }
        else
        {
            UpdatePlayerList(l);
        }
    }

    public void ClearUI()
    {
        // Clear player list, lobby name field, etc.
        foreach (Transform transform in contentParent)
        {
            Destroy(transform.gameObject);
        }
       

    }
}
