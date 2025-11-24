using System.Collections.Generic;
using UnityEngine;

public class RunSettings : MonoBehaviour
{
    ///This script shall carry over all of the logic of the perks chosen into the actual game! 
    /// Needs to be on a gameObject in the lobby scene, will be marked as dont destroy
    /// 
    public static RunSettings Instance;

    //------------------- Perk Data ----------- */
    public List<PerkData> chosenPerks = new List<PerkData>(); //the list of perks, can loop through this and call Apply for each


    // ------------------ Default Values ------- */
    //depends more specifically on what we want our upgrades to be
    //will add more here as we go on.

    //* TOWER MODIFIERS */
    public float towerDamage;
    public float towerSpeed;
    public float towerHealth;
    public float towerRange;

    //* PLAYER MODIFIERS */
    public float playerDamage;
    public float playerMovespeed;
    public float playerXp;
    public float playerHealth;


    //* STARTING ITEMS */
    public List<BaseItem> additionalStartingItems;

    //*DROPRATE MODIFIERS */ (unsure about this one)
    public float droprate;


    //* WORLD SETTINGS *//
    public Difficulty worldDifficulty;
    public Size worldSize; 
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

    private void Start()
    {
        LobbyHandler.Instance.OnGameStarted += ApplyStats;
    }


    public void Reset()
    {
        chosenPerks.Clear();
        additionalStartingItems.Clear();
    }

    private void ApplyStats()
    {
        foreach (PerkData perk in chosenPerks)
        {
            perk.Apply(this);
        }
        LobbyHandler.Instance.OnGameStarted -= ApplyStats;
    }

    public void LoadWorldSettings(Size size, Difficulty difficulty)
    {
        worldSize = size;
        worldDifficulty = difficulty;
    }

}
public enum StatRequirement
    {
        Kill,
        NightsSurvived,
        ItemCrafted,
        StructurePlaced
    }
