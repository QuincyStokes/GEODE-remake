using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewQuestChapter", menuName = "ScriptableObject/Quests/QuestChapter")]
public class QuestChapter : ScriptableObject
{
    public string chapterName;
    public Quest[] quests = new Quest[4];
}


