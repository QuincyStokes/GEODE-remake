using System.ComponentModel;
using System.Linq.Expressions;
using Unity.Burst.Intrinsics;
using UnityEditor;
using UnityEngine;

public abstract class BaseItem : ScriptableObject
{
    public enum ItemType{
        Tool,
        Weapon,
        Structure,
        Consumable
    }

    private new string name;
    private string description;
    private ItemType type;
    private Sprite icon;


    public void Use()
    {
        //define use action for object here. ie swing, consume, place
    }

    public string Name()
    {
        return name;
    }

    public string Description()
    {
        return description;
    }

    public ItemType Type()
    {
       return type;
    }

    public Sprite Icon()
    {
        return icon;
    }



    


}
