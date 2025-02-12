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
    [SerializeField] public PlayerInputActionMap playerInput; //this is the input action map, essentially there are a bunch of actions we can grab and assign
    private InputAction movementInput; //for example, moveAction!

    private Vector2 inputVelocity;

    
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
        playerInput = new PlayerInputActionMap();

    }

    private void OnEnable()
    {
        //We handle constant inputs like this
        movementInput = playerInput.Player.Move;
        movementInput.Enable();

        //since onetime events don't need to be checked constantly, we don't *need* to reference them.
        //can instead do this from wherever needed
        //playerInput.Player.InventoryToggle.performed += InventoryOpen
    }

    private void OnDisable()
    {
        movementInput.Disable();
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
        MovementUpdate();
    }

    private void MovementUpdate()
    {
        inputVelocity = movementInput.ReadValue<Vector2>().normalized * moveSpeed;
        Vector2 finalVelocity = inputVelocity + externalVelocity;
        rb.linearVelocity = finalVelocity;

        externalVelocity = Vector2.Lerp(externalVelocity, Vector2.zero, knockbackDecay * Time.fixedDeltaTime);
    }



    private void OnMoveDown(InputAction.CallbackContext context)
    {
        //this is the same as doing the horizontal = Input.GetAxis...
        inputVelocity = context.ReadValue<Vector2>().normalized;
    }
    
    private void OnMoveUp(InputAction.CallbackContext context)
    {
        inputVelocity = Vector2.zero;
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
