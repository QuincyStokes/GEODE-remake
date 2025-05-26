using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;



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


public class PlayerInventory : NetworkBehaviour, IContainer
{
    // ------------------------- Dev Mode --------------------------------
    [Tooltip("Gives the player lots of items on start.")]
    [Header("Dev Mode")]
    [SerializeField] private bool devMode;


    // -------------------------- Inventory Settings ----------------------
    [Header("Inventory Settings")]
    [SerializeField] private List<ItemAmount> startingItems;
    [SerializeField] private int maxItemStack;

    //initialize the InventoryItems list to the proper length, filled with empty ItemStacks
    public NetworkList<ItemStack> InventoryItems = new NetworkList<ItemStack>(
        Enumerable.Repeat(ItemStack.Empty, TOTAL_SLOTS).ToList(),
        NetworkVariableReadPermission.Owner,
        NetworkVariableWritePermission.Server
    );


    // -------------------------- Public References ----------------------
    [Header("Public References")]
    [SerializeField] private GameObject inventoryObject;
    [SerializeField] private Transform inventorySlotHolder;
    [SerializeField] private Tooltip tooltip;
    [SerializeField] private GameObject hotbarObject;
    [SerializeField] private Transform hotbarSlotHolder;
    [SerializeField] private InventoryHandUI hand;
    public InventoryHandUI HandUI => hand;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameObject healthbarObject;

    //-------------------------- Audio --------------------------
    [Header("Audio")]
    [SerializeField] private AudioClip inventoryOpenSFX;
    [SerializeField] private AudioClip inventoryCloseSFX;


    [Header("Slot Prefab")]
    [SerializeField] private GameObject slotPrefab;


    //* ----------------------- Private --------------------------
    private List<Slot> inventorySlots;
    private List<Slot> hotbarSlots;
    private List<Slot> allInventorySlots = new List<Slot>(); //list of both hotbar + inventory slots
    private int selectedSlotIndex;
    private readonly int numHotbarSlots = 9;
    private readonly int numInventorySlots = 27;
    const int TOTAL_SLOTS = 36;

    private ItemStack cursorStack = ItemStack.Empty;
    public static Dictionary<ulong, ItemStack> TempCursorCache = new Dictionary<ulong, ItemStack>();


    //-------------------------- EVENTS ------------------------
    public event Action<bool> OnInventoryToggled;



    // ------------------------- Methods -----------------------
    private void Awake()
    {
        InitializeHotbarSlots();
        InitializeInventorySlots();
    }

    private void Start()
    {
        ChangeSelectedSlot(0);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();


        if (IsOwner)
        {
            InventoryItems.OnListChanged += OnInventoryListChanged;
            //RedrawFromList();
            inventoryObject.SetActive(false);
            hotbarObject.SetActive(true);

            //reset the hand data
            cursorStack.Id = -1;
            cursorStack.amount = 0;
            hand.SetHandData(null, 0);
        }
        else
        {
            //turn off UI objects if not the owner of this object
            inventoryObject.SetActive(false);
            hotbarObject.SetActive(false);
            healthbarObject.SetActive(false);
            tooltip.gameObject.SetActive(false);
        }


        if (IsServer)
        {
            TempCursorCache[OwnerClientId] = ItemStack.Empty;
            //give starting itms
            foreach (ItemAmount itemAmount in startingItems)
            {
                AddItemInternal(itemAmount.item.Id, itemAmount.amount);
            }
            AddItemInternal(6, 1);
        }

        if (devMode)
        {
            AddItemInternal(1, 20);
            AddItemInternal(2, 20);
            AddItemInternal(24, 20);
            AddItemInternal(26, 1);
            AddItemInternal(27, 1);
            AddItemInternal(28, 1);
            AddItemInternal(31, 5);
            AddItemInternal(32, 1);
        }

    }

    public int GetSelectedSlotIndex()
    {
        return selectedSlotIndex;
    }

    private void InitializeInventorySlots()
    {
        inventorySlots = new List<Slot>(numInventorySlots);
        for (int i = 0; i < numInventorySlots; i++)
        {
            Slot currSlot = Instantiate(slotPrefab).GetComponent<Slot>();
            currSlot.SetItem();
            currSlot.gameObject.transform.SetParent(inventorySlotHolder, false);
            currSlot.InitializeHand(hand);
            currSlot.InitializeInventory(this);
            currSlot.playerInventory = this;
            currSlot.SlotIndex = i + numHotbarSlots;
            inventorySlots.Add(currSlot);
        }
        allInventorySlots.AddRange(inventorySlots);
    }

