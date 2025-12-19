using System;
using System.Collections;
using UnityEngine;

public class BoarEnemy : BaseEnemy
{
    //public because state needs access
    public Hitbox simpleAttackHitbox;
    [SerializeField] private Transform hiboxParent;
    public Hitbox chargeAttackHitbox; 
    [SerializeField] private float simpleAttackRange;
    public float chargeTime;

    

    public float SimpleAttackRange => simpleAttackRange;

    protected override void Awake()
    {
        base.Awake();
        simpleAttackHitbox.OnHitSomething += HandleSimpleAttackHit;
        chargeAttackHitbox.OnHitSomething += HandleChargeAttackHit;

        simpleAttackHitbox.DisableCollider();
        chargeAttackHitbox.DisableCollider();
    }

    private void HandleSimpleAttackHit(IDamageable damageable)
    {
       
    }

    private void HandleChargeAttackHit(IDamageable damageable)
    {
        chargeAttackHitbox.gameObject.SetActive(false);
    }

    public override void Attack()
    {
        if(currentTarget == null || targetClosestPoint == null)
        {
            Debug.Log($"[BoarEnemy] could not attack CurrentTarget = {currentTarget}, TargetClosestPoint = {targetClosestPoint}");
            return;
        }
        
        Vector3 dir = (targetClosestPoint - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        hiboxParent.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        simpleAttackHitbox.Initialize(attackDamage, transform.position, false);
        simpleAttackHitbox.gameObject.SetActive(true);
        StartCoroutine(DisableSimpleHitbox());
    }

    public void ChargeAttack()
    {
        //movement handled by the state, we can just do hitbox things
        Vector3 dir = (targetClosestPoint - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        chargeAttackHitbox.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        chargeAttackHitbox.Initialize(attackDamage*4, transform.position, false);
        chargeAttackHitbox.gameObject.SetActive(true);
    }


    private IEnumerator DisableSimpleHitbox()
    {
        yield return new WaitForSeconds(.2f);
        simpleAttackHitbox.gameObject.SetActive(false);
    }

   


    protected override BaseEnemyState CreateIdleState() => new IdleState();
    protected override BaseEnemyState CreateAttackState() => new BoarAttackState();
    protected override BaseEnemyState CreatePathToCoreState() => new PathToCoreState();
    protected override BaseEnemyState CreatePathToObstructingState() => new PathToObstructingState();
    protected override BaseEnemyState CreateDeathState() => new DeathState();
    protected override BaseEnemyState CreatePathToPlayerState() => new PathToPlayerState();
}
