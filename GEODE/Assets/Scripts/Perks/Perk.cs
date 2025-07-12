using System;
using UnityEngine;
using UnityEngine.UI;

public class Perk : MonoBehaviour
{
    [Header("Perk")]
    [HideInInspector]public PerkData perk;

    [Header("UI References")]
    public Image perkSprite;

    //* ---------- Events ----------- */
    public event Action OnPerkSelected;


    //* ------------ Internal --------- */

    private PerkCollection _pc;
    public void SelectPerk()
    {
        Debug.Log($"Perk {name} selected.");
        OnPerkSelected?.Invoke();
        RunSettings.Instance.chosenPerks.Add(perk);
    }

    public void DeselectPerk()
    {
        RunSettings.Instance.chosenPerks.Remove(perk);
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
}
