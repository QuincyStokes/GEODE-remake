using System;
using Unity.VisualScripting;
using UnityEngine;

public class UpgradeSlot : Slot
{
    public event Action<UpgradeItem> itemAdded;
    public event Action<UpgradeItem> itemRemoved;
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
                itemRemoved?.Invoke(heldItem as UpgradeItem);
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
                itemAdded?.Invoke(heldItem as UpgradeItem); //fire itemAdded event for InspcetionMenu to listen
                heldItem = null;
                heldCount = 0;

                inventoryHand.SetHandData(null, 0);
                
            }
            //SWAP ITEMS
            else if(canInteract)
            {   
                //Trying to add held item ONTO the stack
                if (ItemDatabase.Instance.GetItem(heldItem.Id).Type == ItemType.Upgrade)
                {
                    BaseItem tempItem = heldItem;
                    int tempCount = heldCount;
                    itemRemoved?.Invoke(item as UpgradeItem);
                    heldItem = item;
                    heldCount = count;
                    inventoryHand.SetHandData(item.Icon, count);
                    
                    SetItem(tempItem.Id, tempCount, true);
                    itemAdded?.Invoke(tempItem as UpgradeItem);
                }
                
            }
        }
    }
}
