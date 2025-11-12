using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "NewMusicData", menuName = "ScriptableObject/MusicData")]
public class MusicData : ScriptableObject
{
    public MusicId musicId;
    public AudioClip[] clips;
    public float defaultVolume = 1f;
    public AudioMixerGroup amg;
}

public enum MusicId
{
    NONE,
    Forest_Day,
    Main_Menu,
    Desert_Day,
}
