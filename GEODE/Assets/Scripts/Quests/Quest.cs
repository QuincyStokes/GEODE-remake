using System.IO;
using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest",  menuName = "ScriptableObject/Quests/Quest")]
public class Quest : ScriptableObject
{
    public bool isComplete = false;
    public string description;

    public void Complete()
    {
        isComplete = true;
    }

    public void Incomplete()
    {
        isComplete = false;
    }
}
