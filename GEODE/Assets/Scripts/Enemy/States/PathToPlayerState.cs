using UnityEngine;

public class PathToPlayer : BaseEnemyState
{
    private float attackTimer;
    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        attackTimer = owner.attackCooldown;
        owner.animator.SetBool("Move", true);
        owner.currentTarget = owner.playerTransform.GetComponentInParent<IDamageable>();
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        owner.playerTransform = null;
        owner.animator.SetBool("Move", false);
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        if (DayCycleManager.Instance.IsNighttime())
        {
            stateMachine.ChangeState(new PathToCoreState());
            return;
        }

        if (owner.playerTransform == null)
        {
            stateMachine.ChangeState(new IdleState());
            return;
        }

        Vector2 playerDir = ((Vector2)owner.playerTransform.position - (Vector2)owner.transform.position).normalized;
        Vector2 desiredVel = playerDir * owner.movementSpeed;

        Vector2 steerDir = owner.steering.GetSteeredDirection(owner, desiredVel);

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
                owner.targetClosestPoint = owner.transform.position;
                stateMachine.ChangeState(new AttackState());
                return;
            }

        }

       

    }
}
