using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRefineryMap", menuName = "ScriptableObject/RefineryMap")]
public class RefineryMap : ScriptableObject
{
    public List<RefineryOutputMapping> refineryMaps;   
}

[Serializable]
public struct RefineryOutputMapping
{
    public BaseItem inputItem;
    public BaseItem outputItem;
}