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

    //* ------- PRIVATE INTERNAL -------------
    private GameObject currentInspectedObject;
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PopulateMenu(GameObject go)
    {
        if(InspectionMenuHolder.activeSelf == false)
        {
            InspectionMenuHolder.SetActive(true);
        }

        BaseObject bo = go.GetComponent<BaseObject>();
        IStats stats = go.GetComponent<IStats>();
        IExperienceGain exp = go.GetComponent<IExperienceGain>();
        //since we have passed in our stats, xp, and theobject, we can guarantee that we have:
        //all of the necessary information to populate the menu
        if(bo != null) //health, description, sprite
        {
            inspectName.text = bo.ObjectName;
            inspectImage.sprite = bo.objectSprite;
            healthSlider.maxValue = bo.MaxHealth;
            healthSlider.minValue = 0;
            healthSlider.value = bo.CurrentHealth;
            sturdy.text = bo.MaxHealth.ToString();
            description.text = bo.description;
        }

        if(stats != null) //all of the stat modifiers
        {
            //CAN DO CUSTOM COLOR BY DOING <COLOR=#ffffff>
            //STRENGTH
            strength.text = $"<color=red>{stats.Strength}</color> = {stats.BaseStrength}<color=red>({stats.BaseStrength * stats.StrengthModifier-stats.BaseStrength}</color>)";

            //SPEED
            speed.text = $"<color=yellow>{stats.Speed}</color> = {stats.BaseSpeed}<color=yellow>({stats.BaseSpeed * stats.SpeedModifier-stats.BaseSpeed}</color>)";

            //SIZE
            size.text = $"<color=green>{stats.Size}</color> = {stats.BaseSize}<color=green>({stats.BaseSize * stats.SizeModifier-stats.BaseSize}</color>)";

            //STURDY
            sturdy.text = $"<color=blue>{stats.Sturdy}</color> = {bo.MaxHealth}<color=blue>({bo.MaxHealth * stats.SturdyModifier-bo.MaxHealth}</color>)";

        }

        if(exp != null) //xp things like current/total/level
        {
            level.text = "Level " + exp.Level.ToString();
            xpSlider.maxValue = exp.MaximumLevelXp;
            xpSlider.minValue = 0;
            xpSlider.value = exp.CurrentXp;
        }
    }

    public void CloseInspectionMenu()
    {
        currentInspectedObject = null;
        InspectionMenuHolder.SetActive(false);
    }
}
