
using UnityEngine;
using TMPro;

public abstract class BaseItem : ScriptableObject
{


    public new string name;
    public string description;
    public ItemType type;
    public ItemType subType;
    public Sprite icon;
    public int id;
    public bool isStackable;
    public bool consumeOnUse;

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
        get => isStackable;
        private set => isStackable = value;
    }

    public bool ConsumeOnUse
    {
        get => consumeOnUse;
        private set => consumeOnUse = value;
    }
    public abstract bool Use(Vector3 position, bool snapToGrid = true, bool force = false);


}
