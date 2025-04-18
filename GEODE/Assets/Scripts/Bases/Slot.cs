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
    [SerializeField] private Image backgroundSprite;
    [SerializeField] private Image itemSprite;
    [SerializeField] private TMP_Text itemCount; 
    [SerializeField] private InventoryHandUI inventoryHand;

    [Header("Background Images")]
    [SerializeField] private Sprite selectedBackgroundImage;
    [SerializeField] private Sprite deselectedBackgroundImage;


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

    private static BaseItem heldItem = null;
    private static int heldCount = 0;
    

    private void Update()
    {
        
    }

    public void InitializeHand(InventoryHandUI hand)
    {
        inventoryHand = hand;
    }

    public void SetItem(int id=-1, int newCount = 1, bool interactable=true)
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
        count = newCount;
        canInteract = interactable;

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
        count -= newCount;
        CheckItemDepleted();
        UpdateCountUI();
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

    private void CheckItemDepleted()
    {
        if(count < 1)
        {
            //this means we have no more of that item.
            EmptySlot();
        } 
    }

    private void EmptySlot()
    {
        item = null;
        itemSprite.sprite = null;
        itemSprite.color = new
        Color(1, 1, 1, 0);
        itemCount.text = "";
    }
    //todo

    public void OnPointerEnter(PointerEventData eventData)
    {
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        
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
}
