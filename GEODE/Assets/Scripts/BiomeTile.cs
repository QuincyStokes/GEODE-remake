using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName ="NewBiomeTile", menuName = "Tiles/BiomeTile")]
public class BiomeTile : Tile
{
    [SerializeField] public BiomeType biomeType;

}
