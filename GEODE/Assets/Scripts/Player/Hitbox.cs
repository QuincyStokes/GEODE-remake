using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Hitbox : NetworkBehaviour
{
    public ToolType tool;
    public float damage;
    public Vector2 sourceDirection;
    public bool dropItems;
    [SerializeField] private List<string> hittableTags;
    public ITracksHits parentTracker;

    [Header("References")]
    [SerializeField] private Collider2D hitCollider;
    [SerializeField] private SpriteRenderer spriteRenderer;

    //really should have an InitializeHitbox function or something
    private void OnTriggerEnter2D(Collider2D collision)
    {
        IDamageable damageable = collision.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            foreach (string tag in hittableTags)
            {
                if (collision.CompareTag(tag))
                {
                    if (tool != ToolType.Hammer)
                    {
                        if (parentTracker != null)
                        {
                            parentTracker.HitSomething(damageable);
                        }
                        damageable.TakeDamageServerRpc(new DamageInfo(damage, sourceDirection, tool, dropItems));
                        return;
                    }
                    else
                    {
                        damageable.RestoreHealthServerRpc(damage);
                    }

                }
            }
        }
    }

    public void EnableCollider()
    {
        hitCollider.enabled = true;
    }

    public void DisableCollider()
    {
        hitCollider.enabled = false;
    }

    public void EnableVisuals()
    {
        spriteRenderer.enabled = true;
    }

    public void DisableVisuals()
    {
        spriteRenderer.enabled = false;
    }


}


public struct DamageInfo : INetworkSerializable, IEquatable<DamageInfo>
{
    public int amount;
    public ToolType tool;
    public Vector2 sourceDirection;
    public bool dropItems;
    public DamageType damageType;
    public DamageInfo(float amt, Vector2 srcDir, ToolType t = ToolType.None, bool drops = false, DamageType dmgType=DamageType.None)
    {
        amount = Mathf.RoundToInt(amt);
        tool = t;
        sourceDirection = srcDir;
        dropItems = drops;
        damageType = dmgType;
    }

    // -- Netcode --
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref amount);
        serializer.SerializeValue(ref tool);
        serializer.SerializeValue(ref sourceDirection);
        serializer.SerializeValue(ref dropItems);
    }

    // -- Equator --
    public bool Equals(DamageInfo other)
    {
        return 
            amount == other.amount
        &&  tool == other.tool
        &&  sourceDirection == other.sourceDirection
        &&  dropItems == other.dropItems
        ;
    }
}
