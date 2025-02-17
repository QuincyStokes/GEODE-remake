using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float moveSpeed = 2f;

    void Update()
    {
        // Get the flow direction from the FlowField at our current position
        Vector2 flowDir = FlowFieldManager.Instance.GetFlowDirection(transform.position);

        // Move in that direction
        Vector3 velocity = (Vector3)flowDir * moveSpeed * Time.deltaTime;
        transform.position += velocity;
        
    }
}
