using System;
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

    [SerializeField] private float BASE_SPEED;
    [SerializeField] private float BASE_STRENGTH;
    [SerializeField] private float BASE_SIZE;

    public NetworkVariable<float> baseSpeed { get; set; } = new NetworkVariable<float>(1f);  //attack rate
    public NetworkVariable<float> baseStrength { get; set; } = new NetworkVariable<float>(1f);//damage/power of attack
    public NetworkVariable<float> baseSize { get; set; } = new NetworkVariable<float>(1f);//range of attack

    [SerializeField] private float rotationSpeed;
    [SerializeField] private bool rotates;

    [Header("Experience")]
    [SerializeField] public int maximumLevelXp;  //the xp needed to level up this tower
    [SerializeField] public int currentXp; //the current xp, reset from the last levelup
    [SerializeField] public int currentTotalXp; //the total xp this tower has accumulated;
    [SerializeField] public int level;

    [Header("Stat Modifiers")]
    [SerializeField] public NetworkVariable<float> speedModifier { get; set; } = new NetworkVariable<float>(0); //modifies attack rate
    [SerializeField] public NetworkVariable<float> strengthModifier { get; set; } = new NetworkVariable<float>(0); //modifies power
    [SerializeField] public NetworkVariable<float> sizeModifier { get; set; } = new NetworkVariable<float>(0); //modifies range
    [SerializeField] public NetworkVariable<float> sturdyModifier { get; set; } = new NetworkVariable<float>(0); //modifies max health

    [Header("References")]
    [SerializeField] protected LayerMask targetLayer;
    [SerializeField] protected CircleCollider2D detectionCollider;
    [SerializeField] protected GameObject tower; //separate the tower from the "base" for towers that may rotate



    // final stats
    [HideInInspector] public NetworkVariable<float> speed { get; set; } = new NetworkVariable<float>(1f);
    [HideInInspector] public NetworkVariable<float> strength { get; set; } = new NetworkVariable<float>(1f);
    [HideInInspector] public NetworkVariable<float> size { get; set; } = new NetworkVariable<float>(1f);
    [HideInInspector] public NetworkVariable<float> sturdy { get; set; } = new NetworkVariable<float>(1f);

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
    public event Action OnUpgradesChanged;
    #endregion

    #region ACCESSORS

    public int Level { get => level; set => level = value; }
    public int MaximumLevelXp { get => maximumLevelXp; set => maximumLevelXp = value; }
    public int CurrentXp { get => currentXp; set => currentXp = value; }
    public int CurrentTotalXp { get => currentTotalXp; set => currentTotalXp = value; }


    //UPGRADES 
    public List<Upgrade> Upgrades { get => upgrades; }
    public List<UpgradeItem> UpgradeItems { get => upgradeItems; }

    #endregion


    #region METHODS

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InitializeBaseStats();
        if (detectionCollider != null)
        {
            detectionCollider.radius = size.Value;
        }
    }

    private void InitializeBaseStats()
    {
        if (!IsServer) { return; }
        baseSpeed.Value = BASE_SPEED;
        baseSize.Value = BASE_SIZE;
        baseStrength.Value = BASE_STRENGTH;

        strength.Value = baseStrength.Value * (strengthModifier.Value / 100 + 1);
        speed.Value = baseSpeed.Value * (speedModifier.Value / 100 + 1);
        size.Value = baseSize.Value * (sizeModifier.Value / 100 + 1);
        sturdy.Value = MaxHealth.Value * (sturdyModifier.Value / 100 + 1);

        MaxHealth.Value = sturdy.Value;
    }

    private void Update()
    {
        if (Time.frameCount % 4 == 0)
        {
            currentTarget = GetNearestTarget();
            Debug.Log($"Current target: {currentTarget}");
            if (currentTarget != null)
            {
                Debug.Log("Current target isn't null!");
                if (rotates)
                {
                    Debug.Log("It rotates!");
                    SetTargetRotation();
                    RotateTowardsTarget();
                    if (Time.time >= nextFireTime && IsRotationComplete()) // 
                    {
                        Fire();
                        Debug.Log("Firing.");
                        nextFireTime = Time.time + 1f / speed.Value;
                    }
                }
                else if (Time.time >= nextFireTime)
                {
                    Debug.Log("Does not rotate. Firing!");
                    Fire();
                    nextFireTime = Time.time + 1f / speed.Value;
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



    private Transform GetNearestTarget()
    {
        float currentClosest = size.Value * 2; //arbitrary number, fine to set it to range*2 because that will always be outside of our range
        if (targets.Count == 0)
        {
            currentTarget = null;
            return null;
        }

        foreach (GameObject target in targets)
        {
            if (target != null)
            {
                float dist = Vector3.Distance(target.transform.position, transform.position);
                if (dist < currentClosest)
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
       if (!isRotating) return;

    // How many degrees remain to target?
    float angleDiff = Quaternion.Angle(tower.transform.rotation, targetRotation);

    if (angleDiff > 45)
    {
        // “Fast” approach for large angles, using Lerp
        // We pick a relatively large t so it snaps roughly in a few frames:
        float t = Mathf.Clamp01(6 * Time.deltaTime);
        tower.transform.rotation = Quaternion.Lerp(
            tower.transform.rotation,
            targetRotation,
            t
        );
    }
    else
    {
        // Once within threshold, switch to constant‐speed rotation
        float step = rotationSpeed * Time.deltaTime;
        tower.transform.rotation = Quaternion.RotateTowards(
            tower.transform.rotation,
            targetRotation,
            step
        );
    }

        // When super close, snap exactly on and fire
        if (angleDiff < .5f)
        {
            tower.transform.rotation = targetRotation;
            isRotating = false;
        }
    }

    private bool IsRotationComplete()
    {
        if (Quaternion.Angle(tower.transform.rotation, targetRotation) < 1f)
        {
            isRotating = false;
            return true;
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(tower.transform.position, size.Value);
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

    public void OnXpGain()
    {
        //not sure if we want to do anything here, i don't think so..
    }

    public void OnLevelUp()
    {
        baseStrength.Value += 4;
        baseSpeed.Value += 1;
        baseSize.Value += 1;
        MaxHealth.Value *= 1.05f;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyUpgradeServerRpc(int itemId)
    {
        if (!IsServer)
        {
            return;
        }

        BaseItem item = ItemDatabase.Instance.GetItem(itemId);
        UpgradeItem upgradeItem = item as UpgradeItem;
        //! FOR NOW JUST GOING TO USE A SWITCH/CASE, THIS IS NOT SCALEABLE BUT GETS THE JOB DONE FOR NOW
        if (upgradeItem != null)
        {
            UpgradeItems.Add(upgradeItem);
            serverUpgradeItemIds.Add(upgradeItem.Id);
            foreach (Upgrade upgrade in upgradeItem.upgradeList)
            {
                switch (upgrade.upgradeType)
                {
                    case UpgradeType.Strength:
                        strengthModifier.Value += upgrade.percentIncrease;
                         strength.Value = baseStrength.Value * (strengthModifier.Value / 100 + 1);
                        break;
                    case UpgradeType.Speed:
                        speedModifier.Value += upgrade.percentIncrease;
                        speed.Value = baseSpeed.Value * (speedModifier.Value / 100 + 1);
                        break;
                    case UpgradeType.Size:
                        sizeModifier.Value += upgrade.percentIncrease;
                        size.Value = baseSize.Value * (sizeModifier.Value / 100 + 1);
                        break;
                    case UpgradeType.Sturdy:
                        // Save missing hp so we can apply max hp without fully restoring health 
                        float missingHp = MaxHealth.Value - CurrentHealth.Value;
                        sturdyModifier.Value += upgrade.percentIncrease;

                        
                        sturdy.Value = BASE_HEALTH * (sturdyModifier.Value / 100 + 1);
                        MaxHealth.Value = sturdy.Value;
                        CurrentHealth.Value = MaxHealth.Value - missingHp;
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



    [ServerRpc(RequireOwnership = false)]
    public void RemoveUpgradeServerRpc(int itemId)
    {
        //! FOR NOW JUST GOING TO USE A SWITCH/CASE, THIS IS NOT SCALEABLE BUT GETS THE JOB DONE FOR NOW
        if (!IsServer)
        {
            return;
        }
        BaseItem item = ItemDatabase.Instance.GetItem(itemId);
        UpgradeItem upgradeItem = item as UpgradeItem;
        if (upgradeItem != null)
        {
            UpgradeItems.Remove(upgradeItem);
            serverUpgradeItemIds.Remove(upgradeItem.Id);
            foreach (Upgrade upgrade in upgradeItem.upgradeList)
            {
                switch (upgrade.upgradeType)
                {
                    case UpgradeType.Strength:
                        strengthModifier.Value -= upgrade.percentIncrease;
                        strength.Value = baseStrength.Value * (strengthModifier.Value / 100 + 1);
                        break;
                    case UpgradeType.Speed:
                        speedModifier.Value -= upgrade.percentIncrease;
                        speed.Value = baseSpeed.Value * (speedModifier.Value / 100 + 1);
                        break;
                    case UpgradeType.Size:
                        sizeModifier.Value -= upgrade.percentIncrease;
                        size.Value = baseSize.Value * (sizeModifier.Value / 100 + 1);
                        break;
                    case UpgradeType.Sturdy:
                        sturdyModifier.Value -= upgrade.percentIncrease;
                        
                        sturdy.Value = BASE_HEALTH * (sturdyModifier.Value / 100 + 1);
                        MaxHealth.Value = sturdy.Value;
                        if (CurrentHealth.Value > MaxHealth.Value)
                        {
                            CurrentHealth.Value = MaxHealth.Value;
                        }
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
        //! JUST MAKE THE STATS NETWORK VARIABLES I THINK
        upgradeItems.Clear();
        upgrades.Clear();

        foreach (int id in serverUpgradeItemIds)
        {
            BaseItem item = ItemDatabase.Instance.GetItem(id);
            UpgradeItem upItem = item as UpgradeItem;
            if (upItem != null)
            {
                upgradeItems.Add(upItem);
                upgrades.AddRange(upItem.upgradeList);
            }
        }
        OnUpgradesChanged?.Invoke();
        //PlayerController.Instance.inspectionMenu?.PopulateMenu(this.gameObject, true);
    }

    public void AddXp(int amount)
    {
        
        CurrentXp += amount;
        
        CheckLevelUp();
        OnXpGain();
        //maybe in the future this can be a coroutine that does it slowly for cool effect
    }

    public void CheckLevelUp()
    {
        if(CurrentXp > MaximumLevelXp)
        {
            int newXp = CurrentXp - MaximumLevelXp;
            CurrentXp = 0;
            LevelUp();
            AddXp(newXp);
        }
    }

    public void LevelUp()
    {
        Level++;
        MaximumLevelXp = Mathf.RoundToInt(MaximumLevelXp * 1.2f);
        //need some way for this to interact with stats.. OnLevelUp()? then it's up to the base classes to figure out what they wanna do
        OnLevelUp();
    }

    public void SetLevel(int level)
    {
        Level = level;
    }
    #endregion

}
