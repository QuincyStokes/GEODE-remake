using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "NewSoundData", menuName = "ScriptableObject/SoundData")]
public class SoundData : ScriptableObject
{
    public SoundId soundId;
    public AudioClip[] clips;
    public float defaultVolume = 1f;
    public bool spatial = true;
    public float randomPitchOffsetMax = 0f;
    public float range = 15f;
    public AudioMixerGroup amg;
}


public enum SoundId
{
    NONE,
    Tree_Hit,
    Sword_Swing,
    Footstep_Grass,
    Inventory_Open,
    Inventory_Close,
    Loot_Pickup,
    Rock_Hit,
    Geode_Hit,
    Bush_Hit,
    Bush_Break,
    Grass_Walk,
    Wood_Structure_Place,
    Sand_Walk,
    Wooden_Tower_Break,
    Bow_Tower_Shoot,
    

}


