using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public interface IContainer 
{
    public List<Slot> slots 
    {
        get{return slots;}
        set{slots = value;}
    }

    public void ToggleInventory(InputAction.CallbackContext context);

    public BaseItem GetItemAtPosition();

    public int FindItem(BaseItem item);
    public bool SwapItems(); //might delete

}
