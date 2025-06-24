using UnityEngine;
using UnityEngine.EventSystems;
public interface IInteractable : IPointerEnterHandler, IPointerExitHandler
{  
    public new void OnPointerEnter(PointerEventData eventData); //these work properly, nice
    public new void OnPointerExit(PointerEventData eventData);

    //dont need anything like Inspect() because it's more of a flag, inspect detection comes from PlayerController.cs
}
