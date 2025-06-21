using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Collections;
using System;

public class PlayerHealthAndXP : NetworkBehaviour, IDamageable, IExperienceGain
{
    //* --------------- Player Health ---------------- */
    [Header("Health")]
    public NetworkVariable<float> MaxHealth { get; set; } = new NetworkVariable<float>(100);
    public NetworkVariable<float> CurrentHealth { get; set; } = new NetworkVariable<float>(100);
    [Tooltip("Time in seconds betweeen each health regen tick.")]
    [SerializeField] private float regenRate;
    [Tooltip("Delay in seconds for regen to start after taking damage")]
    [SerializeField] private float regenCooldown;


    //* --------------- XP ---------------- */
    [Header("XP")]
    [SerializeField] private int maxLevelXp;
    [SerializeField] private int currentLevelXp;
    [SerializeField] private int totalXp;
    [SerializeField] private int level;
    [SerializeField] private int droppedXp;

    public int MaximumLevelXp { get => maxLevelXp; set => maxLevelXp = value; }
    public int CurrentXp { get => currentLevelXp; set => currentLevelXp = value; }
    public int CurrentTotalXp { get => totalXp; set => totalXp = value; }
    public int Level { get => level; set => level = value; }

    //* --------------- Dropped Items---------------- */
    [Header("Settings")]
    [SerializeField] private List<DroppedItem> droppedItems;
    public List<DroppedItem> DroppedItems { get => droppedItems; }
    public int deathTimer;


    //* --------------- References ---------------- */
    
