using System.Collections;
using NUnit.Framework.Constraints;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Items/WeaponItem")]
public class WeaponItem : BaseItem
{

    public override bool Use(Vector3 position, bool snapToGrid = true)
    {

        //!TEMPORARY IMPLEMENTATION
        //just enable a hitbox thats attatched to the player
        //how do we where do we do the hitbox
            //its attatched to the player, how do we access it?
                //reference back to PlayerController, which will need some reference to thehitbox
                //i think this works fine for temporary
        PlayerController.Instance.Attack();
        
        return true;
    }


    
}
