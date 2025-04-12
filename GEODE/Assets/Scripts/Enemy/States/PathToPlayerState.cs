using UnityEngine;

public class PathToPlayer : BaseEnemyState
{
     public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        //entering path to player, other logic decides when we enter it
        //should just be set running animation?
        //set running animation?
        owner.animator.SetBool("Move", true);
        owner.currentTarget = owner.coreTransform.GetComponent<BaseObject>();
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {

        //if there is a path to the core
            //change state to PathToCoreState

        //if the player is too far, and there is no path to core
            //change state to PathToObstructingState

    }
}
