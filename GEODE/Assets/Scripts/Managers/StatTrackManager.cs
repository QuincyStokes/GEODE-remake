using System;
using System.Collections.Generic;
using UnityEngine;

public class StatTrackManager : MonoBehaviour
{
    public static StatTrackManager Instance;
    [SerializeField] private PlayerStats playerStats;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddOne(StatTrackType type, string trackedName)
    {
        Debug.Log($"Adding one {trackedName} to {type}");
        switch (type)
        {
            case StatTrackType.Kill:
                if (!playerStats.kills.ContainsKey(trackedName))
                {
                    playerStats.kills.Add(trackedName, 0);
                }
                playerStats.kills[trackedName]++;
                break;
            case StatTrackType.StructurePlace:
                if (!playerStats.structuresPlaced.ContainsKey(trackedName))
                {
                    playerStats.structuresPlaced.Add(trackedName, 0);
                }
                playerStats.structuresPlaced[trackedName]++;
                break;
            case StatTrackType.DamageHealed:
                if (!playerStats.damageHealed.ContainsKey(trackedName))
                {
                    playerStats.damageHealed.Add(trackedName, 0);
                }
                playerStats.damageHealed[trackedName]++;
                break;
            case StatTrackType.ItemCrafted:
                if (!playerStats.itemsCrafted.ContainsKey(trackedName))
                {
                    playerStats.itemsCrafted.Add(trackedName, 0);
                }
                playerStats.itemsCrafted[trackedName]++;
                break;
        }
    }

    public void AddMultiple(StatTrackType type, string trackedName, int num)
    {
        Debug.Log($"Adding multiple {trackedName} to {type}");
        switch (type)
        {
            case StatTrackType.Kill:
                if (!playerStats.kills.ContainsKey(trackedName))
                {
                    playerStats.kills.Add(trackedName, 0);
                }
                playerStats.kills[trackedName] += num;
                break;
            case StatTrackType.StructurePlace:
                if (!playerStats.structuresPlaced.ContainsKey(trackedName))
                {
                    playerStats.structuresPlaced.Add(trackedName, 0);
                }
                playerStats.structuresPlaced[trackedName] += num;
                break;
            case StatTrackType.DamageHealed:
                if (!playerStats.damageHealed.ContainsKey(trackedName))
                {
                    playerStats.damageHealed.Add(trackedName, 0);
                }
                playerStats.damageHealed[trackedName] += num;
                break;
            case StatTrackType.ItemCrafted:
                if (!playerStats.itemsCrafted.ContainsKey(trackedName))
                {
                    playerStats.itemsCrafted.Add(trackedName, 0);
                }
                playerStats.itemsCrafted[trackedName] += num;
                break;
        }
    }   
}


[Serializable]
public class PlayerStats
{
    public Dictionary<string, int> kills = new();
    public Dictionary<string, int> structuresPlaced = new();
    public Dictionary<string, int> damageHealed = new();
    public Dictionary<string, int> itemsCrafted = new();
    public int nightsSurvived;
    public int highestNightSurvived;
}

public enum StatTrackType
{
    Kill,
    StructurePlace,
    DamageHealed,
    ItemCrafted
}