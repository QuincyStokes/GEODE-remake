using UnityEngine;

public class PathToCore : BaseEnemyState
{
    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Entering PathToCoreState");
        //set running animation?
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Exiting PathToCoreState");
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Update for PathToCore");
        if(!DayCycleManager.Instance.IsNighttime())
        {
            stateMachine.ChangeState(new IdleState());
        }

        if(owner.coreTransform != null)
        {
            if(FlowFieldManager.Instance != null && !FlowFieldManager.Instance.IsOnFlowField(owner.transform.position))
            {
                Debug.Log("Unit is not on FlowField");
                owner.rb.linearVelocity = (owner.coreTransform.position - owner.transform.position).normalized;
            }
            else if(FlowFieldManager.Instance != null && FlowFieldManager.Instance.IsOnFlowField(owner.transform.position))
            {
                if(FlowFieldManager.Instance.GetFlowDirection(owner.transform.position) == Vector2.zero)
                {
                    //this means there is no path to the core from our current location, set state to PathToObstructing
                    stateMachine.ChangeState(new PathToObstructing());
                }
                else
                {
                    Debug.Log("Unit is on FlowField, and is on top of a nonzero flow");
                    //owner.rb.linearVelocity = FlowFieldManager.Instance.GetFlowDirection(owner.transform.position) * owner.movementSpeed;
                    //now with steering!
                    Vector2 flowDir = FlowFieldManager.Instance.GetFlowDirection(owner.transform.position) * owner.movementSpeed;

                    Vector2 finalDir = owner.steering.GetSteeredDirection(owner,  flowDir);

                    owner.rb.linearVelocity = finalDir;

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
    
    }
}
