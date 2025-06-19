using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "NewSoundData", menuName = "ScriptableObject/SoundData")]
public class SoundData : ScriptableObject
{
    public SoundId soundId;
    public AudioClip clip;
    public float defaultVolume = 1f;
    public bool spatial = true;
    public float randomPitchOffsetMax = 0f;
    public AudioMixerGroup amg;
}

public enum SoundId
{
    Tree_Hit,
    Sword_Swing,
    Footstep_Grass,
    Inventory_Open,
    InventoryClose,
    

}
