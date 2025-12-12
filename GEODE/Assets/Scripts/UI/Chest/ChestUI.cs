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

    //THIS IS FRAUD. This actually just sets isOpen to true at the start, but never to false since the chest itself is never Disabled.
    private void OnEnable()
    {
        isOpen = true;
    }

    private void OnDisable()
    {
        isOpen = false;
    }
}