    private void InitializeHotbarSlots()
    {
        hotbarSlots = new List<Slot>(numHotbarSlots);
        for (int i = 0; i < 9; i++)
        {
            Slot currSlot = Instantiate(slotPrefab).GetComponent<Slot>();
            currSlot.SetItem();
            currSlot.gameObject.transform.SetParent(hotbarSlotHolder, false);
            currSlot.InitializeHand(hand);
            currSlot.InitializeInventory(this);
            currSlot.SlotIndex = i;
            hotbarSlots.Add(currSlot);
        }
        allInventorySlots.AddRange(hotbarSlots);
    }

    public void ToggleInventory(InputAction.CallbackContext context)
    {
        inventoryObject.SetActive(!inventoryObject.activeSelf);
        OnInventoryToggled?.Invoke(inventoryObject.activeSelf);
        if (inventoryObject.activeSelf)
        {
            Cursor.visible = true;
        }
    }


    public void ChangeSelectedSlot(int newValue)
    {
        if (selectedSlotIndex >= 0)
        {
            hotbarSlots[selectedSlotIndex].Deselect();
        }
        if (newValue > 8)
        {
            hotbarSlots[0].Select();
            selectedSlotIndex = 0;
        }
        else if (newValue < 0)
        {
            hotbarSlots[8].Select();
            selectedSlotIndex = 8;
        }
        else
        {
            hotbarSlots[newValue].Select();
            selectedSlotIndex = newValue;
        }
        if (hotbarSlots[selectedSlotIndex].GetItemInSlot() != null && hotbarSlots[selectedSlotIndex].GetItemInSlot().Type == ItemType.Structure)
        {
            if (GridManager.Instance != null)
            {
                GridManager.Instance.holdingStructure = true;
                GridManager.Instance.currentItemId = hotbarSlots[selectedSlotIndex].GetItemInSlot().Id;
            }

        }
        else
        {
            if (GridManager.Instance != null)
            {
                GridManager.Instance.holdingStructure = false;
                GridManager.Instance.currentItemId = -1;
            }
        }
    }

    public void OnNumberPressed(InputAction.CallbackContext context)
    {
        ChangeSelectedSlot((int)context.ReadValue<float>() - 1);
    }

