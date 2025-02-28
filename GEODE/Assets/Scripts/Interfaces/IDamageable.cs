using UnityEngine;

public interface IDamageable 
{
   public float MaxHealth {get; set;}
   public float CurrentHealth {get; set;}
   public Transform objectTransform {get;}
   public void TakeDamageServerRpc(float amount, bool dropItems);
   public void RestoreHealthServerRpc(float amount);
   public void DestroyThis(bool dropItems);
}
