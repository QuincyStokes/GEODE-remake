using System;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleObject : BaseObject, IInteractable, IDismantleable, ITrackable
{
    [SerializeField] private Color higlightColor;

    public event Action<StatTrackType, string> OnSingleTrack;
    public event Action<StatTrackType, string, int> OnMultiTrack;
    public SoundId placeSfxId;

    protected override void Start()
    {
        base.Start();
        OnSingleTrack += StatTrackManager.Instance.AddOne;
        OnMultiTrack += StatTrackManager.Instance.AddMultiple;
        if(placeSfxId != SoundId.NONE)
            AudioManager.Instance.PlayClientRpc(placeSfxId, transform.position);
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
