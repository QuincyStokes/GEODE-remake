using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;


public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth;
    [SerializeField] private float currentHealth;
    [SerializeField] private Transform objectTransform;
    [SerializeField] private string objectName;
    [SerializeField] private List<DroppedItem> droppedItems;


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
    public void DestroyThisServerRpc(bool dropItems)
    {
        //not sure if this'll ever be called, have to look into the player "dying", my guess is we don't actually want to destroy it
    }

    public void DisplayDamageFloaterClientRpc(float amount)
    {
        GameObject damageFloater = Instantiate(GameAssets.Instance.damageFloater, transform.position, Quaternion.identity);
        damageFloater.GetComponent<DamageFloater>().Initialize(amount);
    }

    public void DropItemsServerRpc()
    {
        foreach(DroppedItem droppedItem in DroppedItems)
        {
            //instantiate the loot with droppedItem.Id and droppedItem.amount
            LootManager.Instance.SpawnLootServerRpc(ObjectTransform.position, droppedItem.Id, droppedItem.amount);
        }
    }

    public void OnTakeDamage(float amount)
    {
        DisplayDamageFloaterClientRpc(amount);
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
        CurrentHealth -= amount;
        OnTakeDamage(amount);
        if(CurrentHealth <= 0)
        {
            //not sure how to handle player death yet, current thought is similar to terraria?
            //drop some portion of items or lose xp? or maybe nothing
            //wait x amount of time, respawn at core
        }
    }
}