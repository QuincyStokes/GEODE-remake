using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public interface IDamageable 
{
   public NetworkVariable<float> MaxHealth {get; set;}
   public NetworkVariable<float> CurrentHealth { get; set; }
   public Transform ObjectTransform { get; }
   public Transform CenterPoint { get; }
   public Collider2D CollisionHitbox { get; }
   public List<DroppedItem> DroppedItems { get; }
   public abstract void TakeDamageServerRpc(DamageInfo info);
   public abstract void ApplyDamage(DamageInfo info);
   public abstract void RestoreHealthServerRpc(float amount);
   public abstract void DestroyThis(bool dropItems);
   public abstract void DropItems();
   public abstract void DisplayDamageFloaterClientRpc(float amount);
   public abstract void OnDamageColorChangeClientRpc();
   

   public abstract void OnTakeDamage(float amount, Vector2 source);

}
