using UnityEngine;

public class PathNode
{
    public PathNode()
    {
        this.gCost = int.MaxValue;
        this.hCost = int.MaxValue;
        this.fCost = int.MaxValue;
        this.previous = new Vector2Int(int.MaxValue, int.MaxValue);
        isWalkable = true;
    }
    public int gCost; //Walking cost from the start node
    public int hCost; //Heuristic cost to reach end node
    public int fCost; //g + h
    public Vector2Int previous;
    public bool isWalkable;

    public void CalculateFCost()
    {
        fCost = gCost + hCost;
    }
}
