using UnityEngine;

public abstract class BaseEnemyState 
{
    public abstract void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine);
    public abstract void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine);
    public abstract void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine);
    public abstract void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine);
    
}
