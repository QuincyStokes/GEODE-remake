using UnityEngine;

public class DeathState : BaseEnemyState
{
  
    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        //play death animation, maybe start coroutine to destroy and drop items after?
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        owner.DropItemsServerRpc();
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
    }

    

}
