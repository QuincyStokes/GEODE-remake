using System.Collections.Generic;
using UnityEngine;

public interface IContainer 
{
    public List<Slot> slots 
    {
        get{return slots;}
        set{slots = value;}
    }

    public void ToggleInventory();

    public BaseItem GetItemAtPosition();

    public int FindItem(BaseItem item);
    public bool SwapItems(); //might delete

}
