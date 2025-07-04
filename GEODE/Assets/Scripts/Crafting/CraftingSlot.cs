using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

public class CraftingSlot : Slot
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public CraftingRecipe craftingRecipe;
    private CraftingManager craftingManager;
    public void InitializeRecipeSlot(CraftingRecipe cr, CraftingManager cm)
    {
        craftingRecipe = cr;
        craftingManager = cm;
        SetItem(cr.results[0].item.Id, cr.results[0].amount, interactable: false);
        itemSprite.preserveAspect = true;
        cm.OnRecipeCheck += CheckCanCraft;
    }

    public override void HandleLeftClick()
    {
        //set the crafting menu's current recipe
        craftingManager.SetRecipe(craftingRecipe);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Slot entered.");
        TooltipService.Instance.RequestShow(this);


    }

    private void CheckCanCraft()
    {
        if (craftingManager.CheckCanCraft(craftingRecipe))
        {
            SetSlotHighlight(true);
        }
        else
        {
            SetSlotHighlight(false);
        }
    }

    private void OnDestroy()
    {
        craftingManager.OnRecipeCheck -= CheckCanCraft;
    }


}
