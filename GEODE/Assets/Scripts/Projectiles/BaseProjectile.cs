using UnityEngine;

public abstract class BaseProjectile : MonoBehaviour
{
    [Header("Properties")]
    public float speed;
    public float damage;
    public float lifetime;
    [SerializeField] private Rigidbody2D rb;


    //Methods
    public void Initialize(float damageAmount, Vector2 velocity)
    {
        damage = damageAmount;
        rb.linearVelocity = velocity * speed;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Enemy"))
        {
            IDamageable dmg = collision.gameObject.GetComponent<IDamageable>();
            dmg?.ApplyDamage(damage, gameObject.transform.position, true);
            Destroy(gameObject);
        }
        
    }
}
