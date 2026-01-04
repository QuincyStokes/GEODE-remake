using System;
using UnityEngine;

[Serializable]
public abstract class BaseEnemyState
{
    public abstract void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine);
    public abstract void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine);
    public abstract void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine);
    public abstract void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine);

    protected Vector2 GetNearestPointOnTarget(BaseEnemy owner)
    {
        if(owner.currentTarget.ObjectTransform == null) return owner.transform.position;
        var targetT = owner.currentTarget.ObjectTransform;
        var col = owner.currentTarget.CollisionHitbox;
        if (col != null)
            return Physics2D.ClosestPoint(owner.transform.position, col);

        return targetT.position;
    }
    
   
    
}
