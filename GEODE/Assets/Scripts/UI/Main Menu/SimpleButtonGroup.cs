using UnityEngine;
using System;
using System.Collections.Generic;
public class SimpleButtonGroup : MonoBehaviour
{
    [SerializeField] private List<SimpleButton> buttons = new();
    private SimpleButton currentlyActivatedButton;

    public SimpleButton CurrentlyActivatedButton => currentlyActivatedButton;
    public event Action<SimpleButton> SelectionChanged;

    private void Start()
    {
        foreach(SimpleButton sb in buttons)
        {
            sb.Activated += HandleButtonActivated;
        }

        currentlyActivatedButton = buttons[0];
        buttons[0].Activate();
        SelectionChanged?.Invoke(currentlyActivatedButton);
    }

    private void HandleButtonActivated(SimpleButton button)
    {
        currentlyActivatedButton.Deactivate();
        currentlyActivatedButton = button;
        SelectionChanged?.Invoke(currentlyActivatedButton);
    }
}
