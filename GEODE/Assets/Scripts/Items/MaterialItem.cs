using UnityEngine;

[CreateAssetMenu(fileName = "NewMaterial", menuName = "Items/MaterialItem")]
public class MaterialItem : BaseItem
{
    public override bool Use(Vector3 position, bool snapToGrid=true)
    {
        Debug.Log("Material does not have a Use");
        return false;
    }

}
