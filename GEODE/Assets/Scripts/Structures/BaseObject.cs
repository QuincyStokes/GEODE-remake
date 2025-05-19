using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class BaseObject : NetworkBehaviour, IDamageable
{
    [Header("Properties")]
    [SerializeField] public float BASE_HEALTH;
    [SerializeField]
    public NetworkVariable<float> MaxHealth { get; set; } = new NetworkVariable<float>(1);
    public NetworkVariable<float> CurrentHealth { get; set; } = new NetworkVariable<float>(1);

    [SerializeField] private List<DroppedItem> droppedItems;
    public List<DroppedItem> DroppedItems 
    { 
        get => droppedItems; 
    }
    [SerializeField] private string objectName;
    public string ObjectName
    { 
        get => objectName; set => objectName = value;
    }

    [SerializeField] private Transform centerPoint;
    public Transform CenterPoint
    {
        get => centerPoint;
    }
    
    [HideInInspector] public string description;
    [HideInInspector] public Sprite objectSprite;

    [SerializeField]private SpriteRenderer sr;

    public Transform ObjectTransform 
    { 
        get => transform;
    }

    //METHODS

    private void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();   
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(IsServer)
        {   
            InitializeHealthServerRpc();
        }
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
        if(!IsServer)
        {
            return;
        }
        CurrentHealth.Value += amount;
        if(CurrentHealth.Value > MaxHealth.Value)
        {
            CurrentHealth.Value = MaxHealth.Value;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float amount,  Vector2 sourceDirection, bool dropItems=false)
    {
        if (!IsServer)
        {
            return;
        }

        ApplyDamage(amount, sourceDirection, dropItems);
    }

    public void ApplyDamage(float amount, Vector2 sourceDirection, bool dropItems = false)
    {
        if(!IsServer)
        {
            return;
        }
        if(amount <= 0)
        {
            return;
        }
        
        
        DisplayDamageFloaterClientRpc(amount);
        Debug.Log($"{name} took {amount} damage");
        CurrentHealth.Value -= amount;
        OnTakeDamage(amount, sourceDirection);
        if(CurrentHealth.Value <= 0)
        {
            DestroyThis(dropItems);
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

    public void OnTakeDamage(float amount, Vector2 sourceDirection)
    {
        OnDamageColorChangeClientRpc();
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
        if(DroppedItems != null && LootManager.Instance != null)
        {
            foreach(DroppedItem item in droppedItems)
            {   
                //If the item has something other than 100% drop chance
                if(item.chance < 100)
                {
                    //roll the dice to see if we should spawn this item
                    float rolledChance = Random.Range(0f, 100f);
                    if(rolledChance <= item.chance)
                    {
                        LootManager.Instance.SpawnLootServerRpc(transform.position, item.Id, Random.Range(item.minAmount, item.maxAmount+1));
                    }
                }
                else
                {
                    LootManager.Instance.SpawnLootServerRpc(transform.position, item.Id, Random.Range(item.minAmount, item.maxAmount+1));
                }
                
                
            }
        }
        else
        {
            Debug.Log($"Tried to drop items | LootManager null?: {LootManager.Instance == null}, DroppedItems null? : {DroppedItems == null} ");
        }

    }

    public void DestroyThis(bool dropItems)
    {
        if(!IsServer)
        {
            return;
        }
        if(dropItems)
        {
           DropItems();
        }
        ChunkManager.Instance.DeregisterObject(gameObject);
        GetComponent<NetworkObject>().Despawn(true);
    }

    [ClientRpc]
    public void InitializeDescriptionAndSpriteClientRpc(int itemId, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"Trying to load Desc + Sprite for {itemId}");//ah
        BaseItem item = ItemDatabase.Instance.GetItem(itemId);
        description = item.Description;
        objectSprite = item.Icon;
    }
}
