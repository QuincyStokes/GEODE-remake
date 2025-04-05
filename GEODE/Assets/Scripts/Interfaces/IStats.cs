using UnityEngine;

public interface IStats
{
    [Header("Base Stats")]
    public float BaseSpeed
    {
        get; set;
    }

    public float BaseStrength
    {
        get; set;
    }

    public float BaseSize
    {
        get; set;
    }

    [Header("Stat Modifiers")]
    public float SpeedModifier
    {
        get; set;
    }
    public float StrengthModifier
    {
        get; set;
    }
    public float SizeModifier
    {
        get; set;
    }
    public float SturdyModifier
    {
        get; set;
    }

    //* ---------- INTERNAL FINAL STATS ---------------
    public float Speed
    {
        get; set;
    }
    public float Strength
    {
        get; set;
    }
    public float Size
    {
        get; set;
    }
    public float Sturdy
    {
        get; set;
    }



}
