using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
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

            //Throw
            playerInput.Player.Throw.performed += ThrowHeldItemWrapper;
            playerInput.Player.Throw.Enable();


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
        playerInput.Player.Menu.Disable();


        //Throw
        playerInput.Player.Throw.performed -= ThrowHeldItemWrapper;
        playerInput.Player.Throw.Disable();

        if (DayCycleManager.Instance != null)
        {
            DayCycleManager.Instance.becameDay -= dayNumber.IncreaseDay;
            DayCycleManager.Instance.becameNight -= dayNumber.IncreaseNight;
        }

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

        //animation?
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
                inspectionMenu.DoMenu(go);
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
                LootManager.Instance.SpawnLootServerRpc(transform.position, CursorStack.Instance.ItemStack.Id, CursorStack.Instance.Amount, 2f, horizOffset, CursorStack.Instance.ItemStack.quality);
                CursorStack.Instance.ItemStack = ItemStack.Empty;
            }
            else
            {
                inspectionMenu.CloseInspectionMenu();

                //if we have a uniqueUI open, close it.
                if (openUniqueUI != null)
                    openUniqueUI.GetComponent<IUniqueMenu>().HideMenu();
            }
        }
    }

    private void MousePositionHandler()
    {
        mousePos = Camera.main.ScreenToWorldPoint(mouseInput.ReadValue<Vector2>());
        Vector3Int mousePosInt = new Vector3Int((int)mousePos.x, (int)mousePos.y, 0);
        if (mousePosInt != previousMousePosInt && GridManager.Instance != null)
        {
            GridManager.Instance.UpdateMousePos(mousePosInt);
        }
    }

    private void ThrowHeldItemWrapper(InputAction.CallbackContext context)
    {

        float offset;
        if (lastMovedDir.x < 0) offset = -1.5f;
        else offset = 1.5f;
        playerInventory.ThrowCurrentlySelectedHeldItem(horizOffset: offset);
    }

    // private void OnTriggerEnter2D(Collider2D collision)
    // {
    //     // if (collision.CompareTag("EnvironmentObject"))
    //     // {
    //     //     EnvironmentObject envObj = collision.gameObject.GetComponentInParent<EnvironmentObject>();
    //     //     if (envObj != null)
    //     //     {
    //     //         envObj.TakeDamageServerRpc(4, gameObject.transform.position, true);
    //     //         //Debug.Log($"Hit {collision.name} for 4 damage");
    //     //     }
    //     // }
    //     // else if (collision.CompareTag("Enemy"))
    //     // {
    //     //     BaseEnemy baseEnemy = collision.gameObject.GetComponentInParent<BaseEnemy>();
    //     //     if (baseEnemy != null)
    //     //     {
    //     //         baseEnemy.TakeDamageServerRpc(4, gameObject.transform.position, true);

    //     //     }
    //     // }
    // }

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


        if(IsServer)
        {
            hitbox.EnableServerCollider();
        }

        hitbox.EnableVisuals();

        yield return new WaitForSeconds(.1f);

        if(IsServer)
        {
            hitbox.DisableServerCollider();
        }

        hitbox.DisableVisuals();
        moveSpeed *= 2;
        swingCooldownTimer = 0f;
    }

    private IEnumerator DoRepairAttack()
    {
        Instance.repairHitbox.gameObject.SetActive(true);
        attackAnimator.SetTrigger("Swing");
        moveSpeed /= 2;
        yield return new WaitForSeconds(.1f);
        Instance.repairHitbox.gameObject.SetActive(false);
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
        TogglePauseMenu();
    }

    public void TogglePauseMenu()
    {
        pauseMenu.SetActive(!pauseMenu.activeSelf);
        movementLocked = pauseMenu.activeSelf;
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
        moveSpeed *= 1.05f;
    }

    [ClientRpc]
    public void TeleportOwnerClientRpc(Vector3 spawnPos, ClientRpcParams p = default)
    {
        transform.position = spawnPos;
    }


}
