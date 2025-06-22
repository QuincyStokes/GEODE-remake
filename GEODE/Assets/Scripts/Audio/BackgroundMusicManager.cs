using UnityEditor.MemoryProfiler;
using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
    //* --------------- Singleton --------------- */
    public static BackgroundMusicManager Instance;

    private void Start()
    {
        LobbyHandler.Instance.OnGameStarted += StopMusic;
        ConnectionManager.Instance.OnPlayerSpawned += PlayDayMusic;
        DayCycleManager.Instance.becameNight += PlayNightMusic; //! error, doesn't exist when this awakens. 
        DayCycleManager.Instance.becameDay += PlayDayMusic; //! error
        AudioManager.Instance.OnMusicEnded += PlayMusic;
        //AudioManager.Instance.PlayMusic(MusicId.Forest_Day); Shoulnd't need this since OnPlayerSpawned does?
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
                Debug.LogWarning("Warning. Trying to play Desert music, but aint no Desert yet.");
                break;
        }
    }

    private void OnDisable()
    {
        LobbyHandler.Instance.OnGameStarted -= StopMusic;
        ConnectionManager.Instance.OnPlayerSpawned -= PlayMusic;
        AudioManager.Instance.OnMusicEnded -= PlayMusic;
    }

}
