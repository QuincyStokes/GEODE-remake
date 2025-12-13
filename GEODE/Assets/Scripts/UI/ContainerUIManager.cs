using System.Collections.Generic;
using UnityEngine;

public class ContainerUIManager<T> : MonoBehaviour
    where T : BaseContainer
{
    //* ----------- Container Reference ------------------
    [Header("Container")]
    [SerializeField] protected T container;


    //* ------------ Slots -------------
    [SerializeField] protected Slot slotPrefab;
    protected List<Slot> slots = new();

    //* --------------- Methods -----------------

    protected virtual void Awake()
    {
        Debug.Log("ContainerManager is awake!");
        container.Ready += OnContainerReady;
    }

    protected virtual void OnDestroy()
    {
        if (container == null) return;
        container.OnSlotChanged -= OnSlotChanged;
        container.OnContainerChanged -= OnContainerChanged;
        container.Ready -= OnContainerReady;
    }   

    private void OnContainerReady()
    {
        Debug.Log($"Container is ready! Initializing Slots.");
        container.OnSlotChanged += OnSlotChanged;
        container.OnContainerChanged += OnContainerChanged;
        ClearSlots();
        InitializeSlots();
        RedrawAll();
    }

    protected virtual void InitializeSlots()
    {
        slots.Clear();
        Debug.Log("Initializing Slots!");
        foreach (SubContainer sc in container.subContainers)
        {
            for (int i = 0; i < sc.numSlots; i++)
            {
                int globalIndex = sc.startIndex + i;
                Debug.Log("Slot Created.");
                Slot currSlot = Instantiate(slotPrefab, sc.containerObject, false);
                currSlot.InitializeContainer(container, globalIndex);
                slots.Add(currSlot);
            }
        }
    }

    protected void ClearSlots()
    {
        foreach (Slot slot in slots)
        {
            Destroy(slot.gameObject);
        }
    }

    protected virtual void OnSlotChanged(int index, ItemStack stack)
    {
        slots[index].SetItem(stack);
    }

    protected virtual void OnContainerChanged()
        => RedrawAll();

    protected void RedrawAll()
    {
        for (int i = 0; i < slots.Count; i++)
            OnSlotChanged(i, container.GetItemAt(i));
    }



}
