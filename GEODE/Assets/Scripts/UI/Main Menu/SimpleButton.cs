using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimpleButton : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private float activatedTextYOffset;
    [SerializeField] private Color activatedTextColor;

    [Header("Sprite References")]
    [SerializeField] private Sprite activatedSprite;
    [SerializeField] private Sprite deactivatedSprite;

    [Header("UI References")]
    [SerializeField] private Image buttonImage;
    [SerializeField] private TMP_Text text;

    //Internal
    private Color deactivatedTextColor;
    public event Action<SimpleButton> Activated;
    public event Action<SimpleButton> Deactivated;

    
    public bool activated;

    private void Awake()
    {
        deactivatedTextColor = text.GetComponent<TMP_Text>().color;
    }

    public void ClickWrapper()
    {
        // if(activated)
        // {
        //     Deactivate();
        //     Deactivated?.Invoke(this);
        // }
        // else
        // {
        if(activated) return;
        Activate();
        Activated?.Invoke(this);
        //}
    }

    public void Activate()
    {
        if(activated) return;
        activated = true;
        buttonImage.sprite = activatedSprite;
        text.transform.localPosition = new Vector3(text.transform.localPosition.x, text.transform.localPosition.y + activatedTextYOffset, text.transform.localPosition.z);
        text.color = activatedTextColor;
        

    }

    public void Deactivate()
    {
        if(!activated) return;
        activated = false;
        buttonImage.sprite = deactivatedSprite;
        text.transform.localPosition = new Vector3(text.transform.localPosition.x, text.transform.localPosition.y - activatedTextYOffset, text.transform.localPosition.z);
        text.color = deactivatedTextColor;
        
    }
}