    public void OnScroll(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() < 0)
        {
            ChangeSelectedSlot(selectedSlotIndex + 1);
        }
        else if (context.ReadValue<float>() > 0)
        {
            ChangeSelectedSlot(selectedSlotIndex - 1);
        }
    }

    //server authoritative inventory adding
    [ServerRpc(RequireOwnership = false)]
    public void AddItemServerRpc(int id, int count)
    {
        AddItemInternal(id, count);
    }


    internal void AddItemInternal(int id, int count)
    {
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].Id == id && ItemDatabase.Instance.GetItem(id).IsStackable)
            {

                ItemStack s = InventoryItems[i];
                int totalAmount = s.amount + count;
                //make sure we aren't overflowing the slot
                if (totalAmount > maxItemStack)
                {
                    //find the amount we can safely place
                    int overflow = totalAmount - maxItemStack;
                    s.amount += totalAmount - maxItemStack;
                    
                    InventoryItems[i] = s;

                    //add the overflow to a different slot

                    int slot1 = GetFirstEmptySlot();
                    if (slot1 != -1)
                    {
                        InventoryItems[slot1] = new ItemStack { Id = id, amount = overflow };
                    }
                    

                    return;
                }
                else
                {
                    Debug.Log($"Setting ItemStack to inventory position {i},  ID:{s.Id}, Amount:{s.amount}");
                    s.amount += count;
                    InventoryItems[i] = s;
                    return;
                }

            }
        }
        ItemStack s2 = new ItemStack { Id = id, amount = count };
        Debug.Log($"{InventoryItems} | {id} | {count} | {s2}");
        int slot2 = GetFirstEmptySlot();
        if (slot2 != -1)
        {
            InventoryItems[slot2] = s2;
        }
       
    }


    private int GetFirstEmptySlot()
    {
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].IsEmpty())
            {
                return i;
            }
        }
        return -1;
    }

    //called by Slot.cs
    public void ProcessSlotClick(Slot slot)
{
    int idx = slot.SlotIndex;
    ItemStack slotStack = InventoryItems[idx];       // server truth (read-only)

    /* ---------- CURSOR EMPTY → PICK UP ---------- */
    if (cursorStack.IsEmpty() && !slotStack.IsEmpty())
    {
        // local prediction — clear slot, fill cursor UI
        slot.SetItem();                               // empty the visuals
        cursorStack = slotStack;
        UpdateCursorVisual();

        MoveStackServerRpc(idx, -1);                  // ask server
        return;
    }

    /* ---------- CURSOR HAS SOMETHING ---------- */
    if (!cursorStack.IsEmpty())
    {
        /* ----- COMBINE ----- */
        if (!slotStack.IsEmpty() &&
            slotStack.Id == cursorStack.Id &&
            ItemDatabase.Instance.GetItem(slotStack.Id).IsStackable)
        {
            int free = maxItemStack - slotStack.amount;
            if (free > 0)
            {
                int moveAmt = Mathf.Min(cursorStack.amount, free);

                /* predicted visuals */
                slot.SetItem(slotStack.Id, slotStack.amount + moveAmt, true);
                cursorStack.amount -= moveAmt;
                if (cursorStack.amount == 0) cursorStack = ItemStack.Empty;
                UpdateCursorVisual();

                MergeStackServerRpc(-1, idx, moveAmt);
            }
            return;
        }

        /* ----- PLACE (slot empty) ----- */
        if (slotStack.IsEmpty())
        {
            slot.SetItem(cursorStack.Id, cursorStack.amount, true); // draw now
            MoveStackServerRpc(-1, idx);

            cursorStack = ItemStack.Empty;
            UpdateCursorVisual();
            return;
        }

        /* ----- SWAP (different item) ----- */
        // predict: slot shows cursor item, cursor shows previous slot item
        slot.SetItem(cursorStack.Id, cursorStack.amount, true);
        UpdateCursorVisualWithStack(slotStack);       // helper shown below
        cursorStack = slotStack;

            // two RPCs, as before
        SwapSlotWithCursorServerRpc(idx);
        return;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SwapSlotWithCursorServerRpc(int slotIdx, ServerRpcParams p = default)
    {
        ulong sender = p.Receive.SenderClientId;

        if (!TempCursorCache.TryGetValue(sender, out var cursor))
            cursor = ItemStack.Empty;                // safety

        ItemStack slot = InventoryItems[slotIdx];

        // swap
        InventoryItems[slotIdx] = cursor;
        TempCursorCache[sender] = slot;
    }

    /* small helper so we don’t recalc icon sprite twice */
    void UpdateCursorVisualWithStack(ItemStack st)
    {
        var it = ItemDatabase.Instance.GetItem(st.Id);
        cursorStack = st;
        hand.SetHandData(it.Icon, st.amount);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoveStackServerRpc(int fromIndex, int toIndex, ServerRpcParams p = default)
    {
        ApplyMove(fromIndex, toIndex, p.Receive.SenderClientId);
    }

    private void UpdateCursorVisual()
    {
        if (cursorStack.IsEmpty())
        {
            HandUI.SetHandData(null, 0);
        }
        else
        {
            BaseItem item = ItemDatabase.Instance.GetItem(cursorStack.Id);
            HandUI.SetHandData(item.Icon, cursorStack.amount);
        }
    }

    [ServerRpc]
    public void MergeStackServerRpc(int fromIndex, int toIndex, int amount, ServerRpcParams p = default)
    {
        if (amount <= 0) return;
        ApplyMerge(fromIndex, toIndex, amount, p.Receive.SenderClientId);
    }

    private bool ApplyMerge(int from, int to, int amount, ulong sender)
    {
        var cache = TempCursorCache[sender];
        if (from == -1 && cache.amount < amount) return false;

        ItemStack toStack = InventoryItems[to];
        toStack.amount += amount;
        InventoryItems[to] = toStack;

        cache.amount -= amount;
        if (cache.amount == 0) cache = ItemStack.Empty;
        TempCursorCache[sender] = cache;
        return true;
    }

    private bool ApplyMove(int from, int to, ulong senderId)
    {
        ItemStack cache = TempCursorCache[senderId];
        ItemStack fromStack;
        ItemStack toStack;

        Debug.Log($"Applying Move, Cache:{cache}");
        //i usually dont like this format but its more readable in this case
        if (from == -1) fromStack = cache; //from cursor to slot
        else fromStack = InventoryItems[from]; //from slot to slot

        if (to == -1) toStack = cache; //from slot to cursor
        else toStack = InventoryItems[to]; //from slot to ..

        if (fromStack.IsEmpty()) return false; //nothing to move

        //if our target is empty, just move the items to there
        if (toStack.IsEmpty())
        {
            if (to == -1) cache = fromStack;
            else InventoryItems[to] = fromStack;

            if (from == -1) cache = ItemStack.Empty;
            else InventoryItems[from] = ItemStack.Empty;

            TempCursorCache[senderId] = cache;
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

            if (to == -1) cache = toStack; else InventoryItems[to] = toStack;
            if (from == -1) cache = fromStack; else InventoryItems[from] = fromStack;

            TempCursorCache[senderId] = cache;
            return true;
        }

        // else swap
        if (to == -1) cache = fromStack; else InventoryItems[to] = fromStack;
        if (from == -1) cache = toStack; else InventoryItems[from] = toStack;

        TempCursorCache[senderId] = cache;
        return true;
    }
    //when the network inventory list changes, redraw the inventory
    private void OnInventoryListChanged(NetworkListEvent<ItemStack> change)
    {
        if (change.Type == NetworkListEvent<ItemStack>.EventType.Value)
        {
            RedrawSlot(change.Index);
        }

    }

    private void RedrawSlot(int index)
    {
        ItemStack stack = InventoryItems[index];
        allInventorySlots[index].SetItem(stack.Id, stack.amount, true);
    }

    private void RedrawFromList()
    {
        for (int i = 0; i < InventoryItems.Count; i++)
        {
            RedrawSlot(i);
        }

    }

    public void UseSelectedItem(Vector3 mousePos)
    {
        // local owner only
        if (!IsOwner) return;

        ItemStack st = InventoryItems[GetSelectedSlotIndex()];
        if (st.IsEmpty()) return;

        BaseItem item = ItemDatabase.Instance.GetItem(st.Id);
        if (item == null) return;

        if (item.Use(mousePos))                // item’s own behaviour
        {
            if (item.ConsumeOnUse)
                ConsumeItemServerRpc(st.Id, 1);   // server-auth removal below
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void ConsumeItemServerRpc(int id, int amount, ServerRpcParams p = default)
    {
        RemoveItemInternal(id, amount);
    }

    internal bool RemoveItemInternal(int id, int amount)
    {
        // iterate once, gather slots holding the item
        List<int> slotsWithItem = new();
        int total = 0;

        for (int i = 0; i < InventoryItems.Count; i++)
        {
            if (InventoryItems[i].Id == id)
            {
                slotsWithItem.Add(i);
                total += InventoryItems[i].amount;
                if (total >= amount) break;
            }
        }

        if (total < amount) return false;          // not enough

        // second pass – subtract
        int remaining = amount;
        foreach (int idx in slotsWithItem)
        {
            if (remaining == 0) break;
            ItemStack st = InventoryItems[idx];

            int take = Mathf.Min(st.amount, remaining);
            st.amount -= take;
            remaining -= take;

            InventoryItems[idx] = st.amount == 0 ? ItemStack.Empty : st;
        }
        return true;
    }


    public bool ContainsItem(BaseItem item)
    {
        foreach (var st in InventoryItems)
        {
            if (st.Id == item.Id && st.amount > 0)
                return true;

        }
        return false;
    }

    public int GetItemCount(BaseItem item)
    {
        int total = 0;
        foreach (var st in InventoryItems)
            if (st.Id == item.Id)
                total += st.amount;
        return total;
    }

    public int FindItem(BaseItem item)
    {
        return -1;
    }

    public BaseItem GetItemAtPosition(int position)
    {
        if (position <= inventorySlots.Count)
        {
            return inventorySlots[position].GetItemInSlot();
        }
        return null;
    }

    public void ShowTooltip(int slotIndex)
    {
        int id = InventoryItems[slotIndex].Id;
        if (id == -1) return;
        tooltip.Build(id);
        tooltip.gameObject.SetActive(true);
    }

    public void HideTooltip()
    {
        tooltip.gameObject.SetActive(false);
    }


}
