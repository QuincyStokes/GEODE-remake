using UnityEngine;

public interface IUniqueMenu
{
    public GameObject UniqueUI { get; }

    public abstract void ShowMenu();
    public abstract void HideMenu();
}
