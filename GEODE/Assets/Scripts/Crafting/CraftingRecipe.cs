using UnityEngine;
using System.Collections.Generic;

public struct ItemAmount
{
    public BaseItem item;
    [Range(1,999)]
    public int amount;
}

public class CraftingRecipe : ScriptableObject
{
    public List<ItemAmount> materials;
    public List<ItemAmount> results;

    //previously I had the recipe handle whether or not it could be crafted, i dont think that makes much sense\
    //lets do it in the CraftingManager


}

