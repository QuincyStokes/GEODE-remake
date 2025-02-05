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
        for(int i = 0; i < contentParent.childCount; i++)
        {
            Destroy(contentParent.transform.GetChild(0).gameObject);
        }

        try
        {
            //HERE WILL CREATE A QUERYLOBBIESOPTIONS TO FILTER RESULTS

            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync();
            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            foreach(Lobby lobby in queryResponse.Results)
            {
                Debug.Log(lobby.Name + " " + lobby.MaxPlayers);
                GameObject lobbyCard = Instantiate(lobbyCardPrefab);
                LobbyCard lc = lobbyCard.GetComponent<LobbyCard>();
                lc.InitializeLobbyCard(lobby);
                lc.transform.SetParent(contentParent, false);
                
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    
    }
}
