using System;
using TMPro;
using UnityEngine;

public class PlayerStatTracker : MonoBehaviour
{
    [SerializeField] private PlayerInventory _pi;
    private void Start()
    {
        _pi.OnItemUsed += HandleItemUsed;
    }

    private void OnDisable()
    {
        _pi.OnItemUsed -= HandleItemUsed;
    }

    private void HandleItemUsed(BaseItem item)
    {
        //cast to find out what kind of item we have, add a stat accordingly. 
        switch (item.type)
        {
            case ItemType.Structure:
                StatTrackManager.Instance.AddOne(StatTrackType.StructurePlace, item.name);
                break;

            case ItemType.Tool:
                StatTrackManager.Instance.AddOne(StatTrackType.ItemUsed, item.name);
                break;
            
            case ItemType.Weapon:
                StatTrackManager.Instance.AddOne(StatTrackType.ItemUsed, item.name);
                break;

            case ItemType.Crystal:
                StatTrackManager.Instance.AddOne(StatTrackType.ItemUsed, item.name);
                break;
            
            case ItemType.Consumable:
                StatTrackManager.Instance.AddOne(StatTrackType.ItemConsumed, item.name);
                break;
        }
        
        
    }
}
