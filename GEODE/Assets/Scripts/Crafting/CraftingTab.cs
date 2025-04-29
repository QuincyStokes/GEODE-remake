using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.UI;

public class CraftingTab : MonoBehaviour
{
    [Header("Properties")]
    public TabRecipies tabRecipes;

    [Header("References")]
    public Transform recipeSlotsHolder;
    public GameObject craftingSlotPrefab;
    public List<CraftingTab> allCraftingTabs;
    public Sprite activeTabSprite;
    public Sprite deactiveTabSprite;
    public GameObject tabActiveLowerSprite;
    public Image tabSprite;


    //INTERNAL PRIVATE
    private CraftingManager craftingManager;

    private void Awake()
    {
       
    }

    private void Start()
    {
        craftingManager = GetComponentInParent<CraftingManager>();
        InitializeRecipeSlots(); 
    }

    private void InitializeRecipeSlots()
    {
        foreach (CraftingRecipe cr in tabRecipes.recipies)
        {
            GameObject slot = Instantiate(craftingSlotPrefab);
            CraftingSlot cs = slot.GetComponent<CraftingSlot>();

            cs.InitializeRecipeSlot(cr, craftingManager);
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
