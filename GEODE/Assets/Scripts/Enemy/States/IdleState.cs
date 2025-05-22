using UnityEngine;

public class IdleState : BaseEnemyState
{
    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {

        //logic here for starting idle state
        //play idle animation
        //animation should actually default to idle..?
        owner.rb.linearVelocity = Vector2.zero;
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        
        //logic here for exiting idle state, not actually sure what i'd put here.
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {

        //logic here for fixedUpdate, again not sure what will go here
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        //* Here can do some random movement, wander around, whatever "idle" things

        //logic here for Update, logic here likely for constantly checking external things like playerPosition, corePosition, and whatnot.
        if (DayCycleManager.Instance != null && DayCycleManager.Instance.IsNighttime() && owner.coreTransform != null)
        {
            stateMachine.ChangeState(new PathToCoreState());
        }
    }
}
