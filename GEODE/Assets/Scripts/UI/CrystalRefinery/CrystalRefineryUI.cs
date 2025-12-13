using UnityEngine;

public class CrystalRefineryUI : BaseContainer
{

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        
    }
     
    private void OnEnable()
    {
        //this is objectively wrong and confusing, but gonna rock with it for now
        isOpen = true;
    }

    public override void ProcessSlotClick(Slot slot)
    {
        //Need to do a few things
        //1. Only allow crystal items. 
        //2. Every time an item is placed (or for that matter, removed), we need to check conditions for starting the Refinery process
            //2.1. (Slightly temporary), if there are three of the same crystal item, combine them into a single, stronger version.
            //2.2. Will need some sort of mapping from crystal type to upgraded crystal type.
            //2.3. Can expand this into cool recipies or something, a lot of potential actually
        //3. If the process is started, (decision to make), we either lock the slots until its done, or stop if the player removes an item.

        //Let's begin.

        int idx = slot.SlotIndex;
        if (idx >= ContainerItems.Count) return;
        ItemStack slotStack = ContainerItems[idx];
        ItemStack cursorStack = CursorStack.Instance.ItemStack;


       
        // --------- CURSOR HOLDING SOMETHING ---------
        if(!cursorStack.IsEmpty())
        {
             //Only allow crystal subtype items for now. 
            if(ItemDatabase.Instance.GetItem(cursorStack.Id).subType != ItemType.Crystal) return;

             //Only allow placement of items in the first subcontainer.
             //Theres a milllion ways to do this, but this is the smoothest/easiest way for now. Not necessarily the "best"  though.
            if(idx >= subContainers[0].numSlots) return;

            //------- PLACE ITEM ---------
            if(!slotStack.IsEmpty())
            {
                // ------- SWAP IF POSSIBLE ------
            }
            // ---------- PLACE ITEM ---------
            else
            {
                int after = CursorStack.Instance.ItemStack.amount - 1;
                if(after == 0)
                {
                    CursorStack.Instance.ItemStack = ItemStack.Empty;
                }
                else
                {
                    CursorStack.Instance.Amount -= 1;
                }
                slot.SetItem(cursorStack.Id, 1);
                MoveStackServerRpc(-1, idx, cursorStack.Id, 1);
            }
        }
        // --------- CURSOR EMPTY ---------
        else
        {
            //PICK UP ITEM 
            if(slotStack.IsEmpty()) return;
            slot.SetItem();
            CursorStack.Instance.ItemStack = slotStack;
            MoveStackServerRpc(idx, -1, slotStack.Id, 0);
            return;
        }
        




    }
     
}
