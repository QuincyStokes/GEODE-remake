using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;

public class QuestUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private List<TMP_Text> quests;
    [SerializeField] private Transform questBackground;

    [Header("Colors")]
    [SerializeField] private Color questCompletedColor;

    [Header("Player Scripts")]
    [SerializeField] private CraftingManager craftingManger;
    [SerializeField] private PlayerInventory playerInventory;
    private int completedQuests;

    private void Start()
    {
        if (!playerInventory.IsOwner)
        {
            enabled = false;
            gameObject.SetActive(false);
            return;
        }
        craftingManger.OnItemCrafted += HandleItemCrafted;
        playerInventory.OnItemUsed += HandleItemUsed;
        playerInventory.OnInventoryToggled += HandleInventoryOpened;
        FlowFieldManager.Instance.corePlaced += HandleCorePlaced;

    }

    private void OnDestroy()
    {
        craftingManger.OnItemCrafted -= HandleItemCrafted;
        playerInventory.OnItemUsed -= HandleItemUsed;
        playerInventory.OnInventoryToggled -= HandleInventoryOpened;
        FlowFieldManager.Instance.corePlaced -= HandleCorePlaced;
    }

    //* QUEST 1
    private void HandleInventoryOpened(bool opened)
    {
        CompleteQuest(0);
        playerInventory.OnInventoryToggled -= HandleInventoryOpened;
    }

    //* QUEST 2
    private void HandleCorePlaced(Transform t)
    {
        CompleteQuest(1);
        FlowFieldManager.Instance.corePlaced -= HandleCorePlaced;

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
        completedQuests++;

        if (completedQuests >= 4)
        {
            StartCoroutine(QuestsComplete());
        }
    }

    private IEnumerator QuestsComplete()
    {

        float elapsed = 0f;
        yield return new WaitForSeconds(2);
        while (elapsed <= 3)
        {
            elapsed += Time.deltaTime;
            questBackground.position = new Vector3(questBackground.position.x + 5, questBackground.position.y, 0);
            yield return null;
        }
        gameObject.SetActive(false);
    
    }
}
