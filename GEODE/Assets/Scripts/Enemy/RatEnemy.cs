using System.Collections;
using UnityEngine;

public class RatEnemy : BaseEnemy
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private BoxCollider2D attackHitbox;

    public override void PostStart()
    {
        
        attackHitbox.enabled = false;
    }

    public override void Attack()
    {
        Debug.Log("RAT ATTACK");
        if(currentTarget != null)
        {
            Vector3 dir = (currentTarget.ObjectTransform.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            attackHitbox.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            //should probably have an event in the animation that does this instead, same with disabling.
            attackHitbox.enabled = true;
            StartCoroutine(DisableHitbox());
        }
        else
        {
            Debug.Log("Could not attack, target is null");
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.CompareTag("Structure"))
        {
            Debug.Log("Hit a structure!");

            //need to loook on the parent objects, BaseStructure script doesnt exist on the collision object
            collision.gameObject.GetComponentInParent<BaseObject>().ApplyDamage(new DamageInfo(attackDamage, gameObject.transform.position));
        }
    }

    private IEnumerator DisableHitbox()
    {
        yield return new WaitForSeconds(.2f);
        attackHitbox.enabled = false;
    }
}


