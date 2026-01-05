using System;
using UnityEngine;

[Serializable]
public class AttackState : BaseEnemyState
{
    private enum Phase { Windup, Execute, Recovery }
    private Phase    currentPhase;
    private float    phaseTimer;

    public override void EnterState(BaseEnemy owner, EnemyStateMachine sm)
    {
        owner.rb.linearVelocity = Vector2.zero;
        owner.animator.ResetTrigger("Attack");        
        owner.animator.SetTrigger("Attack");          

        currentPhase = Phase.Windup;
        phaseTimer   = 0f;        
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine sm)
    {
        phaseTimer -= Time.deltaTime;
        if (phaseTimer > 0f)
            return;

        switch (currentPhase)
        {
            case Phase.Windup:
                //!For now this is where we do the "windup" animation, attackWindupTime should be the time it takes until the impact frame.
                
                phaseTimer = owner.attackWindupTime; 
                currentPhase = Phase.Execute;
                break;

            case Phase.Execute:

                owner.animator.SetTrigger("Attack");
                
                phaseTimer = owner.attackRecoveryTime;
                currentPhase = Phase.Recovery;
                break;

            case Phase.Recovery:
                // Recovery done: go back to your default behavior
                // (you could inspect the time of day, target, etc., here too)
                sm.ChangeState(sm.idleState);
                break;
        }
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine sm)
    {
        // nothing to do in FixedUpdate for an attack
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine sm)
    {
        // any cleanup (e.g. reset flags) goes here
    }
}
