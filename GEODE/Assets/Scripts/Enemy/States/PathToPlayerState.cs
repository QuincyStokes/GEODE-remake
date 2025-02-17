using UnityEngine;

public class PathToPlayer : BaseEnemyState
{
     public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Entering PathToPlayer");
        //set running animation?
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Exiting PathToPlayer");
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Fixed Update for PathToPlayer");
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Update for PathToPlayer");

        //if there is a path to the core
            //change state to PathToCoreState

        //if the player is too far, and there is no path to core
            //change state to PathToObstructingState

    }
}
