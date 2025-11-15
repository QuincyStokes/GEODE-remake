using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class BaseContainer : NetworkBehaviour
{
    //* --------------  Sub Containers -------------- */
    [Tooltip("Each different container within the 'big' container.")]
    [Header("Sub Containers")]
    public List<SubContainer> subContainers;


    //* --------------  Slot Settings -------------- */
    [Header("Slots")]
    [SerializeField] protected int maxItemStack;


    //* ------------- Slot Data ---------------------*/
    public NetworkList<ItemStack> ContainerItems = new NetworkList<ItemStack>();


    //* ----------- Starting Items -------------------*/
    [Header("Starting Items")]
    [SerializeField] protected List<ItemAmount> startingItems;


    //*------------ Internal ----------------- */
    public bool isOpen = false;


    //* ------------- Events ---------------
    public event Action<int, ItemStack> OnSlotChanged;
    public event Action OnContainerChanged;
    public event Action Ready;
    public event Action<Slot> OnSlotHovered;




    //* ------------------------ Methods ----------------------------

    public override void OnNetworkSpawn()
    {

        BuildSubContainerIndices();
        if (IsServer)
        {
            SeedItemList();
        }

        CursorStack.Instance.ItemStack = ItemStack.Empty;

        ContainerItems.OnListChanged += HandleListChanged;
        Debug.Log("Container is ready!");
        Ready?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        ContainerItems.OnListChanged -= HandleListChanged;
    }

    public virtual void InitializeContainers()
    {
        //Set the indexes of the SubContainers
        int runningIndex = 0; //running total of created slots 
        foreach (SubContainer sc in subContainers)
        {
            sc.startIndex = runningIndex;
            runningIndex += sc.numSlots;
        }

        //Clear our list of Items, then add new ones for each inventory slot.
        ContainerItems.Clear();
        for (int i = 0; i < runningIndex; i++)
        {
            ContainerItems.Add(ItemStack.Empty);
        }

    }


    protected void BuildSubContainerIndices()
    {
        int running = 0;
        foreach (var sc in subContainers)
        {
            sc.startIndex = running;
            running += sc.numSlots;
        }
    }

    protected virtual void SeedItemList()
    {
        ContainerItems.Clear();

        // make list as long as total slots
        int totalSlots = subContainers.Sum(sc => sc.numSlots);
        for (int i = 0; i < totalSlots; i++)
            ContainerItems.Add(ItemStack.Empty);

        foreach (var item in startingItems)
            AddItemInternal(item.item.Id, item.amount, item.item.quality);
        
    }


    protected virtual void HandleListChanged(NetworkListEvent<ItemStack> change)
    {
        switch (change.Type)
        {
            case NetworkListEvent<ItemStack>.EventType.Value:
                // A single index was overwritten
                OnSlotChanged?.Invoke(change.Index, change.Value);

                break;

            default:
                // Covers Add, Insert, Remove, Clear, etc.
                OnContainerChanged?.Invoke();
                break;
        }
    }

    public virtual ItemStack GetItemAt(int index)
    {
        if (index < 0 || index >= ContainerItems.Count)
            return ItemStack.Empty;
        return ContainerItems[index];
    }

    internal void AddItemInternal(int id, int count, float quality=0f)
    {
        if (!IsServer) return;
        for (int i = 0; i < ContainerItems.Count; i++)
        {   //if its the same item and there's room in the stack
            if (ContainerItems[i].Id == id && ItemDatabase.Instance.GetItem(id).IsStackable && ContainerItems[i].amount < maxItemStack && ContainerItems[i].quality == quality)
            {

                ItemStack s = ContainerItems[i];
                
                int totalAmount = s.amount + count;
                //make sure we aren't overflowing the slot
                if (totalAmount > maxItemStack)
                {
                    //find the amount we can safely place
                    int overflow = totalAmount - maxItemStack;
                    s.amount += count - (totalAmount - maxItemStack);

                    //OnSlotChanged?.Invoke(i, s);
                    ContainerItems[i] = s;

                    //add the overflow to a different slot

                    AddItemInternal(id, overflow, quality);
                    return;
                }
                else
                {
                    s.amount += count;
                    //OnSlotChanged?.Invoke(i, s);
                    ContainerItems[i] = s;
                    return;
                }

            }
        }
        ItemStack s2 = new ItemStack { Id = id, amount = count, quality = quality };
        int slot2 = GetFirstEmptySlot();
        if (slot2 != -1)
        {
            //OnSlotChanged?.Invoke(slot2, s2);
            ContainerItems[slot2] = s2;
        }

    }

    protected int GetFirstEmptySlot()
    {
        for (int i = 0; i < ContainerItems.Count; i++)
        {
            if (ContainerItems[i].IsEmpty())
            {
                return i;
            }
        }
        return -1;
    }



    //server authoritative inventory adding
    [ServerRpc(RequireOwnership = false)]
    public void AddItemServerRpc(int id, int count, float quality = 0f)
    {
        AddItemInternal(id, count, quality);
    }

    //called by Slot.cs
    public virtual void ProcessSlotClick(Slot slot)
    {
        if (!isOpen) return;
        int idx = slot.SlotIndex;
        ItemStack slotStack = ContainerItems[idx];       // server truth (read-only)

        /* ---------- CURSOR EMPTY → PICK UP ---------- */
        if (CursorStack.Instance.ItemStack.IsEmpty() && !slotStack.IsEmpty())
        {
            // local prediction — clear slot, fill cursor UI
            slot.SetItem();                               // empty the visuals
            CursorStack.Instance.ItemStack = slotStack;

            MoveStackServerRpc(idx, -1, -1, 0, 0);                  // ask server
            return;
        }

        /* ---------- CURSOR HAS SOMETHING ---------- */
        if (!CursorStack.Instance.ItemStack.IsEmpty())
        {
            /* ----- COMBINE ----- */
            if (!slotStack.IsEmpty() &&
                slotStack.Id == CursorStack.Instance.ItemStack.Id &&
                ItemDatabase.Instance.GetItem(slotStack.Id).IsStackable)
            {
                int free = maxItemStack - slotStack.amount;
                if (free > 0)
                {
                    int moveAmt = Mathf.Min(CursorStack.Instance.ItemStack.amount, free);

                    /* predicted visuals */
                    slot.SetItem(slotStack.Id, slotStack.amount + moveAmt, slotStack.quality, interactable:true);
                    MergeStackServerRpc(idx, moveAmt);

                    CursorStack.Instance.Amount -= moveAmt; //this may not work
                    if (CursorStack.Instance.ItemStack.amount == 0) CursorStack.Instance.ItemStack = ItemStack.Empty;
                    //OnSlotChanged?.Invoke(idx, CursorStack.Instance.ItemStack);


                }
                return;
            }

            /* ----- PLACE (slot empty) ----- */
            if (slotStack.IsEmpty())
            {
                slot.SetItem(CursorStack.Instance.ItemStack.Id, CursorStack.Instance.ItemStack.amount, CursorStack.Instance.ItemStack.quality, interactable:true);
                MoveStackServerRpc(-1, idx, CursorStack.Instance.ItemStack.Id, CursorStack.Instance.ItemStack.amount, CursorStack.Instance.ItemStack.quality);

                CursorStack.Instance.ItemStack = ItemStack.Empty;
                return;
            }

            /* ----- SWAP (different item) ----- */
            // predict: slot shows cursor item, cursor shows previous slot item
            slot.SetItem(CursorStack.Instance.ItemStack.Id, CursorStack.Instance.ItemStack.amount, CursorStack.Instance.ItemStack.quality, interactable:true);


            //RPC, as before
            SwapSlotWithCursorServerRpc(idx, CursorStack.Instance.ItemStack.Id, CursorStack.Instance.ItemStack.amount, CursorStack.Instance.ItemStack.quality);
            //OnSlotChanged?.Invoke(idx, new ItemStack { Id = CursorStack.Instance.ItemStack.Id, amount = CursorStack.Instance.ItemStack.amount }); 
            CursorStack.Instance.ItemStack = slotStack;
            return;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    protected void SwapSlotWithCursorServerRpc(int slotIdx, int stackId, int stackAmount, float stackQuality)
    {
        //temp
        ItemStack slotItemStack = ContainerItems[slotIdx];
        //Fake "Cursor"
        ItemStack cursorData = new ItemStack { Id = stackId, amount = stackAmount, quality = stackQuality };
        ContainerItems[slotIdx] = cursorData;

    }




    [ServerRpc]
    public void MergeStackServerRpc(int toIndex, int amount)
    {
        if (amount <= 0) return;
        ApplyMerge(toIndex, amount);
    }

    protected bool ApplyMerge(int to, int amount)
    {

        ItemStack toStack = ContainerItems[to];
        toStack.amount += amount;
        ContainerItems[to] = toStack;

        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    protected void MoveStackServerRpc(int fromIndex, int toIndex, int stackId, int stackAmount, float stackQuality)
    {
        ApplyMove(fromIndex, toIndex, stackId, stackAmount, stackQuality);
    }

    protected bool ApplyMove(int from, int to, int stackId, int stackAmount, float stackQuality)
    {

        ItemStack cache = new ItemStack { Id = stackId, amount = stackAmount, quality=stackQuality };
        ItemStack fromStack;
        ItemStack toStack;

        Debug.Log($"Applying Move, Cache:{cache.Id}");
        //i usually dont like this format but its more readable in this case
        if (from == -1) fromStack = cache; //from cursor to slot
        else fromStack = ContainerItems[from]; //from slot to slot

        if (to == -1) toStack = cache; //from slot to cursor
        else toStack = ContainerItems[to]; //from slot to ..

        Debug.Log($"Applying Move, FromStack:{fromStack.Id}, ToStack:{toStack.Id}");
        if (fromStack.IsEmpty()) return false; //nothing to move

        //if our target is empty, just move the items to there
        if (toStack.IsEmpty())
        {
            if (to == -1) //to the cursor
            {
                cache = fromStack;
            }
            else
            {
                ContainerItems[to] = fromStack;
            }


            if (from == -1) //to the slot
            {
                cache = ItemStack.Empty;
            }
            else
            {
                Debug.Log($"Set slot {from} to Empty");
                ContainerItems[from] = ItemStack.Empty;
            }

            return true;
        }

        //if our target has an item, we need to swap
        // target has item
        if (fromStack.Id == toStack.Id &&
            ItemDatabase.Instance.GetItem(toStack.Id).IsStackable &&
            toStack.amount < maxItemStack)
        {
            int free = maxItemStack - toStack.amount;
            int moveAmt = Mathf.Min(fromStack.amount, free);

            toStack.amount += moveAmt;
            fromStack.amount -= moveAmt;

            if (to == -1) cache = toStack;
            else
            {
                ContainerItems[to] = toStack;
            }
            if (from == -1) cache = fromStack;
            else
            {
                ContainerItems[from] = fromStack;
            }
            return true;
        }

        // else swap
        if (to == -1) cache = fromStack;
        else
        {
            ContainerItems[to] = fromStack;
        }
        if (from == -1) cache = toStack;
        else
        {
            ContainerItems[from] = toStack;
        }
        return true;
    }
    //when the network inventory list changes, redraw the inventory

    internal bool RemoveItemInternal(int id, int amount)
    {
        Debug.Log($"Removing item {id} with count {amount}");
        // iterate once, gather slots holding the item
        List<int> slotsWithItem = new();
        int total = 0;

        for (int i = 0; i < ContainerItems.Count; i++)
        {
            if (ContainerItems[i].Id == id)
            {
                slotsWithItem.Add(i);
                total += ContainerItems[i].amount;
                if (total >= amount) break;
            }
        }

        if (total < amount) return false;          // not enough

        // second pass – subtract
        int remaining = amount;
        foreach (int idx in slotsWithItem)
        {
            if (remaining == 0) break;
            ItemStack st = ContainerItems[idx];

            int take = Mathf.Min(st.amount, remaining);
            st.amount -= take;
            remaining -= take;

            if (st.amount == 0)
            {
                Debug.Log("From removing an item, amount is now 0. Setting stack to empty.");
                ContainerItems[idx] = ItemStack.Empty;
            }
            else
            {
                ContainerItems[idx] = st;
            }
        }
        return true;
    }

    [ServerRpc]
    public void RemoveItemServerRpc(int id, int amount)
    {
        RemoveItemInternal(id, amount);
    }


    internal bool RemoveItemAtSlotInternal(int amount, int slotIndex)
    {
        ItemStack st = ContainerItems[slotIndex];
        if(st.amount < amount) return false;

        int newAmount = st.amount - amount;
        if(newAmount == 0)
        {
            ContainerItems[slotIndex] = ItemStack.Empty;
        }
        else
        {
            ContainerItems[slotIndex] = new ItemStack{Id = st.Id, amount = newAmount};
        }
        return true;

    }

    [ServerRpc(RequireOwnership = false)]
    public void RemoveItemAtSlotServerRpc(int amount, int slotIndex)
    {
        RemoveItemAtSlotInternal(amount, slotIndex);
    }

    public bool ContainsItem(BaseItem item)
    {
        foreach (var st in ContainerItems)
        {
            if (st.Id == item.Id && st.amount > 0)
                return true;

        }
        return false;
    }

    public int GetItemCount(BaseItem item)
    {
        int total = 0;
        foreach (var st in ContainerItems)
            if (st.Id == item.Id)
                total += st.amount;
        return total;
    }

    public int FindItem(BaseItem item)
    {
        return -1;
    }
    public void RaiseOnContainerChanged()
    {
        OnContainerChanged?.Invoke();
    }

    public void HandleSlotHovered(Slot slot)
    {
        OnSlotHovered?.Invoke(slot);
    }
    
}


[System.Serializable]
public class SubContainer
{
    public Transform containerObject;
    public int numSlots;
    [HideInInspector]public int startIndex;
}

[System.Serializable]
public struct ItemStack : INetworkSerializable, System.IEquatable<ItemStack>
{
    /// <summary>
    /// Network Serializable structure to hold an Id and amount of an item
    /// Will be used for storing inventory items
    /// will need to add a Quality as well
    /// </summary>
    public int Id;
    public int amount;
    public float quality;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Id);
        serializer.SerializeValue(ref amount);
        serializer.SerializeValue(ref quality);
    }

    public readonly bool Equals(ItemStack other) => Id == other.Id;
    public static readonly ItemStack Empty = new() { Id = -1, amount = 0 };
    public readonly bool IsEmpty() => Id == -1 || amount == 0;
    
}
