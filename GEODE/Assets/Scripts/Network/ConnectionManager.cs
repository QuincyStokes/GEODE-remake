using Unity.Netcode;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    public static ConnectionManager Instance;


    [HideInInspector] public string LobbyCode;
    [HideInInspector] public string RelayCode;
    [HideInInspector] public bool IsHost;

    [HideInInspector] public string PlayerID;
    [HideInInspector] public string PlayerName;



    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        
    }

    private void Start()
    {
        IsHost = NetworkManager.Singleton.IsHost;
    }


    public void ResetData()
    {
        LobbyCode = null;
        RelayCode = null;
        PlayerID = null;
        PlayerName = null;

    }
}
