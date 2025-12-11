using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInputActionMap playerInputActionMap;

    private InputAction movementInput;
    private InputAction mouseInput;
    
    private Vector3 mousePos;
    private Vector3Int previousMousePosInt;
    
    [SerializeField] private float swingCooldown;
    private float swingCooldownTimer;

    // Events
    public event Action<InputAction.CallbackContext> OnPrimaryFirePerformed;
    public event Action<InputAction.CallbackContext> OnSecondaryFirePerformed;
    public event Action<InputAction.CallbackContext> OnInventoryTogglePerformed;
    public event Action<InputAction.CallbackContext> OnThrowPerformed;
    public event Action<InputAction.CallbackContext> OnMenuOpenedPerformed;
    public event Action<Vector3> OnMousePositionUpdated;
    public event Action OnSwingCooldownRefreshed;

    // Properties
    public float SwingCooldownRemaining
    {
        get { return Mathf.Max(0, swingCooldown - swingCooldownTimer); }
    }

    public bool IsSwingReady
    {
        get { return swingCooldownTimer >= swingCooldown; }
    }

    public Vector3 CurrentMousePosition
    {
        get { return mousePos; }
    }

    public Vector3Int CurrentMouseGridPosition
    {
        get { return previousMousePosInt; }
    }

    private void Awake()
    {
        playerInputActionMap = new PlayerInputActionMap();
        swingCooldownTimer = swingCooldown;
    }

    public void EnableInput()
    {
        if (playerInputActionMap == null)
        {
            playerInputActionMap = new PlayerInputActionMap();
        }

        // Movement
        movementInput = playerInputActionMap.Player.Move;
        movementInput.Enable();

        // Inventory
        playerInputActionMap.Player.InventoryToggle.performed += OnInventoryToggleHandler;
        playerInputActionMap.Player.InventoryToggle.Enable();

        // Numbers
        playerInputActionMap.Player.Numbers.Enable();

        // PrimaryFire
        playerInputActionMap.Player.PrimaryFire.performed += OnPrimaryFireHandler;
        playerInputActionMap.Player.PrimaryFire.Enable();

        // SecondaryFire
        playerInputActionMap.Player.SecondaryFire.performed += OnSecondaryFireHandler;
        playerInputActionMap.Player.SecondaryFire.Enable();

        // Scroll
        playerInputActionMap.Player.Scroll.Enable();

        // Menu
        playerInputActionMap.Player.Menu.performed += OnMenuOpenedHandler;
        playerInputActionMap.Player.Menu.Enable();

        // Mouse position
        mouseInput = playerInputActionMap.Player.Mouse;
        mouseInput.Enable();

        // Throw
        playerInputActionMap.Player.Throw.performed += OnThrowHandler;
        playerInputActionMap.Player.Throw.Enable();
    }

    public void DisableInput()
    {
        if (playerInputActionMap == null)
        {
            return;
        }

        // Movement
        movementInput.Disable();

        // Mouse input
        mouseInput.Disable();

        // Inventory
        playerInputActionMap.Player.InventoryToggle.performed -= OnInventoryToggleHandler;
        playerInputActionMap.Player.InventoryToggle.Disable();

        // Numbers
        playerInputActionMap.Player.Numbers.Disable();

        // PrimaryFire
        playerInputActionMap.Player.PrimaryFire.performed -= OnPrimaryFireHandler;
        playerInputActionMap.Player.PrimaryFire.Disable();

        // SecondaryFire
        playerInputActionMap.Player.SecondaryFire.performed -= OnSecondaryFireHandler;
        playerInputActionMap.Player.SecondaryFire.Disable();

        // Scroll
        playerInputActionMap.Player.Scroll.Disable();

        // Menu
        playerInputActionMap.Player.Menu.performed -= OnMenuOpenedHandler;
        playerInputActionMap.Player.Menu.Disable();

        // Throw
        playerInputActionMap.Player.Throw.performed -= OnThrowHandler;
        playerInputActionMap.Player.Throw.Disable();
    }

    private void Update()
    {
        UpdateCooldownTimer();
        UpdateMousePosition();
    }

    private void UpdateCooldownTimer()
    {
        if (swingCooldownTimer < swingCooldown)
        {
            swingCooldownTimer += Time.deltaTime;

            if (swingCooldownTimer >= swingCooldown)
            {
                OnSwingCooldownRefreshed?.Invoke();
            }
        }
    }

    private void UpdateMousePosition()
    {
        mousePos = Camera.main.ScreenToWorldPoint(mouseInput.ReadValue<Vector2>());
        Vector3Int mousePosInt = new Vector3Int((int)mousePos.x, (int)mousePos.y, 0);

        if (mousePosInt != previousMousePosInt && GridManager.Instance != null)
        {
            GridManager.Instance.UpdateMousePos(mousePosInt);
            OnMousePositionUpdated?.Invoke(mousePos);
            previousMousePosInt = mousePosInt;
        }
    }

    public Vector2 GetMovementInput()
    {
        return movementInput.ReadValue<Vector2>().normalized;
    }

    public Vector2 GetRawMouseInput()
    {
        return mouseInput.ReadValue<Vector2>();
    }

    public void RefreshSwingCooldown()
    {
        swingCooldownTimer = 0f;
    }

    private void OnPrimaryFireHandler(InputAction.CallbackContext context)
    {
        OnPrimaryFirePerformed?.Invoke(context);
    }

    private void OnSecondaryFireHandler(InputAction.CallbackContext context)
    {
        OnSecondaryFirePerformed?.Invoke(context);
    }

    private void OnInventoryToggleHandler(InputAction.CallbackContext context)
    {
        OnInventoryTogglePerformed?.Invoke(context);
    }

    private void OnThrowHandler(InputAction.CallbackContext context)
    {
        OnThrowPerformed?.Invoke(context);
    }

    private void OnMenuOpenedHandler(InputAction.CallbackContext context)
    {
        OnMenuOpenedPerformed?.Invoke(context);
    }

    public InputAction GetNumbersInputAction()
    {
        return playerInputActionMap.Player.Numbers;
    }

    public InputAction GetScrollInputAction()
    {
        return playerInputActionMap.Player.Scroll;
    }
}
