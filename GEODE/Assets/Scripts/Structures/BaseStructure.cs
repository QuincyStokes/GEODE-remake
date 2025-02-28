using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class BaseStructure : NetworkBehaviour, IDamageable
{
    [Header("Properties")]
    [SerializeField] private NetworkVariable<float> maxHealth = new NetworkVariable<float>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    // [SerializeField] private NetworkVariable<string> structureName = new NetworkVariable<string>(
    //     "NO_NAME",
    //     NetworkVariableReadPermission.Everyone,
    //     NetworkVariableWritePermission.Server
    // );

    [SerializeField] private NetworkVariable<float> currentHealth = new NetworkVariable<float>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    [SerializeField] private List<DroppedItem> droppedItems;

    public float MaxHealth { get => maxHealth.Value; set => maxHealth.Value = value; }
    public float CurrentHealth { get => currentHealth.Value; set => currentHealth.Value = value; }
    Transform IDamageable.objectTransform { get => transform;}

    public void DestroyThis(bool dropItems)
    {
        if(!IsServer)
        {
            return;
        }
        if(dropItems)
        {
            foreach(DroppedItem item in droppedItems)
            {   
                LootManager.Instance.SpawnLootServerRpc(transform.position, item.Id, item.amount);
            }
        }
        
        GetComponent<NetworkObject>().Despawn(true);
    }

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
            DestroyThis(dropItems);
        }

    }

    protected virtual void OnTakeDamage(float amount)
    {
        //
    }

}
