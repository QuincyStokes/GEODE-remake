using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

public abstract class BaseEnemy : NetworkBehaviour, IDamageable
{
    ///state machine controlled enemy base class
    ///
    
    #region PROPERTIES
    [Header("ID")]
    public int Id;

    [Header("Enemy Settings")]
    private float maxHealth;
    private float currentHealth;

    public float attackDamage;
    public float attackRange;
    public float attackCooldown;
    public float movementSpeed;
    [SerializeField] private List<DroppedItem> droppedItems = new List<DroppedItem>();
    public LayerMask structureLayerMask;
    public EnemySteering steering;
    private Transform objectTransform;
    private string objectName;
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

    #endregion
    [Header("References")]
    public Animator animator;
    public Rigidbody2D rb;
    [HideInInspector] public Transform coreTransform;
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
            coreTransform = FlowFieldManager.Instance.coreTransform;
        }
        OnDeath += SetDeathState;
        PostStart();
    }

    public virtual void PostStart()
    {

    }

    private void Update()
    {
        stateMachine.CurrentState?.UpdateState(this, stateMachine);
    }

    private void FixedUpdate()
    {
        
        stateMachine.CurrentState?.FixedUpdateState(this, stateMachine);   
    }

    public void TakeDamage(float amount)
    {
        OnTakeDamage(amount);
        currentHealth -= amount;
        if(currentHealth <= 0)
        {
            OnDeath?.Invoke();
        }
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
        }
    }

    public virtual void Attack()
    {
        //we have currentTarget
    }

    public void OnTakeDamage(float amount)
    {
       
    }

    [ServerRpc(RequireOwnership = false)]
    public void DropItemsServerRpc()
    {
        foreach(DroppedItem droppedItem in DroppedItems)
        {
        //instantiate the loot with droppedItem.Id and droppedItem.amount
        LootManager.Instance.SpawnLootServerRpc(ObjectTransform.position, droppedItem.Id, droppedItem.amount);
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
    public void TakeDamageServerRpc(float amount, bool dropItems)
    {
        DisplayDamageFloaterClientRpc(amount);
        CurrentHealth -= amount;
        if(CurrentHealth <= 0)
        {
            DestroyThisServerRpc(dropItems);
        }
    }

    [ClientRpc]
    public void DisplayDamageFloaterClientRpc(float amount)
    {
        GameObject damageFloater = Instantiate(GameAssets.Instance.damageFloater, transform.position, Quaternion.identity);
        damageFloater.GetComponent<DamageFloater>().Initialize(amount);
    }

    [ServerRpc]
    public void DestroyThisServerRpc(bool dropItems)
    {
        DropItemsServerRpc(); // ERROR HERE
        GetComponent<NetworkObject>()?.Despawn();
    }
}
