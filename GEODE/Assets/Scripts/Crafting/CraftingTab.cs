using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.UI;

public class CraftingTab : MonoBehaviour
{
    [Header("Properties")]
    public List<CraftingRecipe> recipes;

    [Header("References")]
    public Transform recipeSlotsHolder;
    public GameObject craftingSlotPrefab;
    public List<CraftingTab> allCraftingTabs;
    public Sprite activeTabSprite;
    public Sprite deactiveTabSprite;
    public GameObject tabActiveLowerSprite;
    public Image tabSprite;

    private void Awake()
    {
       InitializeRecipeSlots(); 
    }

    private void Start()
    {
        
    }

    private void InitializeRecipeSlots()
    {
        foreach (CraftingRecipe cr in recipes)
        {
            GameObject slot = Instantiate(craftingSlotPrefab);
            CraftingSlot cs = slot.GetComponent<CraftingSlot>();

            cs.InitializeRecipeSlot(cr);
            slot.transform.SetParent(recipeSlotsHolder, false);
        }
    }

    public void OnTabPressed()
    {
        //deselect all tabs
        foreach(CraftingTab ct in allCraftingTabs)
        {
            ct.DeselectTab();
            
        }

        //select THIS tab
        SelectTab();

    }

    public void DeselectTab()
    {
        tabSprite.sprite = deactiveTabSprite;
        tabActiveLowerSprite.SetActive(false);
        recipeSlotsHolder.gameObject.SetActive(false);
    }

    public void SelectTab()
    {
        tabSprite.sprite = activeTabSprite;
        tabActiveLowerSprite.SetActive(true);
        recipeSlotsHolder.gameObject.SetActive(true);
    }
}
