using System;
using System.Collections;
using UnityEngine;

public class BoarEnemy : BaseEnemy
{
    //public because state needs access
    [Header("Simple Attack")]
    public float simpleAttackRange;
    public Hitbox simpleAttackHitbox;
    public float simpleAttackWindupTimer;
    public float timeUntilImpactFrame;
    [SerializeField] private Transform hiboxParent;
    [Header("Charge Attack")]
    public Hitbox chargeAttackHitbox; 
    public Hitbox chargeAttackDetectionHitbox;
    public float chargeTime;
    public float chargeDamageModifier;
    public float chargeAttackWindupTimer;

    

    public float AttackRange => attackRange;

    protected override void Awake()
    {
        base.Awake();
        simpleAttackHitbox.OnHitSomething += HandleSimpleAttackHit;
        chargeAttackDetectionHitbox.OnHitSomething += HandleChargeAttackHit;

        simpleAttackHitbox.DisableCollider();
        chargeAttackHitbox.DisableCollider();
        chargeAttackDetectionHitbox.DisableCollider();

        simpleAttackHitbox.DisableVisuals();
        chargeAttackHitbox.DisableVisuals();
    }

    private void HandleSimpleAttackHit(IDamageable damageable)
    {
       
    }

    private void HandleChargeAttackHit(IDamageable damageable)
    {
        Debug.Log($"Boar Charge hit {damageable}");
        //Turn of detection
        chargeAttackDetectionHitbox.DisableCollider();

        //Enable charge hit
        chargeAttackHitbox.EnableCollider();
        chargeAttackHitbox.EnableVisuals();
        StartCoroutine(DisableChargeHitboxes());
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
        
        //Set hitbox angle
        hiboxParent.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        //Initialize Simple Hitbox
        simpleAttackHitbox.Initialize(attackDamage, transform.position, false);
        simpleAttackHitbox.EnableCollider();
        simpleAttackHitbox.EnableVisuals();
        StartCoroutine(DisableSimpleHitbox());
    }

    public void ChargeAttack()
    {
        //movement handled by the state, we can just do hitbox things
        Vector3 dir = (targetClosestPoint - (Vector2)transform.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        //Set hitbox angle
        hiboxParent.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        //Initilize detection hitbox
        chargeAttackDetectionHitbox.Initialize(0, transform.position, false);
        chargeAttackDetectionHitbox.EnableCollider(); //No visuals, simply detect

        //Initialize Damage hitbox
        chargeAttackHitbox.Initialize(attackDamage * chargeDamageModifier, transform.position, false);

        //Disable triggerd by collision
    }


    private IEnumerator DisableSimpleHitbox()
    {
        yield return new WaitForSeconds(.2f);
        simpleAttackHitbox.DisableCollider();
        simpleAttackHitbox.DisableVisuals();
    }

    private IEnumerator DisableChargeHitboxes()
    {
        yield return new WaitForSeconds(.2f);

        chargeAttackHitbox.DisableCollider();
        chargeAttackHitbox.DisableVisuals();
    }

    public void DisableChargeDetectionHitbox()
    {
        chargeAttackDetectionHitbox.DisableCollider();
    }

   


    protected override BaseEnemyState CreateIdleState() => new IdleState();
    protected override BaseEnemyState CreateAttackState() => new BoarAttackState();
    protected override BaseEnemyState CreatePathToCoreState() => new BoarPathToCore();
    protected override BaseEnemyState CreatePathToObstructingState() => new PathToObstructingState();
    protected override BaseEnemyState CreateDeathState() => new DeathState();
    protected override BaseEnemyState CreatePathToPlayerState() => new PathToPlayerState();
}
