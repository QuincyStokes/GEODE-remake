using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class InspectionMenu : MonoBehaviour
{
    public static InspectionMenu Instance;
    [SerializeField] private GameObject InspectionMenuHolder;
    [Header("Name and Image")]
    [SerializeField] private TMP_Text inspectName;
    [SerializeField] private Image inspectImage;

    [Header("Stats")]    
    [SerializeField] private TMP_Text strength;
    [SerializeField] private TMP_Text speed;
    [SerializeField] private TMP_Text size;
    [SerializeField] private TMP_Text sturdy;

    [Header("Health and XP")]
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text level;
    [SerializeField] private TMP_Text health;
    [SerializeField] private TMP_Text xp;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider xpSlider;
    
    [Header("Upgrades")]
    [SerializeField] private GameObject upgradeSlotHolder;
    [SerializeField] private List<Slot> upgradeSlots;

    [Header("Groups")]
    [SerializeField] private List<GameObject> objectThings; //ui elements pertaining to base object

    [SerializeField] private List<GameObject> statsThings; //ui elements pertaining to stats
    [SerializeField] private List<GameObject> xpThings; //ui elements pertaining to xp
    [SerializeField] private List<GameObject> upgradeThings;

    //* ------- PRIVATE INTERNAL -------------
    private GameObject currentInspectedObject;


    //* METHODS
    private void Awake()
    {
    }

    public void PopulateMenu(GameObject go)
    {
        //TODO think about having these things constantly set in Update, so we can see changes live
        if(go == currentInspectedObject)
        {
            return;
        }
        currentInspectedObject = go;
        if(InspectionMenuHolder.activeSelf == false)
        {
            InspectionMenuHolder.SetActive(true);
        }
        
        BaseObject bo = go.GetComponent<BaseObject>();
        IStats stats = go.GetComponent<IStats>();
        IExperienceGain exp = go.GetComponent<IExperienceGain>();
        IUpgradeable upg = go.GetComponent<IUpgradeable>();
        //since we have passed in our stats, xp, and theobject, we can guarantee that we have:
        //all of the necessary information to populate the menu
        if(bo != null) //health, description, sprite
        {
            SetGroup(objectThings, true);
            inspectName.text = bo.ObjectName;
            inspectImage.sprite = bo.objectSprite;
            healthSlider.maxValue = bo.MaxHealth;
            healthSlider.minValue = 0;
            healthSlider.value = bo.CurrentHealth;
            health.text = $"{bo.CurrentHealth}/{bo.MaxHealth}";
            sturdy.text = bo.MaxHealth.ToString();
            description.text = bo.description;
        }
        else
        {
            Debug.Log("BaseObject was null");
            SetGroup(objectThings, false);
        }

        if(stats != null) //all of the stat modifiers
        {
            SetGroup(statsThings, true);
            //CAN DO CUSTOM COLOR BY DOING <COLOR=#ffffff>
            //STRENGTH
            strength.text = $"<color=red>{stats.Strength}</color> = {stats.BaseStrength}<color=red> (+{stats.BaseStrength * stats.StrengthModifier-stats.BaseStrength}</color>)";

            //SPEED
            speed.text = $"<color=yellow>{stats.Speed}</color> = {stats.BaseSpeed}<color=yellow> (+{stats.BaseSpeed * stats.SpeedModifier-stats.BaseSpeed}</color>)";

            //SIZE
            size.text = $"<color=green>{stats.Size}</color> = {stats.BaseSize}<color=green> (+{stats.BaseSize * stats.SizeModifier-stats.BaseSize}</color>)";

            //STURDY
            sturdy.text = $"<color=blue>{stats.Sturdy}</color> = {bo.MaxHealth}<color=blue> +({bo.MaxHealth * stats.SturdyModifier-bo.MaxHealth}</color>)";

        }
        else
        {
            Debug.Log("Stats was null");
            SetGroup(statsThings, false);
        }

        if(exp != null) //xp things like current/total/level
        {
            SetGroup(xpThings, true);
            level.text = "Level " + exp.Level.ToString();
            xpSlider.maxValue = exp.MaximumLevelXp;
            xpSlider.minValue = 0;
            xpSlider.value = exp.CurrentXp;
            xp.text = $"{exp.CurrentXp}/{exp.MaximumLevelXp}";
        }
        else
        {
            Debug.Log("Xp was null");
            SetGroup(xpThings, false);
        }

        if(upg != null)
        {
            SetGroup(upgradeThings, true);
            for(int i = 0; i < upg.Upgrades.Count; ++i)
            {
                upgradeSlots[i].SetItem(upg.UpgradeItems[i].Id, 1, true);
            }
        }
        else
        {
            SetGroup(upgradeThings, false);
        }
    }

    public void CloseInspectionMenu()
    {
        currentInspectedObject = null;
        InspectionMenuHolder.SetActive(false);
    }

    private void SetGroup(List<GameObject> group, bool set)
    {
        foreach (GameObject go in group)
        {
            go.SetActive(set);
        }
    }
}
