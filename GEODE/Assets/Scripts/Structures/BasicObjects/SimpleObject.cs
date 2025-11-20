using System;
using System.Collections.Generic;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleObject : BaseObject, IInteractable, IDismantleable, ITrackable
{
    [SerializeField] private Color higlightColor;

    public event Action<StatTrackType, string> OnSingleTrack;
    public event Action<StatTrackType, string, int> OnMultiTrack;
    public SoundId placeSfxId;

    [SerializeField] private SpriteRenderer biomeOverlaySprite;

    [SerializeField] private List<BiomeSpritePair> biomeSpritePairs;
    private Dictionary<BiomeType, Sprite> biomeSpriteMap;


    private void Awake()
    {
        biomeSpriteMap = new();
        foreach(var item in biomeSpritePairs)
        {
            biomeSpriteMap.Add(item.biomeType, item.overlaySprite);
        }
    }
    protected override void Start()
    {
        base.Start();
        OnSingleTrack += StatTrackManager.Instance.AddOne;
        OnMultiTrack += StatTrackManager.Instance.AddMultiple;
        if(placeSfxId != SoundId.NONE)
            AudioManager.Instance.PlayClientRpc(placeSfxId, transform.position);

        BiomeType biome = WorldGenManager.Instance.GetBiomeAtPosition(new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z));
        if(biomeSpriteMap.ContainsKey(biome))
        {
            biomeOverlaySprite.sprite = biomeSpriteMap[biome];
        }
        
        
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


    public virtual void DoClickedThings()
    {
        
    }

    public virtual void DoUnclickedThings()
    {
        
    }


    [Serializable]
    public struct BiomeSpritePair
    {
        public BiomeType biomeType;
        public Sprite overlaySprite;
    }
}
