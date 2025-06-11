using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewStructure", menuName = "Items/StructureItem")]
public class StructureItem : BaseItem
{
    public GameObject prefab;
    public int width;
    public int height;
    public override bool Use(Vector3 position, bool snapToGrid=true, bool force=false)
    {
        if(FlowFieldManager.Instance.HasCoreBeenPlaced() || Id==6 || force) 
        {
            if(Id == 6 ||force || Vector2.Distance(Core.CORE.transform.position, position) < Core.CORE.buildRadius)
            {
                for(int x = 0; x < width; x++)
                {
                    for(int y = 0; y < height; y++)
                    {
                        if(GridManager.Instance.IsPositionOccupied(new Vector3Int((int)position.x+(1*x), (int)position.y +(1*y), 0)))
                        {
                            return false;
                        }
                    }
                }
                if(snapToGrid)
                {
                    GridManager.Instance.PlaceObjectOnGridServerRpc(Id, new Vector3Int((int)position.x, (int)position.y, 0));
                    return true;
                }
                else
                {
                    WorldGenManager.Instance.PlaceObjectOffGridServerRpc(Id, position);
                    return true;
                }
            }
            else
            {
                Debug.Log($"Cannot place Item {Id}, Distance to Core: {Vector2.Distance(Core.CORE.transform.position, position)} is less than {Core.CORE.buildRadius}");
            }
            
            
        }
        return false;
    }

}
