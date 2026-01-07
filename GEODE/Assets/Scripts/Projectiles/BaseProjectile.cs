using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public abstract class BaseProjectile : NetworkBehaviour
{
    [Header("Properties")]
    public float speed;
    [HideInInspector] public float damage;
    public float lifetime;
    public float rotationSpeed;
    public DamageType damageType;
    private ITracksHits parentTower;
    private bool persistent;
    [SerializeField] private Rigidbody2D rb;
    private List<GameObject> hitTargets = new List<GameObject>();


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Destroy(gameObject, lifetime);
    }

    //Methods
    public void Initialize(float damageAmount, Vector2 velocity, ITracksHits iHits=null, DamageType dmgType = DamageType.None, bool persistent=false)
    {
        if(!IsServer) return;
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        damage = damageAmount;
        rb.linearVelocity = velocity * speed;
        rb.angularVelocity = rotationSpeed;
        parentTower = iHits;
        damageType = dmgType;
        this.persistent = persistent;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!IsServer) return;
        if(hitTargets.Contains(collision.gameObject)) return;
        if(collision.gameObject.CompareTag("Enemy"))
        {
            IDamageable dmg = collision.gameObject.GetComponentInParent<IDamageable>();
            if(dmg == null) return;
            if(dmg.ApplyDamage(new DamageInfo(damage, transform.position, drops:true, dmgType:damageType)))
            {
                parentTower.KilledSomething(dmg);
            }
            parentTower.HitSomething(dmg);
            hitTargets.Add(collision.gameObject);

            if(!persistent)
                Destroy(gameObject);
        }
        
    }

    //Adding this so that projectiles that stay inside enemies (like slow projectiles) can still deal damage 
    private void OnTriggerStay2D(Collider2D collision)
    {
        if(!IsServer) return;
        if(hitTargets.Contains(collision.gameObject)) return;
        if(collision.gameObject.CompareTag("Enemy"))
        {
            IDamageable dmg = collision.gameObject.GetComponentInParent<IDamageable>();
            if(dmg == null) return;
            if(dmg.ApplyDamage(new DamageInfo(damage, transform.position, drops:true, dmgType:damageType)))
            {
                parentTower.KilledSomething(dmg);
            }
            parentTower.HitSomething(dmg);
            hitTargets.Add(collision.gameObject);

            if(!persistent)
                Destroy(gameObject);
        }
    }
}
