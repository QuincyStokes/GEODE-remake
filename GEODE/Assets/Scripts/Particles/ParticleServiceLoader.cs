using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "New ParticleServiceLoader", menuName = "ScriptableObject/ParticleServiceLoader")]
public class ParticleServiceLoader : ScriptableObject
{
    public List<EffectPrefabPair> effectPrefabPairs = new();
}

[System.Serializable]
public struct EffectPrefabPair
{
    public ParticleSystem prefab;
    public EffectType type;
}
