using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Orchestrator for the player character. Coordinates between input, movement, combat, and UI subsystems.
/// Maintains network authority and provides public API for external systems to interact with the player.
/// 
/// Use GetLocalPlayerController() to safely retrieve the local player's controller.
/// This replaces the singleton pattern to support multiplayer games properly.
/// </summary>
public class PlayerController : NetworkBehaviour
{
    /// <summary>
    /// Cached reference to the local player's controller. Only set for the owner client.
    /// This is much more efficient than searching for it every time.
    /// </summary>
    private static PlayerController localPlayerController;

    /// <summary>
    /// Gets the local player's controller. Only valid for the owner client.
    /// Returns null if called on a non-owner client or if local player hasn't spawned yet.
    /// O(1) performance - uses cached reference.
    /// </summary>
    public static PlayerController GetLocalPlayerController()
    {
        return localPlayerController;
    }

    [Header("Subsystem References")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private PlayerUIInteraction playerUIInteraction;

    [Header("Player Component References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerHealthAndXP playerHealth;
    [SerializeField] private DayNumber dayNumber;

    [Header("UI References")]
    [SerializeField] private GameObject playerUICanvas;
    [SerializeField] private GameObject pauseMenu;

    private Transform _coreTransform;

    // Events
    public event Action OnMenuButtonPressed;

    // Public properties for external access to subsystems
    public PlayerInput Input
    {
        get { return playerInput; }
    }

    public PlayerMovement Movement
    {
        get { return playerMovement; }
    }

    public PlayerCombat Combat
    {
        get { return playerCombat; }
    }

    public PlayerUIInteraction UI
    {
        get { return playerUIInteraction; }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
        {
            return;
        }

        // Cache this as the local player controller
        localPlayerController = this;

        // Initialize subsystems
        if (playerInput != null)
        {
            playerInput.EnableInput();
        }

        if (playerMovement != null)
        {
            playerMovement.Initialize();
        }

        if (playerCombat != null)
        {
            playerCombat.Initialize();
        }

        // Wire up input events to handlers
        WireInputHandlers();

        // Setup manager event subscriptions
        SetupManagerSubscriptions();

        // Initialize player position and camera
        InitializePlayerSpawn();

        // Activate UI
        if (playerUICanvas != null)
        {
            playerUICanvas.SetActive(true);
        }
    }

    private void OnDisable()
    {
        if (!IsOwner)
        {
            return;
        }

        // Clear cached reference when despawning
        localPlayerController = null;

        // Disable input
        if (playerInput != null)
        {
            playerInput.DisableInput();
        }

        // Unsubscribe from all events
        UnwireInputHandlers();
        TeardownManagerSubscriptions();
    }

    private void Start()
    {
        if (!IsOwner)
        {
            return;
        }

        if (playerHealth != null)
        {
            playerHealth.playerController = this;
        }

        CameraManager.Instance.FollowPlayer(transform);
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
        {
            return;
        }

        // Pass movement input to the movement subsystem
        if (playerInput != null && playerMovement != null)
        {
            Vector2 movementInput = playerInput.GetMovementInput();
            playerMovement.SetMovementInput(movementInput);
        }
    }

    private void WireInputHandlers()
    {
        if (playerInput == null)
        {
            return;
        }

        // Primary Fire
        playerInput.OnPrimaryFirePerformed += OnPrimaryFireHandler;

        // Inventory Toggle
        playerInput.OnInventoryTogglePerformed += playerInventory.ToggleInventory;
        playerInput.OnInventoryTogglePerformed += OnInventoryToggleHandler;

        // Numbers (hotbar selection)
        playerInput.GetNumbersInputAction().performed += playerInventory.OnNumberPressed;

        // Scroll (hotbar cycling)
        playerInput.GetScrollInputAction().performed += playerInventory.OnScroll;

        // Menu
        playerInput.OnMenuOpenedPerformed += OnMenuOpenedHandler;

        // Throw
        playerInput.OnThrowPerformed += OnThrowHandler;

        // Pause
        PauseController.OnPauseChanged += OnPauseStateChanged;
    }

    private void UnwireInputHandlers()
    {
        if (playerInput == null)
        {
            return;
        }

        playerInput.OnPrimaryFirePerformed -= OnPrimaryFireHandler;
        playerInput.OnInventoryTogglePerformed -= playerInventory.ToggleInventory;
        playerInput.OnInventoryTogglePerformed -= OnInventoryToggleHandler;
        playerInput.GetNumbersInputAction().performed -= playerInventory.OnNumberPressed;
        playerInput.GetScrollInputAction().performed -= playerInventory.OnScroll;
        playerInput.OnMenuOpenedPerformed -= OnMenuOpenedHandler;
        playerInput.OnThrowPerformed -= OnThrowHandler;
        PauseController.OnPauseChanged -= OnPauseStateChanged;
    }

    private void SetupManagerSubscriptions()
    {
        // Day cycle
        if (DayCycleManager.Instance != null)
        {
            DayCycleManager.Instance.becameDay += dayNumber.IncreaseDay;
            DayCycleManager.Instance.becameNight += dayNumber.IncreaseNight;
        }

        // Flow field / Core placement
        if (FlowFieldManager.Instance != null)
        {
            FlowFieldManager.Instance.corePlaced += OnCorePlaced;
        }

        // Player health / leveling
        if (playerHealth != null)
        {
            playerHealth.OnPlayerLevelUp += OnPlayerLevelUp;
        }
    }

    private void TeardownManagerSubscriptions()
    {
        if (DayCycleManager.Instance != null)
        {
            DayCycleManager.Instance.becameDay -= dayNumber.IncreaseDay;
            DayCycleManager.Instance.becameNight -= dayNumber.IncreaseNight;
        }

        if (FlowFieldManager.Instance != null)
        {
            FlowFieldManager.Instance.corePlaced -= OnCorePlaced;
        }

        if (playerHealth != null)
        {
            playerHealth.OnPlayerLevelUp -= OnPlayerLevelUp;
        }
    }

    private void InitializePlayerSpawn()
    {
        if (WorldGenManager.Instance != null)
        {
            transform.position = new Vector3Int(
                WorldGenManager.Instance.WorldSizeX / 2,
                WorldGenManager.Instance.WorldSizeY / 2
            );
        }
        else
        {
            Debug.Log("WorldGenManager is null, cannot place player.");
        }

        CameraWorldConfiner.Instance.SetCameraBoundary();
    }

    private void OnPrimaryFireHandler(InputAction.CallbackContext context)
    {
        if (IsPointerOverUI())
        {
            return;
        }

        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(playerInput.GetRawMouseInput());
        playerInventory.UseSelectedItem(worldMousePos);
    }

    private void OnInventoryToggleHandler(InputAction.CallbackContext context)
    {
        if (!playerInventory.IsInventoryOpen())
        {
            if (playerUIInteraction != null)
            {
                playerUIInteraction.CloseInspectionMenu();
            }
        }
    }

    private void OnMenuOpenedHandler(InputAction.CallbackContext context)
    {
        OnMenuButtonPressed?.Invoke();
    }

    private void OnThrowHandler(InputAction.CallbackContext context)
    {
        if (playerMovement == null)
        {
            return;
        }

        float offset;
        if (playerMovement.LastMovedDirection.x < 0)
        {
            offset = -1.5f;
        }
        else
        {
            offset = 1.5f;
        }

        playerInventory.ThrowCurrentlySelectedHeldItem(horizOffset: offset);
    }

    private void OnPauseStateChanged(bool isPaused)
    {
        if (playerMovement != null)
        {
            if (isPaused)
            {
                playerMovement.LockMovement();
            }
            else
            {
                playerMovement.UnlockMovement();
            }
        }

        if (playerUIInteraction != null)
        {
            playerUIInteraction.HandlePauseStateChanged(isPaused);
        }
    }

    private void OnCorePlaced(Transform core)
    {
        Core.CORE.OnCoreDestroyed += OnCoreDestroyed;
        _coreTransform = core;
    }

    private void OnCoreDestroyed()
    {
        if (playerMovement != null)
        {
            playerMovement.LockMovement();
        }
    }

    private void OnPlayerLevelUp()
    {
        if (playerMovement != null)
        {
            playerMovement.ApplyLevelUpSpeedBonus();
        }
    }

    // Public API for external systems
    public void TeleportToWorldCenter()
    {
        if (playerMovement != null)
        {
            playerMovement.SetPositionCenterWorld();
        }
    }

    public Vector3 GetPlayerPosition()
    {
        return transform.position;
    }

    public Vector2 GetLastMovedDirection()
    {
        if (playerMovement != null)
        {
            return playerMovement.LastMovedDirection;
        }

        return Vector2.down;
    }

    public bool IsMovementLocked
    {
        get
        {
            if (playerMovement != null)
            {
                return playerMovement.IsMovementLocked;
            }

            return false;
        }
    }

    public float GetCurrentHealth()
    {
        if (playerHealth != null)
        {
            return playerHealth.CurrentHealth.Value;
        }

        return 0f;
    }

    public bool IsPointerOverUI()
    {
        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };

        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, hits);

        return hits.Exists(h => h.module is GraphicRaycaster);
    }

    public void LeaveGame()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("Lobby");
    }

    [ClientRpc]
    public void TeleportOwnerClientRpc(Vector3 spawnPos, ClientRpcParams p = default)
    {
        transform.position = spawnPos;
    }
}
