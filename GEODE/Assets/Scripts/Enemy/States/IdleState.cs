using UnityEngine;

public class IdleState : BaseEnemyState
{
    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Entering Idle State!");

        //logic here for starting idle state
           //play idle animation
        //animation should actually default to idle..?
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Exiting Idle State!");
        
        //logic here for exiting idle state, not actually sure what i'd put here.
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Entering FixedUpdate State!");

        //logic here for fixedUpdate, again not sure what will go here
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Entering Update State!");

        //logic here for Update, logic here likely for constantly checking external things like playerPosition, corePosition, and whatnot.
        if(DayCycleManager.Instance != null && DayCycleManager.Instance.IsNighttime())
        {
            stateMachine.ChangeState(new PathToCoreState());
        }
    }
}
