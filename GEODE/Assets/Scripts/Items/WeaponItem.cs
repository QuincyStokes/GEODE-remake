using System.Collections;
using NUnit.Framework.Constraints;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Items/WeaponItem")]
public class WeaponItem : BaseItem
{
    [SerializeField] private float damage;
    [SerializeField] private ToolType toolType;
    public override bool Use(Vector3 position, bool snapToGrid = true, bool force = false)
    {
        PlayerController.Instance.Attack(damage, toolType, true);

        return true;
    }


    
}
