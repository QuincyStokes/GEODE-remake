using UnityEngine;

public class BoarPathToCore : BaseEnemyState
{
    private float attackTimer = 0f;

    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Boar entered BoarPathToCore state");
        if (owner.coreTransform == null)
        {
            stateMachine.ChangeState(stateMachine.idleState);
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
            stateMachine.ChangeState(stateMachine.idleState);
            return;
        }

        //at this point we know its nighttime, so safe to assume below
        if (owner.coreTransform == null || owner.currentTarget == null)
        {
            stateMachine.ChangeState(stateMachine.pathToPlayerState);
            return;
        }


        Vector2 nearestCorePoint = GetNearestPointOnTarget(owner);
        Vector2 coreDir = nearestCorePoint - (Vector2)owner.transform.position;
        float distanceSq = coreDir.sqrMagnitude;


        //If we're not on the flow field, walk towards the core and around obstacles
        if (!FlowFieldManager.Instance.IsOnFlowField(owner.transform.position))
        {
            //owner.rb.linearVelocity = (owner.corePosition - owner.transform.position).normalized;
            

            Vector2 desiredVel = coreDir.normalized;
            Vector2 steerVel = owner.steering.GetSteeredDirection(owner, desiredVel) * owner.movementSpeed;

            owner.rb.linearVelocity = steerVel + owner.externalVelocity;
        }
        //If we are on the flow field
        else 
        {
            Vector2 flowDir = FlowFieldManager.Instance.GetFlowDirection(owner.transform.position);
            if (flowDir == Vector2.zero)
            {
                //this means there is no path to the core from our current location, set state to PathToObstructing
                stateMachine.ChangeState(stateMachine.pathToObstructingState);
                return;
            }
            
            if (distanceSq <= owner.attackRange*owner.attackRange)
            {
                //just stand there for now, attacking logic is handled in Update
                owner.rb.linearVelocity = owner.externalVelocity;

            }
            else
            {
                //owner.rb.linearVelocity = FlowFieldManager.Instance.GetFlowDirection(owner.transform.position) * owner.movementSpeed;
                //now with steering!
                Vector2 desiredVel = flowDir;

                Vector2 finalDir = owner.steering.GetSteeredDirection(owner, desiredVel)  * owner.movementSpeed;

                owner.rb.linearVelocity = finalDir + owner.externalVelocity;
            }

        }
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        attackTimer += Time.deltaTime;

        Vector2 nearestCorePoint = GetNearestPointOnTarget(owner);
        owner.targetClosestPoint = nearestCorePoint;
        float distance = Vector2.Distance(nearestCorePoint, owner.transform.position);
        if (attackTimer >= owner.attackCooldown && distance <= owner.attackRange)
        {
            
            // if current target is in range, attack!
            //if (Vector3.Distance(owner.targetClosestPoint, owner.transform.position) <= owner.attackRange)
            //{
                //Switch to attack state\

            //no need to check range again
            stateMachine.ChangeState(stateMachine.attackState);
            //}
        }
    }
}
