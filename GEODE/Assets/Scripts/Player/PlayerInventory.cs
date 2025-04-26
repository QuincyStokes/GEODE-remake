using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : NetworkBehaviour, IContainer
{
    [Header("Settings")]
    [SerializeField] private int numSlots;
    //hotbar slots will always be 9
    [SerializeField] private List<BaseItem> startingItems;
    [SerializeField] private int maxItemStack;

    [Header("References")]
    [SerializeField] private GameObject inventoryObject;
    [SerializeField] private Transform inventorySlotHolder;

    [SerializeField] private GameObject hotbarObject;
    [SerializeField] private Transform hotbarSlotHolder;
    [SerializeField] private InventoryHandUI hand;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject healthbarObject;

    [Header("Audio" )]
    [SerializeField] private AudioClip inventoryOpenSFX;
    [SerializeField] private AudioClip inventoryCloseSFX;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject slotPrefab;

    //private tings
    private List<Slot> inventorySlots;
    private List<Slot> hotbarSlots;
    private List<Slot> allInventorySlots = new List<Slot>(); //list of both hotbar + inventory slots
    private int selectedSlotIndex;

    private void Awake()
    {
        InitializeInventorySlots();
        InitializeHotbarSlots();
        
    }
    
    private void Start()
    {
        
        
        ChangeSelectedSlot(0);
    }

    public override void OnNetworkSpawn()
    {
        if(!IsOwner)
        {
            inventoryObject.SetActive(false);
            hotbarObject.SetActive(false);
            healthbarObject.SetActive(false);
            enabled = false;
            return;
        }
        else
        {
            inventoryObject.SetActive(false);
            hotbarObject.SetActive(true);
        }
          
        AddItem(1, 10);
        AddItem(2, 10);
        AddItem(6, 1);
        AddItem(24, 10);
        AddItem(26, 1);
        AddItem(27, 1);
        AddItem(28, 1);
    }

    public int GetSelectedSlotIndex()
    {
        return selectedSlotIndex;
    }

    private void InitializeInventorySlots()
    {
        inventorySlots = new List<Slot>(numSlots);
        for(int i = 0; i < numSlots; i++)
        {
            Slot currSlot = Instantiate(slotPrefab).GetComponent<Slot>();
            currSlot.SetItem();
            currSlot.gameObject.transform.SetParent(inventorySlotHolder, false);
            currSlot.InitializeHand(hand);
            inventorySlots.Add(currSlot);
        }
        allInventorySlots.AddRange(inventorySlots);
    }

    private void InitializeHotbarSlots()
    {
        hotbarSlots = new List<Slot>(numSlots);
        for(int i = 0; i < 9; i++)
        {
            Slot currSlot = Instantiate(slotPrefab).GetComponent<Slot>();
            currSlot.SetItem();
            currSlot.gameObject.transform.SetParent(hotbarSlotHolder, false);
            currSlot.InitializeHand(hand);
            hotbarSlots.Add(currSlot);
        }
        allInventorySlots.AddRange(hotbarSlots);
    }

    public void ToggleInventory(InputAction.CallbackContext context)
    {
        inventoryObject.SetActive(!inventoryObject.activeSelf);
    }


    public void ChangeSelectedSlot(int newValue) {
        if(selectedSlotIndex >= 0) {
            hotbarSlots[selectedSlotIndex].Deselect();
        }
        if(newValue > 8) {
            hotbarSlots[0].Select();
            selectedSlotIndex = 0;
        } else if (newValue < 0) {
            hotbarSlots[8].Select();
            selectedSlotIndex = 8;
        } else {
            hotbarSlots[newValue].Select();
            selectedSlotIndex = newValue;
        }
        if(hotbarSlots[selectedSlotIndex].GetItemInSlot() != null && hotbarSlots[selectedSlotIndex].GetItemInSlot().Type == ItemType.Structure)
        {
            if(GridManager.Instance != null)
            {
                GridManager.Instance.holdingStructure = true;
                GridManager.Instance.currentItemId = hotbarSlots[selectedSlotIndex].GetItemInSlot().Id;
            }
            
        }
        else
        {
            if(GridManager.Instance != null)
            {
                GridManager.Instance.holdingStructure = false;
                GridManager.Instance.currentItemId = -1;
            }
        }
    }

    public void OnNumberPressed(InputAction.CallbackContext context)
    {
        ChangeSelectedSlot((int)context.ReadValue<float>()-1);
    }

    public void OnScroll(InputAction.CallbackContext context)
    {
        if(context.ReadValue<float>() < 0)
        {
            ChangeSelectedSlot(selectedSlotIndex+1);
        }
        else if (context.ReadValue<float>() > 0)
        {
            ChangeSelectedSlot(selectedSlotIndex-1);
        }   
    }

    public bool AddItem(int id, int count) 
    {
        //first, search hotbar
        foreach(Slot slot in hotbarSlots)
        {
            //if the current slot isn't holding an item, don't check it.
            if(slot.GetItemInSlot() == null)
            {
                continue;
            }
            //first check if we can stack it on top of anything.
            //in order for it to be a valid slot, we need:
            if(slot.GetItemInSlot().Id == id  //item in this slot needs to be the same as the newly added one
                && slot.GetItemInSlot() != null  //slot needs to *not* be empty
                && slot.GetItemInSlot().IsStackable  //item in slot needs to be stackable
                ) //slot + new count needs to *not* be at the max count
            {
                //if these are true, we can add to this slot!
                slot.AddCount(count);
                return true;
            }
        }
        
        //then, search inventory
        foreach(Slot slot in inventorySlots)
        {

            if(slot.GetItemInSlot() == null)
            {
                continue;
            }
            //first check if we can stack it on top of anything.
            //in order for it to be a valid slot,  we need:
            if(slot.GetItemInSlot().Id == id  //item in this slot needs to be the same as the newly added one
                && slot.GetItemInSlot() != null  //slot needs to *not* be empty
                && slot.GetItemInSlot().IsStackable  //item in slot needs to be stackable
                ) //slot + new count needs to *not* be at the max count
            {
                //if these are true, we can add to this slot!
                slot.AddCount(count);
                return true;
            }
        }


        //look for new *empty* slot in hotbar
        foreach(Slot slot in hotbarSlots)
        {
            if(slot.GetItemInSlot() == null)
            {
                //we've found an empty slot!
                slot.SetItem(id, count, true);
                return true;
            }
        }
        //look for new *empty* slot in inventory
        foreach(Slot slot in inventorySlots)
        {
            if(slot.GetItemInSlot() == null)
            {
                //we've found an empty slot!
                slot.SetItem(id, count, true);
                return true;
            }
        }

        return false;
    }

    public void UseSelectedItem(Vector3 mousePos)
    {
        //attempt to use the item
        BaseItem heldItem = hotbarSlots[selectedSlotIndex].GetItemInSlot();
        if(heldItem == null)
        {
            return;
        }
        if(heldItem.Use(mousePos))
        {
            //if the item was successfully used, *and* if the item is.. consumable?
            //can we check for type being Structure or COnsumable? other ones don't decrease count (tool, material, weapon)
            if (heldItem.ConsumeOnUse)
            {
                hotbarSlots[selectedSlotIndex].SubtractCount();
                if(hotbarSlots[selectedSlotIndex].GetCount() <= 0)
                {
                    GridManager.Instance.holdingStructure = false;
                }
            }
        } 
    }

    public bool ContainsItem(BaseItem item) {
        //check inventory
        foreach(Slot slot in inventorySlots)
        {
            if(slot.GetItemInSlot() == item)
            {
                return true;
            }
        }
        //check hotbar
        foreach(Slot slot in hotbarSlots)
        {
            if(slot.GetItemInSlot() == item)
            {
                return true;
            }
        }
        return false;
    }

    public int GetItemCount(BaseItem item)
    {
        int count = 0;
        foreach (Slot slot in inventorySlots)
        {
            if(slot.GetItemInSlot() == item)
            {
                count += slot.GetCount();
            }
        }
        foreach (Slot slot in hotbarSlots)
        {
            if(slot.GetItemInSlot() == item)
            {
                count += slot.GetCount();
            }
        }
        return count;
    }

    public int FindItem(BaseItem item)
    {
        return -1;
    }

    public BaseItem GetItemAtPosition(int position)
    {
        if(position <= inventorySlots.Count)
        {
            return inventorySlots[position].GetItemInSlot();
        }
        return null;
    }


    public bool RemoveItem(BaseItem item, int amount)
    {
        //this is pretty much our CanCraft function right?

        List<Slot> slots = new List<Slot>();
        int itemAmount = 0;

        //for each inventory slot (notably, this does not look through hotbar slots)
        foreach(Slot slot in allInventorySlots)
        {
            //if the slot holds the item we are looking for
            if(slot.GetItemInSlot() == item)
            {
                //if the slot's amount is >= what we need, remove the amount and return, we're done.
                if(slot.GetCount() >= amount)
                {
                    slot.SubtractCount(amount);
                    return true;
                }
                else //if we're here, that means the slot held the item, but didn't have enough to fulfull the amount
                {
                    //add the slot to a list and keep track of its count, we will loop through it later 
                    itemAmount += slot.GetCount();
                    slots.Add(slot);
                    if(itemAmount >= amount)
                    {
                        break;
                    }
                }
            }
        }
        //the total amount of items removed so far
        int toRemove = amount;
        
        //just  double checking if the total# of items spread across our slots is greater than the amount we need
        if(itemAmount >= amount)
        {
            foreach(Slot slot in slots)
            {
                //if we still have value to remove
                if(toRemove > 0)
                {
                    //get the number in the current slot
                    int slotCount = slot.GetCount();

                    //clamp it between 0 and the maximum amount we need
                    Mathf.Clamp(slotCount, 0, toRemove);

                    //remove that amount
                    toRemove -= slotCount;
                    slot.SubtractCount(slotCount);
                }
                else
                {
                    return true;
                }
            }
        }
        //if we're here, that means we didn't have the means to craft the item.
        return false;
    }



}
