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
    [SerializeField]private bool isStackable;
    [SerializeField]private bool consumeOnUse;

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

    public bool IsStackable
    {
        get=>isStackable;
        private set => isStackable = value;
    }

    public bool ConsumeOnUse
    {
        get=>consumeOnUse;
        private set => consumeOnUse = value;
    }
    public abstract bool Use(Vector3 position);


}
