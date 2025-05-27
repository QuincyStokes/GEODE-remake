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


    //* ------------- Events ---------------
    public event Action<int, ItemStack> OnSlotChanged;
    public event Action OnContainerChanged;
    public event Action Ready;




    //* ------------------------ Methods ----------------------------

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;       // only the server seeds the list

        InitializeContainers();
        CursorStack.Instance.ItemStack = ItemStack.Empty;

        foreach (ItemAmount itemAmount in startingItems)
        {
            AddItemInternal(itemAmount.item.Id, itemAmount.amount);
        }
        AddItemInternal(6, 1);

        Ready?.Invoke();
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

    public virtual ItemStack GetItemAt(int index)
    {
        return ContainerItems[index];
    }

    internal void AddItemInternal(int id, int count)
    {
        for (int i = 0; i < ContainerItems.Count; i++)
        {
            if (ContainerItems[i].Id == id && ItemDatabase.Instance.GetItem(id).IsStackable && ContainerItems[i].amount < maxItemStack)
            {

                ItemStack s = ContainerItems[i];
                int totalAmount = s.amount + count;
                //make sure we aren't overflowing the slot
                if (totalAmount > maxItemStack)
                {
                    //find the amount we can safely place
                    int overflow = totalAmount - maxItemStack;
                    s.amount += count - (totalAmount - maxItemStack);

                    OnSlotChanged?.Invoke(i, s);
                    ContainerItems[i] = s;

                    //add the overflow to a different slot

                    AddItemInternal(id, overflow);
                    return;
                    // int slot1 = GetFirstEmptySlot(); //shouldn't actually be the first empty slot. What if we just called AddItemInternal again..
                    // if (slot1 != -1)
                    // {
                    //     ItemStack st = new ItemStack { Id = id, amount = overflow };
                    //     OnSlotChanged?.Invoke(slot1, st);
                    //     ContainerItems[slot1] = st;
                    // }


                    // return;
                }
                else
                {
                    s.amount += count;
                    OnSlotChanged?.Invoke(i, s);
                    ContainerItems[i] = s;
                    return;
                }

            }
        }
        ItemStack s2 = new ItemStack { Id = id, amount = count };
        int slot2 = GetFirstEmptySlot();
        if (slot2 != -1)
        {
            OnSlotChanged?.Invoke(slot2, s2);
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
    public void AddItemServerRpc(int id, int count)
    {
        AddItemInternal(id, count);
    }

    //called by Slot.cs
    public void ProcessSlotClick(Slot slot)
    {
        int idx = slot.SlotIndex;
        ItemStack slotStack = ContainerItems[idx];       // server truth (read-only)

        /* ---------- CURSOR EMPTY → PICK UP ---------- */
        if (CursorStack.Instance.ItemStack.IsEmpty() && !slotStack.IsEmpty())
        {
            // local prediction — clear slot, fill cursor UI
            //slot.SetItem();                               // empty the visuals
            //CursorStack.Instance.ItemStack = slotStack;

            MoveStackServerRpc(idx, -1);                  // ask server
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
                    //slot.SetItem(slotStack.Id, slotStack.amount + moveAmt, true);
                    //CursorStack.Instance.Amount -= moveAmt;
                    //if (CursorStack.Instance.ItemStack.amount == 0) CursorStack.Instance.ItemStack = ItemStack.Empty;

                    MergeStackServerRpc(-1, idx, moveAmt);
                }
                return;
            }

            /* ----- PLACE (slot empty) ----- */
            if (slotStack.IsEmpty())
            {
                MoveStackServerRpc(-1, idx);

                //CursorStack.Instance.ItemStack = ItemStack.Empty;
                return;
            }

            /* ----- SWAP (different item) ----- */
            // predict: slot shows cursor item, cursor shows previous slot item
            //slot.SetItem(CursorStack.Instance.ItemStack.Id, CursorStack.Instance.ItemStack.amount, true);
            //UpdateCursorVisualWithStack(slotStack);       // helper shown below
            //CursorStack.Instance.ItemStack = slotStack;

            // two RPCs, as before
            SwapSlotWithCursorServerRpc(idx);
            return;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SwapSlotWithCursorServerRpc(int slotIdx, ServerRpcParams p = default)
    {
        //temp
        ItemStack slotItemStack = ContainerItems[slotIdx];

        // swap
        ContainerItems[slotIdx] = CursorStack.Instance.ItemStack;
        OnSlotChanged?.Invoke(slotIdx, CursorStack.Instance.ItemStack);
        CursorStack.Instance.ItemStack = slotItemStack;
    }


    [ServerRpc(RequireOwnership = false)]
    private void MoveStackServerRpc(int fromIndex, int toIndex)
    {
        ApplyMove(fromIndex, toIndex);
    }

    [ServerRpc]
    public void MergeStackServerRpc(int fromIndex, int toIndex, int amount)
    {
        if (amount <= 0) return;
        ApplyMerge(fromIndex, toIndex, amount);
    }

    private bool ApplyMerge(int from, int to, int amount)
    {
        //! I don't quite understand this check
        if (from == -1 && CursorStack.Instance.ItemStack.amount < amount) return false;

        ItemStack toStack = ContainerItems[to];
        toStack.amount += amount;
        ContainerItems[to] = toStack;
        OnSlotChanged?.Invoke(to, toStack);

        ItemStack newStack = new ItemStack { Id = CursorStack.Instance.ItemStack.Id, amount = CursorStack.Instance.ItemStack.amount - amount };
        if (newStack.amount == 0) CursorStack.Instance.ItemStack = ItemStack.Empty;
        else CursorStack.Instance.ItemStack = newStack;

        Debug.Log($"Set cursor stack to {newStack.amount}");
        return true;
    }

    private bool ApplyMove(int from, int to)
    {
        ItemStack cache = CursorStack.Instance.ItemStack;
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
                OnSlotChanged?.Invoke(to, fromStack);
            }


            if (from == -1) //to the slot
            {
                cache = ItemStack.Empty;
            }
            else
            {
                Debug.Log($"Set slot {from} to Empty");
                ContainerItems[from] = ItemStack.Empty;
                OnSlotChanged?.Invoke(from, ItemStack.Empty);
            }

            CursorStack.Instance.ItemStack = cache;
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
                OnSlotChanged?.Invoke(to, toStack);
            }
            if (from == -1) cache = fromStack;
            else
            {
                ContainerItems[from] = fromStack;
                OnSlotChanged?.Invoke(from, fromStack);
            }
            CursorStack.Instance.ItemStack = cache;
            return true;
        }

        // else swap
        if (to == -1) cache = fromStack;
        else
        {
            ContainerItems[to] = fromStack;
            OnSlotChanged?.Invoke(to, fromStack);
        }
        if (from == -1) cache = toStack;
        else
        {
            ContainerItems[from] = toStack;
            OnSlotChanged?.Invoke(from, toStack);
        }

        CursorStack.Instance.ItemStack = cache;
        return true;
    }
    //when the network inventory list changes, redraw the inventory
    
    internal bool RemoveItemInternal(int id, int amount)
    {
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
                ContainerItems[idx] = ItemStack.Empty;
                OnSlotChanged?.Invoke(idx, ItemStack.Empty);
            }
            else
            {
                ContainerItems[idx] = st;
                OnSlotChanged?.Invoke(idx, st);
            }
        }
        return true;
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

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Id);
        serializer.SerializeValue(ref amount);
    }

    public readonly bool Equals(ItemStack other) => Id == other.Id;
    public static readonly ItemStack Empty = new() { Id = -1, amount = 0 };
    public readonly bool IsEmpty() => Id == -1 || amount == 0;
    
}
