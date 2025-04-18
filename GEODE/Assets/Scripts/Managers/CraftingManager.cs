using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    //This script shall handle all (or maybe most) logic for the crafting menu
    //including checking player inventory for crafting validation, current selected recipe, etc
    public static CraftingManager Instance;
    [Header("References")]
    public List<Slot> recipeDisplaySlots;
    public Slot recipeResultSlot;
    
    public CraftingRecipe currentRecipe;


    //need a few different important functions

    //-- actual crafting logic
    //CRAFT, craft a passed in item
    //CanCraft => bool, returns whether or not an item can be crafted

    //ui handling logic
    //onRecipeSelected => updates the currntRecipe, updates UI
    //UpdateCurrentRecipeUI


    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        foreach(Slot slot in recipeDisplaySlots)
        {
            slot.canInteract = false;
            slot.gameObject.SetActive(false);
        }
        recipeResultSlot.canInteract = false;
    }

    public void SetRecipe(CraftingRecipe cr)
    {
        currentRecipe = cr;
        //update UI
        UpdateRecipeUI();
    }

    private void UpdateRecipeUI()
    {
        //turn off all slots
        foreach(Slot slot in recipeDisplaySlots)
        {
            slot.gameObject.SetActive(false);
        }

        //for each material in the recipe, enable a slot, and set its contents
        for(int i = 0; i < currentRecipe.materials.Count && i < recipeDisplaySlots.Count; i++)
        {
            recipeDisplaySlots[i].gameObject.SetActive(true);
            recipeDisplaySlots[i].SetItem(currentRecipe.materials[i].item.Id, currentRecipe.results[0].amount, false);
        }

        recipeResultSlot.SetItem(currentRecipe.results[0].item.Id, currentRecipe.results[0].amount, false);
    }

    public void CheckCanCraft()
    {

    }

    public void Craft()
    {
        //attempt to craft the current recipe
    }

}
