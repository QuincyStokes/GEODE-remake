using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Perk : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Perk")]
    public PerkData perk;
    private PerkCollection _perkCollection;
    public Sprite perkSelectedBG;
    public Sprite perkUnselectedBG;


    [Header("UI References")]
    public Image perkSprite;
    public Image perkBackground;
    public Button button;

    [Header("Tooltip")]
    public GameObject perkTooltip;
    public TMP_Text tooltipName;
    public TMP_Text tooltipDescription;
    public TMP_Text tooltipRequirement;

    //* ---------- Events ----------- */
    public event Action OnPerkSelected;


    //* ------------ Internal --------- */

    private PerkCollection _pc;

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
        tooltipName.text = perk.PerkName;
        tooltipDescription.text = perk.description;
        //tooltiprequirement.text = perk.

        if(perk is PlayerStatPerk)
        {
            PlayerStatPerk p = perk as PlayerStatPerk;
            tooltipRequirement.text = $"{p.statRequirement} {p.requirementAmount} {p.requirementKey}s.";
        }

        if (perk.IsUnlocked(StatTrackManager.Instance.GetPlayerStats()))
        {
            button.interactable = true;
        }
        else
        {
            button.interactable = false;
        }
    }

    public void InitializeSkeleton(PerkData perk)
    {
        this.perk = perk;
        perkSprite.sprite = perk.icon;
        tooltipName.text = perk.PerkName;
        tooltipDescription.text = perk.description;
    }

    private void OnDestroy()
    {
        OnPerkSelected -= _pc.UnselectAllPerks;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        perkTooltip.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        perkTooltip.SetActive(false);
    }
}
