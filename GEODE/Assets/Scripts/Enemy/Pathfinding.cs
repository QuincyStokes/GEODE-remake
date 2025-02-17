using System.Collections.Generic;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
    //we need something to represent our grid
        //CURRENT IDEA, Dictionary
            //this will reduce overhead, but maybe a little bit more difficult to implement
    


    // {(x, y), <PathNode>}

    public Pathfinding()
    {
        grid = new Dictionary<Vector2Int, PathNode>();
    }



    private const int MOVE_STRAIGHT_COST = 10;
    private const int MOVE_DIAGONAL_COST = 14;

    private Dictionary<Vector2Int, PathNode> grid;
    private List<Vector2Int> openList;
    private List<Vector2Int> closedList;

    private void Start()
    {
   
    }

    private void BuildGrid()
    {

    }

    public List<Vector2Int> FindPath(Vector2Int startPos, Vector2Int endPos)
    {
        if(!grid.ContainsKey(startPos))
        {
            grid.Add(startPos, new PathNode());
        }

        if(!grid.ContainsKey(endPos))
        {
            grid.Add(endPos, new PathNode());
        }
         //now we have our start position as a value on our grid
        
        //start with only the starting position in our openList
        //this is our "queue' of PathNodes to follow
        openList = new List<Vector2Int> {startPos};

        //ClosedList initially empty, we will fill
        closedList = new List<Vector2Int>();

        //need to initialize our Grid (dictionary)
            //so here in the video, he initializes the entire grid size into the dictionary
            //i think in a perfect world, we wouldn't do that. If anything we could only load all of the positions within a certain distance of the enemy
            //COME BACK HERE LATER TO RETHINK
        for(int x = 0; x < WorldGenManager.Instance.WorldSizeX; x++)
        {
            for(int y = 0; y < WorldGenManager.Instance.WorldSizeY; y++)
            {
                if(!grid.ContainsKey(new Vector2Int(x, y)))
                {
                    grid.Add(new Vector2Int(x, y), new PathNode());
                }
            }
        }

        //initialize the values for our start position
        grid[startPos].gCost = 0;
        grid[startPos].hCost = CalculateDistanceCost(startPos, endPos);
        grid[startPos].CalculateFCost();


        //now we begin the actual cycle
            //loop while we stil have nodes to visit from our openList
        while(openList.Count > 0)
        {
            //first, get the node from our OpenList with the lowest fCost
                // (this is basically the node closest to the end goal)
            Debug.Log(openList.Count);
            Vector2Int currentNodePos = GetLowestFCostNode(openList);

            //base case of some sort, if our current node is the same as the end, we've reached our destination!
            if(currentNodePos == endPos)
            {
                return CalculatePath(endPos);
            }
            
            //we have searched this node now, remove it from the open list
            openList.Remove(currentNodePos);
            //and add it to the closed list!
            closedList.Add(currentNodePos);

            //cycle through the neighbors of our current node;
            foreach(Vector2Int neighborNodePos in GetNeighborList(currentNodePos))
            {
                if (closedList.Contains(neighborNodePos))
                {
                    continue;
                }
                if(grid[neighborNodePos].isWalkable == false)
                {
                    closedList.Add(neighborNodePos);
                }
                int tentativeGCost = grid[currentNodePos].gCost + CalculateDistanceCost(currentNodePos, neighborNodePos);
                if(tentativeGCost < grid[neighborNodePos].gCost)
                {
                    grid[neighborNodePos].previous = currentNodePos;
                    grid[neighborNodePos].gCost = tentativeGCost;
                    grid[neighborNodePos].hCost = CalculateDistanceCost(neighborNodePos, endPos);
                    grid[neighborNodePos].CalculateFCost();

                    if(!openList.Contains(neighborNodePos))
                    {
                        openList.Add(neighborNodePos);
                    }
                }
            }

        }

        //if we're here, that means we are out of open nodes on the list.
        return null;
    }

    private List<Vector2Int> GetNeighborList(Vector2Int currentNodePos)
    {
        List<Vector2Int> neighborList = new List<Vector2Int>();
        //need to return a list of all 8 nodes surrounding this one (8 since we can go diagonal)
        if(currentNodePos.x - 1 >= 0)
        {
            //Left
            neighborList.Add(new Vector2Int(currentNodePos.x - 1, currentNodePos.y));

            //Left Down
            if(currentNodePos.y - 1 >= 0)
            {
                neighborList.Add(new Vector2Int(currentNodePos.x - 1, currentNodePos.y - 1));
            }

            //Left Up
            if(currentNodePos.y + 1 < WorldGenManager.Instance.WorldSizeY)
            {
                neighborList.Add(new Vector2Int(currentNodePos.x - 1, currentNodePos.y + 1));
            }
        }
        if(currentNodePos.x + 1 < WorldGenManager.Instance.WorldSizeX)
        {
            //Right
            neighborList.Add(new Vector2Int(currentNodePos.x + 1, currentNodePos.y));

            //Right Down
            if(currentNodePos.y - 1 >= 0)
            {
                neighborList.Add(new Vector2Int(currentNodePos.x + 1, currentNodePos.y - 1));
            }
            
            //Right Up
            if(currentNodePos.y + 1 < WorldGenManager.Instance.WorldSizeY)
            {
                neighborList.Add(new Vector2Int(currentNodePos.x + 1, currentNodePos.y + 1));
            }
        }
        //Down
        if(currentNodePos.y - 1 >= 0)
        {
            neighborList.Add(new Vector2Int(currentNodePos.x, currentNodePos.y - 1));
        }
        //up
        if(currentNodePos.y + 1 < WorldGenManager.Instance.WorldSizeY)
        {
            neighborList.Add(new Vector2Int(currentNodePos.x, currentNodePos.y + 1));
        }
        return neighborList;

    }

    private List<Vector2Int> CalculatePath(Vector2Int endPos)
    {
        //given our end position, we are essentially tracing back each previous node, like a linked list
            //then if we reverse our traced path, we will have the path from start to end!

        //begin with our end node
        List<Vector2Int> path = new List<Vector2Int>{ endPos };
        Vector2Int currentNodePos = endPos;
        //look at current node, get it's previous node, and then set previous to current.
        //this is tracing back the path we took.
        while(grid[currentNodePos].previous != new Vector2Int(int.MaxValue, int.MaxValue))
        {
            path.Add(grid[currentNodePos].previous);
            currentNodePos = grid[currentNodePos].previous;
        }
        path.Reverse();
        return path;
    }

    private int CalculateDistanceCost(Vector2Int a, Vector2Int b)
    {
        int xDistance = (int)Mathf.Abs(a.x - b.x);
        int yDistance = (int)Mathf.Abs(a.y - b.y);
        int remaining = Mathf.Abs(xDistance - yDistance);
        //some weird math stuff goin on, but this just calculates the shortest distance from A to B *directly* (ignoring obstacles)
        return MOVE_DIAGONAL_COST * Mathf.Min(xDistance, yDistance) + MOVE_STRAIGHT_COST * remaining;
    }

    private Vector2Int GetLowestFCostNode(List<Vector2Int> pathNodeList)
    {
        //out of a list of positions, find the one with the lowest fCost
        Vector2Int lowestFCostNodePos = pathNodeList[0];
        for(int i = 1; i < pathNodeList.Count; i++)
        {
            if(grid[pathNodeList[i]].fCost < grid[lowestFCostNodePos].fCost)
            {
                lowestFCostNodePos = pathNodeList[i];
            }
        }
        return lowestFCostNodePos;
    }
}
