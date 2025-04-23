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

    public void AddXp(int amount)
    {
        
        CurrentXp += amount;
        
        CheckLevelUp();
        OnXpGain();
        //maybe in the future this can be a coroutine that does it slowly for cool effect
    }
    public void CheckLevelUp()
    {
        if(CurrentXp > MaximumLevelXp)
        {
            int newXp = CurrentXp - MaximumLevelXp;
            CurrentXp = 0;
            LevelUp();
            AddXp(newXp);
        }
    }
    
    public void LevelUp()
    {
        Level++;
        MaximumLevelXp = Mathf.RoundToInt(MaximumLevelXp * 1.2f);
        //need some way for this to interact with stats.. OnLevelUp()? then it's up to the base classes to figure out what they wanna do
        OnLevelUp();
    }
    public void SetLevel(int level)
    {
        Level = level;
    }

    public void OnXpGain();
    //this needs functions like AddXp, 
    public void OnLevelUp();
}
