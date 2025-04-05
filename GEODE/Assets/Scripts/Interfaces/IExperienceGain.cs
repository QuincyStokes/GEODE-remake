using UnityEngine;

public interface IExperienceGain 
{
    public int MaximumLevelXp
    {
        set; get;
    }
    public int CurrentXp
    {
        set; get;
    }
    public int CurrentTotalXp
    {
        set; get;
    }
    public int Level
    {
        set; get;
    }
}
