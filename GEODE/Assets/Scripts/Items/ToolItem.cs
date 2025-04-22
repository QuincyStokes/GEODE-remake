using UnityEngine;


[CreateAssetMenu(fileName = "NewTool", menuName = "Items/ToolItem")]
public class ToolItem : BaseItem
{
    public override bool Use(Vector3 position, bool snapToGrid = true, bool force = false)
    {
        Debug.Log("Tool does not have a use yet.");
        return false;
    }
}
