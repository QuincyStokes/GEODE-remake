using System.Collections;
using System.ComponentModel;
using Unity.Netcode;
using UnityEngine;

public class Loot : NetworkBehaviour
{
    public NetworkVariable<int> itemId = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<int> amount = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private CircleCollider2D col;
    [SerializeField] private float moveSpeed;
    private bool pickedUp;
    

    public override void OnNetworkSpawn()
    {
        itemId.OnValueChanged += OnItemIdChanged;

        OnItemIdChanged(0, itemId.Value);
        StartCoroutine(DelayCollider());
    }


    private IEnumerator DelayCollider()
    {
        col.enabled = false;                 
        yield return new WaitForSeconds(0.1f);
        col.enabled = true;                
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(!IsServer || pickedUp)
        {
            return;
        }

        if (other.gameObject.CompareTag("Player"))
        {

            //check if it *can* be added to the player's inventory
            //if it can, call MoveAndCollect

            PlayerInventory inv = other.gameObject.GetComponent<PlayerInventory>();
            if (inv == null)
            {
                return;
            }

            //OLD
            // if(inv.AddItem(itemId.Value, amount.Value))
            // {
            //     Debug.Log("Item added to inventory!");
            //     pickedUp = true;
            //     col.enabled = false; //this should fix white box error on multiplayer?

            //     PickupClientRpc(other.GetComponent<NetworkObject>().NetworkObjectId);
            //     NetworkObject.Despawn(false);
            // }
            inv.AddItemServerRpc(itemId.Value, amount.Value);
            NetworkObject.Despawn(true);

        }
    }

    [ClientRpc]
    private void PickupClientRpc(ulong playerObjectId)
    {
        var player = NetworkManager.Singleton.SpawnManager.SpawnedObjects[playerObjectId];
        if (player == null) return;

        // Start a local coroutine only for visual flair.
        StartCoroutine(MoveAndCollect(player.transform));
    }

    private IEnumerator MoveAndCollect(Transform target)
    {
        //do something to move towards the player
        while(transform.position !=  target.position && Vector3.Distance(transform.position, target.position) > .01f) {
            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);
            yield return null;
        }
        //AudioManager.instance.Play(popSFX[Random.Range(0,popSFX.Count-1)], .3f);
        Destroy(gameObject);
    }

    private void OnItemIdChanged(int oldValue, int newValue)
    {
        BaseItem item = ItemDatabase.Instance.GetItem(newValue);
        if(item != null)
        {
            sr.sprite = item.Icon;
        }
        else
        {
            sr.sprite = null;
        }
    }

    public void Initialize(int Id, int itemAmount)
    {
        itemId.Value = Id;
        amount.Value = itemAmount;
        OnItemIdChanged(Id, Id);
    }
}
