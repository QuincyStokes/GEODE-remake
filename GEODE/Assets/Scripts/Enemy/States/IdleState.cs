using System;
using Unity.Multiplayer.Tools.NetStatsMonitor;
using UnityEngine;

[Serializable]
public class IdleState : BaseEnemyState
{
    private float wanderTimer;
    private Vector2 wanderTarget;

    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        wanderTimer = UnityEngine.Random.Range(owner.wanderTimeMin, owner.wanderTimeMax);
        SetNewWanderTarget(owner);

        owner.rb.linearVelocity = Vector2.zero;
        owner.animator.SetBool("Move", true);
        if (!DayCycleManager.Instance.IsNighttime())
        {
            owner.canAggro = true;
        }
       
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        owner.animator.SetBool("Move", false);
        owner.rb.linearVelocity = owner.externalVelocity;
        owner.canAggro = false;
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {

        if (DayCycleManager.Instance.IsNighttime())
        {
            stateMachine.ChangeState(stateMachine.pathToCoreState);
            return;
        }

        if (owner.playerTransform != null)
        {
            
            stateMachine.ChangeState(stateMachine.pathToPlayerState);
            return;
            
        }

        //wander!
        wanderTimer -= Time.fixedDeltaTime;
        if (wanderTimer <= 0 || Vector2.Distance(owner.transform.position, wanderTarget) < .2f)
        {
            wanderTimer = UnityEngine.Random.Range(owner.wanderTimeMin, owner.wanderTimeMax);
            SetNewWanderTarget(owner);
        }

        Vector2 targetDir = (wanderTarget - (Vector2)owner.transform.position).normalized;
        Vector2 desiredVel = targetDir;
        Vector2 steerVel = owner.steering.GetSteeredDirection(owner, desiredVel) * owner.movementSpeed;

        owner.rb.linearVelocity = steerVel + owner.externalVelocity;
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        
    }
    
    private void SetNewWanderTarget(BaseEnemy owner)
    {
        Vector2 offset = UnityEngine.Random.insideUnitCircle * owner.wanderRadius;
        wanderTarget  = (Vector2)owner.transform.position + offset;
    }
}
