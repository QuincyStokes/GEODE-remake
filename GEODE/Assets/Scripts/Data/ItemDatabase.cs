using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Items/ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    private static ItemDatabase instance;
    [SerializeField] private List<BaseItem> items;
    private Dictionary<int, BaseItem> itemDictionary = new Dictionary<int, BaseItem>();

    public static ItemDatabase Instance
    {
        get => instance;
    }

    private void OnEnable()
    {
        if(instance == null)
        {
            instance = this;
            BuildDictionary();
        }
        else
        {
            Debug.Log("ERROR. MULTIPLE ITEMDATABASES");
        }
    }

    private void BuildDictionary()
    {
        foreach(BaseItem item in items)
        {
            itemDictionary.Add(item.Id, item);
        }
        Debug.Log("Item Dictionary Loaded.");
    }

    public BaseItem GetItem(int id)
    {
        return itemDictionary[id];
    }
    

    

   
}
