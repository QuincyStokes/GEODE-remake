using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class InspectionMenuUI : ContainerUIManager<InspectionMenu>
{
    //* --------------- InspectionMenu Holder --------------*/
    [Header("Inspection Menu")]
    //* --------------- Name and Image --------------*/
    [Header("Name and Image")]
    [SerializeField] private TMP_Text inspectName;
    [SerializeField] private Image inspectImage;


    //* --------------- Stat TMP References --------------*/
    [Header("Stats")]
    [SerializeField] private TMP_Text strength;
    [SerializeField] private TMP_Text speed;
    [SerializeField] private TMP_Text size;
    [SerializeField] private TMP_Text sturdy;
    [SerializeField] private SimpleToggle rangeToggle;

    //* --------------- Health and XP --------------*/
    [Header("Health and XP")]
    [SerializeField] private TMP_Text description;
    [SerializeField] private TMP_Text level;
    [SerializeField] private TMP_Text health;
    [SerializeField] private TMP_Text xp;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider xpSlider;

    //* --------------- Slots--------------*/
    [Header("Slots")]
    [SerializeField] private GameObject upgradeSlotPrefab;

    //* --------------- Inspection Menu Groups --------------*/
    [Header("Groups")]
    [SerializeField] private List<GameObject> objectThings; //ui elements pertaining to base object
    [SerializeField] private List<GameObject> statsThings; //ui elements pertaining to stats
    [SerializeField] private List<GameObject> xpThings; //ui elements pertaining to xp
    [SerializeField] private List<GameObject> upgradeThings;
    [SerializeField] private List<GameObject> dismantleThings;
    [SerializeField] private List<GameObject> chestThings;


    //* ------- PRIVATE INTERNAL -------------


    //* ---------------- Methods ---------------------- */

    private new void Awake()
    {
        base.Awake();
        container.Ready += InitialSync;
    }

    private void InitialSync()
    {
        container.OnMenuOpened += PopulateMenu;
        container.InspectedObjectChanged += ChangeSubscription;
        //container.OnMenuOpened += container.SyncUpgradesToContainerServerRpc;
    }


    public void PopulateMenu()
    {
        if (container.currentInspectedObject == null) return;

        //need to clear and populate Slots. Don't need to actually destroy them, just fill them?

        BaseObject bo = container.currentInspectedObject.GetComponent<BaseObject>();
        IStats stats = container.currentInspectedObject.GetComponent<IStats>();
        IExperienceGain exp = container.currentInspectedObject.GetComponent<IExperienceGain>();
        IUpgradeable upg = container.currentInspectedObject.GetComponent<IUpgradeable>();
        IDismantleable dis = container.currentInspectedObject.GetComponent<IDismantleable>();
        IChest chest = container.currentInspectedObject.GetComponent<IChest>();
        //since we have passed in our stats, xp, and theobject, we can guarantee that we have:
        //all of the necessary information to populate the menu
        if (bo != null) //health, description, sprite
        {
            SetGroup(objectThings, true);
            inspectName.text = bo.ObjectName;
            inspectImage.sprite = bo.objectSprite;
            healthSlider.maxValue = bo.MaxHealth.Value;
            healthSlider.minValue = 0;
            healthSlider.value = bo.CurrentHealth.Value;
            health.text = $"{bo.CurrentHealth.Value:F1}/{bo.MaxHealth.Value:F1}";
            sturdy.text = bo.MaxHealth.Value.ToString("F1");
            description.text = bo.description;

            bo.CurrentHealth.OnValueChanged += RefreshHealth;
        }
        else
        {
            Debug.Log("BaseObject was null");
            SetGroup(objectThings, false);
        }

        if (stats != null) //all of the stat modifiers
        {
            SetGroup(statsThings, true);

            //* Subscribe to the NV's callbacks

            stats.strength.OnValueChanged += RefreshStats;
            stats.speed.OnValueChanged += RefreshStats;
            stats.size.OnValueChanged += RefreshStats;
            stats.sturdy.OnValueChanged += RefreshStats;

            rangeToggle.SetToggle(stats.ShowingRange);


            RefreshStats(0f, 0f);

        }
        else
        {
            Debug.Log("Stats was null");
            SetGroup(statsThings, false);
        }

        if (exp != null) //xp things like current/total/level
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

        if (upg != null)
        {
            SetGroup(upgradeThings, true);
        }
        else
        {
            SetGroup(upgradeThings, false);
        }

        if (dis != null)
        {
            SetGroup(dismantleThings, true);
        }
        else
        {
            SetGroup(dismantleThings, false);
        }

        if (chest != null)
        {
            SetGroup(chestThings, true);
        }
        else
        {
            Debug.Log("IChest was null."); 
            SetGroup(chestThings, false);
        }


        
    }
    private void ChangeSubscription(GameObject old, GameObject newObj)
    {
        if (old != null)
        {
            IUpgradeable upg = old.GetComponent<IUpgradeable>();
            if (upg != null)
            {
                upg.OnUpgradesChanged -= RefreshUpgrades;
            }
            BaseObject obj = old.GetComponent<BaseObject>();
            if (obj != null)
            {
                obj.CurrentHealth.OnValueChanged -= RefreshHealth;
            }

        }

        if (newObj != null)
        {
            IUpgradeable newUpg = newObj.GetComponent<IUpgradeable>();
            if (newUpg != null)
            {
                newUpg.OnUpgradesChanged += RefreshUpgrades;
            }
        }
        else
        {
            
        }
    }


    private void RefreshStats(float oldValue, float newValue)
    {
        if (container.currentInspectedObject == null) return;
        IStats stats = container.currentInspectedObject.GetComponent<IStats>();
        if (container.currentInspectedObject != null && stats != null)
        {
            strength.text = $"<color=#b4202a>DMG {stats.strength.Value:F1}</color>\n{stats.baseStrength.Value:F1}(<color=#b4202a>+{(stats.baseStrength.Value * ((stats.strengthModifier.Value / 100) + 1)) - stats.baseStrength.Value:F1}</color>)";

            //SPEED
            speed.text = $"<color=#fffc40>SPD {stats.speed.Value:F1}</color>\n{stats.baseSpeed.Value:F1}(<color=#fffc40>+{(stats.baseSpeed.Value * ((stats.speedModifier.Value / 100) + 1)) - stats.baseSpeed.Value:F1}</color>)";

            //SIZE
            size.text = $"<color=#249fde>RNG {stats.size.Value:F1}</color>\n{stats.baseSize.Value:F1}(<color=#249fde>+{(stats.baseSize.Value * ((stats.sizeModifier.Value / 100) + 1)) - stats.baseSize.Value:F1}</color>)";
        }

        BaseObject bo = container.currentInspectedObject.GetComponent<BaseObject>();
        if (container.currentInspectedObject != null && bo != null)
        {
            //STURDY
            sturdy.text = $"<color=#14a02e>HP {stats.sturdy.Value:F1}</color>\n{bo.MaxHealth.Value:F1}(<color=#14a02e>+{(bo.MaxHealth.Value * ((stats.sturdyModifier.Value / 100) + 1)) - bo.MaxHealth.Value:F1}</color>)";
        }
    }

    private void RefreshUpgrades()
    {
        PopulateMenu();
    }

    private void RefreshHealth(float oldValue, float newValue)
    {
        if (container.currentInspectedObject == null) return;
        BaseObject bo = container.currentInspectedObject.GetComponent<BaseObject>();
        if (bo != null)
        {
            healthSlider.maxValue = bo.MaxHealth.Value;
            healthSlider.minValue = 0;
            healthSlider.value = bo.CurrentHealth.Value;
            health.text = $"{bo.CurrentHealth.Value}/{bo.MaxHealth.Value}";
        }
    }


    private void SetGroup(List<GameObject> group, bool set)
    {
        foreach (GameObject go in group)
        {
            go.SetActive(set);
        }
    }

    public void DismantleCurrentObject()
    {
        container.currentInspectedObject.GetComponent<BaseObject>().DestroyThis(true);
        container.CloseInspectionMenu();
    }

    public void ToggleRange()
    {
        BaseTower to = container.currentInspectedObject.GetComponent<BaseTower>();
        if (to != null)
        {
            to.ToggleRangeIndicator();
            rangeToggle.SetToggle(to.ShowingRange);
        }
    }
   


}
