using System;
using UnityEngine;

public class BoarEnemy : BaseEnemy
{
    //public because state needs access
    public Hitbox simpleAttackHitbox;
    [SerializeField] private Transform hiboxParent;
    public Hitbox chargeAttackHitbox; 
    [SerializeField] private float simpleAttackRange;

    

    public float SimpleAttackRange => simpleAttackRange;

    protected override void Awake()
    {
        base.Awake();
        simpleAttackHitbox.OnHitSomething += HandleSimpleAttackHit;
        chargeAttackHitbox.OnHitSomething += HandleChargeAttackHit;
    }

    private void HandleSimpleAttackHit(IDamageable damageable)
    {
       
    }

    private void HandleChargeAttackHit(IDamageable damageable)
    {
        
    }

    public override void Attack()
    {
        if(currentTarget == null || targetClosestPoint == null)
        {
            Debug.Log($"[BoarEnemy] could not attack CurrentTarget = {currentTarget}, TargetClosestPoint = {targetClosestPoint}");
            return;
        }
        //We'll try having the State trigger this attack whenever we're in range to hit something, but here we can do an additional check
        //to see if we should either do the charge attack or simple attack
        
        //boar has simpleAttackRange and attackRange. The latter will be the charge attack range

        //Do simple attack
        if(Vector2.Distance(currentTarget.ObjectTransform.position, transform.position) < simpleAttackRange)
        {
            //Initialize and enable little hitbox infront of dude   
            Vector3 dir = (targetClosestPoint - (Vector2)transform.position).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            hiboxParent.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
        //Do charge attack
        else
        {
            //Initialzie and enable big area damage hitbox. Nice.
        }
    }


    protected override BaseEnemyState CreateIdleState() => new IdleState();
    protected override BaseEnemyState CreateAttackState() => new BoarAttackState();
    protected override BaseEnemyState CreatePathToCoreState() => new PathToCoreState();
    protected override BaseEnemyState CreatePathToObstructingState() => new PathToObstructingState();
    protected override BaseEnemyState CreateDeathState() => new DeathState();
    protected override BaseEnemyState CreatePathToPlayerState() => new PathToPlayerState();
}
