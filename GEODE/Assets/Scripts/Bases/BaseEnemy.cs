using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;

public abstract class BaseEnemy : NetworkBehaviour, IDamageable, IKnockbackable, IExperienceGain
{
    ///state machine controlled enemy base class
    ///

    [Header("ID")]
    public int Id;
    //*         ----------------------------- INSPECTOR REFERENCES ---------------------------

    [Header("References")]
    [SerializeField] private SpriteRenderer sr;
    public Transform CenterPoint { get; set; }
    [SerializeField] private Transform particleSpawnPoint;
    public Transform ParticleSpawnPoint { get => particleSpawnPoint; set => ParticleSpawnPoint = value; }

    [SerializeField] private EffectType hitParticleEffectType;
    public EffectType HitParticleEffectType { get => hitParticleEffectType; }
    public Collider2D CollisionHitbox { get => collisionHitbox; }
    public Transform ObjectTransform { get; set; }
    public EnemySteering steering;
    public Animator animator;
    public Rigidbody2D rb;
    public Collider2D collisionHitbox;
    public CircleCollider2D aggroRadius;


    //*         ----------------------------- PUBLIC PROPERTIES ---------------------------

    [Header("Enemy Stats")]
    public float attackDamage;
    public float attackRange;
    public float movementSpeed;
    [SerializeField] private int BASE_MAX_HEALTH;
    [SerializeField] private int BASE_XP_REQUIRED;

    [Header("Drops")]
    [SerializeField] private List<DroppedItem> DROPPED_ITEMS = new List<DroppedItem>();
    [SerializeField] private ToolType idealToolType;

    [Header("Movement Settings")]
    public LayerMask structureLayerMask;
    [SerializeField] private float knockbackDecay;

    [Header("Attack Settings")]
    public float attackWindupTime;
    public float attackRecoveryTime;
    public float attackCooldown;
    [Header("Idle Settings")]
    public float wanderRadius = 5f;
    public float wanderTimeMin = 2f;
    public float wanderTimeMax = 5f;
    public float aggroRange = 5f;

    public NetworkVariable<float> MaxHealth { get; set; } = new NetworkVariable<float>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> CurrentHealth { get; set; } = new NetworkVariable<float>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public List<DroppedItem> DroppedItems { get; set; }
    private EnemyStateMachine stateMachine;


    //*         ----------------------------- IExperienceGain ---------------------------
    public int MaximumLevelXp { set; get; }
    public int CurrentXp { set; get; }
    public int CurrentTotalXp { set; get; }
    public int Level { set; get; }


    //*         ----------------------------- EVENTS ---------------------------
    public event Action OnDeath;


    //*         ---------------------------- INTERNAL ----------------------------
    [HideInInspector] public Transform coreTransform;
    [HideInInspector] public Vector2 corePosition;
    [HideInInspector] public Transform playerTransform;
    [HideInInspector] public IDamageable currentTarget;
    [HideInInspector] public Vector2 targetClosestPoint;
    [HideInInspector] public Vector2 externalVelocity;





    //*----------------------------- METHODS ---------------------------
    private void Awake()
    {
        stateMachine = new EnemyStateMachine(this);
        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsServer)
        {
            return;
        }

