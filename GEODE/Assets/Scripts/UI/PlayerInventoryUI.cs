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
        //if the new held item is a structure, show the tilemap 
        int newItemId = container.ContainerItems[newIndex].Id;
    
        if (newItemId != -1 && ItemDatabase.Instance.GetItem(newItemId).Type == ItemType.Structure)
        {
            if (GridManager.Instance != null)
            {
                GridManager.Instance.holdingStructure = true;
                GridManager.Instance.currentItemId = newItemId;
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
        
        
        slots[oldIndex].Deselect();
        slots[newIndex].Select();
    }
}
