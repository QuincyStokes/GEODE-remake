using System;
using UnityEngine;

public class Chest : SimpleObject, IChest, IUniqueMenu
{
    public Chest ChestObj { get => this; }
    [Header("Unique UI")]
    [SerializeField] private GameObject uniqueUI;
    public GameObject UniqueUI => uniqueUI;
    [Header("Animations")]
    [SerializeField]private Animator _animator;

    public event Action OnClick;

    //* Internal
    private bool isOpen; //ChestUI also has an isOpen, but this is okay because animation is seprate from ui I think.

    public void ShowMenu()
    {
        UniqueUI.SetActive(true);
    }

    public void HideMenu()
    {
        UniqueUI.SetActive(false);
    }

    public override void DoClickedThings()
    {
        base.DoClickedThings();
        _animator.SetBool("isOpen", true);
        
    }

    public override void DoUnclickedThings()
    {
        base.DoUnclickedThings();
        _animator.SetBool("isOpen", false);
        
    }
}
