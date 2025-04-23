using System;
using System.Collections.Generic;
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
            foreach(DroppedItem droppedItem in DroppedItems)
            {
            //instantiate the loot with droppedItem.Id and droppedItem.amount
                LootManager.Instance.SpawnLootServerRpc(ObjectTransform.position, droppedItem.Id, droppedItem.amount);
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
