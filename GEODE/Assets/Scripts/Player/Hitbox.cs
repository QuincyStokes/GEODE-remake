using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Hitbox : MonoBehaviour
{
    public ToolType tool;
    public float damage;
    public Vector2 sourceDirection;
    public bool dropItems;
    [SerializeField] private List<string> hittableTags;
    public ITracksHits parentTracker;

    //really should have an InitializeHitbox function or something
    private void OnTriggerEnter2D(Collider2D collision)
    {
        IDamageable damageable = collision.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            foreach (string tag in hittableTags)
            {
                Debug.Log($"Comparing object {tag} ?= {collision.tag}");
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
}


public struct DamageInfo : INetworkSerializable, IEquatable<DamageInfo>
{
    public int amount;
    public ToolType tool;
    public Vector2 sourceDirection;
    public bool dropItems;
    public DamageInfo(float amt, Vector2 srcDir, ToolType t = ToolType.None, bool drops = false)
    {
        amount = Mathf.RoundToInt(amt);
        tool = t;
        sourceDirection = srcDir;
        dropItems = drops;

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
