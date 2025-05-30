using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    //This script shall handle all (or maybe most) logic for the crafting menu
    //including checking player inventory for crafting validation, current selected recipe, etc
    public static CraftingManager Instance {get; private set;}
    [Header("References")]
    public List<Slot> recipeDisplaySlots;
    public List<CraftingTab> craftingTabs;
    public Slot recipeResultSlot;
    
    public CraftingRecipe currentRecipe;
    public TMP_Text descriptionText;

    //* --------------------------- Events ----------------------
    public event Action<CraftingRecipe> OnItemCrafted;


    //need a few different important functions

    //-- actual crafting logic
    //CRAFT, craft a passed in item
    //CanCraft => bool, returns whether or not an item can be crafted

    //ui handling logic
    //onRecipeSelected => updates the currntRecipe, updates UI
    //UpdateCurrentRecipeUI
    private PlayerInventory playerInventory;


    private void Start()
    {
        Instance = this;
        playerInventory = GetComponentInParent<PlayerInventory>();
        foreach(Slot slot in recipeDisplaySlots)
        {
            slot.canInteract = false;
            slot.gameObject.SetActive(false);
        }
        recipeResultSlot.canInteract = false;
        
        foreach(CraftingTab ct in craftingTabs)
        {
            ct.DeselectTab();
        }
        craftingTabs[0].SelectTab();
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
            recipeDisplaySlots[i].SetItem(currentRecipe.materials[i].item.Id, currentRecipe.materials[i].amount, false);
        }

        recipeResultSlot.SetItem(currentRecipe.results[0].item.Id, currentRecipe.results[0].amount, false);
        descriptionText.text = currentRecipe.results[0].item.Description;
        Debug.Log($"Can Craft {currentRecipe.name}? => {CheckCanCraft(currentRecipe)}");
    }

    //item parameter so we can use it on any item, not just currentRecipe
    public bool CheckCanCraft(CraftingRecipe cr=null)
    {
        CraftingRecipe recipe;
        if(cr == null)
        {
            recipe = currentRecipe;
        }
        else
        {
            recipe = cr;
        }

        //should be this simple? i think?
        foreach(ItemAmount ia in recipe.materials)
        {
            if(!playerInventory.ContainsItem(ia.item))
            {
                return false;
            }
            if(playerInventory.GetItemCount(ia.item) < ia.amount)
            {
                return false;
            }
        }
        return true;
    }

    public void Craft()
    {
        //attempt to craft the current recipe
        if(CheckCanCraft(currentRecipe))
        {
            OnItemCrafted?.Invoke(currentRecipe);
            foreach (ItemAmount ia in currentRecipe.materials)
            {
                playerInventory.RemoveItemServerRpc(ia.item.Id, ia.amount);
            }

            foreach(ItemAmount ia in currentRecipe.results)
            {
                playerInventory.AddItemServerRpc(ia.item.Id, ia.amount);
            }
            UpdateRecipeUI();
        }
    }

}
