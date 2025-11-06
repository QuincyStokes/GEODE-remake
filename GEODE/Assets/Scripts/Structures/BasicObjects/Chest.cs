using UnityEngine;

public class Chest : SimpleObject, IChest, IUniqueMenu
{
    public Chest ChestObj { get => this; }
    [Header("Unique UI")]
    [SerializeField] private GameObject uniqueUI;
    public GameObject UniqueUI => uniqueUI;

    public void ShowMenu()
    {
        UniqueUI.SetActive(true);
    }

    public void HideMenu()
    {
        UniqueUI.SetActive(false);
    }
}
