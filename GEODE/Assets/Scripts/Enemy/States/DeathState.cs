using System;
using UnityEngine;
[Serializable]
public class DeathState : BaseEnemyState
{
  
    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        //play death animation, maybe start coroutine to destroy and drop items after?
        owner.animator.SetTrigger("Death");
        owner.rb.linearVelocity = Vector2.zero;
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        owner.DropItems();
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
    }

    

}
