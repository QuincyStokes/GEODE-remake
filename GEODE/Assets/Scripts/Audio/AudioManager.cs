using System.Collections.Generic;
using Newtonsoft.Json;
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

    //* ----------- Mixer References ------ */
    [Header("Mixers")]
    [SerializeField] private AudioMixer masterMixer;


    //* ----------- Audio Source Pool ----------- */
    [SerializeField] private int srcPoolSize;
    [SerializeField] private AudioSource bgMusicSource;
    private List<AudioSource> srcPool;

    //* Background music settings and whatnot below here */



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);

            InitializeAudioSourcePools();
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
        bgMusicSource = bgmSource.AddComponent<AudioSource>();
    }


    public void PlayLocal(SoundData data, float ?volume=null, Vector2 ?pos=null)
    {
        AudioSource source = GetAvailableSource();
        source.clip = data.clip;
        source.outputAudioMixerGroup = data.amg;

        if (volume == null) source.volume = data.defaultVolume;
        else source.volume = volume.Value;

        if (data.spatial) source.spatialBlend = .5f;
        else source.spatialBlend = 0f;

        source.pitch = Random.Range(1 - data.randomPitchOffsetMax, 1 + data.randomPitchOffsetMax);
        if(pos != null)source.transform.position = pos.Value;
        source.Play();
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

