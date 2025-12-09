using Steamworks;

public static class SteamPresence
{
    public static void SetJoinable(string connectionString)
    {
        if (!SteamManager.Initialized) return;

        // This enables the Join Game button and provides the payload.
        SteamFriends.SetRichPresence("connect", connectionString);

        // Optionally set other rich presence details.
        //SteamFriends.SetRichPresence("status", "In Lobby - Join me!");
    }

    public static void SetJoinableWrapper(string lobbyId)
    {
        string conn = $"GEODE|{lobbyId}";
        SetJoinable(conn);
    }

    public static void ClearJoinable()
    {
        if (!SteamManager.Initialized) return;

        SteamFriends.SetRichPresence("connect", "");
        SteamFriends.ClearRichPresence();
    }
}
