using UnityEngine;

public class FlowCell 
{
    public bool isWalkable = true; //is this cell walkable
    public byte cost = 1; //cost to traverse this cell
    public byte integrationCost = 255; //initial cost to reach the goal
    public Vector2 flowDirection = Vector2.zero; //direction to move on this cell

}
