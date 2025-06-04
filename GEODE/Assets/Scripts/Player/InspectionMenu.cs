using System;
using Unity.Netcode;
using UnityEngine;

public class InspectionMenu : BaseContainer
{
    //* ---------------- Current Inspected Object ---------------------- */
    [HideInInspector] public GameObject currentInspectedObject;
    [SerializeField] private GameObject InspectionMenuHolder;
    private IUpgradeable currentUpgradeObject;


    //* ---------------- Events ---------------------- */
    public event Action OnMenuOpened;
    public event Action<GameObject, GameObject> InspectedObjectChanged;

    public void DoMenu(GameObject go)
    {
        if (currentUpgradeObject != null)
        {
            currentUpgradeObject.OnUpgradesChanged -= ServerRebuildList;
        }

        if (go == currentInspectedObject)
        {
            return;
        }
        else
        {
            InspectedObjectChanged?.Invoke(currentInspectedObject, go);
            currentInspectedObject = go;
            currentUpgradeObject = currentInspectedObject.GetComponent<IUpgradeable>();
            if (currentUpgradeObject != null)
            {
                currentUpgradeObject.OnUpgradesChanged += ServerRebuildList;
            }
            
        }



        if (InspectionMenuHolder.activeSelf == false)
        {
            InspectionMenuHolder.SetActive(true);
            isOpen = InspectionMenuHolder.activeSelf;
        }

        SyncUpgradesToContainerServerRpc(go);

        OnMenuOpened?.Invoke();

    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (currentUpgradeObject != null)
        {
            currentUpgradeObject.OnUpgradesChanged -= ServerRebuildList;
        }
    }

    public void CloseInspectionMenu()
    {
        if (currentUpgradeObject != null)
        {
            currentUpgradeObject.OnUpgradesChanged -= ServerRebuildList;
        }

        InspectionMenuHolder.SetActive(false);
        currentInspectedObject = null;
        currentUpgradeObject = null;
    }

    public override void ProcessSlotClick(Slot slot)
    {
        if (!isOpen) return;
        Debug.Log($"Slot {slot.SlotIndex} pressed");
        Debug.Log($"Container Length = {ContainerItems.Count}");
        int idx = slot.SlotIndex;
        if (idx >= ContainerItems.Count) return;
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
                int after = CursorStack.Instance.ItemStack.amount - 1;
                if (after == 0)
                {
                    MoveStackServerRpc(-1, idx, CursorStack.Instance.ItemStack.Id, CursorStack.Instance.ItemStack.amount);
                    CursorStack.Instance.ItemStack = ItemStack.Empty;
                    return;
                }
                else
                {
                    MoveStackServerRpc(-1, idx, CursorStack.Instance.ItemStack.Id, 1);
                    CursorStack.Instance.Amount -= 1;
                    return;
                }
                

                
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

    [ServerRpc(RequireOwnership = false)]
    public void SyncUpgradesToContainerServerRpc(NetworkObjectReference towerRef)
    {
        if (!towerRef.TryGet(out var towerGo)) return;
        currentInspectedObject = towerGo.gameObject;
        currentUpgradeObject = currentInspectedObject.GetComponent<IUpgradeable>();

        if (currentUpgradeObject != null)
        {
            currentUpgradeObject.OnUpgradesChanged += ServerRebuildList;
        }

        ServerRebuildList();

    }

    private void ServerRebuildList()
    {
        if(!IsServer) return;
        ContainerItems.Clear();

        var upg = currentInspectedObject.GetComponent<IUpgradeable>();
        for (int i = 0; i < 4; i++)
        {
            if (upg != null && i < upg.UpgradeItems.Count)
                ContainerItems.Add(new ItemStack { Id = upg.UpgradeItems[i].Id, amount = 1 });
            else
                ContainerItems.Add(ItemStack.Empty);
        }

        // Notify local UI & replicate to clients.
        RaiseOnContainerChanged();
    }
    

  
}
