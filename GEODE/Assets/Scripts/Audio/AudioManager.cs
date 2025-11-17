using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : NetworkBehaviour
{
    /// <summary>
    /// Here lies the Audio Manager.
    /// All sound will be played through this script, he is our Audio Lord and Savior.
    /// His contents shall be upheld to the highest of standards.
    /// Scalability, Readability, Visibility, and Usability.
    /// He is here to SURV us all.
    /// </summary>

    //* ---------- Singleton ----------- */
    public static AudioManager Instance { get; private set; }
    [Header("Sound Database SO")]
    [SerializeField] private SoundDatabase soundDatabase;
    private Dictionary<SoundId, SoundData> sounds = new Dictionary<SoundId, SoundData>();

    [Header("Music Database SO")]
    [SerializeField] private MusicDatabase musicDatabase;
    private Dictionary<MusicId, MusicData> musicTracks = new Dictionary<MusicId, MusicData>();
    [SerializeField] private float fadeTime;

    //* ----------- Mixer References ------ */
    [Header("Mixers")]
    [SerializeField] private AudioMixer masterMixer;


    //* ----------- Audio Source Pool ----------- */
    [SerializeField] private int srcPoolSize;
    private AudioSource bgMusicSource;
    private List<AudioSource> srcPool;

    //* ----------- Background Music -------------- */
    private Coroutine currentFade;
    private Coroutine currentSongTimer;

    //* ----------------- Events ------------ */
    public event Action OnMusicStarted;
    public event Action OnMusicEnded;



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSourcePools();
            InitializeSoundDatabase();
            InitializeMusicDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudioSourcePools()
    {
        //initialize the AudioSource pools
        srcPool = new List<AudioSource>(srcPoolSize);

        //Create SFX audio sources
        for (int i = 1; i <= srcPoolSize; i++)
        {
            GameObject obj = new("AudioSource_" + i);
            obj.transform.SetParent(transform);
            AudioSource src = obj.AddComponent<AudioSource>();
            srcPool.Add(src);
        }


        //Create Background music AudioSource
        GameObject bgmSource = new();
        bgmSource.transform.SetParent(transform);
        bgMusicSource = bgmSource.AddComponent<AudioSource>();
    }

    private void InitializeSoundDatabase()
    {
        foreach (SoundData curr in soundDatabase.SoundDataList)
        {
            sounds[curr.soundId] = curr;
        }
    }

    private void InitializeMusicDatabase()
    {
        foreach (MusicData curr in musicDatabase.MusicDataList)
        {
            musicTracks[curr.musicId] = curr;
        }
        
    }

    /// <summary>
    /// Plays a given sound for the local client.
    /// </summary>
    /// <param name="id">SoundID for specific sound to play</param>
    /// <param name="pos">Position at which to play the Sound. Default is 0,0</param>
    /// <param name="volume">Volume to play the sound at. Default is the SoundData default.</param>
    public void PlayLocal(SoundId id, Vector2 pos = default, float volume = 0f)
    {
        DoPlay(id, pos, volume);
    }

    /// <summary>
    /// Plays a given sound for the local client.
    /// </summary>
    /// <param name="id">SoundID for specific sound to play</param>
    /// <param name="pos">Position at which to play the Sound. Default is 0,0</param>
    /// <param name="volume">Volume to play the sound at. Default is the SoundData default.</param>
    [ClientRpc (RequireOwnership = false)]
    public void PlayClientRpc(SoundId id, Vector2 pos=default, float volume = default)
    {
        DoPlay(id, pos, volume);
    }


    private void DoPlay(SoundId id, Vector2 pos = default, float volume = default)
    {
        SoundData data = sounds[id];
        if (data == null)
        {
            Debug.LogWarning($"SoundID {id} not found. Make sure you added it to the SoundDatabase.");
            return;
        }
        if(data.clips.Length == 0) { Debug.LogWarning($"No AudioClips found for {data.soundId}"); return; }

        AudioSource source = GetAvailableSource();
        source.clip = data.clips[UnityEngine.Random.Range(0, data.clips.Length)];
        source.outputAudioMixerGroup = data.amg;

        //source.rolloffMode = AudioRolloffMode.Linear;

        source.maxDistance = data.range;

        if (volume == 0) source.volume = data.defaultVolume;
        else source.volume = volume;

        if (data.spatial) source.spatialBlend = 1f;
        else source.spatialBlend = 0f;

        source.pitch = UnityEngine.Random.Range(1 - data.randomPitchOffsetMax, 1 + data.randomPitchOffsetMax);
        if (pos != Vector2.zero) source.transform.position = pos;
        else source.transform.position = Vector2.zero;
        
        source.Play();
    }


    public void PlayMusic(MusicId id)
    {
        MusicData data = musicTracks[id];
        if (data == null)
        {
            Debug.LogWarning($"MusicID {id} not found. Make sure you added it to the MusicDatabase.");
            return;
        }

        //If PlayMusic was just called, need to stop the coroutines from fighting over the volume.
            //Stop the current one, then play.
        if (currentFade != null) { StopCoroutine(currentFade); }
        currentFade = StartCoroutine(DoMusic(data));
    }

    public void StopMusic()
    {
        if (bgMusicSource.isPlaying)
        {
            //Stop any other fade or music going on right now.
            if (currentFade != null) { StopCoroutine(currentFade); }
            if (currentSongTimer != null) { StopCoroutine(currentSongTimer); }

            StartCoroutine(DoStopMusic());
            
        }
    }

    private IEnumerator DoStopMusic()
    {
        float elapsed = 0f;
        float startVolume = bgMusicSource.volume;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = 1 - (elapsed / fadeTime);
            bgMusicSource.volume = t * startVolume;
            yield return null;
        }
        bgMusicSource.volume = 0f;
        bgMusicSource.Stop();
    }

    private IEnumerator DoMusic(MusicData data)
    {
        //if there's already music playing, need to fade out
        float elapsed = 0f;
        
        if (bgMusicSource.isPlaying)
        {
            float startVolume;
            startVolume = bgMusicSource.volume;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = 1 - (elapsed / fadeTime);
                
                bgMusicSource.volume = t * startVolume;
                yield return null;
            }
        }
        bgMusicSource.outputAudioMixerGroup = data.amg;
        bgMusicSource.spatialBlend = 0f;
        bgMusicSource.volume = 0f;
        //we're here, which means the previous background music (if there was one) is done playing
        
        //now, Fade in new track
        elapsed = 0f;
        float targetVolume = data.defaultVolume;
        Debug.Log($"Target Volume: {data.defaultVolume}");
        bgMusicSource.clip = data.clips[UnityEngine.Random.Range(0, data.clips.Length)];
        if (bgMusicSource.clip == null) yield break;
        bgMusicSource.Play();
        currentSongTimer = StartCoroutine(MusicTimer(bgMusicSource.clip));
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;

            bgMusicSource.volume = t * targetVolume;
            yield return null;
        }
        bgMusicSource.volume = data.defaultVolume;
    }

    private IEnumerator MusicTimer(AudioClip clip)
    {
        //simply tracks when a music track ends naturally-ish
            //just starts a timer of the same length, this can *certainly* go wrong, but there is no built in fix for unity.
        OnMusicStarted?.Invoke();
        yield return new WaitForSeconds(clip.length);
        OnMusicEnded?.Invoke();
    }


    /// <summary>
    /// Used to get the next available audio source from the  pool.
    /// </summary>
    /// <returns>Returns an AudioSource from the pool. If no available sources are present, creates a new one</returns>
    private AudioSource GetAvailableSource()
    {
        //Find an audioSource that isn't playing
        foreach (AudioSource source in srcPool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        //If all audioSources are occupied, lets create a new one and add it to the pool
        GameObject obj = new("AudioSource_" + srcPool.Count + 1);
        obj.transform.SetParent(transform);
        AudioSource src = obj.AddComponent<AudioSource>();
        srcPool.Add(src);

        return src;
    }

    

}

