using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMusicDatabase", menuName = "ScriptableObject/MusicDatabase")]
public class MusicDatabase : ScriptableObject
{
    public List<MusicData> MusicDataList;
}
