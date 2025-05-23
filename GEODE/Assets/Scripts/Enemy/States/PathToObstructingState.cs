using Unity.Multiplayer.Tools.NetStatsMonitor;
using Unity.VisualScripting;
using UnityEngine;

public class PathToObstructingState : BaseEnemyState
{
    private float attackTimer;
    //maybe store a reference to the object in our way
    private BaseObject obstructingObject;

    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        

        if (!DayCycleManager.Instance.IsNighttime())
        {
            stateMachine.ChangeState(new IdleState());
            return;
        }

        attackTimer = owner.attackCooldown;
        obstructingObject = null;
        //set running animation?
        owner.animator.SetBool("Move", true);
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
       owner.animator.SetBool("Move", false);
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        //if there is a path to the core
        //change state to PathToCoreState

        //if the player is within a certain range, and there is.. line of sight to them?
        //mayb for now we just ignore player, BUT
        //change state to PathToPlayerState
            
        if (!DayCycleManager.Instance.IsNighttime())
        {
            stateMachine.ChangeState(new IdleState());
            return;
        }

        if (FlowFieldManager.Instance.IsOnFlowField(owner.transform.position))
        {
            Vector2 flowDir = FlowFieldManager.Instance.GetFlowDirection(owner.transform.position);
            if (flowDir != Vector2.zero)
            {
                //this means there is now a path to the core, lets switch states
                stateMachine.ChangeState(new PathToCoreState());
                return;
            }
           
            
            //this means there is no path to the core. 
            //raycast a line from our position to the core
            //first thing we hit, we shall target and attack
            if (obstructingObject == null)
            {

                Vector2 coreDir = owner.corePosition - (Vector2)owner.transform.position;
                float dist = coreDir.magnitude;

                RaycastHit2D hit = Physics2D.Raycast(owner.transform.position, coreDir.normalized, dist, owner.structureLayerMask);

                if (hit.collider != null)
                {
                    //we have found a target to attack
                    obstructingObject =  hit.collider.GetComponentInParent<BaseObject>();

                    //if the Id of our target happens to be the core, switch to PathToCore
                    if (obstructingObject.matchingItemId == 6)
                    {
                        stateMachine.ChangeState(new PathToCoreState());
                        return;
                    }
                    owner.currentTarget = obstructingObject;
                }
                else
                {
                    return;
                }
            }
            else
            {
                //here means we know what our obstructing object is, now we can go attack it. 
                Vector2 nearestBlockingPoint = GetNearestPointOnTarget(owner);

                Vector2 blockingDir = nearestBlockingPoint - (Vector2)owner.transform.position;
                Vector2 desiredVelocity = blockingDir.normalized * owner.movementSpeed;

                Vector2 steerDir = owner.steering.GetSteeredDirection(owner, desiredVelocity);

                owner.rb.linearVelocity = steerDir + owner.externalVelocity;

            }

            
        }
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        attackTimer += Time.deltaTime;
        Vector2 nearestBlockingPoint = GetNearestPointOnTarget(owner);

        if (obstructingObject != null && attackTimer >= owner.attackCooldown)
        {
            // if current target is in range, attack!
            float sqrDist = ((Vector2)owner.currentTarget.ObjectTransform.position-(Vector2)owner.transform.position).sqrMagnitude;
            //Switch to attack state

            if (sqrDist <= owner.attackRange * owner.attackRange)
            {
                owner.targetClosestPoint = nearestBlockingPoint;
                stateMachine.ChangeState(new AttackState());
                return;
            }

        }

    }

}
