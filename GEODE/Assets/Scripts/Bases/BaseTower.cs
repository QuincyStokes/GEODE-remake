using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public abstract class BaseTower : BaseObject, IInteractable, IStats, IExperienceGain, IUpgradeable, IDismantleable, ITracksHits, ITrackable
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
    public DamageType damageType;
    public NetworkVariable<int> kills {get; set;} = new();

    [SerializeField] private float rotationSpeed;
    [SerializeField] private bool rotates;
    [Header("Audio")]
    [SerializeField] private SoundId placeSfxId;

    [Header("Experience")]
    [SerializeField] public NetworkVariable<int> maximumLevelXp = new(100);  //the xp needed to level up this tower
    [SerializeField] public NetworkVariable<int>  currentXp = new(0); //the current xp, reset from the last levelup
    [SerializeField] public NetworkVariable<int> currentTotalXp = new(0); //the total xp this tower has accumulated;
    [SerializeField] public NetworkVariable<int> level = new(1);

    [Header("Stat Modifiers")]
    [SerializeField] public NetworkVariable<float> speedModifier { get; set; } = new NetworkVariable<float>(0); //modifies attack rate
    [SerializeField] public NetworkVariable<float> strengthModifier { get; set; } = new NetworkVariable<float>(0); //modifies power
    [SerializeField] public NetworkVariable<float> sizeModifier { get; set; } = new NetworkVariable<float>(0); //modifies range
    [SerializeField] public NetworkVariable<float> sturdyModifier { get; set; } = new NetworkVariable<float>(0); //modifies max health

    [Header("References")]
    [SerializeField] protected LayerMask targetLayer;
    [SerializeField] protected CircleCollider2D detectionCollider;
    [SerializeField] protected GameObject tower; //separate the tower from the "base" for towers that may rotate
    [SerializeField] protected GameObject rangeIndicator;
    public bool ShowingRange { get => rangeIndicator.activeSelf; set => rangeIndicator.SetActive(value); }
    [SerializeField] private Color higlightColor = new Color(0.56f, 1f, 0.44f, 1f);

    [Header("Unique Menu")]
    [SerializeField] private GameObject uniqueUI;
    public GameObject UniqueUI { get => uniqueUI; }

    [Header("Biome Overlays")]
    [SerializeField] private List<SimpleObject.BiomeSpritePair> biomeSpritePairs;
    private Dictionary<BiomeType, Sprite> biomeSpriteMap;
    [SerializeField] private SpriteRenderer biomeOverlaySprite;

    //* ----------------- final stats ------------
    [HideInInspector] public NetworkVariable<float> speed { get; set; } = new NetworkVariable<float>(1f);
    [HideInInspector] public NetworkVariable<float> strength { get; set; } = new NetworkVariable<float>(1f);
    [HideInInspector] public NetworkVariable<float> size { get; set; } = new NetworkVariable<float>(1f);
    [HideInInspector] public NetworkVariable<float> sturdy { get; set; } = new NetworkVariable<float>(1f);

    //* -------------- Upgrade privates ----------
    [HideInInspector] public List<Upgrade> upgrades = new List<Upgrade>();
    [HideInInspector] public List<UpgradeItem> upgradeItems = new List<UpgradeItem>();

    //* ----------------Internal ---------------
    protected List<GameObject> targets = new List<GameObject>();
    protected Transform currentTarget;
    private Quaternion targetRotation;
    private bool isRotating;
    private float nextFireTime;
    private List<int> serverUpgradeItemIds = new();

    //* ---------- Events -------
    public event Action OnUpgradesChanged;
    public event Action<StatTrackType, string> OnSingleTrack;
    public event Action<StatTrackType, string, int> OnMultiTrack;
    #endregion

    #region ACCESSORS

    public int Level { get => level.Value; set => level.Value = value; }
    public int MaximumLevelXp { get => maximumLevelXp.Value; set => maximumLevelXp.Value = value; }
    public int CurrentXp { get => currentXp.Value; set => currentXp.Value = value; }
    public int CurrentTotalXp { get => currentTotalXp.Value; set => currentTotalXp.Value = value; }


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

        OnSingleTrack += StatTrackManager.Instance.AddOne;
        OnMultiTrack += StatTrackManager.Instance.AddMultiple;

        OnSingleTrack?.Invoke(StatTrackType.StructurePlace, ObjectName);
        //* This plays the sound effect no matter what on spawn, could be an issue if we ever place a tower *not* by the player.
        if(placeSfxId != SoundId.NONE)
            AudioManager.Instance.PlayClientRpc(placeSfxId, transform.position);
    }

    private void Awake()
    {
        biomeSpriteMap = new();
        foreach(var item in biomeSpritePairs)
        {
            biomeSpriteMap.Add(item.biomeType, item.overlaySprite);
        }
    }

    private new void Start()
    {   
        base.Start();
        BiomeType biome = WorldGenManager.Instance.GetBiomeAtPosition(new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z));
        if(biomeSpriteMap.ContainsKey(biome))
        {
            biomeOverlaySprite.sprite = biomeSpriteMap[biome];
        }
    }

    private void InitializeBaseStats()
    {
        if (IsServer) {
            baseSpeed.Value = BASE_SPEED;
            baseSize.Value = BASE_SIZE;
            baseStrength.Value = BASE_STRENGTH;

            strength.Value = baseStrength.Value * (strengthModifier.Value / 100 + 1);
            speed.Value = baseSpeed.Value * (speedModifier.Value / 100 + 1);
            size.Value = baseSize.Value * (sizeModifier.Value / 100 + 1);
            sturdy.Value = MaxHealth.Value * (sturdyModifier.Value / 100 + 1);

            MaxHealth.Value = sturdy.Value;
        }
        RefreshRangeIndicator(0, size.Value);
        size.OnValueChanged += RefreshRangeIndicator;
    }

    private void RefreshStats()
    {
        if (!IsServer) return;
        strength.Value = baseStrength.Value * (strengthModifier.Value / 100 + 1);
        speed.Value = baseSpeed.Value * (speedModifier.Value / 100 + 1);
        size.Value = baseSize.Value * (sizeModifier.Value / 100 + 1);
        sturdy.Value = MaxHealth.Value * (sturdyModifier.Value / 100 + 1);

        MaxHealth.Value = sturdy.Value;
        RefreshRangeIndicator(0, size.Value);
    }

    private new void OnDestroy()
    {
        base.OnDestroy();
        HideRangeIndicator();
        size.OnValueChanged -= RefreshRangeIndicator;
        OnSingleTrack -= StatTrackManager.Instance.AddOne;
        OnMultiTrack -= StatTrackManager.Instance.AddMultiple;
    }

    private void FixedUpdate()
    {
        if(!IsServer) return;
        if (Time.frameCount % 4 == 0)
        {
            currentTarget = GetNearestTarget();
            if (currentTarget != null)
            {
                if (rotates)
                {
                    SetTargetRotation();
                    RotateTowardsTarget();
                    if (Time.time >= nextFireTime && IsRotationComplete()) // 
                    {
                        StartCoroutine(Fire());
                        nextFireTime = Time.time + 1f / speed.Value;
                    }
                }
                else if (Time.time >= nextFireTime)
                {
                    StartCoroutine(Fire());
                    nextFireTime = Time.time + 1f / speed.Value;
                }

            }
        }
    }


    protected Transform GetNearestTarget()
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

        float step = rotationSpeed * (speedModifier.Value+1) * Time.deltaTime;
            tower.transform.rotation = Quaternion.RotateTowards(
                tower.transform.rotation,
                targetRotation,
                step
            );

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

    public abstract IEnumerator Fire();
    public abstract void OnTriggerEnter2D(Collider2D collision);
    public abstract void OnTriggerExit2D(Collider2D collision);

    public void OnPointerEnter(PointerEventData eventData)
    {
        sr.color = higlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        sr.color = Color.white;
    }

    public void OnXpGain()
    {
        //not sure if we want to do anything here, i don't think so..
    }

    public void OnLevelUp()
    {
        baseStrength.Value += 4;
        baseSpeed.Value += .2f;
        baseSize.Value += .5f;
        MaxHealth.Value *= 1.05f;

        RefreshStats();
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
    public void RemoveUpgradeServerRpc(int itemId, int slotIndex)
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
            // Remove by slot index to preserve slot positions (fixes slot shifting bug)
            if (slotIndex >= 0 && slotIndex < UpgradeItems.Count)
            {
                UpgradeItems.RemoveAt(slotIndex);
                serverUpgradeItemIds.RemoveAt(slotIndex);
            }
            else
            {
                // Fallback: remove by value if slot index is invalid
                UpgradeItems.Remove(upgradeItem);
                serverUpgradeItemIds.Remove(upgradeItem.Id);
            }
            
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

        // Trigger event so InspectionMenu can rebuild the container display
        OnUpgradesChanged?.Invoke();
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

    public void AddXp(IDamageable damageable)
    {

        CurrentXp += damageable.DroppedXP;

        CheckLevelUp();
        OnXpGain();
        damageable.OnDeath -= AddXp;
        //maybe in the future this can be a coroutine that does it slowly for cool effect
    }


    public void CheckLevelUp()
    {
        if (CurrentXp > MaximumLevelXp)
        {
            int newXp = CurrentXp - MaximumLevelXp;
            CurrentXp = 0;
            LevelUp();
            AddXp(newXp);
        }
    }

    private void RefreshRangeIndicator(float old, float curr)
    {
        if (detectionCollider != null)
        {
            detectionCollider.radius = size.Value;
        }
        rangeIndicator.transform.localScale = new Vector3(curr * 2, curr * 2);
    }

    public void ShowRangeIndicator()
    {
        rangeIndicator.SetActive(true);
    }

    public void HideRangeIndicator()
    {
        rangeIndicator.SetActive(false);
    }

    public void ToggleRangeIndicator()
    {
        rangeIndicator.SetActive(!rangeIndicator.activeSelf);
    }

    public void LevelUp()
    {
        Level++;
        MaximumLevelXp = Mathf.RoundToInt(MaximumLevelXp * 1.6f);
        //need some way for this to interact with stats.. OnLevelUp()? then it's up to the base classes to figure out what they wanna do
        OnLevelUp();
    }

    public void SetLevel(int level)
    {
        Level = level;
    }

    public void HitSomething(IDamageable damageable)
    {

    }

    public void KilledSomething(IDamageable damageable)
    {
        TrackKill(damageable);
        AddXp(damageable);
    }

    private void TrackKill(IDamageable damageable)
    {
        kills.Value++;
        damageable.OnDeath -= TrackKill;
    }

    public virtual void DoClickedThings()
    {
        
    }

    public virtual void DoUnclickedThings()
    {
        
    }
    #endregion

}
