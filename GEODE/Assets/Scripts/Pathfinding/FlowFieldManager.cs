using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class FlowFieldManager : NetworkBehaviour
{
    public static FlowFieldManager Instance;

    [Header("Flow Field Settings")]
    [SerializeField] private int fieldWidth;
    [SerializeField] private int fieldHeight;
    //private int cellSize = 1; dont need this since we're using 1x1, but maybe thatll change idk

    //  INTERNAL

    private Vector2Int flowFieldOrigin = Vector2Int.zero;
    private FlowCell[,] flowField;
    private bool hasCoreBeenPlaced;
    public event Action<Transform> corePlaced;
    public Transform coreTransform;

    public Tilemap debugTilemap;
    public Tile[] debugTiles;
    public Tile redXTile;


    //precalculating these to save overhead
    private static readonly Vector2Int[] neighborOffsets =
    {
        new Vector2Int(-1, 0),  //left
        new Vector2Int(1, 0),   //right
        new Vector2Int(0, -1),  //down
        new Vector2Int(0, 1),   //up
        // new Vector2Int(-1, -1), //down left
        // new Vector2Int(-1, 1),  //up left
        // new Vector2Int(1, -1),  //down right
        // new Vector2Int(1, 1)    //up right
    };

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        //initialize grid array of FlowCells
        flowField = new FlowCell[fieldWidth, fieldHeight];
        for(int x = 0; x < fieldWidth; x++)
        {
            for(int y = 0; y < fieldHeight; y++)
            {
                flowField[x,y] = new FlowCell()
                {
                    isWalkable=true,
                    cost=1,
                };
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if(!IsServer)
        {
            this.enabled = false;
        }
    }

    public Vector2Int WorldToFlowFieldPositionClamped(Vector3 worldPos)
    {
        float localX = worldPos.x - flowFieldOrigin.x; //could divide by cell size here, but we are using 1x1 so no need
        float localY = worldPos.y - flowFieldOrigin.y;

        int x = Mathf.FloorToInt(localX);
        int y = Mathf.FloorToInt(localY);

        //clamp coordinates to within boundary of the flow field, this does two things:
            //1. makes sure we don't get an out of bounds error
            //2. makes any enemy outside of the flow field follow the vector2 of the boundary tile.
        x = Mathf.Clamp(x, 0, fieldWidth-1);
        y = Mathf.Clamp(y, 0, fieldHeight-1);

        return new Vector2Int(x, y);
    }

    public Vector2 GetFlowDirection(Vector3 worldPos)
    {
        //convert position to our grid position
        Vector2Int cellPos = WorldToFlowFieldPositionClamped(worldPos);
        //return the flowDirection of that grid position;
        return flowField[cellPos.x, cellPos.y].flowDirection;
    }

    public bool IsOnFlowField(Vector3 position)
    {
        float localX = position.x - flowFieldOrigin.x; //could divide by cell size here, but we are using 1x1 so no need
        float localY = position.y - flowFieldOrigin.y;

        int x = Mathf.FloorToInt(localX);
        int y = Mathf.FloorToInt(localY);

        return localX >= 0 && localX < fieldWidth && localY >= 0 && localY < fieldHeight;

    }

    private bool IsInBounds(Vector2Int pos)
    {
        //check if the given position is valid on our flowfield.
        return pos.x >= 0 && pos.x < fieldWidth && pos.y >= 0 && pos.y < fieldHeight;
    }

    public void SetCorePosition(Transform transfo)
    {
        int px = Mathf.FloorToInt(transfo.position.x);
        int py = Mathf.FloorToInt(transfo.position.y);
        flowFieldOrigin = new Vector2Int(px - fieldWidth/2, py - fieldHeight/2);

        Debug.Log($"New Core position at {flowFieldOrigin}");
        hasCoreBeenPlaced = true;
        coreTransform = transfo;
        corePlaced?.Invoke(transfo);
    }

    public bool HasCoreBeenPlaced()
    {
        return hasCoreBeenPlaced;
    }

    //now the MEAT of this script
    /// <summary>
    /// This function takes in a core position, and calculates the flow field around it in our given width/height
    /// This will need to be re-called every time the player places a structure. Might sound yucky, but better than A* for 200 units every update :thumbs_up:
    /// </summary>
    public void CalculateFlowField()
    {
        if(!hasCoreBeenPlaced)
        {
            return;
        }

        //reset the direction and integration cost of each grid position
            //we clear it because we dont want old data every time we recalculate it
        for(int x = 0; x < fieldWidth; x++)
        {
            for (int y = 0; y < fieldHeight; y++)
            {
                flowField[x, y].integrationCost = 255;
                flowField[x, y].flowDirection = Vector2.zero;
                flowField[x, y].isWalkable = true;
            }
        }


        for (int x = 0; x < fieldWidth; x++)
        {
            for (int y = 0; y < fieldHeight; y++)
            {
                Vector3Int worldTile = new Vector3Int(
                    flowFieldOrigin.x + x,
                    flowFieldOrigin.y + y,
                    0
                );
                if (GridManager.Instance.IsPositionOccupied(worldTile))
                {
                    flowField[x, y].isWalkable = false;
                }
            }
        }

        //now we run BFS
            Queue<Vector2Int> queue = new Queue<Vector2Int>();

        Vector2Int centerPosition = new Vector2Int(fieldWidth/2, fieldHeight/2);
        //initialize goal cell
        flowField[centerPosition.x, centerPosition.y].integrationCost = 0;
        //queue.Enqueue(centerPosition);

        //chatgpt says to try this
        //Vector2Int coreCell = WorldToFlowFieldPositionClamped(new Vector3(corePosition.x, corePosition.y));
        //flowField[coreCell.x, coreCell.y].integrationCost = 0;
        queue.Enqueue(centerPosition);

        //BFS from the goal outward
        while(queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            FlowCell currentCell = flowField[current.x, current.y];

            //iterate through each of our current position's neighbors
            foreach(Vector2Int offset in neighborOffsets)
            {
                //up, left, right, down, diagonals, etc are added to position for neighbor
                Vector2Int neighborPos = current + offset;
                //if this neighbor position is within our flowfield
                if(IsInBounds(neighborPos))
                {
                    //grab the neighbor's flowCell data
                    FlowCell neighborCell = flowField[neighborPos.x, neighborPos.y];
                    //if the neighborcell isn't walkable, skip it.
                    if(!neighborCell.isWalkable)
                    {
                        continue;
                    }

                    //store the tentative cost (the cost of walking to the neighbor cell + our current integration cost)
                    byte tentativeCost = (byte)(currentCell.integrationCost + neighborCell.cost);

                    //if we've found a smaller cost (a shorter distance), make that our neighbor's new integration cost and add it to the queue
                    if(tentativeCost < neighborCell.integrationCost)
                    {
                        neighborCell.integrationCost = tentativeCost;
                        queue.Enqueue(neighborPos);
                    }
                }
            }
        }

        //now we have the integration cost for every cell.
        //now we need to compute all of their flow directions
        debugTilemap.ClearAllTiles();
        for(int x = 0; x < fieldWidth; x++)
        {
            for (int y = 0; y < fieldHeight; y++)
            {
                //if the current position isn't walkable, skip it
                if (!flowField[x, y].isWalkable)
                {
                    debugTilemap.SetTile(new Vector3Int(x + flowFieldOrigin.x, y + flowFieldOrigin.y, 0), redXTile);
                    continue;
                    
                }

                //initialize our best direction, set to 0
                Vector2 bestDir = Vector2.zero;
                //grab our current cost, beacuse this is currently our best. Goal is to beat this
                byte bestCost = flowField[x, y].integrationCost;

                //loop through each neighbor.
                foreach (Vector2Int offset in neighborOffsets)
                {
                    //neighbor position up down left right etc
                    Vector2Int neighborPos = new Vector2Int(x + offset.x, y + offset.y);

                    //if the position is within bounds and it's a walkable cell
                    if (IsInBounds(neighborPos) && flowField[neighborPos.x, neighborPos.y].isWalkable)
                    {
                        //grab the new neighbor's integration cost, we will use this to compare vs current cost
                        byte neighborCost = flowField[neighborPos.x, neighborPos.y].integrationCost;
                        //IF the new cost is less than our best cost, we want this position's direction to point INTO the neighbor
                        if (neighborCost < bestCost)
                        {
                            bestCost = neighborCost;

                            Vector2 dir = (Vector2)neighborPos - (Vector2)new Vector2Int(x, y);
                            bestDir = dir.normalized;
                        }
                    }
                }
                //finally, set our current position's flow direction towards the cheapest neighbor
                //Debug.Log($"Placing flow direction at ({x}, {y}) with direction {bestDir}");
                flowField[x, y].flowDirection = bestDir;

                //debug tilemap
                if (bestDir == neighborOffsets[0])
                {
                    debugTilemap.SetTile(new Vector3Int(x + flowFieldOrigin.x, y + flowFieldOrigin.y, 0), debugTiles[0]);
                }
                else if (bestDir == neighborOffsets[1])
                {
                    debugTilemap.SetTile(new Vector3Int(x + flowFieldOrigin.x, y + flowFieldOrigin.y, 0), debugTiles[1]);
                }
                else if (bestDir == neighborOffsets[2])
                {
                    debugTilemap.SetTile(new Vector3Int(x + flowFieldOrigin.x, y + flowFieldOrigin.y, 0), debugTiles[2]);
                }
                else if (bestDir == neighborOffsets[3])
                {
                    debugTilemap.SetTile(new Vector3Int(x + flowFieldOrigin.x, y + flowFieldOrigin.y, 0), debugTiles[3]);
                }
            }
        }
    }


}
