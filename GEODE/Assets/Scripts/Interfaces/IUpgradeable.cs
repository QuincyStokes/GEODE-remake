using System;
using System.Collections.Generic;
using UnityEngine;

public interface IUpgradeable
{
    public List<Upgrade> Upgrades {get;}
    public List<UpgradeItem> UpgradeItems{get;} //will store the references to the actual UpgradeItems so we can refill the slots when populating the inspection menu
    public abstract void ApplyUpgradeServerRpc(int itemId, float quality=100f);
    public abstract void RemoveUpgradeServerRpc(int itemId, float quality=100f);    
    public event Action OnUpgradesChanged;
    //this all makes sense to me I think
}