    [Header("References")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D collisionHitbox;
    [SerializeField] private Transform objectTransform;
    [SerializeField] private Transform particleSpawnPoint;

    [SerializeField] private EffectType hitParticleEffectType;
    public EffectType HitParticleEffectType { get => hitParticleEffectType; }
    public Transform ObjectTransform { get => objectTransform; }
    public Transform ParticleSpawnPoint { get => particleSpawnPoint; }
    [SerializeField] private Transform centerPoint;
    public Transform CenterPoint { get => centerPoint; }
    public Collider2D CollisionHitbox { get => collisionHitbox; }
    public int DroppedXP { get => droppedXp; }
    public PlayerController playerController;
    private bool invulnerable = false;
    private Coroutine regenCoroutine;

    //* --------------- Events ---------------- */

    public event Action OnDamageTaken;
    public event Action<IDamageable> OnDeath;
    public event Action OnRevive;
    public event Action OnXpGain;
    public event Action OnPlayerLevelUp;

    //* -------------- Internal ----------------- */
    private float timeSinceDamaged;
    private bool isRegenning;

    private void Update()
    {
        if (!IsServer) return;

        timeSinceDamaged += Time.deltaTime;
        if (timeSinceDamaged > regenCooldown && !isRegenning)
        {
            regenCoroutine = StartCoroutine(RegenHealth());
        }


    }

    public void DestroyThis(bool dropItems)
    {

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
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

    public void DropItems()
    {
        foreach (DroppedItem item in DroppedItems)
        {
            //If the item has something other than 100% drop chance
            if (item.chance < 100)
            {
                //roll the dice to see if we should spawn this item
                float rolledChance = UnityEngine.Random.Range(0f, 100f);
                if (rolledChance <= item.chance)
                {
                    LootManager.Instance.SpawnLootServerRpc(transform.position, item.Id, UnityEngine.Random.Range(item.minAmount, item.maxAmount + 1));
                }
            }
            else
            {
                LootManager.Instance.SpawnLootServerRpc(transform.position, item.Id, UnityEngine.Random.Range(item.minAmount, item.maxAmount + 1));
            }


        }
    }

    public void OnTakeDamage(float amount, Vector2 sourceDirection)
    {
        DisplayDamageFloaterClientRpc(amount);
        OnDamageColorChangeClientRpc();
        StartCoroutine(DoInvulnerableFrame());
        timeSinceDamaged = 0f;
        OnDamageTaken?.Invoke();
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }
        isRegenning = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RestoreHealthServerRpc(float amount)
    {
        CurrentHealth.Value += amount;
        if (CurrentHealth.Value > MaxHealth.Value)
        {
            CurrentHealth.Value = MaxHealth.Value;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(DamageInfo info)
    {
        if (!IsServer)
        {
            return;
        }
        if (!invulnerable)
        {
            ApplyDamage(info);
        }

    }

    public void ApplyDamage(DamageInfo info)
    {
        CurrentHealth.Value -= info.amount;
        OnTakeDamage(info.amount, info.sourceDirection);
        if (CurrentHealth.Value <= 0)
        {
            //not sure how to handle player death yet, current thought is similar to terraria?
            //drop some portion of items or lose xp? or maybe nothing
            //wait x amount of time, respawn at core
            PlayerDeath();
        }
    }

    private void PlayerDeath()
    {
        Debug.Log($"[SERVER] PlayerDeath() called for OwnerClientId={OwnerClientId}, CurrentHealth={CurrentHealth.Value}");
        StopAllCoroutines();
        StartCoroutine(DoPlayerDeath());
    }

   
    private IEnumerator DoPlayerDeath()
    {
        NotifyDeathClientRpc();
        playerController.movementLocked = true;
        invulnerable = true;
        yield return new WaitForSeconds(deathTimer);
        Vector3 spawnPos = Vector3.zero;
        if (FlowFieldManager.Instance.coreTransform == null)
        {
            spawnPos = new Vector3Int(WorldGenManager.Instance.WorldSizeX / 2, WorldGenManager.Instance.WorldSizeY / 2, 0);
        }
        else
        {
            spawnPos = FlowFieldManager.Instance.coreTransform.position;
        }
        transform.position = spawnPos;
        TeleportOwnerClientRpc(spawnPos);
        CurrentHealth.Value = MaxHealth.Value;

        playerController.movementLocked = false;
        
        
        invulnerable = false;
        NotifyReviveClientRpc();
    }

     [ClientRpc]
    private void NotifyDeathClientRpc()
    {
        OnDeath?.Invoke(this);
        if (sr != null) 
            sr.color = new Color(1, 1, 1, 0);

        // Disable collision so the “dead” player isn’t blocking or getting hit
        if (collisionHitbox != null)
            collisionHitbox.enabled = false;

        // Lock this client’s movement so FixedUpdate() can no longer move the Rigidbody
        if (playerController != null)
            playerController.movementLocked = true;
    }

    [ClientRpc]
    private void NotifyReviveClientRpc()
    {
         OnRevive?.Invoke();
        if (sr != null) 
            sr.color = new Color(1, 1, 1, 1);

        if (collisionHitbox != null)
            collisionHitbox.enabled = true;

        if (playerController != null)
            playerController.movementLocked = false;
    }

    [ClientRpc]
    private void TeleportOwnerClientRpc(Vector3 spawnPos, ClientRpcParams p = default)
    {
        transform.position = spawnPos;
    }

    private IEnumerator DoInvulnerableFrame(float time = 1f)
    {
        invulnerable = true;
        yield return new WaitForSeconds(time);
        invulnerable = false;
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


    public void OnLevelUp()
    {
        OnPlayerLevelUp?.Invoke();
    }

    public void AddXp(IDamageable damageable)
    {

        CurrentXp += damageable.DroppedXP;
        CurrentTotalXp += damageable.DroppedXP;

        CheckLevelUp();
        OnXpGain?.Invoke();
        //maybe in the future this can be a coroutine that does it slowly for cool effect
    }

    public void AddXp(int amount)
    {
        CurrentXp += amount;
        CurrentTotalXp += amount;

        CheckLevelUp();
        OnXpGain?.Invoke();
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

    private IEnumerator RegenHealth()
    {
        isRegenning = true;
        while (CurrentHealth.Value < MaxHealth.Value)
        {
            CurrentHealth.Value = Mathf.Min(CurrentHealth.Value + 1, MaxHealth.Value);
            yield return new WaitForSeconds(regenRate);
        }
        isRegenning = false;
        regenCoroutine = null;
    }

   
}