using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public class LootManager : NetworkBehaviour
{
    public static LootManager Instance;
    [SerializeField] private GameObject lootPrefab;


    private void Awake()
    {   
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [ServerRpc]
    public void SpawnLootServerRpc(Vector3 position, int itemId, int amount)
    {
        GameObject lootGO = Instantiate(lootPrefab, position, Quaternion.identity);
        lootGO.GetComponent<Loot>().Initialize(itemId, amount);
        lootGO.GetComponent<NetworkObject>().Spawn();
    }


}
