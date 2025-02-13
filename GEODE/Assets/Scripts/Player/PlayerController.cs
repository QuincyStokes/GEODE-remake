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
    

    private Vector2 inputVelocity;

    
    [Header("Health Settings")]
    [SerializeField] private float maxHealth;


    //INPUTS  -----------------------

    [SerializeField] public PlayerInputActionMap playerInput; //this is the input action map, essentially there are a bunch of actions we can grab and assign
    private InputAction movementInput; 
    private InputAction mouseInput;

    [Header("Internal Use")]
    private Vector2 externalVelocity;
    private float currentHealth;
    private Vector3 mousePos;
    private Vector3Int previousMousePosInt;

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
        //ENABLE CALLBACKS
        
        //Movement
        movementInput = playerInput.Player.Move;
        movementInput.Enable();

        //Inventory
        playerInput.Player.InventoryToggle.performed += playerInventory.ToggleInventory;
        playerInput.Player.InventoryToggle.Enable();

        //Numbers
        playerInput.Player.Numbers.performed += playerInventory.OnNumberPressed;
        playerInput.Player.Numbers.Enable();

        //PrimaryFire
        playerInput.Player.PrimaryFire.performed += OnPrimaryFire;
        playerInput.Player.PrimaryFire.Enable();
        
        //Scroll
        playerInput.Player.Scroll.performed += playerInventory.OnScroll;
        playerInput.Player.Scroll.Enable();

        //Mouse position
        mouseInput = playerInput.Player.Mouse;
        mouseInput.Enable();
    }

    private void OnDisable()
    {
        //DISABLE CALLBACKS

        //Movement
        movementInput.Disable();

        //Mouse input
        mouseInput.Disable();

        //Inventory
        playerInput.Player.InventoryToggle.performed -= playerInventory.ToggleInventory;
        playerInput.Player.InventoryToggle.Disable();

        //Numbers
        playerInput.Player.Numbers.performed -= playerInventory.OnNumberPressed;
        playerInput.Player.Numbers.Disable();

        //PrimaryFire
        playerInput.Player.PrimaryFire.performed -= OnPrimaryFire;
        playerInput.Player.PrimaryFire.Disable();

        //Scroll
        playerInput.Player.Scroll.performed -= playerInventory.OnScroll;
        playerInput.Player.Scroll.Disable();

    }

    
    private void Start()
    {
        CameraManager.Instance.FollowPlayer(transform);
    }

    private void Update()
    {
        MousePositionHandler();
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

    //this works pretty well
    [ServerRpc]
    public void ApplyKnockbackServerRpc()
    //public void ApplyKnockback(Vector2 direction, float force)
    {
        // Normalize the direction to ensure consistent behavior, then add the knockback force.
        //externalVelocity += direction.normalized * force;
        //GameObject loot = Instantiate(lootPrefab, transform.position, Quaternion.identity);
       
        // loot.GetComponent<NetworkObject>().Spawn();
        // loot.GetComponent<Loot>().itemId.Value = 1;

    }


    private void OnPrimaryFire(InputAction.CallbackContext context)
    {
        playerInventory.UseSelectedItem(Camera.main.ScreenToWorldPoint(mouseInput.ReadValue<Vector2>()));
    }

    private void MousePositionHandler()
    {
        mousePos = Camera.main.ScreenToWorldPoint(mouseInput.ReadValue<Vector2>());
        Vector3Int mousePosInt = new Vector3Int((int)mousePos.x, (int)mousePos.y, 0);
        if(mousePosInt != previousMousePosInt)
        {
            GridManager.Instance.UpdateMousePos(mousePosInt);
        }
    }

    
}
