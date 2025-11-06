using UnityEngine;

[CreateAssetMenu(fileName = "NewRelic", menuName = "Items/RelicItem")]

public class RelicItem : BaseItem
{
    public override bool Use(Vector3 position, bool snapToGrid = true, bool force = false)
    {
        //Relics dont have uses
        return false;
    }
}
