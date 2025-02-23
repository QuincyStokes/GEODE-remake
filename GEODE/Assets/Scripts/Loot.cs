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
    

    public override void OnNetworkSpawn()
    {
        itemId.OnValueChanged += OnItemIdChanged;
    }

    

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.CompareTag("Player"))
        {

            //check if it *can* be added to the player's inventory
            //if it can, call MoveAndCollect

            bool canAdd = other.gameObject.GetComponent<PlayerInventory>().AddItem(itemId.Value, amount.Value);
            if(canAdd) {
                StartCoroutine(MoveAndCollect(other.transform));
            }

        }
    }

    private IEnumerator MoveAndCollect(Transform target)
    {
        Destroy(col);
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

    public void Initialize(int Id, int amount)
    {
        this.itemId = Id;
        this.amount = amount;
    }
}
