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
        if(currentTarget != null)
        {
            if (targetClosestPoint == null) return;
            Vector3 dir = (targetClosestPoint - (Vector2)transform.position).normalized;
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

    protected override BaseEnemyState CreateIdleState() => new IdleState();
    protected override BaseEnemyState CreateAttackState() => new AttackState();
    protected override BaseEnemyState CreatePathToCoreState() => new PathToCoreState();
    protected override BaseEnemyState CreatePathToObstructingState() => new PathToObstructingState();
    protected override BaseEnemyState CreateDeathState() => new DeathState();
    protected override BaseEnemyState CreatePathToPlayerState() => new PathToPlayerState();

}


