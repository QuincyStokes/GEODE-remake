using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyDisplay : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject lobbyCardPrefab;
    [SerializeField] private Button refreshButton;

    void Start()
    {
        refreshButton.onClick.AddListener(RefreshLobbyMenu);
    }

    private async void RefreshLobbyMenu()
    {
        try
        {
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            foreach(Lobby lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers);
                GameObject lobbyCard = Instantiate(lobbyCardPrefab);
                LobbyCard lc = lobbyCard.GetComponent<LobbyCard>();
                lc.lobbyName.text = lobby.Name;
                lc.currentPlayers.text = lobby.Players.Count.ToString() + " / " + lobby.MaxPlayers.ToString();
                lc.transform.SetParent(contentParent);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    
    }
}
