using System;
using NUnit.Framework;
using Unity.Netcode.Editor;
using UnityEngine;

[Serializable]
public class BoarAttackState : BaseEnemyState
{
    private enum Phase { Deciding, ChargeWindup, Charging, ChargeRecovery, SimpleWindup, SimpleExecute, SimpleRecovery, Exiting }
    private enum AttackType { None, SimpleAttack, ChargeAttack }
    
    private Phase nextPhase;
    private AttackType attackType;
    private float currentPhaseTimer;
    private Vector2 chargeDirection;
    private EnemyStateMachine stateMachine;
    private BoarEnemy owner;
    private bool isCharging;
    private float chargeTime;
    private float chargeTimeElapsed;

    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Boar entered attack state");
        owner.rb.linearVelocity = Vector2.zero;

        nextPhase = Phase.Deciding;
        currentPhaseTimer = 0f;
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
        currentPhaseTimer -= Time.deltaTime;
        if (currentPhaseTimer > 0f || isCharging)
            return;

        //These cases are entered ONCE. A phase sets its next phase, then starts its phase timer.
            //This is applicable to all phases except for the Charge, where we need to end early if we hit something. But either way, our next phase is Recover
        switch (nextPhase)
        {
            ///! CAN DO ANIMATIONS THROUGHOUT THESE! 
            case Phase.Deciding:

                DecideAttackType(owner);

                break;

            case Phase.ChargeWindup:

                // Charge-up animation windup
                owner.animator.SetBool("Winding", true);
                owner.animator.SetBool("Move", false);

                currentPhaseTimer = this.owner.chargeAttackWindupTimer;

                nextPhase = Phase.Charging;
                chargeTimeElapsed = 0f;
                break;

            case Phase.Charging:
                owner.animator.SetBool("Charging", true);
                owner.animator.SetBool("Winding", false);

                this.owner.ChargeAttack();

                isCharging = true;
                
                nextPhase = Phase.ChargeRecovery; //I think we can do this since no matter what we're going to chargeRecovery next;

                // Charging is handled in FixedUpdate, we just wait here
                // (collision detection will trigger transition to ChargeRecovery)
                break;

            case Phase.ChargeRecovery:

                //Need to have this here in
                isCharging = false;
                this.owner.DisableChargeDetectionHitbox();
                owner.rb.linearVelocity = Vector2.zero;

                owner.animator.SetBool("Recovering", true);
                owner.animator.SetBool("Charging", false);

                currentPhaseTimer = owner.attackRecoveryTime;
                nextPhase = Phase.Exiting;

                break;

            case Phase.SimpleWindup:
                // Simple attack windup 
                Debug.Log("Boar Simple Windup");
                currentPhaseTimer = owner.attackWindupTime;
                owner.animator.SetBool("Winding", true);
                owner.animator.SetTrigger("Attack");
                nextPhase = Phase.SimpleExecute;
                break;

            case Phase.SimpleExecute:
                // Execute simple attack
                Debug.Log("Boar Simple Execute");
                currentPhaseTimer = owner.attackRecoveryTime;
                nextPhase = Phase.SimpleRecovery;
                break;

            case Phase.SimpleRecovery:
                Debug.Log("Boar Recovery");
                // Recovery done: go back to idle
                nextPhase = Phase.Exiting;
                break;

            case Phase.Exiting:
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
                nextPhase = Phase.ChargeRecovery;
                isCharging = false;

            }
        }
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        owner.rb.linearVelocity = Vector2.zero;
        owner.animator.SetBool("Move", false);
        owner.animator.SetBool("Charging", false);
        owner.animator.SetBool("Winding", false);
        owner.animator.SetBool("Recovering", false);

        Debug.Log("Boar left attack state");
    }

    private void DecideAttackType(BaseEnemy owner)
    {
        if (owner.currentTarget == null)
        {
            attackType = AttackType.None;
            stateMachine.ChangeState(stateMachine.idleState);
            return;
        }


        Debug.Log($"{owner.name} current target is {owner.currentTarget}");

        BoarEnemy boar = owner as BoarEnemy;
        if (boar == null)
            return;

        float distanceToTarget = Vector2.Distance(owner.currentTarget.CenterPoint.position, owner.transform.position);

        //Reset recovering flag incase we just came from there.
        owner.animator.SetBool("Recovering", false);

        // Simple attack if target is close
        if (distanceToTarget < boar.simpleAttackRange)
        {
            attackType = AttackType.SimpleAttack;
            nextPhase = Phase.SimpleWindup;
            currentPhaseTimer = 0f;
        }
        // Charge attack if target is further away
        else 
        {
            attackType = AttackType.ChargeAttack;
            chargeDirection = (owner.targetClosestPoint - (Vector2)owner.transform.position).normalized;
            nextPhase = Phase.ChargeWindup;
            currentPhaseTimer = 0f;
        }
    }

    private void HandleChargeHitSomething(IDamageable dmg)
    {
        if(isCharging)
        {
            //DOn't need to handle switching phase to Recover, if we're in charging it should handle itself
            isCharging = false;
            currentPhaseTimer = 0f;
            owner.rb.linearVelocity = Vector2.zero;
        }
    }
}
