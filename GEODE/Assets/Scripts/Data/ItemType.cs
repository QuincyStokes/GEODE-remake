using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public enum ItemType
{
    Tool,
    Weapon,
    
    Structure,
    Consumable,
    Material,
    Upgrade
}

[System.Serializable]
public struct DroppedItem
{
    public DroppedItem(int Id, int minAmount, int maxAmount, float chance, float minQuality, float maxQuality)
    {
        this.Id = Id;
        this.minAmount = minAmount;
        this.maxAmount = maxAmount;
        this.chance = chance;
        this.minItemQuality = minQuality;
        this.maxItemQuality = maxQuality;
    }
    public int Id;
    public int minAmount;
    public int maxAmount;
    public float chance;
    public float minItemQuality;
    public float maxItemQuality;

}