        MaxHealth.Value = BASE_MAX_HEALTH;
        CurrentHealth.Value = MaxHealth.Value;
        MaximumLevelXp = BASE_XP_REQUIRED;
        ObjectTransform = transform;
        DroppedItems = DROPPED_ITEMS;
        aggroRadius.radius = aggroRange;
        if (DayCycleManager.Instance != null)
        {
            DayCycleManager.Instance.becameDay += NewDayStats;
        }
        if (FlowFieldManager.Instance != null && coreTransform == null && FlowFieldManager.Instance.coreTransform == null)
        {
            FlowFieldManager.Instance.corePlaced += SetCorePosition;
        }
        else if (FlowFieldManager.Instance != null && coreTransform == null && FlowFieldManager.Instance.coreTransform != null)
        {
            //coreTransform = FlowFieldManager.Instance.coreTransform;
            SetCorePosition(FlowFieldManager.Instance.coreTransform);
        }
        OnDeath += SetDeathState;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        DayCycleManager.Instance.becameDay -= NewDayStats;
        FlowFieldManager.Instance.corePlaced -= SetCorePosition;
        OnDeath -= SetDeathState;

    }
    private void Start()
    {
        PostStart();
    }

    public virtual void PostStart()
    {
        externalVelocity = Vector2.zero;
    }

    private void Update()
    {
        stateMachine.CurrentState?.UpdateState(this, stateMachine);
        if (rb.linearVelocity.x < 0)
        {
            sr.flipX = true;
        }
        else
        {
            sr.flipX = false;
        }
    }

    private void FixedUpdate()
    {

        stateMachine.CurrentState?.FixedUpdateState(this, stateMachine);
        externalVelocity = Vector2.Lerp(externalVelocity, Vector2.zero, knockbackDecay * Time.fixedDeltaTime);
    }

    private void SetDeathState()
    {
        stateMachine.ChangeState(new DeathState());
    }

    private void SetCorePosition(Transform coreTransform)
    {
        if (FlowFieldManager.Instance != null && FlowFieldManager.Instance.HasCoreBeenPlaced())
        {
            this.coreTransform = coreTransform;
            corePosition = (Vector2)coreTransform.position + new Vector2(1, 1);
        }
    }

    public abstract void Attack();

    public void OnTakeDamage(float amount, Vector2 source)
    {
        DisplayDamageFloaterClientRpc(amount);
        OnDamageColorChangeClientRpc();
        if (source != Vector2.zero)
        {
            Vector3 dir = (Vector2)transform.position - source;
            TakeKnockback(dir, Mathf.Clamp(amount, 1, 10));
        }

    }

    public void DropItems()
    {
        if (DroppedItems != null && LootManager.Instance != null)
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
                        LootManager.Instance.SpawnLootServerRpc(CenterPoint.position, item.Id, UnityEngine.Random.Range(item.minAmount, item.maxAmount + 1));
                    }
                }
                else
                {
                    LootManager.Instance.SpawnLootServerRpc(CenterPoint.position, item.Id, UnityEngine.Random.Range(item.minAmount, item.maxAmount + 1));
                }
            }
        }

    }

    [ServerRpc(RequireOwnership = false)]
    public void RestoreHealthServerRpc(float amount)
    {
        RestoreHealth(amount);
    }

    public void RestoreHealth(float amount)
    {
        if (!IsServer)
        {
            return;
        }
        CurrentHealth.Value += amount;
        if (CurrentHealth.Value > MaxHealth.Value)
        {
            CurrentHealth.Value = MaxHealth.Value;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(DamageInfo info)
    {
        ApplyDamage(info);
    }

    //this will be the code that *actually* applies damage to the enemy. The Server RPC is a wrapper for strange edge cases that would need it. 
    public void ApplyDamage(DamageInfo info)
    {
        if (!IsServer)
        {
            return;
        }
        
        if (info.tool == idealToolType || info.tool == ToolType.None)
        {
            CurrentHealth.Value -= info.amount;
            OnTakeDamage(info.amount, info.sourceDirection);
        }
        else
        {
            CurrentHealth.Value -= info.amount / 4;
            OnTakeDamage(info.amount / 4, info.sourceDirection);
        }


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

    public void DestroyThis(bool dropItems)
    {
        if (dropItems)
        {
            DropItems();
        }
        OnDeath?.Invoke();
        GetComponent<NetworkObject>()?.Despawn();
    }

    public void TakeKnockback(Vector2 direction, float force)
    {
        externalVelocity += direction.normalized * (Mathf.Log(force)/2);
    }

    public void NewDayStats()
    {
        AddXp(125);
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
        MaximumLevelXp = Mathf.RoundToInt(MaximumLevelXp * 1.1f);
        //need some way for this to interact with stats.. OnLevelUp()? then it's up to the base classes to figure out what they wanna do
        OnLevelUp();
    }

    public void SetLevel(int level)
    {
        Level = level;
    }

    public void OnXpGain()
    {

    }

    public void OnLevelUp()
    {
        attackDamage *= 1.1f;
        movementSpeed *= 1.04f;
        attackCooldown *= .97f;
        MaxHealth.Value *= 1.1f;
    }

    [HideInInspector] public bool canAggro;
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!canAggro) return;
        if (other.CompareTag("Player"))
        {
            playerTransform = other.transform;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player left aggro range.");
            playerTransform = null;
        }
    }
}
