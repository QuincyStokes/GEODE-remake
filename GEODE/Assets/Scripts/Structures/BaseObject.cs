using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class BaseObject : NetworkBehaviour, IDamageable
{
    [Header("Properties")]
    [SerializeField] private NetworkVariable<float> maxHealth = new NetworkVariable<float>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public float MaxHealth 
    { 
        get => maxHealth.Value; 
        set => maxHealth.Value = value; 
    }
    
    [SerializeField] private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public float CurrentHealth 
    { 
        get => currentHealth.Value; 
        set => currentHealth.Value = value; 
    }
    [SerializeField] private List<DroppedItem> droppedItems;
    public List<DroppedItem> DroppedItems 
    { 
        get => droppedItems; 
    }
    [SerializeField] private string objectName;
    string IDamageable.ObjectName
    { 
        get => objectName; set => objectName = value;
    }
    

    Transform IDamageable.ObjectTransform 
    { 
        get => transform;
    }
    

    //METHODS

   

   

    [ServerRpc(RequireOwnership = false)]
    public void RestoreHealthServerRpc(float amount)
    {
        if(!IsServer)
        {
            return;
        }
        currentHealth.Value += amount;
        if(currentHealth.Value > maxHealth.Value)
        {
            currentHealth.Value = maxHealth.Value;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float amount, bool dropItems=false)
    {
        if(!IsServer)
        {
            return;
        }
        if(amount <= 0)
        {
            return;
        }
        Debug.Log($"{name} took {amount} damage");
        currentHealth.Value -= amount;
        OnTakeDamage(amount);
        if(currentHealth.Value <= 0)
        {
            DestroyThisServerRpc(dropItems);
        }

    }

    public void OnTakeDamage(float amount)
    {
        //
    }

    [ServerRpc]
    public void DropItemsServerRpc()
    {
        foreach(DroppedItem item in droppedItems)
        {   
            LootManager.Instance.SpawnLootServerRpc(transform.position, item.Id, item.amount);
        }
    }

    [ServerRpc]
    public void DestroyThisServerRpc(bool dropItems)
    {
        if(!IsServer)
        {
            return;
        }
        if(dropItems)
        {
           DropItemsServerRpc();
        }
        
        GetComponent<NetworkObject>().Despawn(true);
    }
}
