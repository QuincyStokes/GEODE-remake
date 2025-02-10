using UnityEngine;

[CreateAssetMenu(fileName = "NewMaterial", menuName = "Items/MaterialItem")]
public class MaterialItem : BaseItem
{
    public override bool Use()
    {
        Debug.Log("Material does not have a Use");
        return false;
    }

}
