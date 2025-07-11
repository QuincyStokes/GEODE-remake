using UnityEngine;

[CreateAssetMenu(fileName = "NewStatPerk", menuName = "Perk/StatPerk")]
public class PlayerStatPerk : PerkData
{
    [Header("How to Unlock?")]
                                                             public StatRequirement statRequirement;
    [Tooltip("Name of the requirement, e.g. 'Red_Hare'")]    public string requirementKey;
    [Tooltip("Amount of the requirement needed, e.g. '50'")] public int requirementAmount;

    [Header("Perk Reward")]
    public PlayerStatType statType;
    public float statIncrease;

    public override void Apply(RunSettings settings)
    {
        switch (statType)
        {
            case PlayerStatType.Damage:
                settings.playerDamage += statIncrease;
                break;
            case PlayerStatType.Health:
                settings.playerHealth += statIncrease;
                break;
            case PlayerStatType.Speed:
                settings.playerMovespeed += statIncrease;
                break;
        }
    }

    public override bool IsUnlocked(PlayerStats stats)
    {
        stats.kills.TryGetValue(requirementKey, out int val);
        return val >= requirementAmount;
    }

    public enum PlayerStatType
    {
        Damage,
        Speed,
        Health
    }
}
