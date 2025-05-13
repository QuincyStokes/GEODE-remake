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
    [SerializeField] private InventoryHandUI inventoryHandUI;

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
    [SerializeField] private GameObject upgradeSlotPrefab;

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

    public void PopulateMenu(GameObject go, bool refresh=false)
    {
        //TODO think about having these things constantly set in Update, so we can see changes live

        //if we're repopulating the same object but *dont* want to refresh, return;
        if(go == currentInspectedObject && refresh==false)
        {
            return;
        }

        //if we're not inspecting the passed in object, but *do* want to refresh it, that means this player is inspecting a different object, we should not update
        if(go != currentInspectedObject && refresh == true)
        {
            return;
        }

    
        foreach(UpgradeSlot upgradeSlot in upgradeSlots)
        {
            UnsubscribeFromSlot(upgradeSlot);
            Destroy(upgradeSlot.gameObject);
           
        }
        upgradeSlots.Clear();
        
        
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
            strength.text = $"<color=red>{stats.strength.Value}</color> = {stats.baseStrength.Value}(<color=red>+{(stats.baseStrength.Value * ((stats.strengthModifier.Value/100)+1))-stats.baseStrength.Value}</color>)";

            //SPEED
            speed.text = $"<color=yellow>{stats.speed.Value}</color> = {stats.baseSpeed.Value}(<color=yellow>+{(stats.baseSpeed.Value * ((stats.speedModifier.Value/100)+1))-stats.baseSpeed.Value}</color>)";

            //SIZE
            size.text = $"<color=green>{stats.size.Value}</color> = {stats.baseSize.Value}(<color=green>+{(stats.baseSize.Value * ((stats.sizeModifier.Value/100)+1))-stats.baseSize.Value}</color>)";

            //STURDY
            sturdy.text = $"<color=blue>{stats.sturdy.Value}</color> = {bo.MaxHealth}(<color=blue>+{(bo.MaxHealth * ((stats.sturdyModifier.Value/100)+1))-bo.MaxHealth}</color>)";

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
            //! FOR NOW HARD CODING AMOUNT OF SLOTS, THIS NEEDS TO BE DYNAMIC WITH THE LEVEL LATER ON
            int numUpgradeSlots = 3;

            //CREATE THE SLOTS
            for(int i = 0; i < numUpgradeSlots; i++)
            {
                GameObject slot = Instantiate(upgradeSlotPrefab, upgradeSlotHolder.transform);
                UpgradeSlot upgradeSlot = slot.GetComponent<UpgradeSlot>();
                upgradeSlots.Add(upgradeSlot);
                SubscribeToSlot(upgradeSlot);
                upgradeSlot.InitializeHand(inventoryHandUI);
                
            }

            //SET THE SLOTS
            for(int i = 0; i < upg.UpgradeItems.Count; ++i)
            {
                upgradeSlots[i].SetItem(upg.UpgradeItems[i].Id, 1, true);
            }

        }
        else
        {
            SetGroup(upgradeThings, false);
        }
    }

    private void SubscribeToSlot(UpgradeSlot upgradeSlot)
    {
        upgradeSlot.itemAdded += HandleUpgradeAdded;
        upgradeSlot.itemRemoved += HandleUpgradeRemoved;
    }

    private void UnsubscribeFromSlot(UpgradeSlot upgradeSlot)
    {
        upgradeSlot.itemAdded -= HandleUpgradeAdded;
        upgradeSlot.itemRemoved -= HandleUpgradeRemoved;
    }

    private void HandleUpgradeAdded(UpgradeItem upgradeItem)
    {
        //we have access to the Upgrade and our CurrentItem, should be able to handle all of the logic here..?
        ///currentInspectedObject
        currentInspectedObject.GetComponent<IUpgradeable>().ApplyUpgradeServerRpc(upgradeItem.Id);
        
    }

    private void HandleUpgradeRemoved(UpgradeItem upgradeItem)
    {
        currentInspectedObject.GetComponent<IUpgradeable>().RemoveUpgradeServerRpc(upgradeItem.Id);
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

    public void RefreshMenu()
    {
        PopulateMenu(currentInspectedObject, true);
    }
}
