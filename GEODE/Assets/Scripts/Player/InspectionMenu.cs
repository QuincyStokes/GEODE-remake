using System;
using System.ComponentModel;
using UnityEngine;

public class InspectionMenu : BaseContainer
{
    //* ---------------- Current Inspected Object ---------------------- */
    [HideInInspector] public GameObject currentInspectedObject;
    [SerializeField] private GameObject InspectionMenuHolder;
    private IUpgradeable currentUpgradeableObject;


    //* ---------------- Events ---------------------- */
    public event Action OnMenuOpened;

    public void DoMenu(GameObject go)
    {
        if (go == currentInspectedObject)
        {
            return;
        }
        else
        {
            currentInspectedObject = go;
        }

        if (InspectionMenuHolder.activeSelf == false)
        {
            InspectionMenuHolder.SetActive(true);
        }

        currentUpgradeableObject = currentInspectedObject.GetComponent<IUpgradeable>();
        if (currentUpgradeableObject != null)
        {
            currentUpgradeableObject.OnUpgradesChanged += SyncUpgradesToContainer;
        }
        SyncUpgradesToContainer();

        OnMenuOpened?.Invoke();
        
    }

    public void CloseInspectionMenu()
    {
        InspectionMenuHolder.SetActive(false);
        currentInspectedObject = null;
    }

    public override void ProcessSlotClick(Slot slot)
    {
        int idx = slot.SlotIndex;
        ItemStack slotStack = ContainerItems[idx];       // server truth (read-only)
        

        /* ---------- CURSOR EMPTY → PICK UP ---------- */
        if (CursorStack.Instance.ItemStack.IsEmpty() && !slotStack.IsEmpty())
        {
            // local prediction — clear slot, fill cursor UI
            slot.SetItem();                               // empty the visuals

            //! remove upgrade?
            Debug.Log($"Removing Upgrade {slotStack.Id}");
            IUpgradeable removeUpg = currentInspectedObject.GetComponent<IUpgradeable>();
            removeUpg?.RemoveUpgradeServerRpc(slotStack.Id);


            CursorStack.Instance.ItemStack = slotStack;
            MoveStackServerRpc(idx, -1, -1, 0);                  // ask server
            return;
        }

        /* ---------- CURSOR HAS SOMETHING ---------- */
        if (!CursorStack.Instance.ItemStack.IsEmpty())
        {
            //! Note, removed COMBINE logic, we dont want to combine upgrades
            /* ----- PLACE (slot empty) ----- */
            if (slotStack.IsEmpty())
            {
                //only allow upgrade items
                if (ItemDatabase.Instance.GetItem(CursorStack.Instance.ItemStack.Id).Type != ItemType.Upgrade) return;

                slot.SetItem(CursorStack.Instance.ItemStack.Id, CursorStack.Instance.ItemStack.amount, true);
                //! apply upgrade
                Debug.Log($"Applying Upgrade {CursorStack.Instance.ItemStack.Id}");
                IUpgradeable applyUpg = currentInspectedObject.GetComponent<IUpgradeable>();
                applyUpg?.ApplyUpgradeServerRpc(CursorStack.Instance.ItemStack.Id);

                MoveStackServerRpc(-1, idx, CursorStack.Instance.ItemStack.Id, CursorStack.Instance.ItemStack.amount);

                CursorStack.Instance.ItemStack = ItemStack.Empty;
                return;
            }

            /* ----- SWAP (different item) ----- */
            // predict: slot shows cursor item, cursor shows previous slot item
            slot.SetItem(CursorStack.Instance.ItemStack.Id, CursorStack.Instance.ItemStack.amount, true);


            //RPC, as before
            //! swap upgrades
            IUpgradeable swapApplyUpg = currentInspectedObject.GetComponent<IUpgradeable>();
            Debug.Log($"Swapping upgrades {ContainerItems[idx].Id} for {CursorStack.Instance.ItemStack.Id}");
            swapApplyUpg?.ApplyUpgradeServerRpc(CursorStack.Instance.ItemStack.Id);
            swapApplyUpg?.RemoveUpgradeServerRpc(ContainerItems[idx].Id);


            SwapSlotWithCursorServerRpc(idx, CursorStack.Instance.ItemStack.Id, CursorStack.Instance.ItemStack.amount);
            //OnSlotChanged?.Invoke(idx, new ItemStack { Id = CursorStack.Instance.ItemStack.Id, amount = CursorStack.Instance.ItemStack.amount }); 
            CursorStack.Instance.ItemStack = slotStack;
            return;
        }
    }

    public void SyncUpgradesToContainer()
    {
        ContainerItems.Clear();
        var upg = currentInspectedObject.GetComponent<IUpgradeable>();
        if (upg != null)
        {
            for (int i = 0; i < 4; i++)
            {
                if (i < upg.UpgradeItems.Count)
                {
                    ContainerItems.Add(new ItemStack { Id = upg.UpgradeItems[i].Id, amount = 1 });
                }
                else
                {
                    ContainerItems.Add(ItemStack.Empty);
                }
            }

            RaiseOnContainerChanged();
        }
        
    }

  
}
