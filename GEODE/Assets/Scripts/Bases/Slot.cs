using System;
using TMPro;
using Unity.IO.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class Slot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] protected Image backgroundSprite;
    [SerializeField] protected Image itemSprite;
    [SerializeField] protected TMP_Text itemCount; 
    [HideInInspector] protected InventoryHandUI inventoryHand;
    [SerializeField] protected GameObject tooltip;

    [Header("Tooltip References")]
    [SerializeField] protected TMP_Text tooltipItemName;
    [SerializeField] protected TMP_Text tooltipItemDescription;
    [SerializeField] protected TMP_Text tooltipItemType;
    [SerializeField] protected TMP_Text tooltipItemStats;
    [SerializeField] protected TMP_Text tooltipItemQuality;
    

    [Header("Background Images")]
    [SerializeField] protected Sprite selectedBackgroundImage;
    [SerializeField] protected Sprite deselectedBackgroundImage;


    [Header("Settings")]
    [SerializeField] protected int maxStackSize;
    protected BaseItem item; //item this slot is holding
    protected Sprite icon;
    protected int count;
    public bool canInteract;
    [HideInInspector] public Transform parentAfterDrag;

    //----------
    //PLAYERS "HAND"
    //----------

    protected static BaseItem heldItem = null;
    protected static int heldCount = 0;
    

    private void Update()
    {
        
    }

    public void InitializeHand(InventoryHandUI hand)
    {
        inventoryHand = hand;
    }

    public virtual void SetItem(int id=-1, int newCount = 1, bool interactable=true)
    {
        //set the internal item data
        if(id != -1)
        {
            
            item = ItemDatabase.Instance.GetItem(id);
            itemSprite.sprite = item.Icon;
            itemSprite.color = new Color(1, 1, 1, 1);
        }
        else
        {
            itemSprite.color = new Color(1, 1, 1, 0);
        }

        count = newCount;
        if(count > 1)
        {
            itemCount.text = count.ToString();
        }
        else
        {
            itemCount.text = "";
        }
        canInteract = interactable;
        if(item != null)
        {
            BuildTooltip();
        }
        //set the UI to match
        CheckItemDepleted();
    }

    public BaseItem GetItemInSlot()
    {
        return item;
    }

    public void UpdateCountUI()
    {
        if(count > 1)
        {
            itemCount.text = count.ToString();
        }
        else
        {
            itemCount.text = "";
        }
       
    }

    public void AddCount(int newCount=1)
    {
        count += newCount;
        UpdateCountUI();
    }

    public void SubtractCount(int newCount=1)
    {
        if(newCount <= count)
        {
            count -= newCount;
            CheckItemDepleted();
            UpdateCountUI();
        }
        else
        {
            Debug.Log($"Cannot remove that many {item.name}, {newCount} > {count}");
        }
        
    }

    public void SetCount(int newCount)
    {
        count = newCount;
        UpdateCountUI();
    }
    public int GetCount()
    {
        return count;
    }

    protected void CheckItemDepleted()
    {
        if(count < 1)
        {
            
            //this means we have no more of that item.
            EmptySlot();
        } 
    }

    protected void EmptySlot()
    {
        item = null;
        itemSprite.sprite = null;
        itemSprite.color = new
        Color(1, 1, 1, 0);
        itemCount.text = "";

        if(tooltip.activeSelf == true)
        {
            tooltip.SetActive(false);
        }
    }
    //todo

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(tooltip != null && item != null)
        {
            tooltip.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(tooltip != null  && item != null)
        {
            tooltip.SetActive(false);
        }
    }


    internal void Deselect()
    {
        backgroundSprite.sprite = deselectedBackgroundImage;
    }

    internal void Select()
    {
        backgroundSprite.sprite = selectedBackgroundImage;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left)
        {
            HandleLeftClick();
        }
    }

    public virtual void HandleLeftClick()
    {   
        //if the item is null, this means we need to pick up the slot item
        
        //PICK UP AN ITEM
        if(heldItem == null && canInteract)
        {
            if(item != null)
            {
                //set the "hand" information
                heldItem = item;
                heldCount = count;
                inventoryHand.SetHandData(item.Icon, count);

                //clear the "slot" information
                EmptySlot();
            }
           
        }
        else 
        {
            //PLACE YOUR ITEM
            if(item == null && canInteract)
            {
                SetItem(heldItem.Id, heldCount, true);

                heldItem = null;
                heldCount = 0;

                inventoryHand.SetHandData(null, 0);
            }
            //SWAP ITEMS
            else if(canInteract)
            {   
                if(item == heldItem)
                {
                    if(count + heldCount <= maxStackSize)
                    {
                        AddCount(heldCount);
                        inventoryHand.SetHandData(null);
                        heldItem = null;
                        heldCount = 0;
                    }
                    else
                    {
                        heldCount = count + heldCount - maxStackSize;
                        SetCount(maxStackSize);
                        inventoryHand.SetHandData(heldItem.Icon, heldCount);
                        
                    }
                }
                else
                {
                    BaseItem tempItem = heldItem;
                    int tempCount = heldCount;

                    heldItem = item;
                    heldCount = count;
                    inventoryHand.SetHandData(item.Icon, count);

                    SetItem(tempItem.Id, tempCount, true);
                }
                
            }
        }
    }

    private void BuildTooltip()
    {
        //TODO 
        //First disable all text objects (could put these in a list, but I want them by name)
        tooltipItemName.gameObject.SetActive(false);
        tooltipItemDescription.gameObject.SetActive(false);
        tooltipItemType.gameObject.SetActive(false);
        tooltipItemStats.gameObject.SetActive(false);
        tooltipItemQuality.gameObject.SetActive(false);
        
        //Enable and Set the text of each relevant tooltip section

        //First, Basic Item properties
        tooltipItemName.gameObject.SetActive(true);
        tooltipItemName.text = item.name;

        tooltipItemDescription.gameObject.SetActive(true);
        tooltipItemDescription.text = item.Description;

        tooltipItemType.gameObject.SetActive(true);
        tooltipItemType.text = item.Type.ToString();

        //Now we need to cover different itemTypes
        tooltipItemStats.text = "";
        switch(item.Type)
        {
            case ItemType.Tool:
                tooltipItemStats.gameObject.SetActive(true);
                
                //tooltipItemStats.text += item.speed; HAVENT ACTUALLY IMPLEMENTED SWING SPEED YET (or damage)
                tooltipItemStats.text += "DMG | NA\nSPD | NA";
                
                break;

            case ItemType.Weapon:
                tooltipItemStats.gameObject.SetActive(true);
                //tooltipItemStats.text += item.speed; HAVENT ACTUALLY IMPLEMENTED SWING SPEED YET (or damage)
                tooltipItemStats.text += "DMG | NA\nSPD | NA";
                break;

            case ItemType.Upgrade:
                tooltipItemStats.gameObject.SetActive(true);
                UpgradeItem upgItem = item as UpgradeItem;
                if(upgItem != null)
                {
                    tooltipItemQuality.text = "\n" + upgItem.Quality.ToString() + "%";
                    foreach(Upgrade upgrade in upgItem.upgradeList)
                    {
                        tooltipItemStats.text += $"{upgrade.upgradeType} : {upgrade.percentIncrease}\n";
                    }
                }
                break;
            
            case ItemType.Structure:
                StructureItem structItem = item as StructureItem;
                if(structItem != null)
                {
                    tooltipItemStats.text += $"Health = {structItem.prefab.GetComponent<BaseObject>()?.MaxHealth}\n";
                    tooltipItemStats.text += $"Size = {structItem.width}x{structItem.height}\n";
                }
                
                break;
        }
    }
}
