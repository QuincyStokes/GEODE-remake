using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerUIInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InspectionMenu inspectionMenu;
    [SerializeField] private LayerMask interactableLayerMask;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerInventory playerInventory;

    private GameObject openUniqueUI;
    private IInteractable currentInteractedObject;

    // Events
    public event Action<IInteractable> OnObjectInteracted;
    public event Action<IUniqueMenu> OnUniqueMenuOpened;
    public event Action OnUniqueMenuClosed;
    public event Action OnInspectionMenuClosed;
    

    private void Start()
    {
        if (playerInput != null)
        {
            playerInput.OnSecondaryFirePerformed += HandleSecondaryFireInput;
            playerInput.OnInventoryTogglePerformed += HandleInventoryToggleInput;
        }
    }

    private void Update()
    {
        CheckUniqueUIRange();
    }

    private void HandleSecondaryFireInput(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        Vector3 mouseWorldPos = playerInput.CurrentMousePosition;
        PerformInteractionRaycast(mouseWorldPos);
    }

    private void HandleInventoryToggleInput(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        if (playerInventory != null && !playerInventory.IsInventoryOpen())
        {
            if (openUniqueUI != null)
            {
                HideUniqueMenu();
                CloseInspectionMenu();
            }

            if (currentInteractedObject != null)
            {
                currentInteractedObject.DoUnclickedThings();
            }
        }
    }

    public void PerformInteractionRaycast(Vector3 worldPosition)
    {
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero, 10, interactableLayerMask);

        if (hit)
        {
            HandleInteractableHit(hit);
        }
        else if (!IsPointerOverUI())
        {
            HandleEmptySpaceClick();
        }
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

    private void HandleInteractableHit(RaycastHit2D hit)
    {
        Debug.Log($"Raycast Hit {hit.collider.gameObject.name}");

        MonoBehaviour[] parentComponents = hit.collider.gameObject.GetComponentsInParent<MonoBehaviour>();

        GameObject interactableGameObject = null;
        GameObject uniqueMenuGameObject = null;

        // Find IInteractable component
        foreach (MonoBehaviour component in parentComponents)
        {
            if (component is IInteractable)
            {
                interactableGameObject = component.gameObject;
                break;
            }
        }

        // Find IUniqueMenu component
        foreach (MonoBehaviour component in parentComponents)
        {
            if (component is IUniqueMenu)
            {
                uniqueMenuGameObject = component.gameObject;
                break;
            }
        }

        if (interactableGameObject != null)
        {
            // Close previous interactable if different
            if (currentInteractedObject != null && interactableGameObject.GetComponent<IInteractable>() != currentInteractedObject)
            {
                currentInteractedObject.DoUnclickedThings();
            }

            // Open inspection menu
            inspectionMenu.DoMenu(interactableGameObject);

            // If this object has a unique menu, open inventory alongside
            if (uniqueMenuGameObject != null && playerInventory != null)
            {
                playerInventory.OpenInventory();
            }

            // Notify the interactable it was clicked
            IInteractable interactable = interactableGameObject.GetComponent<IInteractable>();
            interactable.DoClickedThings();
            currentInteractedObject = interactable;

            OnObjectInteracted?.Invoke(interactable);
        }
        else
        {
            CloseInspectionMenu();
        }

        // Handle unique menu opening/closing
        if (uniqueMenuGameObject != null)
        {
            IUniqueMenu uniqueMenu = uniqueMenuGameObject.GetComponent<IUniqueMenu>();

            // Close old unique UI if different
            if (openUniqueUI != null && openUniqueUI != uniqueMenuGameObject)
            {
                openUniqueUI.GetComponent<IUniqueMenu>().HideMenu();
            }

            // Show new unique menu
            uniqueMenu.ShowMenu();
            openUniqueUI = uniqueMenuGameObject;
            OnUniqueMenuOpened?.Invoke(uniqueMenu);
        }
        else
        {
            HideUniqueMenu();
        }
    }

    private void HandleEmptySpaceClick()
    {
        // Check if we should drop the held item
        if (!CursorStack.Instance.ItemStack.Equals(ItemStack.Empty))
        {
            float horizOffset;
            if (playerMovement.LastMovedDirection.x < 0)
            {
                horizOffset = -1.5f;
            }
            else
            {
                horizOffset = 1.5f;
            }

            LootManager.Instance.SpawnLootServerRpc(
                transform.position,
                CursorStack.Instance.ItemStack.Id,
                CursorStack.Instance.Amount,
                2f,
                horizOffset
            );

            CursorStack.Instance.ItemStack = ItemStack.Empty;
        }
        else
        {
            CloseInspectionMenu();
            currentInteractedObject?.DoUnclickedThings();

            if (openUniqueUI != null)
            {
                HideUniqueMenu();
            }
        }
    }

    private void CheckUniqueUIRange()
    {
        if (openUniqueUI == null)
        {
            return;
        }

        float distanceToUI = Vector2.Distance(openUniqueUI.transform.position, transform.position);
        if (distanceToUI > 10)
        {
            HideUniqueMenu();
        }
    }

    public void OpenInspectionMenu(GameObject target)
    {
        if (inspectionMenu != null)
        {
            inspectionMenu.DoMenu(target);
        }
    }

    public void CloseInspectionMenu()
    {
        if (inspectionMenu != null)
        {
            inspectionMenu.CloseInspectionMenu();
            OnInspectionMenuClosed?.Invoke();
        }
    }

    private void HideUniqueMenu()
    {
        if (openUniqueUI != null)
        {
            openUniqueUI.GetComponent<IUniqueMenu>().HideMenu();
            OnUniqueMenuClosed?.Invoke();
            openUniqueUI = null;
        }
    }

    public void HandlePauseStateChanged(bool isPaused)
    {
        if (isPaused)
        {
            // Don't close menus on pause; they should remain visible
            // This is handled by PauseController showing the pause menu
        }
        else
        {
            // Resume state; no special action needed
        }
    }

    private void OnDestroy()
    {
        if (playerInput != null)
        {
            playerInput.OnSecondaryFirePerformed -= HandleSecondaryFireInput;
            playerInput.OnInventoryTogglePerformed -= HandleInventoryToggleInput;
        }
    }
}
