using UnityEngine;

[CreateAssetMenu(fileName = "NewMaterial", menuName = "Items/MaterialItem")]
public class StructureItem : BaseItem
{
    [SerializeField] private GameObject prefab;
    public override bool Use()
    {
    //we need 3 things to send to the gridmanager
        //1. the object we are going to spawn <- easy
        //2. where we are going to (want to) spawn it <- medium
            //get the mouse's position.
            //turn it into world coordinates. (is this the same as grid coords?)
            //turn *that* into grid position, profit.
        //3  rotation.  <- ???
        //Vector2 mousePos = InputHandler.
        return false;
    }

}
