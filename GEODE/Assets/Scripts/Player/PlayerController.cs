using System;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting.APIUpdating;

public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float knockbackDecay;

    
    [Header("Health Settings")]
    [SerializeField] private float maxHealth;


    //EVENTS -----------------------
    public static event Action inventoryToggled;

    [Header("Internal Use")]
    private Vector2 externalVelocity;
    private float currentHealth;
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private GameObject lootPrefab;
    [SerializeField] private BaseItem wood;

    //WEIRD THING, just going to create a field to the ItemDatabase so it loads.. kindof a hack but it works?
    [SerializeField]private ItemDatabase itemDatabase;


    
    //input movement, has 

    private Rigidbody2D rb;

    public override void OnNetworkSpawn()
    {
        if(!IsOwner)
        {
            enabled = false;
        }
    }
    private void Awake()
    {
       
        rb = GetComponentInChildren<Rigidbody2D>();
    }
    
    private void Start()
    {
        CameraManager.Instance.FollowPlayer(transform);
    }

    private void Update()
    {
        
        HandleEvents();
    }

    

    private void FixedUpdate()
    {
        Move();
    }
   

    private void Move()
    {
        //movement direction input
        float horizontal = inputHandler.Horizontal;
        float vertical = inputHandler.Vertical;
        
        //normalize the direction input to keep speed consistent, then apply movemetn speed
        Vector2 inputVelocity = new Vector2(horizontal, vertical).normalized * moveSpeed;

        //add input velocity to external velocity, this ensures things like knockback aren't overwritten by inputvelocity.
        Vector2 finalVelocity = inputVelocity + externalVelocity;
        

        rb.linearVelocity = finalVelocity;
        externalVelocity = Vector2.Lerp(externalVelocity, Vector2.zero, knockbackDecay * Time.fixedDeltaTime);
    }

    //this works pretty well
    [ServerRpc]
    public void ApplyKnockbackServerRpc()
    //public void ApplyKnockback(Vector2 direction, float force)
    {
        // Normalize the direction to ensure consistent behavior, then add the knockback force.
        //externalVelocity += direction.normalized * force;
        GameObject loot = Instantiate(lootPrefab, transform.position, Quaternion.identity);
       
        loot.GetComponent<NetworkObject>().Spawn();
        loot.GetComponent<Loot>().itemId.Value = 1;

    }

    private void HandleEvents()
    {
        if(Input.GetKeyDown(inputHandler.useKeybind))
        {
            playerInventory.UseSelectedItem();
        }

        if(Input.GetKeyDown(inputHandler.inventoryKeybind))
        {
            Debug.Log("Inventory toggled!");
            inventoryToggled?.Invoke();
        }
        if(Input.GetKeyDown(inputHandler.spaceKeybind))
        {
            ApplyKnockbackServerRpc();
        }
        if(inputHandler.InputString != null)
        {
            bool isNumber = int.TryParse(inputHandler.InputString, out int number);
            if(isNumber && number > 0 && number < 10)
            {
                if(playerInventory != null)
                {
                    playerInventory.ChangeSelectedSlot(number-1);
                }
               
            }
        }
        if(inputHandler.ScrollY > 0)
        {
            if(playerInventory != null)
            {
                playerInventory.ChangeSelectedSlot(playerInventory.GetSelectedSlotIndex()-1);
            }
        }
        else if(inputHandler.ScrollY < 0)
        {
            if(playerInventory != null)
            {
                playerInventory.ChangeSelectedSlot(playerInventory.GetSelectedSlotIndex()+1);
            }
        }

    }

    
}
