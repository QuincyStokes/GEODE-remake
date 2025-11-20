using System;
using System.ComponentModel;
using UnityEngine;

public class ChestUI : BaseContainer
{
    public Chest chest;

    private void Start()
    {
        chest.OnClick += HandleChestClicked;
    }

    private void HandleChestClicked()
    {
        
    }

    private void OnEnable()
    {
        isOpen = true;
    }

    private void OnDisable()
    {
        isOpen = false;
    }
}
