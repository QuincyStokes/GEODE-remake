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
    [SerializeField] private Animator animator;
    private Rigidbody2D rb;

    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float knockbackDecay;
    private Vector2 localInputVelocity;
    private Vector2 lastMovedDir;
    private Vector2 externalVelocity;
    
    private NetworkVariable<Vector2> networkDirection = new NetworkVariable<Vector2>( //store our directoin as a network variable so other clients can update
        Vector2.down, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> networkVelocity = new NetworkVariable<float>( //same as networkDirection
        0f, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );


    
    [Header("Health")]
    [SerializeField] private float maxHealth;
    private float currentHealth;


    //INPUTS  -----------------------
    [Header("Input")]
    [SerializeField] public PlayerInputActionMap playerInput; //this is the input action map, essentially there are a bunch of actions we can grab and assign
    private InputAction movementInput; 
    private InputAction mouseInput;
    private Vector3 mousePos;
    private Vector3Int previousMousePosInt;


    //WEIRD THING, just going to create a field to the ItemDatabase so it loads.. kindof a hack but it works?
    [SerializeField]private ItemDatabase itemDatabase;


    
    //PRIVATE INTERNAL
    

    public override void OnNetworkSpawn()
    {
        
        networkDirection.OnValueChanged += OnNetworkDirectionChanged;
        networkVelocity.OnValueChanged += OnNetworkVelocityChanged;
        
    }
    private void Awake()
    {
        rb = GetComponentInChildren<Rigidbody2D>();
        playerInput = new PlayerInputActionMap();

    }

    private void OnEnable()
    {
        if(!IsOwner)
        {
            return;
        }
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
        if(!IsOwner)
        {
            return;
        }
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
        if(!IsOwner)
        {
            return;
        }
        
    }

    private void Update()
    {
        if(!IsOwner)
        {
            return;
        }
        MousePositionHandler();
    }

    private void FixedUpdate()
    {
        if(!IsOwner)
        {
            return;
        }
        MovementUpdate();
    }

    private void MovementUpdate()
    {
        if(!IsOwner)
        {
            return;
        }
        localInputVelocity = movementInput.ReadValue<Vector2>().normalized * moveSpeed;
        Vector2 finalVelocity = localInputVelocity + externalVelocity;
        rb.linearVelocity = finalVelocity;

        //animation?
        if(finalVelocity.sqrMagnitude > 0.01f)
        {
            lastMovedDir = finalVelocity.normalized;
        }

        animator.SetFloat("moveX", lastMovedDir.x);
        animator.SetFloat("moveY", lastMovedDir.y);
        animator.SetFloat("velocity", finalVelocity.magnitude);

        bool directionChanged = Vector2.Distance(networkDirection.Value, lastMovedDir) > .001f;
        bool velocityChanged = Math.Abs(networkVelocity.Value - finalVelocity.sqrMagnitude) > .001f;

        if(directionChanged || velocityChanged)
        {
            UpdateMovementServerRpc(lastMovedDir, finalVelocity.magnitude);
        }

        externalVelocity = Vector2.Lerp(externalVelocity, Vector2.zero, knockbackDecay * Time.fixedDeltaTime);
    }

    private void OnNetworkDirectionChanged(Vector2 oldValue, Vector2 newValue)
    {
        if(!IsOwner)
        {
            animator.SetFloat("moveX", newValue.x);
            animator.SetFloat("moveY", newValue.y);
        }
    }

    private void OnNetworkVelocityChanged(float oldValue, float newValue)
    {
        if(!IsOwner)
        {
            animator.SetFloat("velocity", newValue);
        }
    }

    [ServerRpc]
    private void UpdateMovementServerRpc(Vector2 direction, float velocity)
    {
        networkDirection.Value = direction;
        networkVelocity.Value = velocity;
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
        if(!IsOwner)
        {
            return;
        }

        Debug.Log("Camera.main: " + Camera.main);
        Debug.Log("mouseInput: " + mouseInput);

        mousePos = Camera.main.ScreenToWorldPoint(mouseInput.ReadValue<Vector2>());
        Vector3Int mousePosInt = new Vector3Int((int)mousePos.x, (int)mousePos.y, 0);
        if(mousePosInt != previousMousePosInt && GridManager.Instance != null)
        {
            GridManager.Instance.UpdateMousePos(mousePosInt);
        }
    }

    
}
