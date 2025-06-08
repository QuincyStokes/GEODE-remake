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
            health.text = $"{bo.CurrentHealth.Value}/{bo.MaxHealth.Value}";
            sturdy.text = bo.MaxHealth.Value.ToString();
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
    }


    private void RefreshStats(float oldValue, float newValue)
    {
        IStats stats = container.currentInspectedObject.GetComponent<IStats>();
        if (container.currentInspectedObject != null && stats != null)
        {
            strength.text = $"<color=red>DMG: {stats.strength.Value}</color> = {stats.baseStrength.Value}(<color=red>+{(stats.baseStrength.Value * ((stats.strengthModifier.Value / 100) + 1)) - stats.baseStrength.Value}</color>)";

            //SPEED
            speed.text = $"<color=yellow>SPD: {stats.speed.Value}</color> = {stats.baseSpeed.Value}(<color=yellow>+{(stats.baseSpeed.Value * ((stats.speedModifier.Value / 100) + 1)) - stats.baseSpeed.Value}</color>)";

            //SIZE
            size.text = $"<color=blue>RNG: {stats.size.Value}</color> = {stats.baseSize.Value}(<color=blue>+{(stats.baseSize.Value * ((stats.sizeModifier.Value / 100) + 1)) - stats.baseSize.Value}</color>)";
        }

        BaseObject bo = container.currentInspectedObject.GetComponent<BaseObject>();
        if (container.currentInspectedObject != null && bo != null)
        {
            //STURDY
            sturdy.text = $"<color=green>HP: {stats.sturdy.Value}</color> = {bo.BASE_HEALTH}(<color=green>+{(bo.MaxHealth.Value * ((stats.sturdyModifier.Value / 100) + 1)) - bo.MaxHealth.Value}</color>)";
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




}
