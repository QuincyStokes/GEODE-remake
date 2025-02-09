using System.Collections.Generic;
using JetBrains.Annotations;
using Unity.Netcode;
using UnityEngine;

public class PlayerInventory : NetworkBehaviour, IContainer
{

    

    [Header("Settings")]
    [SerializeField] private int numSlots;
    //hotbar slots will always be 9

    [Header("UI References")]
    [SerializeField] private GameObject inventoryObject;
    [SerializeField] private Transform inventorySlotHolder;

    [SerializeField] private GameObject hotbarObject;
    [SerializeField] private Transform hotbarSlotHolder;

    [Header("Audio" )]
    [SerializeField] private AudioClip inventoryOpenSFX;
    [SerializeField] private AudioClip inventoryCloseSFX;
    
    [Header("Prefabs")]
    [SerializeField] private GameObject slotPrefab;

    //private tings
    private List<Slot> InventorySlots;
    private List<Slot> HotbarSlots;


       

    public override void OnNetworkSpawn()
    {
        if(!IsOwner)
        {
            enabled = false;
            return;
        }
        
    }

    private void Awake()
    {
        InitializeInventorySlots();
        InitializeHotbarSlots();
    
    }
    private void Start()
    {
        
        PlayerController.inventoryToggled += ToggleInventory;
        inventoryObject.SetActive(false);
        hotbarObject.SetActive(true);
    }

    private void InitializeInventorySlots()
    {
        InventorySlots = new List<Slot>(numSlots);
        for(int i = 0; i < numSlots; i++)
        {
            Slot currSlot = Instantiate(slotPrefab).GetComponent<Slot>();
            currSlot.SetItem();
            currSlot.gameObject.transform.SetParent(inventorySlotHolder, false);
            InventorySlots.Add(currSlot);
        }
    }

    private void InitializeHotbarSlots()
    {
        HotbarSlots = new List<Slot>(numSlots);
        for(int i = 0; i < 9; i++)
        {
            Slot currSlot = Instantiate(slotPrefab).GetComponent<Slot>();
            currSlot.SetItem();
            currSlot.gameObject.transform.SetParent(hotbarSlotHolder, false);
            HotbarSlots.Add(currSlot);
        }
    }

    public void ToggleInventory()
    {
        Debug.Log($"Toggled from PlayerInventory! Setting inventory to {!inventoryObject.activeSelf}");
        inventoryObject.SetActive(!inventoryObject.activeSelf);
    }
    public int FindItem(BaseItem item)
    {
        return -1;
    }

    public BaseItem GetItemAtPosition()
    {
        return null;
    }

    
    public bool SwapItems()
    {
        return false;
    }

}
