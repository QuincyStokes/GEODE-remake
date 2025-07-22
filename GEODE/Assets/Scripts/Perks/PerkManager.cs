using System.Collections.Generic;
using UnityEngine;

public class PerkManager : MonoBehaviour
{
    [Header("Perk Collections")]
    public List<PerkCollection> perkCollections;

    [Header("Perk Prefab")]
    public GameObject perkPrefab;


    private void Awake()
    {
        PopulatePerks();
    }

    private void PopulatePerks()
    {
        Debug.Log("Populating perks.");
        foreach (PerkCollection perkCollection in perkCollections)
        {
            perkCollection.Initialize(this);
        }
    }
}
