using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
[CreateAssetMenu(fileName = "NewPoI", menuName = "ScriptableObject/Point of Interest")]
public class PointOfInterest : ScriptableObject
{
    public string poiPame;
    public BiomeType biomeType;
    public List<PoIObject> poiObjects;
    public int numSpawns;
}

[System.Serializable]
public struct PoIObject
{
    public Vector3Int position;
    public int itemId;
}
