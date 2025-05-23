using Unity.Multiplayer.Tools.NetStatsMonitor;
using Unity.VisualScripting;
using UnityEngine;

public class PathToCoreState : BaseEnemyState
{
    private float attackTimer = 0f;
    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        //set running animation?
        if (owner.coreTransform == null)
        {
            stateMachine.ChangeState(new IdleState());
            return;
        }
        owner.animator.SetBool("Move", true);
        owner.currentTarget = owner.coreTransform.GetComponent<BaseObject>();

        attackTimer = owner.attackCooldown;
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        owner.animator.SetBool("Move", false);
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        if (!DayCycleManager.Instance.IsNighttime())
        {
            stateMachine.ChangeState(new IdleState());
            return;
        }

        if (owner.coreTransform == null)
        {
            stateMachine.ChangeState(new IdleState());
            return;
        }

        Vector2 nearestCorePoint = GetNearestPointOnTarget(owner);
        Vector2 coreDir = nearestCorePoint - (Vector2)owner.transform.position;
        float distanceSq = coreDir.sqrMagnitude;

        if (!FlowFieldManager.Instance.IsOnFlowField(owner.transform.position))
        {
            //owner.rb.linearVelocity = (owner.corePosition - owner.transform.position).normalized;
            //HERE might be a little cringe, but can we just add (1, 1) to the position, so that way the enemies path towards the center of it?

            Vector2 desiredVel = coreDir.normalized * owner.movementSpeed;
            Vector2 steerVel = owner.steering.GetSteeredDirection(owner, desiredVel);

            owner.rb.linearVelocity = steerVel + owner.externalVelocity;
        }
        else 
        {
            Vector2 flowDir = FlowFieldManager.Instance.GetFlowDirection(owner.transform.position);
            if (flowDir == Vector2.zero)
            {
                //this means there is no path to the core from our current location, set state to PathToObstructing
                stateMachine.ChangeState(new PathToObstructingState());
                return;
            }
            
            if (distanceSq <= owner.attackRange*owner.attackRange)
            {
                //just stand there for now
                owner.rb.linearVelocity = owner.externalVelocity;

            }
            else
            {
                //owner.rb.linearVelocity = FlowFieldManager.Instance.GetFlowDirection(owner.transform.position) * owner.movementSpeed;
                //now with steering!
                Vector2 desiredVel = flowDir * owner.movementSpeed;

                Vector2 finalDir = owner.steering.GetSteeredDirection(owner, desiredVel);

                owner.rb.linearVelocity = finalDir + owner.externalVelocity;
            }

        }
 
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {

        attackTimer += Time.deltaTime;

        Vector2 nearestCorePoint = GetNearestPointOnTarget(owner);
        float distance = Vector2.Distance(nearestCorePoint, owner.transform.position);
        if (attackTimer >= owner.attackCooldown && distance <= owner.attackRange)
        {
            // if current target is in range, attack!
            if (Vector3.Distance(owner.corePosition, owner.transform.position) <= owner.attackRange)
            {
                //Switch to attack state\
                owner.targetClosestPoint = nearestCorePoint;
                stateMachine.ChangeState(new AttackState());
            }
        }
    }
}
