using System.Collections.Generic;
using UnityEngine;
using System;
public class PlayerAnimationSFX : MonoBehaviour
{
    [SerializeField] private List<BiomeSoundPair> biomeWalkSounds;
    private Dictionary<BiomeType, SoundId> biomeAudioMap;


    private void Awake()
    {
        biomeAudioMap = new();
        foreach(var item in biomeWalkSounds)
        {
            biomeAudioMap.Add(item.biomeType, item.soundId);
        }
    }

    public void PlayWalkSFX()
    {
        //Do some logic based on what we're standing on.
        BiomeType b = WorldGenManager.Instance.GetBiomeAtPosition(new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z));

        AudioManager.Instance.PlayLocal(biomeAudioMap[b], transform.position);
    }
}


[Serializable]
public struct BiomeSoundPair
{
    public BiomeType biomeType;
    public SoundId soundId;
}
