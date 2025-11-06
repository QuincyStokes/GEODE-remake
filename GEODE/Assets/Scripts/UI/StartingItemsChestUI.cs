using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StartingItemsChestUI : ChestUI
{
    public List<int> possibleItemSpawns;
    public int numItemsRolled;
    protected override void SeedItemList()
    {
        ContainerItems.Clear();

        // make list as long as total slots
        int totalSlots = subContainers.Sum(sc => sc.numSlots);
        for (int i = 0; i < totalSlots; i++)
            ContainerItems.Add(ItemStack.Empty);

        for (int i = 0; i < numItemsRolled; i++)
        {
            AddItemInternal(possibleItemSpawns[UnityEngine.Random.Range(0, possibleItemSpawns.Count)], 1, 0);
        }
        
        foreach (var item in startingItems)
            AddItemInternal(item.item.Id, item.amount, item.item.quality);
    }
}
