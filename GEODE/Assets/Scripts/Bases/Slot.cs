using System;
using System.Runtime.Serialization.Json;
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
    //protected BaseItem item; //item this slot is holding
    protected Sprite icon;
    //protected int count;
    public bool canInteract = true;
    public int SlotIndex { get; set; }
    [HideInInspector] public Transform parentAfterDrag;

    //----------
    //PLAYERS "HAND"
    //----------
    public BaseContainer container;
    protected static BaseItem heldItem = null;
    protected static int heldCount = 0;
    public ItemStack displayedStack { get; private set; }

    public void InitializeContainer(BaseContainer newContainer, int index)
    {
        container = newContainer;
        SlotIndex = index;
        //container.OnInventoryToggled += ToggleCanInteract;
        ToggleCanInteract(true);
    }

    private void ToggleCanInteract(bool active)
    {
        canInteract = active;
        Debug.Log($"Slot canInteract set to {active}");
    }

    public virtual void SetItem(ItemStack itemStack)
    {
        SetItem(itemStack.Id, itemStack.amount, true);
    }

    public virtual void SetItem(int id = -1, int newCount = 1, bool interactable = false)
    {
        //set the internal item data
        if (id == -1)
        {
            itemSprite.color = new Color(1, 1, 1, 0);
        }
        else
        {
            //item = ItemDatabase.Instance.GetItem(id);
            itemSprite.sprite = ItemDatabase.Instance.GetItem(id).Icon;
            itemSprite.color = new Color(1, 1, 1, 1);
        }
        if (newCount > 1)
        {
            itemCount.text = newCount.ToString();
        }
        else
        {
            itemCount.text = "";
        }
        canInteract = interactable;
        displayedStack = new ItemStack { Id = id, amount = newCount };

        //set the UI to match
        //CheckItemDepleted();
    }

    public BaseItem GetItemInSlot()
    {
        ItemStack stack = container.ContainerItems[SlotIndex];
        if (!stack.IsEmpty())
        {
            return ItemDatabase.Instance.GetItem(stack.Id);
        }
        else
        {
            return null;
        }
    }

    //todo

    public void OnPointerEnter(PointerEventData eventData)
    {
        //TODO
        //container.ShowTooltip(SlotIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //TODO
        //container.HideTooltip();
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
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            HandleLeftClick();
        }
    }

    public virtual void HandleLeftClick()
    {

        if (!container.IsOwner)
        {
            return;
        }
        //TODO
        //container.ProcessSlotClick(this);
    }
    
    private void OnDestroy()
    {
        if (container != null)
        {
            //TODO
            //container.OnInventoryToggled -= ToggleCanInteract;
        }

    }
}
