using UnityEngine;

public class PlayerInventoryUI : ContainerUIManager<PlayerInventory>
{
    private void Start()
    {
        container.OnSelectedSlotChanged += SelectNewSlot;
    }

    private new void OnDestroy()
    {
        base.OnDestroy();
        container.OnSelectedSlotChanged -= SelectNewSlot;
    }

    private void SelectNewSlot(int oldIndex, int newIndex)
    {
        slots[oldIndex].Deselect();
        slots[newIndex].Select();
    }
}
