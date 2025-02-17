using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class PathfindTester : MonoBehaviour
{
    private Pathfinding pathfinding;

    private void Start()
    {
        pathfinding = new Pathfinding();
    }

    private void Update()
    {
        if(Input.GetMouseButton(0))
        {
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            List<Vector2Int> path = pathfinding.FindPath(new Vector2Int(0, 0), new Vector2Int((int)mouseWorldPosition.x, (int)mouseWorldPosition.y));

            if(path != null)
            {
               
                for(int i = 0; i < path.Count - 1; i++)
                {
                    Debug.Log($"Position: ({path[i].x}, {path[i].y})");
                    Debug.DrawLine(new Vector3(path[i].x, path[i].y), new Vector3(path[i+1].x, path[i+1].y), new Color(1,0,0), 20f, depthTest:false);
                }
            }
            else
            {
                Debug.Log("Path is null");
            }
        }
    }
}
