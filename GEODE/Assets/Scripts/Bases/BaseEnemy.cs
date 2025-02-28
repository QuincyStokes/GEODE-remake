using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public abstract class BaseEnemy : NetworkBehaviour
{
    ///state machine controlled enemy base class

    [Header("Enemy Settings")]
    public float maxHealth;
    public float attackDamage;
    public float attackRange;
    public float attackCooldown;
    public float movementSpeed;
    [SerializeField] private List<DroppedItem> droppedItems;
    public LayerMask structureLayerMask;
    public EnemySteering steering;


    [Header("References")]
    public Animator animator;
    public Rigidbody2D rb;
    [HideInInspector] public Transform coreTransform;
    [HideInInspector] public Transform playerTransform;
    [HideInInspector] public IDamageable currentTarget;
    


    //EVENTS
    public static event Action OnDeath;

    //INTERNAL
    private float currentHealth;
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
        
        if(FlowFieldManager.Instance != null)
        {
            FlowFieldManager.Instance.corePlaced += SetCorePosition;
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
        currentHealth -= amount;
        if(currentHealth <= 0)
        {
            OnDeath?.Invoke();
        }
    }

    public void DropItems()
    {
        foreach(DroppedItem droppedItem in droppedItems)
        {
            //instantiate the loot with droppedItem.Id and droppedItem.amount
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

}
