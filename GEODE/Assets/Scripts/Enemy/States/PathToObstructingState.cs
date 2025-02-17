using UnityEngine;

public class PathToObstructing : BaseEnemyState
{
    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Entering PathToObstructing");
        //set running animation?
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Exiting PathToObstructing");
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Fixed Update for PathToObstructing");
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Update for PathToObstructing");

        //if there is a path to the core
            //change state to PathToCoreState

        //if the player is within a certain range, and there is.. line of sight to them?
            //mayb for now we just ignore player, BUT
            //change state to PathToPlayerState

    }

}
