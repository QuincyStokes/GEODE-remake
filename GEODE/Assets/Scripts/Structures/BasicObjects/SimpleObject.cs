using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleObject : BaseObject, IInteractable, IDismantleable, ITrackable
{
    [SerializeField] private Color higlightColor;

    public event Action<StatTrackType, string> OnSingleTrack;
    public event Action<StatTrackType, string, int> OnMultiTrack;

    private void Start()
    {
        OnSingleTrack += StatTrackManager.Instance.AddOne;
        OnMultiTrack += StatTrackManager.Instance.AddMultiple;

        //tell the stat tracker we placed an object
        OnSingleTrack?.Invoke(StatTrackType.StructurePlace, ObjectTransform.name);
    }

    private void OnDisable()
    {
        OnSingleTrack -= StatTrackManager.Instance.AddOne;
        OnMultiTrack -= StatTrackManager.Instance.AddMultiple;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        sr.color = higlightColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        sr.color = Color.white;
    }
}
