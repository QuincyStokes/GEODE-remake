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
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            //Destroy(gameObject);
        }

    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnLootServerRpc(Vector3 position, int itemId, int amount, float delay = 0f, float horizOffset = 0f, float quality = 0f, float minQuality=1f, float maxQuality=100f)
    {
        if (itemId == 0) return;
        GameObject lootGO = Instantiate(lootPrefab, position, Quaternion.identity);
        lootGO.GetComponent<NetworkObject>().Spawn();
        lootGO.GetComponent<Loot>().Initialize(itemId, amount, delay, horizOffset, quality, minQuality, maxQuality);
    }
}
