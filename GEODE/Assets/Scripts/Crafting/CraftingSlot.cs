using UnityEngine;

public class CraftingSlot : Slot
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public CraftingRecipe craftingRecipe;
    private CraftingManager craftingManager;
    public void InitializeRecipeSlot(CraftingRecipe cr, CraftingManager cm)
    {
        craftingRecipe = cr;
        craftingManager = cm;
        SetItem(cr.results[0].item.Id, cr.results[0].amount, false);
    }

    public override void HandleLeftClick()
    {
        //set the crafting menu's current recipe
        craftingManager.SetRecipe(craftingRecipe);
    }
}
