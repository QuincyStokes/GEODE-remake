using UnityEditor.MemoryProfiler;
using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
    //* --------------- Singleton --------------- */
    public static BackgroundMusicManager Instance;

    private void Start()
    {
        LobbyHandler.Instance.OnGameStarted += StopMusic;
        ConnectionManager.Instance.OnPlayerSpawned += PlayMusic;
        DayCycleManager.Instance.becameNight += PlayNightMusic;
        AudioManager.Instance.OnMusicEnded += PlayMusic;
        AudioManager.Instance.PlayMusic(MusicId.Forest_Day);
    }

    private void StopMusic()
    {
        AudioManager.Instance.StopMusic();
    }

    private void PlayMusic()
    {
        AudioManager.Instance.PlayMusic(MusicId.Forest_Day);
    }

    private void PlayNightMusic()
    {
        //AudioManager.Instance.PlayClientRpc(MusicId.Forest_Night);     <------- Eventually something like this
    }

    private void OnDisable()
    {
        LobbyHandler.Instance.OnGameStarted -= StopMusic;
        ConnectionManager.Instance.OnPlayerSpawned -= PlayMusic;
        AudioManager.Instance.OnMusicEnded -= PlayMusic;
    }

}
