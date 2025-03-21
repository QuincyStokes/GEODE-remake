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
    Material
}

[System.Serializable]public struct DroppedItem
{
    public DroppedItem(int Id, int amount)
    {
        this.Id = Id;
        this.amount = amount;
    }
    public int Id; 
    public int amount;
}
