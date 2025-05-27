using System;
using Unity.VisualScripting;
using UnityEngine;

public class UpgradeSlot : Slot
{
    public event Action<UpgradeItem> itemAdded;
    public event Action<UpgradeItem> itemRemoved;
    public event Action<UpgradeItem, UpgradeItem> itemSwapped;
    // public override void HandleLeftClick()
    // {
    //     //if the item is null, this means we need to pick up the slot item

    //     // //PICK UP AN ITEM
    //     // if (heldItem == null && canInteract)
    //     // {
    //     //     if (item != null)
    //     //     {
    //     //         //set the "hand" information
    //     //         heldItem = item;
    //     //         heldCount = count;
    //     //         inventoryHand.SetHandData(item.Icon, count);

    //     //         //clear the "slot" information
    //     //         itemRemoved?.Invoke(heldItem as UpgradeItem);
    //     //         EmptySlot();
    //     //     }

    //     // }
    //     // else
    //     // {
    //     //     Debug.Log($"Trying to place item {heldItem.name}");
    //     //     //PLACE YOUR ITEM
    //     //     if (item == null && canInteract && ItemDatabase.Instance.GetItem(heldItem.Id).Type == ItemType.Upgrade)
    //     //     {
    //     //         if (heldCount == 1)
    //     //         {
    //     //             //SetItem(heldItem.Id, heldCount, true);
    //     //             itemAdded?.Invoke(heldItem as UpgradeItem); //fire itemAdded event for InspcetionMenu to listen
    //     //             heldItem = null;
    //     //             heldCount = 0;

    //     //             inventoryHand.SetHandData(null, 0);
    //     //         }
    //     //         else if (heldCount != 0)
    //     //         {
    //     //             //SetItem(heldItem.Id, 1, true);
    //     //             itemAdded?.Invoke(heldItem as UpgradeItem); //fire itemAdded event for InspcetionMenu to listen
    //     //             heldCount--;

    //     //             inventoryHand.SetHandData(heldItem.Icon, heldCount);
    //     //         }
    //     //     }
    //     //     //SWAP ITEMS
    //     //     else if (canInteract)
    //     //     {
    //     //         //Trying to add held item ONTO the stack
    //     //         if (ItemDatabase.Instance.GetItem(heldItem.Id).Type == ItemType.Upgrade && heldCount == 1)
    //     //         {
    //     //             itemSwapped?.Invoke(item as UpgradeItem, heldItem as UpgradeItem);
    //     //             //temp copy of the item we're holding
    //     //             BaseItem tempItem = heldItem;
    //     //             int tempCount = heldCount;

    //     //             //set the hand data to the item in the slot
    //     //             heldItem = item;
    //     //             heldCount = count;
    //     //             inventoryHand.SetHandData(item.Icon, count);

    //     //             //now we can empty the slot
                    
    //     //             EmptySlot();
    //     //             //now set the data of the slot to what we're holding
    //     //             //SetItem(tempItem.Id, tempCount, true);
    //     //         }

    //     //     }
    //     // }
    // }
}
