using UnityEngine;

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
        phaseTimer   = owner.attackWindupTime;        
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine sm)
    {
        phaseTimer -= Time.deltaTime;
        if (phaseTimer > 0f)
            return;

        switch (currentPhase)
        {
            case Phase.Windup:
                // Windup is done: deal damage or spawn projectile
                owner.Attack();
                
                // move into Recovery (or a very short Execute, if you want to split them)
                currentPhase = Phase.Recovery;
                phaseTimer   = owner.attackRecoveryTime; // e.g. 0.2s of pause after the hit
                break;

            case Phase.Recovery:
                // Recovery done: go back to your default behavior
                // (you could inspect the time of day, target, etc., here too)
                sm.ChangeState(new IdleState());
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
