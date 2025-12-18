using System;
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

    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        owner.rb.linearVelocity = Vector2.zero;
        owner.animator.ResetTrigger("Attack");
        owner.animator.SetTrigger("Attack");

        currentPhase = Phase.Deciding;
        phaseTimer = 0f;
        attackType = AttackType.None;

        //I think this is fine..
        this.stateMachine = stateMachine;

        if(owner is BoarEnemy)
        {
            BoarEnemy be = owner as BoarEnemy;
            be.chargeAttackHitbox.OnHitSomething += HandleChargeHitSomething;
        }
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        phaseTimer -= Time.deltaTime;
        if (phaseTimer > 0f)
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
                phaseTimer = owner.attackWindupTime;
                currentPhase = Phase.Charging;
                break;

            case Phase.Charging:
                Debug.Log("Boar Charging");
                // Charging is handled in FixedUpdate, we just wait here
                // (collision detection will trigger transition to ChargeRecovery)
                break;

            case Phase.ChargeRecovery:
                // Recovery after charge   
                Debug.Log("Boar Charge Recovery");
                phaseTimer = owner.attackRecoveryTime;
                currentPhase = Phase.Deciding;
                // After recovery, decide again or exit
                stateMachine.ChangeState(stateMachine.idleState);
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
        if (currentPhase == Phase.Charging)
        {
            owner.rb.linearVelocity = chargeDirection * owner.movementSpeed * 2f; // Charge faster than normal movement
        }
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        owner.rb.linearVelocity = Vector2.zero;
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
        }
    }

    private void HandleChargeHitSomething(IDamageable dmg)
    {
        if(currentPhase == Phase.Charging)
        {
            currentPhase = Phase.ChargeRecovery;
        }
    }
}
