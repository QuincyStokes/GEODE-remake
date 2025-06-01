using System.Collections;
using UnityEngine;

public class RatEnemy : BaseEnemy
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Hitbox attackHitbox;

    public override void PostStart()
    {
        attackHitbox.gameObject.SetActive(false);
    }

    public override void Attack()
    {
        Debug.Log("RAT ATTACK");
        if(currentTarget != null)
        {
            if (currentTarget.ObjectTransform == null) return;
            Vector3 dir = (currentTarget.ObjectTransform.position - transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            attackHitbox.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            //should probably have an event in the animation that does this instead, same with disabling.

            //lets use our new little hitbox system
            //set hitbox info, then enable it
            attackHitbox.damage = attackDamage;
            attackHitbox.sourceDirection = transform.position;
            attackHitbox.dropItems = false;

            attackHitbox.gameObject.SetActive(true);
            StartCoroutine(DisableHitbox());
        }
        else
        {
            Debug.Log("Could not attack, target is null");
        }
    }

    private IEnumerator DisableHitbox()
    {
        yield return new WaitForSeconds(.2f);
        attackHitbox.gameObject.SetActive(false);
    }
}


