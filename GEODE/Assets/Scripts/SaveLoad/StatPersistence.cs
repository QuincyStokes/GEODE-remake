using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System;


public class StatPersistence
{
    private readonly string savePath = Path.Combine(Application.persistentDataPath, "playerStats.json");
    public void Save(PlayerStats stats)
    {
        Debug.Log("Saving!");
        try
        {
            string json = JsonConvert.SerializeObject(stats, Formatting.Indented);
            File.WriteAllText(savePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save stats: {e}");
        }
    }

    public PlayerStats Load()
    {
        Debug.Log("Loading!");
        if (!File.Exists(savePath))
        {
            Debug.Log("Creating a new save file.");
            return new PlayerStats();
        }

        try
        {
            Debug.Log($"Loading player stats from {savePath}");
            string json = File.ReadAllText(savePath);
            return JsonConvert.DeserializeObject<PlayerStats>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load stats: {e}");
            return new PlayerStats();
        }
    }
}
