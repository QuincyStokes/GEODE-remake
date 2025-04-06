using UnityEngine;
using UnityEngine.EventSystems;
public interface IInteractable : IPointerEnterHandler, IPointerExitHandler
{
    public new void OnPointerEnter(PointerEventData eventData); //these work properly, nice
    public new void OnPointerExit(PointerEventData eventData);

    public abstract void OnInteract();
    public abstract void PopulateInteractionMenu();

}
