using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

public class LootManager : NetworkBehaviour
{
    public static LootManager Instance;
    [SerializeField] private GameObject lootPrefab;


    private void Awake()
    {   
        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            //Destroy(gameObject);
        }

    }

    [ServerRpc]
    public void SpawnLootServerRpc(Vector3 position, int itemId, int amount, float delay=0f, float horizOffset = 0f)
    {
        GameObject lootGO = Instantiate(lootPrefab, position, Quaternion.identity);
        lootGO.GetComponent<Loot>().Initialize(itemId, amount, delay, horizOffset);
        lootGO.GetComponent<NetworkObject>().Spawn();
    }


}
