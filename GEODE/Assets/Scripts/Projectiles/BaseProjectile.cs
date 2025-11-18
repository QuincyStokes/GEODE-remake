using Unity.Netcode;
using UnityEngine;

public abstract class BaseProjectile : NetworkBehaviour
{
    [Header("Properties")]
    public float speed;
    [HideInInspector] public float damage;
    public float lifetime;
    public float rotationSpeed;
    private ITracksHits parentTower;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D col;


    //Methods
    public void Initialize(float damageAmount, Vector2 velocity, ITracksHits iHits=null)
    {
        if(!IsServer) return;
        damage = damageAmount;
        rb.linearVelocity = velocity * speed;
        rb.angularVelocity = rotationSpeed;
        parentTower = iHits;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(!IsServer) return;
        if(collision.gameObject.CompareTag("Enemy"))
        {
            IDamageable dmg = collision.gameObject.GetComponentInParent<IDamageable>();
            dmg?.ApplyDamage(new DamageInfo(damage, transform.position, drops:true));
            if (dmg != null)
            {
                parentTower.HitSomething(dmg);
            }
            Destroy(gameObject);
        }
        
    }
}
