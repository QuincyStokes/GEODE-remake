using UnityEngine;


[CreateAssetMenu(fileName = "NewTool", menuName = "Items/ToolItem")]
public class ToolItem : BaseItem
{
    [SerializeField] public float damage;
    [SerializeField] public ToolType toolType;

    public override bool Use(Vector3 position, bool snapToGrid = true, bool force = false)
    {
        PlayerController controller = PlayerController.GetLocalPlayerController();
        if (controller != null)
        {
            controller.Combat.PerformAttack(damage, toolType, true);
        }
        return true;
    }
}


public enum ToolType
{
    None,
    Pickaxe,
    Axe,
    Shovel,
    Hammer,
    Sword
}
