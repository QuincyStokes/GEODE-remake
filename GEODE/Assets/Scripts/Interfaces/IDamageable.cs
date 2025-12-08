using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public interface IDamageable 
{
   public NetworkVariable<float> MaxHealth {get; set;}
   public NetworkVariable<float> CurrentHealth { get; set; }
   public Transform ObjectTransform { get; }
   public Transform CenterPoint { get; }
   public Transform ParticleSpawnPoint { get; }
   public Collider2D CollisionHitbox { get; }
   public List<DroppedItem> DroppedItems { get; }
   public EffectType HitParticleEffectType { get; }
   public int DroppedXP { get; }
   public event Action<IDamageable> OnDeath;
   public abstract void TakeDamageServerRpc(DamageInfo info, ServerRpcParams rpcParams = default);
   public abstract bool ApplyDamage(DamageInfo info, ServerRpcParams rpcParams = default); //returns true if the object is killed
   public abstract void RestoreHealthServerRpc(float amount);
   public abstract void DestroyThisServerRpc(bool dropItems);
   public abstract void DropItems();
   public abstract void DisplayDamageFloaterClientRpc(float amount);
   public abstract void OnDamageColorChangeClientRpc();
   public abstract void OnTakeDamage(float amount, Vector2 source, ToolType tool);

}
