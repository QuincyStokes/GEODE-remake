using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Unity.Cinemachine;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : BaseContainer
{
    // ------------------------- Dev Mode --------------------------------
    [Tooltip("Gives the player lots of items on start.")]
    [Header("Dev Mode")]
    [SerializeField] private bool devMode;

    // -------------------------- Public References ----------------------
    [Header("Public References")]
    [SerializeField] private GameObject inventoryObject; //PlayerInventoryUImanager ideally
    [SerializeField] private Tooltip tooltip; //actually not sure how to handle tooltips
    [SerializeField] private GameObject hotbarObject; //PlayerInventoryUIManager
    [SerializeField] private GameObject healthbarObject; // I think this should go in PlayerController? or maybe a PlayerUIHandler


    //* ----------------------- Private --------------------------
    private int selectedSlotIndex;

    //-------------------------- EVENTS ------------------------
    public event Action<bool> OnInventoryToggled;
    public event Action<int, int> OnSelectedSlotChanged;
    public event Action<BaseItem> OnItemUsed;

    // ------------------------- Methods -----------------------


    private void Start()
    {
        selectedSlotIndex = 0;
        OnSelectedSlotChanged?.Invoke(selectedSlotIndex, selectedSlotIndex);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();


        if (IsOwner)
        {
            //RedrawFromList();
            inventoryObject.SetActive(false);
            hotbarObject.SetActive(true);
        }
        else
        {
            //turn off UI objects if not the owner of this object
            inventoryObject.SetActive(false);
            hotbarObject.SetActive(false);
            healthbarObject.SetActive(false);
            tooltip.gameObject.SetActive(false);
        }
        if (devMode)
        {
            AddItemServerRpc(1, 20);
            AddItemServerRpc(2, 20);
            AddItemServerRpc(24, 20);
            AddItemServerRpc(26, 1);
            AddItemServerRpc(27, 1);
            AddItemServerRpc(28, 1);
            AddItemServerRpc(31, 5);
            AddItemServerRpc(32, 1);
        }

    }

    public int GetSelectedSlotIndex()
    {
        return selectedSlotIndex;
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

    public void OnNumberPressed(InputAction.CallbackContext context)
    {
        int oldSlotIndex = selectedSlotIndex;
        selectedSlotIndex = (int)context.ReadValue<float>() - 1;
        OnSelectedSlotChanged?.Invoke(oldSlotIndex, selectedSlotIndex);
    }

    public void OnScroll(InputAction.CallbackContext context)
    {
        int oldSlotIndex = selectedSlotIndex;
        if (context.ReadValue<float>() < 0)
        {
            selectedSlotIndex += 1;
            if(selectedSlotIndex > 8)
            {
                selectedSlotIndex = 0;
            }
            OnSelectedSlotChanged?.Invoke(oldSlotIndex, selectedSlotIndex);
            //ChangeSelectedSlot(selectedSlotIndex + 1);
        }
        else if (context.ReadValue<float>() > 0)
        {
            selectedSlotIndex -= 1;
            if(selectedSlotIndex < 0)
            {
                selectedSlotIndex = 8;
            }
            OnSelectedSlotChanged?.Invoke(oldSlotIndex, selectedSlotIndex);
        }
    }

    public void UseSelectedItem(Vector3 mousePos)
    {
        // local owner only
        if (!IsOwner) return;

        ItemStack st = ContainerItems[GetSelectedSlotIndex()];
        if (st.IsEmpty()) return;

        BaseItem item = ItemDatabase.Instance.GetItem(st.Id);
        if (item == null) return;

        if (item.Use(mousePos))                // item’s own behaviour
        {
            OnItemUsed?.Invoke(item);
            if (item.ConsumeOnUse)

                ConsumeItemServerRpc(st.Id, 1);   // server-auth removal below
                OnSelectedSlotChanged?.Invoke(selectedSlotIndex, selectedSlotIndex);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void ConsumeItemServerRpc(int id, int amount)
    {
        RemoveItemInternal(id, amount);
    }

    

    public void ShowTooltip(int slotIndex)
    {
        int id = ContainerItems[slotIndex].Id;
        if (id == -1) return;
        tooltip.Build(id);
        tooltip.gameObject.SetActive(true);
    }

    public void HideTooltip()
    {
        tooltip.gameObject.SetActive(false);
    }


}
