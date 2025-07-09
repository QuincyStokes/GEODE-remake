using UnityEngine;
using UnityEngine.Experimental.Rendering;

public abstract class PerkData : ScriptableObject
{
    [Header("UI")]
    public string PerkName;
    [TextArea] public string description;
    public Sprite icon;

    //IsUnlocked, "Does the player have this upgrade unlocked?"
    public abstract bool IsUnlocked(PlayerStats stats);

    //Apply this upgrade.
    public abstract void Apply(RunSettings settings);

}
