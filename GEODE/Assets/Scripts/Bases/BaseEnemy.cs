using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public abstract class BaseEnemy : NetworkBehaviour
{
    ///state machine controlled enemy base class

     
    private EnemyStateMachine stateMachine;

    [HideInInspector] public Transform coreTransform;
    [HideInInspector] public Transform playerTransform;

    [Header("Enemy Settings")]
    [SerializeField] private float maxHealth;
   
    [SerializeField] private float attackDamage;
    [SerializeField] private float attackRange;
    public float movementSpeed;
    [SerializeField] private List<DroppedItem> droppedItems;
    public LayerMask structureLayerMask;


    private float currentHealth;

    [SerializeField] struct DroppedItem
    {
        int itemId;
        int amount;
    }

    [Header("References")]
    public Animator animator;
    public Rigidbody2D rb;


    //EVENTS
    public static event Action OnDeath;

    private void Awake()
    {
        stateMachine = new EnemyStateMachine(this);
        
    }
    private void Start()
    {
        if(FlowFieldManager.Instance != null)
        {
            FlowFieldManager.Instance.corePlaced += SetCorePosition;
        }
        OnDeath += SetDeathState;
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

}
