using Unity.Services.Lobbies.Models;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCrystalItem", menuName = "Items/Special/ConcentratedCrystal")]
public class ConcentratedCrystal : BaseItem
{
    public override bool Use(Vector3 position, bool snapToGrid=true, bool force=true)
    {
        Vector3 spawnPos;
        if (FlowFieldManager.Instance.coreTransform == null)
        {
            spawnPos = new Vector3Int(WorldGenManager.Instance.WorldSizeX / 2, WorldGenManager.Instance.WorldSizeY / 2, 0);
        }
        else
        {
            spawnPos = FlowFieldManager.Instance.coreTransform.position;
        }
        PlayerController.Instance.transform.position = spawnPos;
        PlayerController.Instance.TeleportOwnerClientRpc(spawnPos);
        return true;
    }

}
