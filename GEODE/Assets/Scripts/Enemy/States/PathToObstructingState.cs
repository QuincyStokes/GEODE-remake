using UnityEngine;

public class PathToObstructing : BaseEnemyState
{
    //maybe store a reference to the object in our way
    private GameObject obstructingObject;
    public override void EnterState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Entering PathToObstructing");
        //set running animation?
    }

    public override void ExitState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Exiting PathToObstructing");
    }

    public override void FixedUpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        //if there is a path to the core
            //change state to PathToCoreState

        //if the player is within a certain range, and there is.. line of sight to them?
            //mayb for now we just ignore player, BUT
            //change state to PathToPlayerState

        if(FlowFieldManager.Instance != null && FlowFieldManager.Instance.IsOnFlowField(owner.transform.position))
        {
            if(FlowFieldManager.Instance.GetFlowDirection(owner.transform.position) != Vector2.zero)
            {
                //this means there is now a path to the core, lets switch states
                stateMachine.ChangeState(new PathToCore());
            }
            else
            {
                //this means there is no path to the core. 
                //raycast a line from our position to the core
                    //first thing we hit, we shall target and attack
                if(obstructingObject == null)
                {
                    RaycastHit hit;
                    Vector3 dir = (owner.coreTransform.position - owner.transform.position).normalized;
                    float distance = Vector3.Distance(owner.transform.position, owner.coreTransform.position);

                    Physics.Raycast(owner.transform.position, dir, out hit, distance, owner.structureLayerMask);
                    if(hit.collider != null)
                    {
                        //we have found a target to attack
                        obstructingObject = hit.transform.gameObject;
                    }
                    else
                    {
                        //we really shouldn't ever get here, because we KNOW something is blocking the path, and we are raycasting through that path.
                            //but, in the case we don't hit anything... 
                            //just gonna debug for now?
                        Debug.Log("ENEMY ERROR. Enemy thinks there is no path, and also no obstructing object");
                    }
                }
                else
                {
                    //here means we know what our obstructing object is, now we can go attack it. 
                    Vector2 dir = (obstructingObject.transform.position - owner.transform.position).normalized;
                    owner.rb.MovePosition(owner.rb.position + dir * owner.movementSpeed * Time.deltaTime);

                }

            }
        }
    }

    public override void UpdateState(BaseEnemy owner, EnemyStateMachine stateMachine)
    {
        Debug.Log("Update for PathToObstructing");

    }

}
