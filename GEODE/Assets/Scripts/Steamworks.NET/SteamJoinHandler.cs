using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
public class SteamJoinHandler : MonoBehaviour
{
    private Callback<GameRichPresenceJoinRequested_t> _joinRequestCallback;

    private void Awake()
    {
        if (!SteamManager.Initialized) return;

        _joinRequestCallback = Callback<GameRichPresenceJoinRequested_t>.Create(OnGameRichPresenceJoinRequested);
        DontDestroyOnLoad(gameObject); // Must survive scene loads.
    }

    private void OnDestroy()
    {
        _joinRequestCallback?.Dispose();
    }

    private void OnGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t data)
    {
        string connect = data.m_rgchConnect; // This is your "connect" value.
        Debug.Log($"[STEAM] Join requested from {data.m_steamIDFriend}, connect={connect}");

        // Get the friend's actual Steam username
        string friendName = SteamFriends.GetFriendPersonaName(data.m_steamIDFriend);
        _ = HandleConnectStringAsync(connect, friendName);
    }

    private async Task HandleConnectStringAsync(string connect, string friendName)
    {
        if (string.IsNullOrEmpty(connect)) return;

        // Example connect string: "GEODE|<lobbyCode>"
        string[] parts = connect.Split('|');
        if (parts.Length != 2 || parts[0] != "GEODE")
        {
            Debug.LogWarning("[STEAM] Invalid connect string format.");
            return;
        }
        string lobbyCode = parts[1];
        Debug.Log($"[STEAM] Attempting to join lobby with code {lobbyCode}");

 
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
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, friendName) }
                        //I believe here is where we would store other player data that we want to define, unsure of what exactly to put here for now.
                    }
                }
            };
            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);
            Debug.Log("Joined lobby " + lobby.Name);
            LobbyHandler.Instance.SetJoinedLobby(lobby);
            MainMenuController.Instance.ShowPanel("LobbyPanel");

            //ANYTHING PUT HERE WILL NOT RUN
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }


}
