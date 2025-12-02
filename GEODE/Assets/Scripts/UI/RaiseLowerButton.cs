using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class RaiseLowerButton : MonoBehaviour
{
    [SerializeField] private LowerableWall lowerableWall;
    [SerializeField] private TMP_Text buttonText;

    private void Start()
    {
        lowerableWall.OnWallLowered += HandleWallLowered;
        lowerableWall.OnWallRaised += HandleWallRaised;
    }

    private void OnDestroy()
    {
        if(lowerableWall != null)
        {
            lowerableWall.OnWallLowered -= HandleWallLowered;
            lowerableWall.OnWallRaised -= HandleWallRaised;
        }
       
    }

    private void HandleWallRaised()
    {
        buttonText.text = "LOWER";
    }

    private void HandleWallLowered()
    {
        buttonText.text = "RAISE";
    }

    public void ButtonClicked()
    {
        lowerableWall.ToggleWallServerRpc();
    }
}
