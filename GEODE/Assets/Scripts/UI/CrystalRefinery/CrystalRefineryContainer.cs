using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Netcode;
using UnityEngine;

public class CrystalRefineryContainer : BaseContainer
{

    [Header("Refinery Output->Input map")]
    public RefineryMap refineryMapsList;
    public float refineryProcessTime;


    //* ---------- Internal ----------- */
    private Dictionary<int, int> refineryMap;
    private Coroutine refineryCoroutine;


    //* ---------- Events ----------- */
    public event Action OnRefineryStarted;
    public event Action OnRefineryEnded;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        OnContainerChanged += CheckRefineryConditions;
        OnSlotChanged += CheckRefineryConditionsWrapper;
        SeedRefineryMap();
    }

    

    private void OnEnable()
    {
        //this is objectively wrong and confusing, but gonna rock with it for now
        isOpen = true;
    }

    public override void ProcessSlotClick(Slot slot)
    {
        //Need to do a few things
        //1. Only allow crystal items. 
        //2. Every time an item is placed (or for that matter, removed), we need to check conditions for starting the Refinery process
            //2.1. (Slightly temporary), if there are three of the same crystal item, combine them into a single, stronger version.
            //2.2. Will need some sort of mapping from crystal type to upgraded crystal type.
            //2.3. Can expand this into cool recipies or something, a lot of potential actually
        //3. If the process is started, (decision to make), we either lock the slots until its done, or stop if the player removes an item.

        //Let's begin.

        int idx = slot.SlotIndex;
        if (idx >= ContainerItems.Count) return;
        ItemStack slotStack = ContainerItems[idx];
        ItemStack cursorStack = CursorStack.Instance.ItemStack;


       
        // --------- CURSOR HOLDING SOMETHING ---------
        if(!cursorStack.IsEmpty())
        {
             //Only allow crystal subtype items for now. 
            if(ItemDatabase.Instance.GetItem(cursorStack.Id).subType != ItemType.Crystal) return;

             //Only allow placement of items in the first subcontainer.
             //Theres a milllion ways to do this, but this is the smoothest/easiest way for now. Not necessarily the "best"  though.
            if(idx >= subContainers[0].numSlots) return;

            //------- SWAP ITEM ---------
            if(!slotStack.IsEmpty() && cursorStack.amount == 1)
            {
                ItemStack temp = cursorStack;
                //Set slot UI
                slot.SetItem(cursorStack.Id, 1);
                //Set Cursor items
                CursorStack.Instance.ItemStack = slotStack;
                //Apply the actual swap
                SwapSlotWithCursorServerRpc(idx, cursorStack.Id, 1);
            }
            // ---------- PLACE ITEM ---------
            else if (slotStack.IsEmpty())
            {
                int after = CursorStack.Instance.ItemStack.amount - 1;
                if(after == 0)
                {
                    CursorStack.Instance.ItemStack = ItemStack.Empty;
                }
                else
                {
                    CursorStack.Instance.Amount -= 1;
                }
                slot.SetItem(cursorStack.Id, 1);
                MoveStackServerRpc(-1, idx, cursorStack.Id, 1);
            }
        }
        // --------- CURSOR EMPTY ---------
        else
        {
            //PICK UP ITEM 
            if(slotStack.IsEmpty()) return;
            slot.SetItem();
            CursorStack.Instance.ItemStack = slotStack;
            MoveStackServerRpc(idx, -1, slotStack.Id, 0);
            return;
        }
    }


    private void CheckRefineryConditionsWrapper(int arg1, ItemStack stack)
    {
        //Can do some additional logic here to check what item we're dealing with.
        Debug.Log("Refinery Detected through slot change.");
        CheckRefineryConditions();   
    }


    //Okay, the process for actually.. processing the crystals
    //When the ContainerItems is updated, we check if conditions are met.
    //If they are, lock all of the slots(?) and begin some sort of coroutine
    //When that coroutine ends, delete the items in the upper slots, and add one corresponding item into a lower slot. Easypeasy?

    private void CheckRefineryConditions()
    {
        Debug.Log("Checking Refinery Conditions!");
        //if all items in the first subcontainer are the same
        int targetItemId = ContainerItems[0].Id;
        //! THIS CAN CHANGE TO BE A SPECIFIC NUMBER INSTEAD OF ALL THE SLOTS
            //* In the case that we want to give them more input slots, but keep it 3 crystals to combine or something
        for(int i = 0; i < subContainers[0].numSlots; i++)
        {
            if(ContainerItems[i].Id != targetItemId)
            {
                //Not all items are the same, do nothing.
                return;
            }
        }

        //If we get herem that means we have a valid requirement met! 
        //Need a map from item to item, can just be dictionary<int,int> but need to serialize it.
        int outputItemId;
        try
        {
            outputItemId = refineryMap[targetItemId];
        }
        catch
        {
            Debug.Log($"[Crystal Refinery] No valid mapping for {targetItemId}");
            outputItemId = -1;
        }

        if(outputItemId == -1)
        {
            return;
        }

        //If we're here, we can finally start the process.

        if(refineryCoroutine == null)
        {
            refineryCoroutine = StartCoroutine(RefineryProcess(outputItemId));
        }
        else
        {
            Debug.Log("[CrystalRefinery] Cannot start refinery, it's already running!");
            return;
        }
    }

    private IEnumerator RefineryProcess(int outputItemId)
    {
        OnRefineryStarted?.Invoke();
        //wait some amount of time
        yield return new WaitForSeconds(refineryProcessTime);

        //Done! Can delete the items and add the new one.
        for(int i = 0; i < subContainers[0].numSlots; i++)
        {
            //Remove/delete the items

            // UH OHHHH PROBLEM.
            //BaseContainer actually has no references to any of its slots, the logic is all driven by the slots themselves.
            //THis works great for normal containers, but now that we have some special logic where the container has a brain, this needs to change.
            // I don't really see any other way than giving this script a reference to the slots somehow..
            
            RemoveItemAtSlotServerRpc(1, i);
        }
        //Add the new targetItem into the first open slot in the second subcontainer.
        int targetSlot = GetFirstEmptySlotAfterIndex(subContainers[0].numSlots);
        SetItemAtSlotServerRpc(targetSlot, outputItemId, 1);
        //profit?
        //Does this work okay?
        refineryCoroutine = null;
        OnRefineryEnded?.Invoke();
    }

    private void SeedRefineryMap()
    {
        refineryMap = new();
        foreach(RefineryOutputMapping rom in refineryMapsList.refineryMaps)
        {
            refineryMap.Add(rom.inputItem.Id, rom.outputItem.Id);
        }
    }

}


//Right now this does NOT have anything to do with the amount required, but can add that later perchance.


