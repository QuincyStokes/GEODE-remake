using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Perk : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Perk")]
    public PerkData perk;
    [SerializeField] private PerkCollection _perkCollection;
    public Sprite perkSelectedBG;
    public Sprite perkUnselectedBG;


    [Header("UI References")]
    public Image perkSprite;
    public Image perkBackground;

    //* ---------- Events ----------- */
    public event Action OnPerkSelected;


    //* ------------ Internal --------- */

    private PerkCollection _pc;

    private void Awake()
    {
        if (perk != null)
        {
            Initialize(perk, _perkCollection);
        }

    }
    public void SelectPerk()
    {
        Debug.Log($"Perk {name} selected.");
        OnPerkSelected?.Invoke();
        perkBackground.sprite = perkSelectedBG;
        RunSettings.Instance.chosenPerks.Add(perk);
    }

    public void DeselectPerk()
    {
        RunSettings.Instance.chosenPerks.Remove(perk);
        perkBackground.sprite = perkUnselectedBG;
    }

    public void Initialize(PerkData perk, PerkCollection pc)
    {
        Debug.Log($"Initializing Perk {name}");
        _pc = pc;
        OnPerkSelected += _pc.UnselectAllPerks;
        this.perk = perk;
        perkSprite.sprite = perk.icon;
        
    }

    private void OnDestroy()
    {
        OnPerkSelected -= _pc.UnselectAllPerks;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        
    }
}
