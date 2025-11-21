using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
    //* --------------- Singleton --------------- */
    public static BackgroundMusicManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Warning | 2 BackgroundMusicManagers are alive.");
            gameObject.SetActive(false);
        }
    }


    private void Start()
    {
        LobbyHandler.Instance.OnGameStarted += StopMusic;
        GameManager.OnPlayerSpawned += PlayDayMusic;
        DayCycleManager.becameNightGlobal += PlayNightMusic; 
        DayCycleManager.becameDayGlobal += PlayDayMusic; 
        AudioManager.Instance.OnMusicEnded += PlayMusic;
        AudioManager.Instance.PlayMusic(MusicId.Main_Menu);
    }

    private void StopMusic()
    {
        AudioManager.Instance.StopMusic();
    }

    private void PlayMusic()
    {
        if (DayCycleManager.Instance.IsNighttime())
        {
            PlayNightMusic();
        }
        else
        {
            PlayDayMusic();
        }
    }

    private void PlayNightMusic()
    {
        Vector3 playerPos = PlayerController.Instance.transform.position;
        Vector3Int pos = new Vector3Int((int)playerPos.x, (int)playerPos.y, (int)playerPos.z);
        BiomeType biomeType = WorldGenManager.Instance.GetBiomeAtPosition(pos);
        switch (biomeType)
        {
            case BiomeType.Forest:
                //AudioManager.Instance.PlayClientRpc(MusicId.Forest_Night);
                break;
            case BiomeType.Desert:
                //AudioManager.Instance.PlayMusic(MusicId.Desert_Night);
                break;
        }
        
    }

    private void PlayDayMusic()
    {
        Vector3 playerPos = PlayerController.Instance.transform.position;
        Vector3Int pos = new Vector3Int((int)playerPos.x, (int)playerPos.y, (int)playerPos.z);
        BiomeType biomeType = WorldGenManager.Instance.GetBiomeAtPosition(pos);
        switch (biomeType)
        {
            case BiomeType.Forest:
                AudioManager.Instance.PlayMusic(MusicId.Forest_Day);
                break;
            case BiomeType.Desert:
                AudioManager.Instance.PlayMusic(MusicId.Desert_Day);
                break;
        }
    }

    private void OnDisable()
    {
        LobbyHandler.Instance.OnGameStarted -= StopMusic;
        GameManager.OnPlayerSpawned -= PlayMusic;
        AudioManager.Instance.OnMusicEnded -= PlayMusic;
    }

}
