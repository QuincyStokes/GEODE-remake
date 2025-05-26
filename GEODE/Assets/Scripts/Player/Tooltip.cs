using TMPro;
using UnityEngine;

public class Tooltip : MonoBehaviour
{
    [SerializeField] private TMP_Text _itemNameTMP;
    [SerializeField] private TMP_Text _itemDescriptionTMP;
    [SerializeField] private TMP_Text _itemTypeTMP;
    [SerializeField] private TMP_Text _itemStatsTMP;
    [SerializeField] private TMP_Text _itemQualityTMP;

    public void Build(int itemId)
    {
        BaseItem item = ItemDatabase.Instance.GetItem(itemId);

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
                    _itemQualityTMP.text = "\n" + upgItem.Quality.ToString() + "%";
                    foreach (Upgrade upgrade in upgItem.upgradeList)
                    {
                        _itemStatsTMP.text += $"{upgrade.upgradeType} : {upgrade.percentIncrease}\n";
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

    }

    public void Reset()
    {
        
    }
}
