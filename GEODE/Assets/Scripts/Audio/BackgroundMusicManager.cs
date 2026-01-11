using UnityEngine;


//this starts off in the lobby scene
public class BackgroundMusicManager : MonoBehaviour
{
    //* --------------- Singleton --------------- */
    public static BackgroundMusicManager Instance;

    //* -------------  Background Music Setting ------------ */
    [SerializeField] private float biomeCheckFreq;


    //* ------------ Internal ---------- */
    private bool isInGame;
    private BiomeType currentBiome;
    private float biomeCheckTimer;

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
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LobbyHandler.Instance.OnGameStarted += StopMusic;
        GameManager.OnPlayerSpawned += PlayDayMusic;
        GameManager.OnPlayerSpawned += BeginBiomeCheck;
        DayCycleManager.becameNightGlobal += PlayNightMusic; 
        DayCycleManager.becameDayGlobal += PlayDayMusic; 
        AudioManager.Instance.OnMusicEnded += PlayMusic;
        AudioManager.Instance.PlayMusic(MusicId.Main_Menu);
    }


    private void Update()
    {
        //Check what biome the player is in, if it's not the biome we're currently in, and we've waited a cooldown, transition into the new biome's music. corresponding to the time of day.
            //we have two audio sources
            //one lies dormant, while the other plays
            // take the playing one, slowly turn its volume down
            //take the dormant one, set it to the new target music, set its volume to zero, then slowly turn it back up to the target voluem
                //ALSO, set it's time through the track to the current time of the other track for a seamless integration.
        if(!isInGame) return;

        biomeCheckTimer += Time.deltaTime;
        if(biomeCheckTimer > biomeCheckFreq)
        {
            biomeCheckTimer = 0f;
            BiomeType bt = GetBiomeOfPlayerPos();
            if(currentBiome == bt) return;

            //call playmusic for the new track, the AudioManagfer will handle the reat


            
        }

    }

    

    private void BeginBiomeCheck()
    {
        isInGame = true;


        currentBiome = GetBiomeOfPlayerPos();
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
        
        BiomeType bt = GetBiomeOfPlayerPos();
        
        switch (bt)
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
        PlayerController player = PlayerController.GetLocalPlayerController();
        if (player == null) return;
        
        Vector3 playerPos = player.transform.position;
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

    private BiomeType GetBiomeOfPlayerPos()
    {
        PlayerController player = PlayerController.GetLocalPlayerController();
        if (player == null) return BiomeType.None;

        Vector3 playerPos = player.transform.position;
        Vector3Int pos = new Vector3Int((int)playerPos.x, (int)playerPos.y, (int)playerPos.z);
        return WorldGenManager.Instance.GetBiomeAtPosition(pos);
    }

}
