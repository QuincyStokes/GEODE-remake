using System.Collections.Generic;
using UnityEngine;

public interface IDamageable 
{
   public float MaxHealth {get; set;}
   public float CurrentHealth {get; set;}
   public Transform ObjectTransform {get;}
   public string ObjectName {get; set;}
   public List<DroppedItem> DroppedItems{get;}
   public abstract void TakeDamageServerRpc(float amount, bool dropItems);
   public abstract void RestoreHealthServerRpc(float amount);
   public abstract void DestroyThisServerRpc(bool dropItems);
   public abstract void DropItemsServerRpc();
   public abstract void OnTakeDamage(float amount);

}
