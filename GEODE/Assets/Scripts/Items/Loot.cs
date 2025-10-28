using System.Collections;
using System.ComponentModel;
using Unity.Netcode;
using UnityEngine;

public class Loot : NetworkBehaviour
{

    [Header("Inspector References")]
    [SerializeField] private SpriteRenderer sr;


    [SerializeField] private CircleCollider2D col;
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float duration = 0.4f;           // total bounce time
    [SerializeField] private float peakHeight = 0.5f;         // how “high” it hops, in world units
    [SerializeField] private float maxXOffset = 0.3f;         // max horizontal pop distance
    [SerializeField] private AnimationCurve curve;
    private float horizontalOffset = 0;
    private float pickupDelay;
    public NetworkVariable<bool> pickedUp = new NetworkVariable<bool>(false);


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
    
    public NetworkVariable<float> quality = new NetworkVariable<float>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
   
    

    public override void OnNetworkSpawn()
    {
        itemId.OnValueChanged += OnItemIdChanged;

        OnItemIdChanged(0, itemId.Value);
        if (horizontalOffset == 0)
            horizontalOffset = Random.Range(-maxXOffset, +maxXOffset);
        BeginSpawnAnimation();
        //StartCoroutine(DelayCollider());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(!IsServer || pickedUp.Value)
        {
            return;
        }

        if (other.gameObject.CompareTag("Player"))
        {
            pickedUp.Value = true;
            col.enabled = false;
            //check if it *can* be added to the player's inventory
            //if it can, call MoveAndCollect

            PlayerInventory inv = other.gameObject.GetComponent<PlayerInventory>();
            if (inv == null)
            {
                return;
            }
            
            StartCoroutine(MoveAndCollect(other.transform, inv));
            

        }
    }

    void BeginSpawnAnimation()
    {
        // Disable pickup while we animate
        col.enabled = false;
        StartCoroutine(PlayBounceRoutine());
    }

    private IEnumerator PlayBounceRoutine()
    {
        
        Vector3 basePos = transform.position;
        
        curve.AddKey(1f, 0);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            //evaluate bounce height based on curve
            float heightFactor = curve.Evaluate(t);
            float y = peakHeight * heightFactor;

            //lerp x position to target location
            float x = Mathf.Lerp(0, horizontalOffset, t);

            transform.position = basePos + Vector3.right * x+ Vector3.up    * y;

            yield return null;
        }

        // Snap back exactly
        OnBounceComplete();
    }

    private void OnBounceComplete()
    {
        StartCoroutine(DelayPickup());
    }

    private IEnumerator DelayPickup()
    {
        yield return new WaitForSeconds(pickupDelay);
        col.enabled = true;
    }



    private IEnumerator MoveAndCollect(Transform target, PlayerInventory inv)
    {
        //do something to move towards the player
        while (Vector3.Distance(transform.position, target.position) > .01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime);

            //the idea here is to fix the issue where if the player is moving, the item will never "reach" them.
            if (Vector3.Distance(transform.position, target.position) <= .01f)
            {
                break;
            }
            yield return null;
        }
        AudioManager.Instance.PlayLocal(SoundId.Loot_Pickup);
        inv.AddItemServerRpc(itemId.Value, amount.Value, quality.Value);
        NetworkObject.Despawn(true);
    }

    private void OnItemIdChanged(int oldValue, int newValue)
    {
        if (newValue == 0) return;
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

    public void Initialize(int Id, int itemAmount, float delay=0f, float horizOffset=0f, float qual=0, float minQual=1f, float maxQual = 100f)
    {
        itemId.Value = Id;
        amount.Value = itemAmount;
        pickupDelay = delay;
        //THIS MEANS only upgrades have Quality's for now, which I think is okay
            //! Here is where we would change though if we wanted to give tools or something quality.
        BaseItem item = ItemDatabase.Instance.GetItem(Id);
        Debug.Log($"Initializing Loot with quality {qual}");
        if (qual == 0 && item.type == ItemType.Upgrade)
        {
            //silly little trick to round to nearest 1 decimal place.
            float f = Random.Range(minQual, maxQual);
            f = Mathf.Round(f * 10.0f) * 0.1f;
            quality.Value = f;
        }
        else
        {
            quality.Value = qual;
        }
        if (horizOffset != 0)
        {
            horizontalOffset = horizOffset;
        }
        OnItemIdChanged(Id, Id);
    }
}
