using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections;


public class StatTrackManager : MonoBehaviour
{
    public static StatTrackManager Instance;
    public StatPersistence persistence;
    [SerializeField] private PlayerStats playerStats;

    private bool isCoroutineRunning;

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
            return;
        }

        persistence = new StatPersistence();
        playerStats = persistence.Load();
    }

    private void Update()
    {
        if (!isCoroutineRunning)
        {
            StartCoroutine(ScheduledSave());
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
            case StatTrackType.ItemUsed:
                if (!playerStats.itemsUsed.ContainsKey(trackedName))
                {
                    playerStats.itemsUsed.Add(trackedName, 0);
                }
                playerStats.itemsUsed[trackedName]++;
                break;
            case StatTrackType.ItemConsumed:
                if (!playerStats.itemsConsumed.ContainsKey(trackedName))
                {
                    playerStats.itemsConsumed.Add(trackedName, 0);
                }
                playerStats.itemsConsumed[trackedName]++;
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
            case StatTrackType.ItemUsed:
                if (!playerStats.itemsUsed.ContainsKey(trackedName))
                {
                    playerStats.itemsUsed.Add(trackedName, 0);
                }
                playerStats.itemsUsed[trackedName] += num;
                break;
        }
    }



    private void OnApplicationQuit()
    {
        persistence.Save(playerStats);
    }

    private IEnumerator ScheduledSave()
    {
        isCoroutineRunning = true;
        yield return new WaitForSeconds(30f);
        persistence.Save(playerStats);
        isCoroutineRunning = false;
    }

    public PlayerStats GetPlayerStats()
    {
        return playerStats;
    }



}


[Serializable]
public class PlayerStats
{
    public Dictionary<string, int> kills = new();
    public Dictionary<string, int> structuresPlaced = new();
    public Dictionary<string, int> damageHealed = new();
    public Dictionary<string, int> itemsCrafted = new();
    public Dictionary<string, int> itemsUsed = new();
    public Dictionary<string, int> itemsConsumed = new();
    public int nightsSurvived;
    public int highestNightSurvived;
}

public enum StatTrackType
{
    Kill,
    StructurePlace,
    DamageHealed,
    ItemCrafted,
    ItemUsed,
    ItemConsumed,
}