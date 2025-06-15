using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleObject : BaseObject, IInteractable, IDismantleable
{
    [SerializeField] private Color higlightColor;
    public void OnPointerEnter(PointerEventData eventData)
    {
        sr.color = higlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        sr.color = Color.white;
    }
}
