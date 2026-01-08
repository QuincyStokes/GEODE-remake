using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;

/// <summary>
/// </summary>
public class QuestUI : MonoBehaviour
{

    //!RIGHT NOW QUESTS ARE HARD CODED. THIS IS BAD. But, the cost to reward ratio for implementing an entire system to work dynamically didn't feel worth it. 
    [Header("Quest Chapters")]


    [Header("References")]
    [SerializeField] private List<TMP_Text> questTexts;
    [SerializeField] private Transform questBackground;
    [SerializeField] private Transform questBackgroundParent;
    [SerializeField] private TMP_Text questChapterTitle;
    [SerializeField] private List<QuestChapter> questChapters;

    [Header("Colors")]
    [SerializeField] private Color questCompletedColor;
    private Color questNotCompleteColor;

    [Header("Player Scripts")]
    [SerializeField] private CraftingManager craftingManger;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerUIInteraction playerUIInteraction;
    //* ----------- Internal ---------- *//
    private QuestChapter currentQuestChapter;
    private int currentQuestChapterIndex;
    private int completedQuests;
    private int dayNum = 1;

    private void Start()
    {
        if (!playerInventory.IsOwner)
        {
            enabled = false;
            gameObject.SetActive(false);
            return;
        }

        ResetQuests();

        craftingManger.OnItemCrafted += HandleItemCrafted;
        playerInventory.OnItemUsed += HandleItemUsed;
        playerInventory.OnInventoryToggled += HandleInventoryOpened;
        FlowFieldManager.Instance.corePlaced += HandleCorePlaced;
        playerUIInteraction.OnObjectInteracted += HandleObjectInteracted;
        DayCycleManager.Instance.becameDay += HandleBecameDay;

        questNotCompleteColor = questTexts[0].color;


        LoadQuestChapter(questChapters[0]);
        currentQuestChapterIndex = 0;
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
        CompleteQuest(0, 0); //Press E to open inventory
        playerInventory.OnInventoryToggled -= HandleInventoryOpened;
    }

    //* QUEST 2
    private void HandleCorePlaced(Transform t)
    {
        CompleteQuest(0, 3); //Place your core
        FlowFieldManager.Instance.corePlaced -= HandleCorePlaced;

    }

    //* QUEST 3
    private void HandleItemCrafted(CraftingRecipe cr)
    {
        if (cr.results[0].item.Name == "Stone Pickaxe")
        {
            CompleteQuest(0, 1); // Craft a stone pickaxe
            
        }
        if (cr.results[0].item.Type == ItemType.Structure)
        {
            CompleteQuest(0, 2); //Craft a structure
        }
        if (cr.results[0].item.Id == 64)
        {
            CompleteQuest(1, 3); // Craft the crystal refinery
        }

    }

    //* QUEST 4
    private void HandleItemUsed(BaseItem item)
    {
        if (item.Type == ItemType.Structure && item.Id != 6) //make sure it's not the core
        {
            CompleteQuest(1, 0); // Place a structure.
            playerInventory.OnItemUsed -= HandleItemUsed;
        }
    }

    private void HandleObjectInteracted(IInteractable interactable)
    {
        CompleteQuest(1, 1);
        playerUIInteraction.OnObjectInteracted -= HandleObjectInteracted;

    }

    private void HandleBecameDay()
    {
        Debug.Log($"Quest hears BecameDay, finished day {dayNum}");
        dayNum++;
        if(dayNum == 2)
        {
            CompleteQuest(1, 2); //this means we beat night 1
        }
    }


    private void CompleteQuest(int questChapter, int questNum)
    {
        if(questChapter == currentQuestChapterIndex)
        {
            questTexts[questNum].text = $"<s>{questTexts[questNum].text}</s>";
            questTexts[questNum].color = questCompletedColor;
        }
        
        questChapters[questChapter].quests[questNum].Complete();

        //check if we've completed the current questChapter
        foreach(Quest q in questChapters[currentQuestChapterIndex].quests)
        {
            if(!q.isComplete)
            {
                return;
            }
        }
        //if we're here, we've completed the current chapter
        if(currentQuestChapterIndex >= questChapters.Count-1)
        {
            StartCoroutine(QuestsComplete());
        }
        else
        {
            currentQuestChapterIndex++;
            LoadQuestChapter(questChapters[currentQuestChapterIndex]);
        }
        
    }

    private IEnumerator QuestsComplete()
    {

        float elapsed = 0f;
        yield return new WaitForSeconds(2);
        while (elapsed <= 3)
        {
            elapsed += Time.deltaTime;
            questBackgroundParent.position = new Vector3(questBackgroundParent.position.x + 5, questBackgroundParent.position.y, 0);
            yield return null;
        }
        gameObject.SetActive(false);
    
    }

    private void LoadQuestChapter(QuestChapter qc)
    {
        questChapterTitle.text = qc.chapterName;
        for(int i = 0; i < questTexts.Count; i++)
        {
            if(qc.quests[i] == null)
            {
                questTexts[i].text = "";
                continue;
            }
            

            if(qc.quests[i].isComplete)
            {
                questTexts[i].text = $"<s>{qc.quests[i].description}</s>";
                questTexts[i].color = questCompletedColor;
            }
            else
            {
                questTexts[i].text = qc.quests[i].description;
                questTexts[i].color = questNotCompleteColor;
            }
        }

        currentQuestChapter = qc;
    }

    private void ResetQuests()
    {
        for (int i = 0; i < questChapters.Count; i++)
        {
            for(int j = 0; j < questChapters[i].quests.Length; j++)
            {
                questChapters[i].quests[j].Incomplete();
            }
        }
    }

}
