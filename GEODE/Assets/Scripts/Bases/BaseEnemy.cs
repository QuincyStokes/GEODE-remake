using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

public abstract class BaseEnemy : NetworkBehaviour, IDamageable, IKnockbackable
{
    ///state machine controlled enemy base class
    ///
    
    #region PROPERTIES
    [Header("ID")]
    public int Id;

    [Header("References")]
    [SerializeField] private SpriteRenderer sr;

    [Header("Enemy Settings")]
    [SerializeField]private float maxHealth;
    [SerializeField]private float currentHealth;

    public float attackDamage;
    public float attackRange;
    public float attackCooldown;
    public float movementSpeed;
    [SerializeField] private List<DroppedItem> droppedItems = new List<DroppedItem>();
    public LayerMask structureLayerMask;
    public EnemySteering steering;
    private Transform objectTransform;
    private string objectName;
    public Vector2 externalVelocity;
    [SerializeField] private float knockbackDecay;
    #endregion
    #region ACCESSORS
    public float MaxHealth
    {
        get => maxHealth;
        set => maxHealth = value;
    }

    public float CurrentHealth
    {
        get => currentHealth;
        set => currentHealth = value;
    }

    public Transform ObjectTransform
    {
        get => objectTransform;
    }

    public string ObjectName
    {
        get => objectName;
        set => objectName = value;
    }
    public List<DroppedItem> DroppedItems
    {
        get => droppedItems;
    }

    [SerializeField] private Transform centerPoint;
    public Transform CenterPoint
    {
        get => centerPoint;
    }

    #endregion
    [Header("References")]
    public Animator animator;
    public Rigidbody2D rb;
    [HideInInspector] public Transform coreTransform;
    [HideInInspector] public Vector2 corePosition;
    [HideInInspector] public Transform playerTransform;
    [HideInInspector] public IDamageable currentTarget;
    


    //EVENTS
    public static event Action OnDeath;

    //INTERNAL
    private EnemyStateMachine stateMachine;

    private void Awake()
    {
        stateMachine = new EnemyStateMachine(this);
        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if(!IsServer)
        {
            this.enabled = false;
        }
    }
    private void Start()
    {
        if(FlowFieldManager.Instance != null && coreTransform == null && FlowFieldManager.Instance.coreTransform == null)
        {
            FlowFieldManager.Instance.corePlaced += SetCorePosition;
        }
        else if (FlowFieldManager.Instance != null && coreTransform == null && FlowFieldManager.Instance.coreTransform != null)
        {
            //coreTransform = FlowFieldManager.Instance.coreTransform;
            SetCorePosition(FlowFieldManager.Instance.coreTransform);
        }
        OnDeath += SetDeathState;
        PostStart();
    }

    public virtual void PostStart()
    {
        externalVelocity = Vector2.zero;
    }

    private void Update()
    {
        stateMachine.CurrentState?.UpdateState(this, stateMachine);
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
        if(FlowFieldManager.Instance != null && FlowFieldManager.Instance.HasCoreBeenPlaced())
        {
            this.coreTransform = coreTransform;
            corePosition = (Vector2)coreTransform.position + new Vector2(1, 1);
        }
    }

    public virtual void Attack()
    {
        //we have currentTarget
    }

    public void OnTakeDamage(float amount, Vector2 source)
    {
        DisplayDamageFloaterClientRpc(amount);
        OnDamageColorChangeClientRpc();
        if(source!=Vector2.zero)
        {
            Vector3 dir = (Vector2)transform.position - source;
            TakeKnockbackServerRpc(dir, amount);
        }

    }

    [ServerRpc(RequireOwnership = false)]
    public void DropItemsServerRpc()
    {
        if(DroppedItems != null && LootManager.Instance != null)
        {
            foreach(DroppedItem item in DroppedItems)
            {   
            //If the item has something other than 100% drop chance
                if(item.chance < 100)
                {
                    //roll the dice to see if we should spawn this item
                    float rolledChance = UnityEngine.Random.Range(0f, 100f);
                    if(rolledChance <= item.chance)
                    {
                        LootManager.Instance.SpawnLootServerRpc(transform.position, item.Id, UnityEngine.Random.Range(item.minAmount, item.maxAmount+1));
                    }
                }
                else
                {
                    LootManager.Instance.SpawnLootServerRpc(transform.position, item.Id, UnityEngine.Random.Range(item.minAmount, item.maxAmount+1));
                }
            }
        }
        
    }

    [ServerRpc(RequireOwnership = false)]
    public void RestoreHealthServerRpc(float amount)
    {
        CurrentHealth += amount;
        if(CurrentHealth > MaxHealth)
        {
            CurrentHealth = MaxHealth;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float amount, Vector2 sourceDirection, bool dropItems=false)
    {
        CurrentHealth -= amount;
        OnTakeDamage(amount, sourceDirection);
        if(CurrentHealth <= 0)
        {
            DestroyThisServerRpc(dropItems);
        }
    }

    [ClientRpc]
    public void DisplayDamageFloaterClientRpc(float amount)
    {
        GameObject damageFloater;
        if(CenterPoint != null){
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

    [ServerRpc]
    public void DestroyThisServerRpc(bool dropItems)
    {
        DropItemsServerRpc(); // ERROR HERE
        OnDeath?.Invoke();
        GetComponent<NetworkObject>()?.Despawn();
    }

    [ServerRpc]
    public void TakeKnockbackServerRpc(Vector2 direction, float force)
    {
        externalVelocity += direction.normalized * Mathf.Log(force);
    }
}
