using Unity.Netcode;
using UnityEngine;

public interface IStats
{
    [Header("Base Stats")]
    public NetworkVariable<float> baseSpeed
    {
        get; set;
    }

    public NetworkVariable<float> baseStrength
    {
        get; set;
    }

    public NetworkVariable<float> baseSize
    {
        get; set;
    }

    [Header("Stat Modifiers")]
    public NetworkVariable<float> speedModifier
    {
        get; set;
    }
    public NetworkVariable<float> strengthModifier
    {
        get; set;
    }
    public NetworkVariable<float> sizeModifier
    {
        get; set;
    }
    public NetworkVariable<float> sturdyModifier
    {
        get; set;
    }

    //* ---------- INTERNAL FINAL STATS ---------------
    public NetworkVariable<float> speed
    {
        get; set;
    }
    public NetworkVariable<float> strength
    {
        get; set;
    }
    public NetworkVariable<float> size
    {
        get; set;
    }
    public NetworkVariable<float> sturdy
    {
        get; set;
    }



}
