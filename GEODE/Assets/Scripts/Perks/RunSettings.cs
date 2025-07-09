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
    public float playerDamageMultiplier;
    public List<BaseItem> additionalStartingItems;
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


    public void Reset()
    {
        chosenPerks.Clear();
        additionalStartingItems.Clear();
    }


    public enum StatRequirement
    {
        Kill,
        NightsSurvived,
        ItemCrafted,
        
        
    }

}
