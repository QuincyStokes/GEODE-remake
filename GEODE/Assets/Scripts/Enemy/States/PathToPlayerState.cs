using UnityEngine;
using System;

[Serializable]
public class PathToPlayerState : BaseEnemyState
{
    private float attackTimer;
    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        attackTimer = owner.attackCooldown;
        owner.animator.SetBool("Move", true);

        //not sure how yucky this is..
        owner.currentTarget = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<IDamageable>();
        owner.playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        ///owner.playerTransform = null;
        //owner.currentTarget = null;
        owner.animator.SetBool("Move", false);
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        //we can stay in the PathToPlayer state during nighttime **as long as there is no core** 
        if (DayCycleManager.Instance.IsNighttime())
        {

            if (owner.coreTransform != null)
            {
                stateMachine.ChangeState(stateMachine.pathToCoreState);
                return;
            }

        }

        if (owner.playerTransform == null)
        {
            stateMachine.ChangeState(stateMachine.idleState);
            return;
        }

        Vector2 playerDir = ((Vector2)owner.playerTransform.position - (Vector2)owner.transform.position).normalized;
        Vector2 desiredVel = playerDir;

        Vector2 steerDir = owner.steering.GetSteeredDirection(owner, desiredVel) * owner.movementSpeed;

        owner.rb.linearVelocity = steerDir + owner.externalVelocity;

    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        attackTimer += Time.deltaTime;

        if (owner.playerTransform != null && attackTimer >= owner.attackCooldown)
        {
            // if current target is in range, attack!
            float sqrDist = ((Vector2)owner.playerTransform.position-(Vector2)owner.transform.position).sqrMagnitude;
            //Switch to attack state

            if (sqrDist <= owner.attackRange * owner.attackRange)
            {
                owner.targetClosestPoint = owner.playerTransform.position;
                stateMachine.ChangeState(stateMachine.attackState);
                return;
            }
        }
    }
}
