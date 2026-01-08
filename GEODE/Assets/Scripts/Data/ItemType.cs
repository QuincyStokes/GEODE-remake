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
    Upgrade,
    Crystal,
    Relic
}

[System.Serializable]
public struct DroppedItem
{
    public DroppedItem(int Id, int minAmount, int maxAmount, float chance)
    {
        this.Id = Id;
        this.minAmount = minAmount;
        this.maxAmount = maxAmount;
        this.chance = chance;
    }
    public int Id;
    public int minAmount;
    public int maxAmount;
    public float chance;

}
