using Unity.VisualScripting;
using UnityEngine;

public class PathToCoreState : BaseEnemyState
{
    private float attackTimer = 0f;
    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        //set running animation?
        if(owner.coreTransform == null)
        {
            stateMachine.ChangeState(new IdleState());
        }
        owner.animator.SetBool("Move", true);
        owner.currentTarget = owner.coreTransform.GetComponent<BaseObject>();
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        owner.animator.SetBool("Move", false);
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        if(!DayCycleManager.Instance.IsNighttime())
        {
            stateMachine.ChangeState(new IdleState());
        }

        if(owner.coreTransform != null)
        {
          
            if(FlowFieldManager.Instance != null && !FlowFieldManager.Instance.IsOnFlowField(owner.transform.position))
            {
                //owner.rb.linearVelocity = (owner.corePosition - owner.transform.position).normalized;
                //HERE might be a little cringe, but can we just add (1, 1) to the position, so that way the enemies path towards the center of it?
                Vector2 coreDir = (owner.corePosition - (Vector2)owner.transform.position).normalized;

                Vector2 finalDir = owner.steering.GetSteeredDirection(owner, coreDir);

                owner.rb.linearVelocity = finalDir;
            }
            else if(FlowFieldManager.Instance != null && FlowFieldManager.Instance.IsOnFlowField(owner.transform.position))
            {
                if(FlowFieldManager.Instance.GetFlowDirection(owner.transform.position) == Vector2.zero)
                {
                    //this means there is no path to the core from our current location, set state to PathToObstructing
                    stateMachine.ChangeState(new PathToObstructingState());
                }
                else
                {
                    if(Vector3.Distance(owner.corePosition, owner.transform.position) <= owner.attackRange)
                    {
                        //just stand there for now
                        owner.rb.linearVelocity = Vector3.zero;
                    }
                    else
                    {
                        //owner.rb.linearVelocity = FlowFieldManager.Instance.GetFlowDirection(owner.transform.position) * owner.movementSpeed;
                        //now with steering!
                        Vector2 flowDir = FlowFieldManager.Instance.GetFlowDirection(owner.transform.position) * owner.movementSpeed;

                        Vector2 finalDir = owner.steering.GetSteeredDirection(owner,  flowDir);

                        owner.rb.linearVelocity = finalDir;
                    }
                    

                }
                
            }
        }
        else
        {
            Debug.Log("Core is null, cannot path to it.");
        }
        
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        if(attackTimer >= owner.attackCooldown)
        {
            // if current target is in range, attack!
            if(Vector3.Distance(owner.corePosition, owner.transform.position) <= owner.attackRange)
            {
                //Switch to attack state
                stateMachine.ChangeState(new AttackState());
            }
            //else, we are ready to attack but the core is not in range 
          
        }
        else
        {
            attackTimer += Time.deltaTime;
        }
    }
}
