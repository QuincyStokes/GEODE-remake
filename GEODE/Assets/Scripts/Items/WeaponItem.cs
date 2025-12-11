using System.Collections;
using NUnit.Framework.Constraints;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Items/WeaponItem")]
public class WeaponItem : BaseItem
{
    public float damage;
    public ToolType toolType;
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
