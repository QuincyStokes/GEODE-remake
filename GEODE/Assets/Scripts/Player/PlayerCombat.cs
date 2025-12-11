using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerCombat : NetworkBehaviour, ITracksHits
{
    [Header("References")]
    [SerializeField] private Hitbox hitbox;
    [SerializeField] private Hitbox repairHitbox;
    [SerializeField] private Animator attackAnimator;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerPerkStats playerPerkStats;

    private NetworkVariable<int> kills = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool isAttacking;
    private bool isRepairing;

    // Events
    public event Action<float, ToolType> OnAttackStarted;
    public event Action OnAttackFinished;
    public event Action<IDamageable> OnEnemyHit;
    public event Action<int> OnKillsChanged;

    // Properties
    NetworkVariable<int> ITracksHits.kills
    {
        get { return kills; }
        set { kills = value; }
    }

    public bool CanAttack
    {
        get { return playerInput != null && playerInput.IsSwingReady && !isAttacking && !isRepairing; }
    }

    public bool IsAttacking
    {
        get { return isAttacking || isRepairing; }
    }

    private void Awake()
    {
        isAttacking = false;
        isRepairing = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (kills != null)
        {
            kills.OnValueChanged += OnKillsValueChanged;
        }
    }

    public void Initialize()
    {
        // Initialize is called by PlayerController for any additional setup if needed
    }

    private void OnKillsValueChanged(int oldValue, int newValue)
    {
        OnKillsChanged?.Invoke(newValue);
    }

    public Vector2 GetAttackDirection()
    {
        return playerMovement.LastMovedDirection;
    }

    public void PerformAttack(float baseDamage, ToolType toolType, bool dropItems)
    {
        if (!CanAttack)
        {
            return;
        }

        float finalDamage = baseDamage;
        if (playerPerkStats != null)
        {
            finalDamage = baseDamage * playerPerkStats.DamageMultiplier.Value;
        }

        OnAttackStarted?.Invoke(finalDamage, toolType);

        if (toolType != ToolType.Hammer)
        {
            StartCoroutine(ExecuteNormalAttack(finalDamage, toolType, dropItems));
        }
        else
        {
            StartCoroutine(ExecuteRepairAttack(finalDamage, toolType, dropItems));
        }
    }

    private IEnumerator ExecuteNormalAttack(float damage, ToolType toolType, bool dropItems)
    {
        isAttacking = true;

        AudioManager.Instance.PlayClientRpc(SoundId.Sword_Swing, transform.position);
        attackAnimator.SetTrigger("Swing");

        playerMovement.LockMovement();

        hitbox.damage = damage;
        hitbox.tool = toolType;
        hitbox.sourceDirection = transform.position;
        hitbox.dropItems = dropItems;
        hitbox.parentTracker = this;

        hitbox.EnableCollider();
        hitbox.EnableVisuals();

        yield return new WaitForSeconds(0.1f);

        hitbox.DisableCollider();
        hitbox.DisableVisuals();

        playerMovement.UnlockMovement();

        playerInput.RefreshSwingCooldown();
        isAttacking = false;

        OnAttackFinished?.Invoke();
    }

    private IEnumerator ExecuteRepairAttack(float damage, ToolType toolType, bool dropItems)
    {
        isRepairing = true;

        AudioManager.Instance.PlayClientRpc(SoundId.Sword_Swing, transform.position);
        attackAnimator.SetTrigger("Swing");

        playerMovement.LockMovement();

        repairHitbox.damage = damage;
        repairHitbox.tool = toolType;
        repairHitbox.sourceDirection = transform.position;
        repairHitbox.dropItems = dropItems;

        repairHitbox.EnableCollider();
        repairHitbox.EnableVisuals();

        yield return new WaitForSeconds(0.1f);

        repairHitbox.DisableCollider();
        repairHitbox.DisableVisuals();

        playerMovement.UnlockMovement();

        playerInput.RefreshSwingCooldown();
        isRepairing = false;

        OnAttackFinished?.Invoke();
    }

    public void HitSomething(IDamageable damageable)
    {
        if (damageable == null)
        {
            return;
        }

        OnEnemyHit?.Invoke(damageable);

        // Subscribe to death event to track kills
        damageable.OnDeath -= HandleEnemyDeath;
        damageable.OnDeath += HandleEnemyDeath;
    }

    public void KilledSomething(IDamageable damageable)
    {
        // This is called when the player gets credit for the kill
        // Increment kills counter via RPC
        IncrementKillsServerRpc();
    }

    private void HandleEnemyDeath(IDamageable damageable)
    {
        // Called when an enemy the player hit dies
        // We rely on KilledSomething being called by damage system
    }

    [ServerRpc]
    private void IncrementKillsServerRpc()
    {
        kills.Value++;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (kills != null)
        {
            kills.OnValueChanged -= OnKillsValueChanged;
        }
    }
}
