using Unity.VisualScripting;
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

    public abstract void AddXp(int amount);
    public abstract void CheckLevelUp();
    public abstract void LevelUp();
    public void SetLevel(int level);
    //this needs functions like AddXp, 
    public void OnLevelUp();
}
