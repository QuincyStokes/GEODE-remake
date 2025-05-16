using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour, IKnockbackable
{
    public static PlayerController Instance;
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerHealthAndXP playerHealth;
    [SerializeField] public InspectionMenu inspectionMenu;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject playerUICanvas;
    [SerializeField] private GameObject pauseMenu;
    private Rigidbody2D rb;
    [SerializeField] public GameObject attackHitbox; //TEMP
    [SerializeField] public Animator attackAnimator;

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

    //INPUTS  -----------------------
    [Header("Input")]
    [SerializeField] public PlayerInputActionMap playerInput; //this is the input action map, essentially there are a bunch of actions we can grab and assign
    private InputAction movementInput;
    private InputAction mouseInput;
    private Vector3 mousePos;
    private Vector3Int previousMousePosInt;
    [SerializeField] private float swingCooldown;
    private float swingCooldownTimer = 0f;

    [Header("Interaction Layer Mask")]
    [SerializeField] private LayerMask interactableLayerMask;


    //PRIVATE INTERNAL


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        networkDirection.OnValueChanged += OnNetworkDirectionChanged;
        networkVelocity.OnValueChanged += OnNetworkVelocityChanged;
        //ENABLE CALLBACKS
        //Movement
        if (IsOwner)
        {
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

            //Secondary FIre
            playerInput.Player.SecondaryFire.performed += OnSecondaryFire;
            playerInput.Player.SecondaryFire.Enable();

            //Scroll
            playerInput.Player.Scroll.performed += playerInventory.OnScroll;
            playerInput.Player.Scroll.Enable();

            //Escape
            playerInput.Player.Menu.performed += OnMenuOpened;
            playerInput.Player.Menu.Enable();

            //Mouse position
            mouseInput = playerInput.Player.Mouse;
            mouseInput.Enable();



            Instance = this;
            CameraWorldConfiner.Instance.SetCameraBoundary();

            playerUICanvas.SetActive(true);
        }
    }

    private void Awake()
    {
        rb = GetComponentInChildren<Rigidbody2D>();
        playerInput = new PlayerInputActionMap();

    }

    private void OnEnable()
    {
        if (!IsOwner)
        {
            return;
        }

    }

    private void OnDisable()
    {
        if (!IsOwner)
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

        //Secondary
        playerInput.Player.SecondaryFire.performed -= OnSecondaryFire;
        playerInput.Player.SecondaryFire.Disable();

        //Scroll
        playerInput.Player.Scroll.performed -= playerInventory.OnScroll;
        playerInput.Player.Scroll.Disable();

        //Escape
        playerInput.Player.Menu.performed += OnMenuOpened;
        playerInput.Player.Menu.Enable();


    }


    private void Start()
    {

        if (!IsOwner)
        {
            return;
        }

        if (WorldGenManager.Instance != null)
        {
            transform.position = new Vector3Int(WorldGenManager.Instance.WorldSizeX / 2, WorldGenManager.Instance.WorldSizeY / 2);
        }
        else
        {
            Debug.Log("WorldGenManager is null, cannot place player.");
        }

        if (playerHealth != null)
        {
            playerHealth.UpdateHealthbar();
            playerHealth.UpdateXpbar();
        }
        CameraManager.Instance.FollowPlayer(transform);


    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }
        MousePositionHandler();
        CooldownTimer();
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
        {
            return;
        }
        MovementUpdate();
    }

    private void MovementUpdate()
    {
        if (!IsOwner)
        {
            return;
        }
        localInputVelocity = movementInput.ReadValue<Vector2>().normalized * moveSpeed;
        Vector2 finalVelocity = localInputVelocity + externalVelocity;
        rb.linearVelocity = finalVelocity;

        //animation?
        if (finalVelocity.sqrMagnitude > 0.01f)
        {
            lastMovedDir = finalVelocity.normalized;
        }

        float hitboxAngle = Mathf.Atan2(lastMovedDir.y, lastMovedDir.x) * Mathf.Rad2Deg;
        float angle = Mathf.Round(hitboxAngle / 90) * 90f + 90;
        attackHitbox.transform.localRotation = Quaternion.Euler(0, 0, angle);

        animator.SetFloat("moveX", lastMovedDir.x);
        animator.SetFloat("moveY", lastMovedDir.y);
        animator.SetFloat("velocity", finalVelocity.magnitude);

        bool directionChanged = Vector2.Distance(networkDirection.Value, lastMovedDir) > .001f;
        bool velocityChanged = Math.Abs(networkVelocity.Value - finalVelocity.sqrMagnitude) > .001f;

        if (directionChanged || velocityChanged)
        {
            UpdateMovementServerRpc(lastMovedDir, finalVelocity.magnitude);
        }
        //attackHitbox.gameObject.transform.localPosition = lastMovedDir;
        externalVelocity = Vector2.Lerp(externalVelocity, Vector2.zero, knockbackDecay * Time.fixedDeltaTime);
    }

    private void OnNetworkDirectionChanged(Vector2 oldValue, Vector2 newValue)
    {
        if (!IsOwner)
        {
            animator.SetFloat("moveX", newValue.x);
            animator.SetFloat("moveY", newValue.y);
        }
    }

    private void OnNetworkVelocityChanged(float oldValue, float newValue)
    {
        if (!IsOwner)
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
    public void TakeKnockbackServerRpc(Vector2 direction, float force)

    {
        externalVelocity += direction.normalized * Mathf.Log(force);
    }

    private void SetPositionCenterWorld()
    {
        transform.position = new Vector3Int(WorldGenManager.Instance.WorldSizeX / 2, WorldGenManager.Instance.WorldSizeY / 2);
    }

    private void OnPrimaryFire(InputAction.CallbackContext context)
    {
        if (!IsPointerOverUI())
        {
            playerInventory.UseSelectedItem(Camera.main.ScreenToWorldPoint(mouseInput.ReadValue<Vector2>()));
        }
    }

    private void OnSecondaryFire(InputAction.CallbackContext context)
    {
        //raycast at mouse position, check for any IInteractables?
        Debug.Log("Secondary Fire!");
        Vector3 pos = Camera.main.ScreenToWorldPoint(mouseInput.ReadValue<Vector2>());

        RaycastHit2D hit = Physics2D.Raycast(pos, Vector2.zero, 10, interactableLayerMask);
        if (hit)
        {
            Debug.Log($"Raycast Hit {hit.collider.gameObject.name}");
            //IInteractable interactable = hit.collider.gameObject.GetComponentInParent<IInteractable>();

            //This is a bit strange, but:
            //get all of the MonoBehaviour components in the objects parent
            MonoBehaviour[] gos = hit.collider.gameObject.GetComponentsInParent<MonoBehaviour>();

            GameObject go = null;

            //if any of the retrieved Monos are also of type IInteractable (which Core should be for example), choose that gameObject. 
            foreach (MonoBehaviour mono in gos)
            {
                if (mono is IInteractable)
                {
                    go = mono.gameObject;
                    break;
                }
            }

            if (go != null)
            {
                inspectionMenu.DoMenu(go);
            }
            else
            {
                //can just give player a direct reference
                inspectionMenu.CloseInspectionMenu();
            }


        }
        else
        {
            inspectionMenu.CloseInspectionMenu();
            Debug.Log($"Raycast Hit nothing");
        }
    }

    private void MousePositionHandler()
    {
        Debug.Log("Camera.main: " + Camera.main);
        Debug.Log("mouseInput: " + mouseInput);

        mousePos = Camera.main.ScreenToWorldPoint(mouseInput.ReadValue<Vector2>());
        Vector3Int mousePosInt = new Vector3Int((int)mousePos.x, (int)mousePos.y, 0);
        if (mousePosInt != previousMousePosInt && GridManager.Instance != null)
        {
            GridManager.Instance.UpdateMousePos(mousePosInt);
        }
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("EnvironmentObject"))
        {
            EnvironmentObject envObj = collision.gameObject.GetComponentInParent<EnvironmentObject>();
            if (envObj != null)
            {
                envObj.TakeDamageServerRpc(4, gameObject.transform.position, true);
                Debug.Log($"Hit {collision.name} for 4 damage");
            }
        }
    }

    public void Attack()
    {
        if (swingCooldownTimer >= swingCooldown)
        {
            StartCoroutine(DoAttack());
        }

    }

    private IEnumerator DoAttack()
    {
        //Debug.Log("Attacking!");
        Instance.attackHitbox.SetActive(true);
        attackAnimator.SetTrigger("Swing");
        moveSpeed /= 2;
        yield return new WaitForSeconds(.1f);
        Instance.attackHitbox.SetActive(false);
        moveSpeed *= 2;
        swingCooldownTimer = 0f;
    }

    private void CooldownTimer()
    {
        if (swingCooldownTimer < swingCooldown)
        {
            swingCooldownTimer += Time.deltaTime;
        }
    }

    public void OnMenuOpened(InputAction.CallbackContext context)
    {
        TogglePauseMenu();
    }

    public void TogglePauseMenu()
    {
        pauseMenu.SetActive(!pauseMenu.activeSelf);
    }

    public bool IsPointerOverUI()
    {
         var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        // 2. Ray-cast through **all** raycasters
        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, hits);

        // 3. Accept only hits coming from a GraphicRaycaster (i.e. real Canvas UI)
        return hits.Exists(h => h.module is GraphicRaycaster);
    }

    public void LeaveGame()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            // Disconnect client, stop the server/host, and flush the queue
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("Lobby");

    }




}
