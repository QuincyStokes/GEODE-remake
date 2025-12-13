using UnityEngine;

public class CrystalRefineryUIManager : ContainerUIManager<CrystalRefineryUI>
{
    
    // protected override void InitializeSlots()
    // {
    //     base.InitializeSlots();
    //     //HOLDUP there might be a better way to do this.
    //     //Let's just make a new Slot type that doesn't allow users to place things in through clicks, their items can only be set through other means..?
    //     //Set all of the subContainers[1] slots to not interactable
    //     foreach(Slot s in slots)
    //     {
    //         if(s.SlotIndex >= container.subContainers[0].numSlots)
    //         {
    //             Debug.Log($"Setting slot {s.SlotIndex} to not interactable");
    //             s.canInteract = false;
    //         }
    //     }
    // }
    
}
