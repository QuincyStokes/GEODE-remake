using System;
using Unity.Netcode.Editor;
using UnityEngine;

[Serializable]
public class BoarAttackState : BaseEnemyState
{
    private enum Phase { Deciding, ChargeWindup, Charging, ChargeRecovery, SimpleWindup, SimpleExecute, SimpleRecovery }
    private enum AttackType { None, SimpleAttack, ChargeAttack }
    
    private Phase currentPhase;
    private AttackType attackType;
    private float phaseTimer;
    private Vector2 chargeDirection;
    private EnemyStateMachine stateMachine;
    private BoarEnemy owner;
    private bool isCharging;
    private float chargeTime;
    private float chargeTimeElapsed;

    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        owner.rb.linearVelocity = Vector2.zero;

        currentPhase = Phase.Deciding;
        phaseTimer = 0f;
        attackType = AttackType.None;

        //I think this is fine..
        this.stateMachine = stateMachine;
       

        if(owner is BoarEnemy)
        {
            BoarEnemy be = owner as BoarEnemy;
            this.owner = be;
            this.chargeTime = be.chargeTime;
            be.chargeAttackHitbox.OnHitSomething += HandleChargeHitSomething;
        }
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        phaseTimer -= Time.deltaTime;
        if (phaseTimer > 0f || isCharging)
            return;

        switch (currentPhase)
        {
            ///! CAN DO ANIMATIONS THROUGHOUT THESE! 
            case Phase.Deciding:
                Debug.Log("Boar Deciding");
                DecideAttackType(owner);
                break;

            case Phase.ChargeWindup:
                Debug.Log("Boar Charge Windup");
                // Charge-up animation windup
                owner.animator.SetTrigger("Windup");
                phaseTimer = owner.attackWindupTime;
                currentPhase = Phase.Charging;
                
                chargeTimeElapsed = 0f;
                break;

            case Phase.Charging:
                Debug.Log("Boar Charging");
                owner.animator.SetBool("Charging", true);
                this.owner.ChargeAttack();
                isCharging = true;
                
                // Charging is handled in FixedUpdate, we just wait here
                // (collision detection will trigger transition to ChargeRecovery)
                break;

            case Phase.ChargeRecovery:
                // Recovery after charge  
                isCharging = false;
                Debug.Log("Boar Charge Recovery");
                owner.animator.SetTrigger("Recover");
                owner.animator.SetBool("Charging", false);
                phaseTimer = owner.attackRecoveryTime;
                currentPhase = Phase.Deciding;
                // After recovery, decide again or exit
                break;

            case Phase.SimpleWindup:
                // Simple attack windup 
                Debug.Log("Boar Simple Windup");
                phaseTimer = owner.attackWindupTime;
                currentPhase = Phase.SimpleExecute;
                break;

            case Phase.SimpleExecute:
                // Execute simple attack
                Debug.Log("Boar Simple Execute");
                owner.animator.SetTrigger("Attack");
                owner.Attack();
                phaseTimer = owner.attackRecoveryTime;
                currentPhase = Phase.SimpleRecovery;
                break;

            case Phase.SimpleRecovery:
                Debug.Log("Boar Recovery");
                // Recovery done: go back to idle
                stateMachine.ChangeState(stateMachine.idleState);
                break;
        }
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        // Handle charge movement
        if (isCharging)
        {
            if(chargeTimeElapsed <= chargeTime)
            {
                chargeTimeElapsed += Time.fixedDeltaTime;
                owner.rb.linearVelocity = chargeDirection * owner.movementSpeed * 2f; // Charge faster than normal movement
            }
            else
            {
                currentPhase = Phase.ChargeRecovery;
                isCharging = false;
            }
        }
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        owner.rb.linearVelocity = Vector2.zero;
        owner.animator.SetBool("Move", false);
        owner.animator.SetBool("Charging", false);
    }

    private void DecideAttackType(BaseEnemy owner)
    {
        if (owner.currentTarget == null)
        {
            attackType = AttackType.None;
            return;
        }

        BoarEnemy boar = owner as BoarEnemy;
        if (boar == null)
            return;

        float distanceToTarget = Vector2.Distance(owner.currentTarget.ObjectTransform.position, owner.transform.position);

        // Simple attack if target is close
        if (distanceToTarget < boar.SimpleAttackRange)
        {
            attackType = AttackType.SimpleAttack;
            currentPhase = Phase.SimpleWindup;
            phaseTimer = 0f;
        }
        // Charge attack if target is further away
        else if (distanceToTarget < owner.attackRange)
        {
            attackType = AttackType.ChargeAttack;
            chargeDirection = (owner.targetClosestPoint - (Vector2)owner.transform.position).normalized;
            currentPhase = Phase.ChargeWindup;
            phaseTimer = 0f;
        }
        else
        {
            // Out of range, shouldn't be in attack state
            attackType = AttackType.None;
            stateMachine.ChangeState(stateMachine.idleState);
        }
    }

    private void HandleChargeHitSomething(IDamageable dmg)
    {
        if(currentPhase == Phase.Charging)
        {
            currentPhase = Phase.ChargeRecovery;
            isCharging = false;
        }
    }
}
