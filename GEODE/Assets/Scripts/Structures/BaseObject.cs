using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public abstract class BaseObject : NetworkBehaviour, IDamageable
{
    //* ----------------- Health --------------- */
    [Header("Health")]
    public float BASE_HEALTH;
    public NetworkVariable<float> MaxHealth { get; set; } = new NetworkVariable<float>(1);
    public NetworkVariable<float> CurrentHealth { get; set; } = new NetworkVariable<float>(1);

    //* ----------------- Breaking ----------------- */
    [Header("Breaking")]
    [SerializeField] private ToolType idealToolType;
    [SerializeField] private List<DroppedItem> droppedItems;
    [SerializeField] private int droppedXp;
    [Tooltip("2 Elements, higher health first.")][SerializeField] private List<Sprite> healthStateSprites;
    [SerializeField] private Transform particleSpawnPoint;
    [SerializeField] private EffectType hitParticleEffectType;
    [SerializeField] private EffectType destroyParticleEffectType;
    [SerializeField] private EffectType healParticleEffectType;
    public EffectType HitParticleEffectType { get => hitParticleEffectType; }
    public List<DroppedItem> DroppedItems { get => droppedItems; }
    public int DroppedXP { get => droppedXp; }
    public Transform ParticleSpawnPoint { get => particleSpawnPoint; set => particleSpawnPoint = value; }

    //* ---------------- Audio ------------------------- */
    [Header("Audio")]
    [SerializeField] private SoundId hitSoundId;   
    [SerializeField] private SoundId breakSoundId;   


    //* ---------------- Object Info -------------------- */
    [Header("Object Info")]
    [SerializeField] private string objectName;
    public string ObjectName{ get => objectName; set => objectName = value;}
    [SerializeField] private Transform centerPoint;
    public Transform CenterPoint { get => centerPoint; }
    public Transform ObjectTransform{ get => transform; }
    public Collider2D CollisionHitbox { get => collisionHitbox; }
    [HideInInspector] public string description;
    [HideInInspector] public Sprite objectSprite;

    //* ------------------ Inspector References ---------------- */
    [Header("Private References")]
    [SerializeField] protected SpriteRenderer sr;
    [SerializeField] private Collider2D collisionHitbox;

    //* ------------------ Internal Use -------------------- */
    [HideInInspector] public int matchingItemId = -1;
    private int healthState = -1;

    //* ------------------- Events ----------------
    public event Action<IDamageable> OnDeath;

    //METHODS

    private void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            InitializeHealthServerRpc();
        }
    }

    public void InitializeItemId(int id)
    {
        matchingItemId = id;
    }

    [ServerRpc(RequireOwnership = false)]
    public void InitializeHealthServerRpc()
    {
        MaxHealth.Value = BASE_HEALTH;
        CurrentHealth.Value = MaxHealth.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RestoreHealthServerRpc(float amount)
    {
        if (!IsServer)
        {
            return;
        }
        float amountToHeal = Mathf.Min(amount, MaxHealth.Value - CurrentHealth.Value);

        if(amountToHeal > 0)
        {
            CurrentHealth.Value += amountToHeal;

            if (healParticleEffectType != EffectType.None)
            {
                ParticleService.Instance.PlayClientRpc(healParticleEffectType, particleSpawnPoint.position);
            }   
        }
        Debug.Log($"Restoring {amount} health to {name}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(DamageInfo info)
    {
        if (!IsServer)
        {
            return;
        }

        ApplyDamage(info);
    }

    public void ApplyDamage(DamageInfo info)
    //public void ApplyDamage(float amount, Vector2 sourceDirection, bool dropItems = false)
    {
        if (!IsServer)
        {
            return;
        }
        if (info.amount <= 0)
        {
            return;
        }



        //take full damage if hit by the proper weapon.
        if (info.tool == idealToolType)
        {
            CurrentHealth.Value -= info.amount;
            OnTakeDamage(info.amount, info.sourceDirection);
        }
        else //else, take 25% damage
        {
            CurrentHealth.Value -= info.amount / 4;
            OnTakeDamage(info.amount / 4, info.sourceDirection);
        }

        Debug.Log($"Current Health < 0? {CurrentHealth.Value}");
        if (CurrentHealth.Value <= 0)
        {
            DestroyThis(info.dropItems);
        }

    }

    [ClientRpc]
    public void DisplayDamageFloaterClientRpc(float amount)
    {
        GameObject damageFloater;
        if (CenterPoint != null)
        {
            damageFloater = Instantiate(GameAssets.Instance.damageFloater, CenterPoint.position, Quaternion.identity);
        }
        else
        {
            damageFloater = Instantiate(GameAssets.Instance.damageFloater, transform.position, Quaternion.identity);
        }
        damageFloater.GetComponent<DamageFloater>().Initialize(amount);
    }

    public void OnTakeDamage(float amount, Vector2 sourceDirection)
    {
        OnDamageColorChangeClientRpc();
        DisplayDamageFloaterClientRpc(amount);
        CheckSpriteChangeClientRpc();
        if (particleSpawnPoint != null)
        {
            ParticleService.Instance.PlayClientRpc(hitParticleEffectType, particleSpawnPoint.position);
        }

        if (hitSoundId != SoundId.NONE)
        {
            AudioManager.Instance.PlayClientRpc(hitSoundId, transform.position);
        }
            
    }

    [ClientRpc]
    public void OnDamageColorChangeClientRpc()
    {
        StartCoroutine(FlashDamage(.15f));
    }

    private IEnumerator FlashDamage(float time)
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(time);
        sr.color = Color.white;
    }

    public void DropItems()
    {
        if (DroppedItems != null && LootManager.Instance != null)
        {
            foreach (DroppedItem item in droppedItems)
            {
                //If the item has something other than 100% drop chance
                if (item.chance < 100)
                {
                    //roll the dice to see if we should spawn this item
                    float rolledChance = UnityEngine.Random.Range(0f, 100f);
                    if (rolledChance <= item.chance)
                    {
                        LootManager.Instance.SpawnLootServerRpc(centerPoint.position, item.Id, UnityEngine.Random.Range(item.minAmount, item.maxAmount + 1), minQuality:item.minItemQuality, maxQuality:item.maxItemQuality);
                    }
                }
                else
                {
                    LootManager.Instance.SpawnLootServerRpc(centerPoint.position, item.Id, UnityEngine.Random.Range(item.minAmount, item.maxAmount + 1), minQuality:item.minItemQuality, maxQuality:item.maxItemQuality);
                }


            }
        }
        else
        {
            Debug.Log($"Tried to drop items | LootManager null?: {LootManager.Instance == null}, DroppedItems null? : {DroppedItems == null} ");
        }

    }

    public virtual void DestroyThis(bool dropItems)
    {
        if (!IsServer)
        {
            return;
        }
        OnDeath?.Invoke(this);
        if (dropItems)
        {
            DropItems();
        }
        if (breakSoundId != SoundId.NONE)
        {
            AudioManager.Instance.PlayClientRpc(breakSoundId, transform.position);
        }
        ChunkManager.Instance.DeregisterObject(gameObject);
        GetComponent<NetworkObject>().Despawn(true);

        FlowFieldManager.Instance.CalculateFlowField();
    }

    [ClientRpc]
    public void InitializeDescriptionAndSpriteClientRpc(int itemId=-1, ClientRpcParams rpcParams = default)
    {
        BaseItem item;
        if (itemId == -1 && matchingItemId != -1)
            item = ItemDatabase.Instance.GetItem(matchingItemId);
        else
            item = ItemDatabase.Instance.GetItem(itemId);
        description = item.Description;
        objectSprite = item.Icon;
    }

    [ClientRpc]
    public void CheckSpriteChangeClientRpc()
    {
        //if (healthStateSprites.Count != 2) { return; }

        float healthPercentage = CurrentHealth.Value / MaxHealth.Value;

        int stateCount = healthStateSprites.Count;
        //a bit of a bandaid fix, but prevents any sprite change from happening above 75%
        if (healthPercentage < .75)
        {
            float fIndex = (1f - healthPercentage) * stateCount;
            int newState = Mathf.FloorToInt(fIndex);
            newState = Mathf.Clamp(newState, 0, stateCount - 1);

            if (newState != healthState)
            {
                ApplyHealthState(newState);
                healthState = newState;
            }
        }
        
    }

    private void ApplyHealthState(int i)
    {
        sr.sprite = healthStateSprites[i];
        //OnSpriteChanged?.Invoke(sr);
        ParticleService.Instance.PlayClientRpc(destroyParticleEffectType, particleSpawnPoint.position);
        //TODO here could do lighting changes
    }
}
