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
        Debug.Log("Fixed Update for PathToCoreState");
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
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
                owner.rb.linearVelocity = (owner.coreTransform.position - owner.transform.position).normalized;
            }

            if(FlowFieldManager.Instance != null && FlowFieldManager.Instance.IsOnFlowField(owner.transform.position))
            {
                owner.rb.linearVelocity = FlowFieldManager.Instance.GetFlowDirection(owner.transform.position);
            }
        }
        else
        {
            Debug.Log("Core is null, cannot path to it.");
        }
        

        


    }
}
