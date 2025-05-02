using Unity.VisualScripting;
using UnityEngine;

public class UpgradeSlot : Slot
{
    public override void HandleLeftClick()
    {   
        //if the item is null, this means we need to pick up the slot item
        
        //PICK UP AN ITEM
        if(heldItem == null && canInteract)
        {
            if(item != null)
            {
                //set the "hand" information
                heldItem = item;
                heldCount = count;
                inventoryHand.SetHandData(item.Icon, count);

                //clear the "slot" information
                EmptySlot();
            }
           
        }
        else 
        {
            Debug.Log($"Trying to place item {heldItem.name}");
            //PLACE YOUR ITEM
            if(item == null && canInteract && ItemDatabase.Instance.GetItem(heldItem.Id).Type == ItemType.Upgrade)
            {
                SetItem(heldItem.Id, heldCount, true);

                heldItem = null;
                heldCount = 0;

                inventoryHand.SetHandData(null, 0);
            }
            //SWAP ITEMS
            else if(canInteract)
            {   
                if(item == heldItem && ItemDatabase.Instance.GetItem(heldItem.Id).Type == ItemType.Upgrade)
                {
                    if(count + heldCount <= maxStackSize)
                    {
                        AddCount(heldCount);
                        inventoryHand.SetHandData(null);
                        heldItem = null;
                        heldCount = 0;
                    }
                    else
                    {
                        heldCount = count + heldCount - maxStackSize;
                        SetCount(maxStackSize);
                        inventoryHand.SetHandData(heldItem.Icon, heldCount);
                        
                    }
                }
                else if (ItemDatabase.Instance.GetItem(heldItem.Id).Type == ItemType.Upgrade)
                {
                    BaseItem tempItem = heldItem;
                    int tempCount = heldCount;

                    heldItem = item;
                    heldCount = count;
                    inventoryHand.SetHandData(item.Icon, count);

                    SetItem(tempItem.Id, tempCount, true);
                }
                
            }
        }
    }
}
