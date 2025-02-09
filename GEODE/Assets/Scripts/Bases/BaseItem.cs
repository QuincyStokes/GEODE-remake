using System.ComponentModel;
using System.Linq.Expressions;
using Unity.Burst.Intrinsics;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements.Experimental;


public abstract class BaseItem : ScriptableObject
{
    

    [SerializeField]private new string name;
    [SerializeField]private string description;
    [SerializeField]private ItemType type;
    [SerializeField]private Sprite icon;
    [SerializeField]private int id;

    public string Name 
    {
        get => name;
        private set => name = value;
    }

    public string Description 
    {
        get => description;
        private set => description = value;
    }

    public ItemType Type 
    {
        get => type;
        private set => type = value;
    }

    public Sprite Icon 
    {
        get => icon;
        private set => icon = value;
    }

    public int Id
    {
        get => id;
        private set => id = value;
    }

    public abstract void Use();


}
