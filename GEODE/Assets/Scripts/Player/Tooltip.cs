using TMPro;
using UnityEngine;

public class TooltipService : MonoBehaviour
{
    public static TooltipService Instance { get; private set; }

    private Slot currentProvider;
    [SerializeField] private Transform tooltipRoot;
    [SerializeField] private TMP_Text _itemNameTMP;
    [SerializeField] private TMP_Text _itemDescriptionTMP;
    [SerializeField] private TMP_Text _itemTypeTMP;
    [SerializeField] private TMP_Text _itemStatsTMP;
    [SerializeField] private TMP_Text _itemQualityTMP;


    private void Awake()
    {
        Instance = this;
    }

    private void LateUpdate()
    {
        if (currentProvider != null)
        {
            tooltipRoot.position = Input.mousePosition;
        }
    }

    public void RequestShow(Slot provider)
    {
        Debug.Log("Attempting to show tooltip.");
        if (!provider.HasTooltip)
        {
            return;
        }
        currentProvider = provider;
        int id = provider.displayedStack.Id;
        float quality = provider.displayedStack.quality;
        if (id == -1) return;
        Debug.Log($"Made it past guards, id = {id}");
        BaseItem item = ItemDatabase.Instance.GetItem(id);

        _itemNameTMP.gameObject.SetActive(false);
        _itemDescriptionTMP.gameObject.SetActive(false);
        _itemTypeTMP.gameObject.SetActive(false);
        _itemStatsTMP.gameObject.SetActive(false);
        _itemQualityTMP.gameObject.SetActive(false);

        //Enable and set the text of each relevant tooltip section
        //First, Basic Item properties
        _itemNameTMP.gameObject.SetActive(true);
        _itemNameTMP.text = item.name;

        _itemDescriptionTMP.gameObject.SetActive(true);
        _itemDescriptionTMP.text = item.Description;

        _itemTypeTMP.gameObject.SetActive(true);

        //Now we need to cover different itemTypes
        _itemStatsTMP.text = "";
        _itemTypeTMP.text = item.Type.ToString();

        switch (item.Type)
        {
            case ItemType.Tool:
                ToolItem tool = item as ToolItem;
                if (tool != null)
                {
                    _itemStatsTMP.gameObject.SetActive(true);

                    //tooltipItemStats.text += item.speed; HAVENT ACTUALLY IMPLEMENTED SWING SPEED YET (or damage)
                    _itemStatsTMP.text += $"DMG | {tool.damage}";
                }
                break;

            case ItemType.Weapon:
                WeaponItem weapon = item as WeaponItem;
                if (weapon != null)
                {
                    _itemStatsTMP.gameObject.SetActive(true);
                    //tooltipItemStats.text += item.speed; HAVENT ACTUALLY IMPLEMENTED SWING SPEED YET (or damage)
                    _itemStatsTMP.text += $"DMG | {weapon.damage}";
                }

                break;

            case ItemType.Upgrade:
                _itemStatsTMP.gameObject.SetActive(true);
                UpgradeItem upgItem = item as UpgradeItem;
                if (upgItem != null)
                {
                    _itemQualityTMP.gameObject.SetActive(true);
                    _itemQualityTMP.text = "\n" + quality.ToString("N1") + "%";
                    foreach (Upgrade upgrade in upgItem.upgradeList)
                    {
                        _itemStatsTMP.text += $"{upgrade.upgradeType} : {upgrade.percentIncrease * (quality/100)}\n";
                    }
                }
                break;

            case ItemType.Structure:
                StructureItem structItem = item as StructureItem;
                if (structItem != null)
                {
                    _itemStatsTMP.text += $"Health = {structItem.prefab.GetComponent<BaseObject>()?.MaxHealth}\n";
                    _itemStatsTMP.text += $"Size = {structItem.width}x{structItem.height}\n";
                }

                break;
        }
        //lastly, enable it!
        Debug.Log("Enabling tooltip!");
        tooltipRoot.gameObject.SetActive(true);
        
    }


    public void Hide()
    {
        currentProvider = null;
        tooltipRoot.gameObject.SetActive(false);
    }
}
