using UnityEngine;

[CreateAssetMenu(fileName = "NewStructure", menuName = "Items/StructureItem")]
public class StructureItem : BaseItem
{
    [SerializeField] public GameObject prefab;
    [SerializeField] private int width;
    [SerializeField] private int height;
    public override bool Use(Vector3 position)
    {
        for(int x = 0; x < width; x++)
        {
            for(int y = 0; y < height; y++)
            {
                if(GridManager.Instance.IsPositionOccupied(new Vector3Int((int)position.x+(1*x), (int)position.y +(1*y), 0)))
                {
                    Debug.Log($"Collided with something at {position.x} + {1*x}, {position.y} + {1*y}");
                    return false;
                }
            }
        }
        GridManager.Instance.PlaceObjectServerRpc(Id, new Vector3Int((int)position.x, (int)position.y, 0));
        return true;
    }

}
