using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class BaseTower : BaseObject, IInteractable, IStats, IExperienceGain, IUpgradeable
{
    //all towers shall inherit from this script, so we must include all things that *all* towers need
    //hold up, should towers have states? i think that doin too much, towers are simple
    #region PROPERTIES

    [Header("Tower Stats")]
    [SerializeField] public float baseSpeed;  //attack rate
    [SerializeField] public float baseStrength; //damage/power of attack
    [SerializeField] public float baseSize; //range of attack
    [SerializeField] private float rotationSpeed;
    [SerializeField] private bool rotates;

    [Header("Experience")]
    [SerializeField] public int maximumLevelXp;  //the xp needed to level up this tower
    [SerializeField] public int currentXp; //the current xp, reset from the last levelup
    [SerializeField] public int currentTotalXp; //the total xp this tower has accumulated;
    [SerializeField] public int level;

    [Header("Stat Modifiers")]
    [SerializeField] private float speedModifier = 1.0f; //modifies attack rate
    [SerializeField] private float strengthModifier = 1.0f; //modifies power
    [SerializeField] private float sizeModifier = 1.0f; //modifies range
    [SerializeField] private float sturdyModifier = 1.0f; //modifies max health

    [Header("References")]
    [SerializeField] protected LayerMask targetLayer;
    [SerializeField] protected CircleCollider2D detectionCollider;
    [SerializeField] protected GameObject tower; //separate the tower from the "base" for towers that may rotate



    // final stats
    [HideInInspector]public float speed;
    [HideInInspector]public float strength;
    [HideInInspector]public float size;
    [HideInInspector]public float sturdy;

    //Upgrade privates
    [HideInInspector] public List<Upgrade> upgrades = new List<Upgrade>();
    [HideInInspector] public List<UpgradeItem> upgradeItems = new List<UpgradeItem>();

    //Internal
    protected List<GameObject> targets = new List<GameObject>();
    protected Transform currentTarget;
    private Quaternion targetRotation;
    private bool isRotating;
    private float nextFireTime;
    private List<int> serverUpgradeItemIds = new();
    #endregion

    #region ACCESSORS

    //* BASE STAT VALUES
    public float BaseSpeed{get=>baseSpeed; set=> baseSpeed = value;} //
    public float BaseStrength{get=>baseStrength; set=> baseStrength = value;}
    public float BaseSize{get=>baseSize; set=> baseSize = value;}

    //* STAT MODIFIERS
    public float SturdyModifier { get => sturdyModifier; set => sturdyModifier = value;}
    public float SpeedModifier { get => speedModifier; set => speedModifier = value; }
    public float SizeModifier { get => sizeModifier; set => sizeModifier = value; }
    public float StrengthModifier { get => strengthModifier; set => strengthModifier = value; }

    //* STAT TOTALS

    public float Sturdy { get => sturdy; set => sturdy = value;}
    public float Speed { get => speed; set => speed = value; }
    public float Size { get => size; set => size = value; }
    public float Strength { get => strength; set => strength = value; }

    public int Level { get => level; set => level = value;}
    public int MaximumLevelXp { get => maximumLevelXp; set => maximumLevelXp = value;}
    public int CurrentXp { get => currentXp; set => currentXp = value; }
    public int CurrentTotalXp { get => currentTotalXp; set => currentTotalXp = value;}


    //UPGRADES 
    public List<Upgrade> Upgrades { get => upgrades; }
    public List<UpgradeItem> UpgradeItems { get => upgradeItems;}

    #endregion


    #region METHODS

    private void Awake()
    {
        if(detectionCollider != null)
        {
            detectionCollider.radius = size;
        }

        RefreshStats();

        detectionCollider.radius = size;
    }

    private void Update()
    {
        if(Time.frameCount % 4 == 0)
        {
            currentTarget = GetNearestTarget();
            Debug.Log($"Current target: {currentTarget}");
            if(currentTarget != null)
            {
                Debug.Log("Current target isn't null!");
                if(rotates)
                {
                    Debug.Log("It rotates!");
                    SetTargetRotation();
                    RotateTowardsTarget();
                    if(Time.time >= nextFireTime && IsRotationComplete()) // 
                    {
                        Fire();
                        Debug.Log("Firing.");
                        nextFireTime = Time.time + 1f / speed;
                    }
                }
                else if(Time.time >= nextFireTime)
                {
                    Debug.Log("Does not rotate. Firing!");
                    Fire();
                    nextFireTime = Time.time + 1f / speed;
                }
                
            }
        }
    }

    // public void AddXp(int amount)
    // {
    //     CurrentXp += amount;
    //     currentTotalXp += amount;
    //     //TODO update level UI here
    //     if(CurrentXp >= maximumLevelXp)
    //     {
    //         LevelUp();
    //     }
    // }

    // private void LevelUp()
    // {
    //     currentXp = 0;
    //     level++;
    //     //TODO apply some modifier to stats (increase base stats)
    // }

  
    private void RefreshStats()
    {
        strength = baseStrength * (StrengthModifier/100+1);
        speed = baseSpeed * (SpeedModifier/100+1);
        size = baseSize * (SizeModifier/100+1);
        sturdy =  MaxHealth * (SturdyModifier/100+1);//notably, MaxHealth is derived from BaseObject
    }

    private Transform GetNearestTarget()
    {
        float currentClosest = size*2; //arbitrary number, fine to set it to range*2 because that will always be outside of our range
        if(targets.Count == 0)
        {
            currentTarget = null;
            return null;
        }

        foreach(GameObject target in targets)
        {
            if(target != null)
            {
                float dist = Vector3.Distance(target.transform.position, transform.position);
                if(dist < currentClosest)
                {
                    currentClosest = dist;
                    currentTarget = target.transform;
                }
            }
            
        }
        return currentTarget;
    }
    protected void AddTarget(GameObject target)
    {
        targets.Add(target);
    }

    protected void RemoveTarget(GameObject target)
    {
        targets.Remove(target);
    }

     private void SetTargetRotation()
    {
        Debug.Log("Setting target rotation!");
        Vector2 direction = (currentTarget.position - tower.transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        targetRotation = Quaternion.Euler(new Vector3(0, 0, angle));
        isRotating = true;
        
    }

    private void RotateTowardsTarget()
    {
        if(isRotating)
        {
            Debug.Log($"Tower is rotating towards {targetRotation}");
            tower.transform.rotation = Quaternion.Lerp(tower.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private bool IsRotationComplete()
    {
        if(Quaternion.Angle(tower.transform.rotation, targetRotation) < 1f)
        {
            isRotating = false;
            return true;
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(tower.transform.position, size);
    }

    public abstract void Fire();
    public abstract void OnTriggerEnter2D(Collider2D collision);
    public abstract void OnTriggerExit2D(Collider2D collision);

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Pointer Entered from BaseTower");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Pointer Exited from BaseTower");
    }

    public void OnInteract()
    {
        PopulateInteractionMenu();
    }

    public void PopulateInteractionMenu()
    {
        InspectionMenu.Instance.PopulateMenu(this.gameObject);
    }

    public void OnXpGain()
    {
        //not sure if we want to do anything here, i don't think so..
    }

    public void OnLevelUp()
    {
        //add to stat modifiers
    }

    [ServerRpc(RequireOwnership =false)]
    public void ApplyUpgradeServerRpc(int itemId)
    {
        if(!IsServer)
        {
            return;
        }

        BaseItem item = ItemDatabase.Instance.GetItem(itemId);
        UpgradeItem upgradeItem = item as UpgradeItem;
        //! FOR NOW JUST GOING TO USE A SWITCH/CASE, THIS IS NOT SCALEABLE BUT GETS THE JOB DONE FOR NOW
        if(upgradeItem != null)
        {
            UpgradeItems.Add(upgradeItem);
            serverUpgradeItemIds.Add(upgradeItem.Id);
            foreach(Upgrade upgrade in upgradeItem.upgradeList)
            {
                switch(upgrade.upgradeType)
                {
                    case UpgradeType.Strength:
                        StrengthModifier += upgrade.percentIncrease;
                        break;
                    case UpgradeType.Speed:
                        SpeedModifier += upgrade.percentIncrease;
                        break;
                    case UpgradeType.Size:
                        SizeModifier += upgrade.percentIncrease;
                        break;
                    case UpgradeType.Sturdy:
                        SturdyModifier += upgrade.percentIncrease;
                        break;
                    default:
                        break;

                }
            }
            
        }
        else
        {   
            Debug.Log("Did not apply stats. UpgradeItem is null");
        }
        SyncUpgradesAndStatsClientRpc(serverUpgradeItemIds.ToArray());
    }



    [ServerRpc(RequireOwnership =false)]
    public void RemoveUpgradeServerRpc(int itemId)
    {
        //! FOR NOW JUST GOING TO USE A SWITCH/CASE, THIS IS NOT SCALEABLE BUT GETS THE JOB DONE FOR NOW
        if(!IsServer)
        {
            return;
        }
        BaseItem item = ItemDatabase.Instance.GetItem(itemId);
        UpgradeItem upgradeItem = item as UpgradeItem;
        if(upgradeItem != null)
        {
            UpgradeItems.Remove(upgradeItem);
            serverUpgradeItemIds.Remove(upgradeItem.Id);
            foreach(Upgrade upgrade in upgradeItem.upgradeList)
            {
                switch(upgrade.upgradeType)
                {
                    case UpgradeType.Strength:
                        StrengthModifier -= upgrade.percentIncrease;
                        break;
                    case UpgradeType.Speed:
                        SpeedModifier -= upgrade.percentIncrease;
                        break;
                    case UpgradeType.Size:
                        SizeModifier -= upgrade.percentIncrease;
                        break;
                    case UpgradeType.Sturdy:
                        SturdyModifier -= upgrade.percentIncrease;
                        break;
                    default:
                        break;

                }
            }
            
        }
        SyncUpgradesAndStatsClientRpc(serverUpgradeItemIds.ToArray());
    }

    [ClientRpc]
    private void SyncUpgradesAndStatsClientRpc(int[] itemIds)
    {
        //First, clear our ItemUpgrades list to start fresh
        serverUpgradeItemIds = new List<int>(itemIds);

        upgradeItems.Clear();
        upgrades.Clear();

        foreach(int id in serverUpgradeItemIds)
        {
            BaseItem item = ItemDatabase.Instance.GetItem(id);
            UpgradeItem upItem = item as UpgradeItem;
            if(upItem != null)
            {
                upgradeItems.Add(upItem);
                upgrades.AddRange(upItem.upgradeList);
            }
        }

        strength = baseStrength * (StrengthModifier/100+1);
        speed = baseSpeed * (SpeedModifier/100+1);
        size = baseSize * (SizeModifier/100+1);
        sturdy =  MaxHealth * (SturdyModifier/100+1);//notably, MaxHealth is derived from BaseObject
    }

    public void RefreshUpgrades()
    {
        //this.upgradeItems = new List<UpgradeItem>();
        
    }


    #endregion

}
