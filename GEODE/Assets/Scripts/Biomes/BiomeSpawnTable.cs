using System.Collections.Generic;
using UnityEngine;



/// <summary>
/// This is the object for each and every thing we are going to spawn in a biome
/// </summary>
[System.Serializable]
public class BiomeSpawnEntry
{
    public string entryName;
    public BaseItem baseItem;
    public int weight;
}

[CreateAssetMenu (fileName = "NewBiomeSpawnTable", menuName = "Biomes/Biome Spawn Table")]
public class BiomeSpawnTable : ScriptableObject
{
    [Header("Optional: Name")]
    public string biomeName;

    [Tooltip("Each entry holds a prefab and a weight for spawning")]
    public List<BiomeSpawnEntry> spawnEntries;
    [HideInInspector] public int totalWeight = 0;


    private void Awake()
    {
        foreach(BiomeSpawnEntry bse in spawnEntries)
        {

            totalWeight += bse.weight;
        }
        Debug.Log($"Total weight of {name} is {totalWeight}");
    }
}
