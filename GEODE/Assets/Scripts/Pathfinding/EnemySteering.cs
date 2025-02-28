using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public class EnemySteering
{
    [Header("Steering Settings")]
    public float checkInterval = 0.2f;
    public float rayDistance = .5f;
    public float steeringAngle = 30f;

    private float nextCheckTime;
    private Vector2 lastSteeredDirection = Vector2.zero;

    public Vector2 GetSteeredDirection(BaseEnemy owner, Vector2 desiredDir)
    {
        //if we've elapsed our time interval, this keeps performance up
        if(Time.time > nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;

            //Do the raycasts
            lastSteeredDirection = PerformSteerCheck(owner, desiredDir);
        }
        //return the direction from the most recent check
        return lastSteeredDirection;
    }


    private Vector2 PerformSteerCheck(BaseEnemy owner, Vector2 desiredDir)
    {
        if(desiredDir.sqrMagnitude < .0001f)
        {
            return Vector2.zero;
        }

        //Raycast forward
        Vector2 origin = owner.transform.position;

        Vector2 forward = desiredDir.normalized;
        Vector2 slightLeft = Rotate(forward, steeringAngle/2);
        Vector2 slightRight = Rotate(forward, -steeringAngle/2);

        RaycastHit2D slightLeftHit = Physics2D.Raycast(origin, slightLeft, rayDistance, owner.structureLayerMask);
        RaycastHit2D slightRightHit = Physics2D.Raycast(origin, slightRight, rayDistance, owner.structureLayerMask);
        Debug.DrawLine(origin, origin+slightLeft * rayDistance, Color.red, checkInterval);
        Debug.DrawLine(origin, origin+slightRight * rayDistance, Color.red, checkInterval);

        //this means there is no obstacle infront of us, just go straight
        if(slightLeftHit.collider == null && slightRightHit.collider == null)
        {
            Debug.Log("No obstacle found.");
            return desiredDir;
        }

        Vector2 steerLeft = Rotate(forward, steeringAngle);
        Vector2 steerRight = Rotate(forward, -steeringAngle);

        RaycastHit2D leftHit = Physics2D.Raycast(origin, steerLeft, rayDistance, owner.structureLayerMask);
        RaycastHit2D rightHit = Physics2D.Raycast(origin, steerRight, rayDistance, owner.structureLayerMask);

        Debug.DrawLine(origin, origin + steerLeft * rayDistance, Color.yellow, checkInterval);
        Debug.DrawLine(origin, origin + steerRight * rayDistance, Color.yellow, checkInterval);

        bool leftBlocked = leftHit.collider != null;
        bool rightBlocked = rightHit.collider != null;

        //if left is open and right isn't, go left
        if(!leftBlocked && rightBlocked)
        {
            return steerLeft;
        }//if right is open
        else if (!rightBlocked && leftBlocked)
        {
            return steerRight;
        } //if both are open
        else if (!leftBlocked && !rightBlocked)
        {
            //could do either, this'll probably change
            return desiredDir;
        }
        else
        {
            return desiredDir; //this means forward, left, and right are all blocked.
        }

    }

    private Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);

        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }
}
