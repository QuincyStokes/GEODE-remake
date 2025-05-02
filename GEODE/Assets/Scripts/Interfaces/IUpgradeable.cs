using System.Collections.Generic;
using UnityEngine;

public interface IUpgradeable
{
    public List<Upgrade> Upgrades {get;}
    public List<UpgradeItem> UpgradeItems{get;} //will store the references to the actual UpgradeItems so we can refill the slots when populating the inspection menu
    public abstract void ApplyUpgrade(Upgrade upgrade);
    public abstract void RemoveUpgrade(Upgrade upgrade);
    public abstract void RefreshUpgrades(); //loops through our Upgrades list and applies all of their affects. 

    //this all makes sense to me I think
}
