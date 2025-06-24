using System.ComponentModel;
using UnityEngine;

public class ChestUI : BaseContainer
{
    private void OnEnable()
    {
        isOpen = true;
    }

    private void OnDisable()
    {
        isOpen = false;
    }
}
