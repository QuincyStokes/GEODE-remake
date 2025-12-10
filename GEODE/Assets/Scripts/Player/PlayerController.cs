using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerController : NetworkBehaviour, IKnockbackable, ITracksHits
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
    [SerializeField] public Hitbox hitbox; //TEMP
    [SerializeField] public Hitbox repairHitbox;
    [SerializeField] public GameObject hitboxParent;
    [SerializeField] public Animator attackAnimator;
    [SerializeField] private DayNumber dayNumber;
    public NetworkVariable<int> kills { get; set; } = new NetworkVariable<int>(0);  //attack rate
    [Header("Movement")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float baseMoveSpeed; // Store original speed for perk calculations
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
    public bool movementLocked;

    [Header("Interaction Layer Mask")]
    [SerializeField] private LayerMask interactableLayerMask;

    //PRIVATE INTERNAL
    private Transform _coreTransform;
    public GameObject openUniqueUI;
    public IInteractable currentInteractedObject;


    //* Events
    public event Action OnMenuButtonPressed;


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
            playerInput.Player.InventoryToggle.performed += OnInventoryPressed;
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

            //Throw
            playerInput.Player.Throw.performed += ThrowHeldItemWrapper;
            playerInput.Player.Throw.Enable();

            //Handle Pause player movement
            PauseController.OnPauseChanged += HandlePauseChanged;


            Instance = this;
            CameraWorldConfiner.Instance.SetCameraBoundary();

            if (DayCycleManager.Instance != null)
            {
                DayCycleManager.Instance.becameDay += dayNumber.IncreaseDay;
                DayCycleManager.Instance.becameNight += dayNumber.IncreaseNight;
            }

            if (FlowFieldManager.Instance != null)
            {
                FlowFieldManager.Instance.corePlaced += HandleCorePlaced;
            }

            playerHealth.OnPlayerLevelUp += HandleLevelUp;

            playerUICanvas.SetActive(true);
        }
    }

    private void Awake()
    {
        rb = GetComponentInChildren<Rigidbody2D>();
        playerInput = new PlayerInputActionMap();
        baseMoveSpeed = moveSpeed; // Store the original move speed
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
        playerInput.Player.InventoryToggle.performed -= OnInventoryPressed;
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
        playerInput.Player.Menu.performed -= OnMenuOpened;
        playerInput.Player.Menu.Disable();


        //Throw
        playerInput.Player.Throw.performed -= ThrowHeldItemWrapper;
        playerInput.Player.Throw.Disable();

        if (DayCycleManager.Instance != null)
        {
            DayCycleManager.Instance.becameDay -= dayNumber.IncreaseDay;
            DayCycleManager.Instance.becameNight -= dayNumber.IncreaseNight;
        }

        PauseController.OnPauseChanged -= HandlePauseChanged;

        playerHealth.OnPlayerLevelUp -= HandleLevelUp;

        // Unsubscribe from perk stats changes
        var perkStats = GetComponent<PlayerPerkStats>();
        if (perkStats != null)
        {
            perkStats.SpeedMultiplier.OnValueChanged -= OnSpeedMultiplierChanged;
        }
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

        if (IsOwner && playerHealth != null)
        {
            playerHealth.playerController = this;
        }

        CameraManager.Instance.FollowPlayer(transform);

        // Speed perks will be applied via PlayerPerkStats when it synchronizes from server
        // No need to apply RunSettings here as it's not networked properly
        
        // Try to apply speed perks if PlayerPerkStats is already available
        var perkStats = GetComponent<PlayerPerkStats>();
        if (perkStats != null)
        {
            // Subscribe to speed changes or apply immediately if value is already set
            StartCoroutine(WaitForSpeedPerksAndApply(perkStats));
        }
    }

    private System.Collections.IEnumerator WaitForSpeedPerksAndApply(PlayerPerkStats perkStats)
    {
        // Wait a frame for network variables to be initialized
        yield return null;
        
        // Apply speed multiplier from perks
        ApplySpeedPerks(perkStats.SpeedMultiplier.Value);
        
        // Subscribe to future changes
        perkStats.SpeedMultiplier.OnValueChanged += OnSpeedMultiplierChanged;
    }

    private void OnSpeedMultiplierChanged(float oldValue, float newValue)
    {
        ApplySpeedPerks(newValue);
    }

    private void ApplySpeedPerks(float speedMultiplier)
    {
        moveSpeed = baseMoveSpeed * speedMultiplier;
        Debug.Log($"[PlayerController] Applied speed multiplier {speedMultiplier}, new speed: {moveSpeed}");
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }
        MousePositionHandler();
        CooldownTimer();
        UniqueUIRangeCheck();
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
        {
            return;
        }
        if (!movementLocked)
        {
            MovementUpdate();
        }

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

        if (finalVelocity.sqrMagnitude > 0.01f)
        {
            lastMovedDir = finalVelocity.normalized;
        }

        float hitboxAngle = Mathf.Atan2(lastMovedDir.y, lastMovedDir.x) * Mathf.Rad2Deg;
        float angle = Mathf.Round(hitboxAngle / 90) * 90f + 90;
        hitboxParent.transform.localRotation = Quaternion.Euler(0, 0, angle);

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

    public void TakeKnockback(Vector2 direction, float force)

    {
        externalVelocity += direction.normalized * Mathf.Log(force);
    }

    public void SetPositionCenterWorld()
    {
        transform.position = new Vector3Int(WorldGenManager.Instance.WorldSizeX / 2, WorldGenManager.Instance.WorldSizeY / 2);
    }

    private void OnPrimaryFire(InputAction.CallbackContext context)
    {
        if (IsPointerOverUI()) return;
        
        playerInventory.UseSelectedItem(Camera.main.ScreenToWorldPoint(mouseInput.ReadValue<Vector2>()));

    }

    private void OnSecondaryFire(InputAction.CallbackContext context)
    {
        if (IsPointerOverUI()) return;

        //raycast at mouse position, check for any IInteractables?
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
            GameObject uniqueUI = null;

            //if any of the retrieved Monos are also of type IInteractable (which Core should be for example), choose that gameObject. 
            foreach (MonoBehaviour mono in gos)
            {
                if (mono is IInteractable)
                {   
                    go = mono.gameObject;
                    break;
                }
            }

            foreach (MonoBehaviour mono in gos)
            {
                if (mono is IUniqueMenu)
                {
                    uniqueUI = mono.gameObject;
                    break;
                }
            }

            if (go != null)
            {
                if(currentInteractedObject != null && go.GetComponent<IInteractable>() != currentInteractedObject)
                {
                    currentInteractedObject.DoUnclickedThings();
                }

                inspectionMenu.DoMenu(go);
                if(uniqueUI != null)
                {
                    playerInventory.OpenInventory();
                }
                //we know "go" is IInteractable
                go.GetComponent<IInteractable>().DoClickedThings();
                currentInteractedObject = go.GetComponent<IInteractable>();
                
            }
            else
            {
                //can just give player a direct reference
                inspectionMenu.CloseInspectionMenu();
            }

            if (uniqueUI != null)
            {
                //turn off the old unique ui
                if (openUniqueUI != null)
                    openUniqueUI.GetComponent<IUniqueMenu>().HideMenu();

                //show the new unique ui
                uniqueUI.GetComponent<IUniqueMenu>().ShowMenu();
                //set our current open ui to the one we just opened
                openUniqueUI = uniqueUI;
            }
            else
            {
                if(openUniqueUI != null)
                    openUniqueUI.GetComponent<IUniqueMenu>().HideMenu();
                    openUniqueUI = null;
            }
        }
        else
        {
            //if our CursorStack isn't empty, we are holding something! Which means we should drop it.
            if (!CursorStack.Instance.ItemStack.Equals(ItemStack.Empty))
            {
                float horizOffset;
                if (lastMovedDir.x < 0) horizOffset = -1.5f;
                else horizOffset = 1.5f;
                LootManager.Instance.SpawnLootServerRpc(transform.position, CursorStack.Instance.ItemStack.Id, CursorStack.Instance.Amount, 2f, horizOffset);
                CursorStack.Instance.ItemStack = ItemStack.Empty;
            }
            else
            {
                inspectionMenu.CloseInspectionMenu();
                currentInteractedObject?.DoUnclickedThings();

                //if we have a uniqueUI open, close it.
                if (openUniqueUI != null)
                    openUniqueUI.GetComponent<IUniqueMenu>().HideMenu();
            }
        }
    }

    private void OnInventoryPressed(InputAction.CallbackContext context)
    {
        if(!playerInventory.IsInventoryOpen())
        {
            if(openUniqueUI != null)
            {
                openUniqueUI.GetComponent<IUniqueMenu>().HideMenu();
                inspectionMenu.CloseInspectionMenu();
            }
            if(currentInteractedObject != null)
            {
                currentInteractedObject.DoUnclickedThings();
            }
        }
    }

    private void UniqueUIRangeCheck()
    {
        if(openUniqueUI == null) return;
        if(Vector2.Distance(openUniqueUI.transform.position, transform.position) > 10)
        {
            openUniqueUI.GetComponent<IUniqueMenu>().HideMenu();
        }
    }



    private void MousePositionHandler()
    {
        mousePos = Camera.main.ScreenToWorldPoint(mouseInput.ReadValue<Vector2>());
        Vector3Int mousePosInt = new Vector3Int((int)mousePos.x, (int)mousePos.y, 0);
        if (mousePosInt != previousMousePosInt && GridManager.Instance != null)
        {
            GridManager.Instance.UpdateMousePos(mousePosInt);
            previousMousePosInt = mousePosInt;
        }
    }

    private void ThrowHeldItemWrapper(InputAction.CallbackContext context)
    {

        float offset;
        if (lastMovedDir.x < 0) offset = -1.5f;
        else offset = 1.5f;
        playerInventory.ThrowCurrentlySelectedHeldItem(horizOffset: offset);
    }


    public void Attack(float dmg, ToolType t, bool drops)
    {
        if (swingCooldownTimer >= swingCooldown)
        {
            Debug.Log($"Attacking with {t}");
            
            // Apply damage multiplier from perks
            var perkStats = GetComponent<PlayerPerkStats>();
            float finalDamage = dmg * (perkStats != null ? perkStats.DamageMultiplier.Value : 1f);
            
            if (t != ToolType.Hammer)
            {
                Instance.hitbox.damage = finalDamage;
                Instance.hitbox.tool = t;
                Instance.hitbox.sourceDirection = transform.position;
                Instance.hitbox.dropItems = drops;
                Instance.hitbox.parentTracker = this;
                StartCoroutine(DoAttack());
            }
            //if we're holding a hammer, we want to do the healy thing instead
            else
            {
                Instance.repairHitbox.damage = finalDamage;
                Instance.repairHitbox.tool = t;
                Instance.repairHitbox.sourceDirection = transform.position;
                Instance.repairHitbox.dropItems = drops;
                StartCoroutine(DoRepairAttack());
            }

        }

    }

    private IEnumerator DoAttack()
    {
        AudioManager.Instance.PlayClientRpc(SoundId.Sword_Swing, transform.position);
        attackAnimator.SetTrigger("Swing");

        moveSpeed /= 2;

        // Enable collider on owner client (for hit detection) and server (for consistency)
        hitbox.EnableCollider();

        hitbox.EnableVisuals();

        yield return new WaitForSeconds(.1f);

        // Disable collider and clear hit tracking
        hitbox.DisableCollider();

        hitbox.DisableVisuals();
        moveSpeed *= 2;
        swingCooldownTimer = 0f;
    }

    private IEnumerator DoRepairAttack()
    {
        AudioManager.Instance.PlayClientRpc(SoundId.Sword_Swing, transform.position);
        attackAnimator.SetTrigger("Swing");
        moveSpeed /= 2;

        repairHitbox.EnableCollider();
        repairHitbox.EnableVisuals();
        

        yield return new WaitForSeconds(.1f);

        repairHitbox.DisableCollider();
        repairHitbox.DisableVisuals();

        moveSpeed *= 2;
        swingCooldownTimer = 0f;
    }

    public void HitSomething(IDamageable damageable)
    {
        damageable.OnDeath -= playerHealth.AddXp;
        damageable.OnDeath += playerHealth.AddXp;
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
        OnMenuButtonPressed?.Invoke();
    }

    public bool IsPointerOverUI()
    {
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, hits);

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

    private void HandleCorePlaced(Transform core)
    {
        Core.CORE.OnCoreDestroyed += HandleCoreDestroyed;
        _coreTransform = core;
    }

    private void HandleCoreDestroyed()
    {
        movementLocked = true;
    }

    private void HandleLevelUp()
    {
        var perkStats = GetComponent<PlayerPerkStats>();
        float speedMultiplier = perkStats != null ? perkStats.SpeedMultiplier.Value : 1f;
        moveSpeed = baseMoveSpeed * speedMultiplier * 1.05f; // 5% increase per level
    }

    [ClientRpc]
    public void TeleportOwnerClientRpc(Vector3 spawnPos, ClientRpcParams p = default)
    {
        transform.position = spawnPos;
    }


    private void HandlePauseChanged(bool value)
    {
        movementLocked = value;
    }

    public void KilledSomething(IDamageable damageable)
    {
        
    }
}
