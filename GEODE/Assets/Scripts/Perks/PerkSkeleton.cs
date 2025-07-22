using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PerkSkeleton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    [Header("Perk")]
    public PerkData perk;


    [Header("UI References")]
    public Image perkSprite;

    [Header("Tooltip")]
    public GameObject perkTooltip;
    public TMP_Text tooltipName;
    public TMP_Text tooltipDescription;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void Initialize(PerkData perk)
    {
        this.perk = perk;
        perkSprite.sprite = perk.icon;
        tooltipName.text = perk.PerkName;
        tooltipDescription.text = perk.description;
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
