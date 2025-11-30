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
    [SerializeField] private SoundId healthStateChangeSoundId;


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
    [HideInInspector] public NetworkVariable<int> matchingItemId = new NetworkVariable<int>(-1);
    private int healthState = -1;
    private ulong lastAttackerClientId;
    //* ------------------- Events ----------------
    public event Action<IDamageable> OnDeath;

    //METHODS

    protected virtual void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        matchingItemId.OnValueChanged += HandleIDChanged;

        //If the ID is already set (this should be the catch for late joining clients)
        if (matchingItemId.Value != -1)
        {
            HandleIDChanged(-1, matchingItemId.Value);
        }
    }

    private void HandleIDChanged(int previousValue, int newValue)
    {
        if(newValue != -1)
        {
            BaseItem baseItem = ItemDatabase.Instance.GetItem(newValue);
            description = baseItem.Description;
            objectSprite = baseItem.Icon;
        }

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            // Initialize health directly on server instead of using ServerRpc
            // This ensures health is set immediately before any other systems interact with the object
            MaxHealth.Value = BASE_HEALTH;
            CurrentHealth.Value = MaxHealth.Value;
        }

        
    }



    public void InitializeItemId(int id)
    {
        matchingItemId.Value = id;
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
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(DamageInfo info, ServerRpcParams rpcParams = default)
    {
        if (!IsServer)
        {
            return;
        }
        lastAttackerClientId = rpcParams.Receive.SenderClientId;
        ApplyDamage(info, rpcParams);
    }

    public void ApplyDamage(DamageInfo info, ServerRpcParams rpcParams = default)
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

        if (CurrentHealth.Value <= 0)
        {
            DestroyThisServerRpc(info.dropItems);
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

    public void OnTakeDamage(float amount, Vector2 sourceDirection, ToolType tool=ToolType.None)
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
            AudioManager.Instance.PlayClientRpc(hitSoundId, CenterPoint.position);
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
        }

    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void DestroyThisServerRpc(bool dropItems)
    {
        if (!IsServer)
        {
            return;
        }
        DestroyThisInternal(dropItems);
    }

    protected virtual void DestroyThisInternal(bool dropItems)
    {
        if (!IsServer)
        {
            return;
        }
        if(NetworkManager.Singleton.ConnectedClients.TryGetValue(lastAttackerClientId, out var client))
        {
            client.PlayerObject.GetComponent<PlayerHealthAndXP>().AddXp(this);
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
    public void CheckSpriteChangeClientRpc()
    {
        //if (healthStateSprites.Count != 2) { return; }

        float healthPercentage = CurrentHealth.Value / MaxHealth.Value;

        int stateCount = healthStateSprites.Count;
        //a bit of a bandaid fix, but prevents any sprite change from happening above 75%

        float fIndex = (1f - healthPercentage) * stateCount;
        int newState = Mathf.FloorToInt(fIndex);
        newState = Mathf.Clamp(newState, 0, stateCount - 1);

        if (newState != healthState)
        {
            ApplyHealthState(newState);
            healthState = newState;
        }
        
        
    }

    private void ApplyHealthState(int i)
    {
        sr.sprite = healthStateSprites[i];
        //OnSpriteChanged?.Invoke(sr);
        if(healthStateChangeSoundId != SoundId.NONE)
            AudioManager.Instance.PlayClientRpc(healthStateChangeSoundId, transform.position);
        ParticleService.Instance.PlayClientRpc(destroyParticleEffectType, particleSpawnPoint.position);
        //TODO here could do lighting changes
    }
}
