using UnityEngine;

public class Chest : SimpleObject, IChest
{
    public ChestUI chestUI;
    public Chest ChestObj { get => this; }
}
