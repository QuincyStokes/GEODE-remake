using System.Collections;
using UnityEngine;

public class AOETower : BaseTower
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private int framesToWait;
    ///This tower should basically just deal damage to all targets in range
    public override IEnumerator Fire()
    {
        animator.SetTrigger("Shoot");
        for (int i = 0; i < framesToWait; i++)
        {
            yield return new WaitForEndOfFrame();
        }

        //Every target within our range
        foreach(GameObject go in targets)
        {
            //If this gameobject is null, remove it from thelist and continue
            if(go == null)
            {
                RemoveTarget(go);
                continue;
            }
            IDamageable dmg = go.GetComponentInParent<IDamageable>();
            if(dmg == null) continue;
            if(dmg.ApplyDamage(new DamageInfo(strength.Value, transform.position, drops:true, dmgType:damageType)))
            {
                KilledSomething(dmg);
            }
            HitSomething(dmg);
        }
    }

    public override void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Enemy"))
        {
            if(collision.TryGetComponent<BaseEnemy>(out BaseEnemy bo))
            {
                AddTarget(bo.CenterPoint.gameObject);
            }
            else
            {
                AddTarget(collision.gameObject);
            }
            Debug.Log("Enemy entered range, adding it to targets.");
        }
    }

    public override void OnTriggerExit2D(Collider2D collision)
    {
         if(collision.gameObject.CompareTag("Enemy"))
        {
            RemoveTarget(collision.gameObject);
            Debug.Log("Enemy entered range, adding it to targets.");
        }
    }
}
