using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private List<TMP_Text> quests;

    [Header("Colors")]
    [SerializeField] private Color questCompletedColor;

    [Header("Player Scripts")]
    [SerializeField] private CraftingManager craftingManger;
    [SerializeField] private PlayerInventory playerInventory;

    private void Start()
    {
        craftingManger.OnItemCrafted += HandleItemCrafted;
        //Core.CORE.OnCorePlaced += HandleCorePlaced;
        playerInventory.OnItemUsed += HandleItemUsed;
        playerInventory.OnInventoryToggled += HandleInventoryOpened;

    }

    private void OnDestroy()
    {
        craftingManger.OnItemCrafted -= HandleItemCrafted;
        playerInventory.OnItemUsed -= HandleItemUsed;
        Core.CORE.OnCorePlaced -= HandleCorePlaced;
        playerInventory.OnInventoryToggled -= HandleInventoryOpened;
    }

    //* QUEST 1
    private void HandleInventoryOpened(bool opened)
    {
        CompleteQuest(0);
        playerInventory.OnInventoryToggled -= HandleInventoryOpened;
        

    }

    //* QUEST 2
    private void HandleCorePlaced()
    {
        CompleteQuest(1);
        Core.CORE.OnCorePlaced -= HandleCorePlaced;
    }

    //* QUEST 3
    private void HandleItemCrafted(CraftingRecipe cr)
    {
        if (cr.results[0].item.Name == "Stone Pickaxe")
        {
            CompleteQuest(2);
            craftingManger.OnItemCrafted -= HandleItemCrafted;
        }
    }

    //* QUEST 4
    private void HandleItemUsed(BaseItem item)
    {
        if (item.Type == ItemType.Structure && item.Name != "Core")
        {
            CompleteQuest(3);
            playerInventory.OnItemUsed -= HandleItemUsed;
        }
    }


    private void CompleteQuest(int questNum)
    {
        Debug.Log($"QUEST COMPLETED #{questNum}");
        quests[questNum].text = $"<s>{quests[questNum].text}</s>";
        quests[questNum].color = questCompletedColor;
    }
}
