using UnityEngine;

public class DeathState : BaseEnemyState
{
  
    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Entering DeathState");
        //play death animation, maybe start coroutine to destroy and drop items after?
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        owner.DropItems();
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Fixed Update for DeathState");
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Update for DeathState");
    }

    

}
