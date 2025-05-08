using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewUpgradeItem", menuName = "Items/UpgradeItem")]
public class UpgradeItem : BaseItem
{
    [Header("Upgrade Settings")]
    [SerializeField] public List<Upgrade> upgradeTypes;


    public override bool Use(Vector3 position, bool snapToGrid = true, bool force = false)
    {
        return false;
    }
}

[Serializable]
public enum UpgradeType
{
    Strength,
    Speed,
    Size,
    Sturdy

};

[Serializable]
public struct Upgrade
{
    [SerializeField] public UpgradeType upgradeType; //the type of stat it upgrades
    [SerializeField] public float percentIncrease; //the increase this upgrade has on the stat (eg 10, would increase a stat by 10%)
};

