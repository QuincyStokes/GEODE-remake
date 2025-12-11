using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour, IKnockbackable
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject hitboxParent;
    [SerializeField] private PlayerPerkStats playerPerkStats;

    private Rigidbody2D rb;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float baseMoveSpeed;
    [SerializeField] private float knockbackDecay;

    private Vector2 localInputVelocity;
    private Vector2 lastMovedDir;
    private Vector2 externalVelocity;
    private bool movementLocked;

    private NetworkVariable<Vector2> networkDirection = new NetworkVariable<Vector2>(
        Vector2.down,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<float> networkVelocity = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Events
    public event Action<Vector2> OnDirectionChanged;
    public event Action OnMovementLocked;
    public event Action OnMovementUnlocked;
    public event Action<float> OnSpeedMultiplierApplied;

    // Properties
    public Vector2 LastMovedDirection
    {
        get { return lastMovedDir; }
    }

    public Vector2 CurrentVelocity
    {
        get { return rb.linearVelocity; }
    }

    public bool IsMovementLocked
    {
        get { return movementLocked; }
    }

    public float MoveSpeed
    {
        get { return moveSpeed; }
    }

    private void Awake()
    {
        rb = GetComponentInChildren<Rigidbody2D>();
        baseMoveSpeed = moveSpeed;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        networkDirection.OnValueChanged += OnNetworkDirectionChanged;
        networkVelocity.OnValueChanged += OnNetworkVelocityChanged;
    }

    public void Initialize()
    {
        // Initialize is called by PlayerController for any additional setup if needed
    }

    private void Start()
    {
        if (playerPerkStats != null)
        {
            StartCoroutine(WaitForSpeedPerksAndApply());
        }
    }

    private System.Collections.IEnumerator WaitForSpeedPerksAndApply()
    {
        yield return null;

        ApplySpeedPerks(playerPerkStats.SpeedMultiplier.Value);
        playerPerkStats.SpeedMultiplier.OnValueChanged += OnSpeedMultiplierChanged;
    }

    private void OnSpeedMultiplierChanged(float oldValue, float newValue)
    {
        ApplySpeedPerks(newValue);
    }

    private void ApplySpeedPerks(float speedMultiplier)
    {
        moveSpeed = baseMoveSpeed * speedMultiplier;
        OnSpeedMultiplierApplied?.Invoke(speedMultiplier);
        Debug.Log($"[PlayerMovement] Applied speed multiplier {speedMultiplier}, new speed: {moveSpeed}");
    }

    private void Update()
    {
        // Update loop can be used for any per-frame non-physics updates if needed
    }

    private void FixedUpdate()
    {
        if (movementLocked)
        {
            return;
        }

        PerformMovementUpdate();
    }

    public Vector2 GetMovementVelocity()
    {
        return localInputVelocity;
    }

    public void SetMovementInput(Vector2 input)
    {
        localInputVelocity = input * moveSpeed;
    }

    private void PerformMovementUpdate()
    {
        Vector2 finalVelocity = localInputVelocity + externalVelocity;
        rb.linearVelocity = finalVelocity;

        if (finalVelocity.sqrMagnitude > 0.01f)
        {
            lastMovedDir = finalVelocity.normalized;
        }

        UpdateHitboxRotation();
        UpdateAnimatorParameters(finalVelocity);
        SyncMovementWithServer(finalVelocity);

        externalVelocity = Vector2.Lerp(externalVelocity, Vector2.zero, knockbackDecay * Time.fixedDeltaTime);
    }

    private void UpdateHitboxRotation()
    {
        float hitboxAngle = Mathf.Atan2(lastMovedDir.y, lastMovedDir.x) * Mathf.Rad2Deg;
        float angle = Mathf.Round(hitboxAngle / 90) * 90f + 90;
        hitboxParent.transform.localRotation = Quaternion.Euler(0, 0, angle);
    }

    private void UpdateAnimatorParameters(Vector2 finalVelocity)
    {
        animator.SetFloat("moveX", lastMovedDir.x);
        animator.SetFloat("moveY", lastMovedDir.y);
        animator.SetFloat("velocity", finalVelocity.magnitude);
    }

    private void SyncMovementWithServer(Vector2 finalVelocity)
    {
        bool directionChanged = Vector2.Distance(networkDirection.Value, lastMovedDir) > 0.001f;
        bool velocityChanged = Mathf.Abs(networkVelocity.Value - finalVelocity.sqrMagnitude) > 0.001f;

        if (directionChanged || velocityChanged)
        {
            UpdateMovementServerRpc(lastMovedDir, finalVelocity.magnitude);
        }
    }

    private void OnNetworkDirectionChanged(Vector2 oldValue, Vector2 newValue)
    {
        animator.SetFloat("moveX", newValue.x);
        animator.SetFloat("moveY", newValue.y);
    }

    private void OnNetworkVelocityChanged(float oldValue, float newValue)
    {
        animator.SetFloat("velocity", newValue);
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

    public void LockMovement()
    {
        if (movementLocked)
        {
            return;
        }

        movementLocked = true;
        rb.linearVelocity = Vector2.zero;
        OnMovementLocked?.Invoke();
    }

    public void UnlockMovement()
    {
        if (!movementLocked)
        {
            return;
        }

        movementLocked = false;
        OnMovementUnlocked?.Invoke();
    }

    public void SetPositionCenterWorld()
    {
        if (WorldGenManager.Instance != null)
        {
            transform.position = new Vector3Int(
                WorldGenManager.Instance.WorldSizeX / 2,
                WorldGenManager.Instance.WorldSizeY / 2
            );
        }
    }

    public void ApplyLevelUpSpeedBonus()
    {
        if (playerPerkStats != null)
        {
            float currentMultiplier = playerPerkStats.SpeedMultiplier.Value;
            moveSpeed = baseMoveSpeed * currentMultiplier * 1.05f;
            Debug.Log($"[PlayerMovement] Applied level up speed bonus. New speed: {moveSpeed}");
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (playerPerkStats != null)
        {
            playerPerkStats.SpeedMultiplier.OnValueChanged -= OnSpeedMultiplierChanged;
        }
    }
}
