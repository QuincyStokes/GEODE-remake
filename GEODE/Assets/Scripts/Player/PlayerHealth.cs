using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using System.Collections;
using System;


public class PlayerHealthAndXP : NetworkBehaviour, IDamageable, IExperienceGain
{
    //* --------------- Player Health ---------------- */
    [Header("Health")]
    [SerializeField] public NetworkVariable<float> MaxHealth { get; set; } = new NetworkVariable<float>(100);
    [SerializeField] public NetworkVariable<float> CurrentHealth { get; set; } = new NetworkVariable<float>(100);


    //* --------------- XP ---------------- */
    [Header("XP")]
    [SerializeField] private int maxLevelXp;
    [SerializeField] private int currentLevelXp;
    [SerializeField] private int totalXp;
    [SerializeField] private int level;

    public int MaximumLevelXp { get => maxLevelXp; set => maxLevelXp = value; }
    public int CurrentXp { get => currentLevelXp; set => currentLevelXp = value; }
    public int CurrentTotalXp { get => totalXp; set => totalXp = value; }
    public int Level { get => level; set => level = value; }

    //* --------------- Dropped Items---------------- */
    [Header("Settings")]
    [SerializeField] private List<DroppedItem> droppedItems;
    public List<DroppedItem> DroppedItems{ get => droppedItems; }
    public int deathTimer;


    //* --------------- References ---------------- */
    //! Later on this should move into PlayerHealthUIManager
    [Header("References")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D collisionHitbox;
    [SerializeField] private Transform objectTransform;


    public Transform ObjectTransform { get => objectTransform; }
    [SerializeField] private Transform centerPoint;
    public Transform CenterPoint{ get => centerPoint; }
    public Collider2D CollisionHitbox { get => collisionHitbox; }
    [HideInInspector]public PlayerController playerController;
    private bool invulnerable = false;

    //* --------------- Events ---------------- */

    public event Action OnDamageTaken;
    public event Action OnDeath;
    public event Action OnRevive;
    public event Action OnXpGain;


    public void DestroyThis(bool dropItems)
    {

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    [ClientRpc]
    public void DisplayDamageFloaterClientRpc(float amount)
    {
        GameObject damageFloater;
        if (CenterPoint != null)
        {
            damageFloater = Instantiate(GameAssets.Instance.damageFloater, CenterPoint.position, Quaternion.identity);
        }
        else
        {
            damageFloater = Instantiate(GameAssets.Instance.damageFloater, transform.position, Quaternion.identity);
        }
        damageFloater.GetComponent<DamageFloater>().Initialize(amount);
    }

    public void DropItems()
    {
        foreach (DroppedItem item in DroppedItems)
        {
            //If the item has something other than 100% drop chance
            if (item.chance < 100)
            {
                //roll the dice to see if we should spawn this item
                float rolledChance = UnityEngine.Random.Range(0f, 100f);
                if (rolledChance <= item.chance)
                {
                    LootManager.Instance.SpawnLootServerRpc(transform.position, item.Id, UnityEngine.Random.Range(item.minAmount, item.maxAmount + 1));
                }
            }
            else
            {
                LootManager.Instance.SpawnLootServerRpc(transform.position, item.Id, UnityEngine.Random.Range(item.minAmount, item.maxAmount + 1));
            }


        }
    }

    public void OnTakeDamage(float amount, Vector2 sourceDirection)
    {
        DisplayDamageFloaterClientRpc(amount);
        OnDamageColorChangeClientRpc();
        StartCoroutine(DoInvulnerableFrame());
    }

    [ServerRpc(RequireOwnership = false)]
    public void RestoreHealthServerRpc(float amount)
    {
        CurrentHealth.Value += amount;
        if (CurrentHealth.Value > MaxHealth.Value)
        {
            CurrentHealth.Value = MaxHealth.Value;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(DamageInfo info)
    {
        if (!IsServer)
        {
            return;
        }
        if (!invulnerable)
        {
            ApplyDamage(info);
        }

    }

    public void ApplyDamage(DamageInfo info)
    {
        CurrentHealth.Value -= info.amount;
        OnTakeDamage(info.amount, info.sourceDirection);
        if (CurrentHealth.Value <= 0)
        {
            //not sure how to handle player death yet, current thought is similar to terraria?
            //drop some portion of items or lose xp? or maybe nothing
            //wait x amount of time, respawn at core
            PlayerDeath();
        }
    }

    private void PlayerDeath()
    {
        StopAllCoroutines();
        StartCoroutine(DoPlayerDeath());
    }

    private IEnumerator DoPlayerDeath()
    {
        OnDeath?.Invoke();
        playerController.movementLocked = true;
        invulnerable = true;
        sr.color = new Color(1, 1, 1, 0);
        yield return new WaitForSeconds(deathTimer);
        if (FlowFieldManager.Instance.coreTransform == null)
        {
            playerController.SetPositionCenterWorld();
        }
        else
        {
            transform.position = FlowFieldManager.Instance.coreTransform.position;
        }
        playerController.movementLocked = false;
        sr.color = new Color(1, 1, 1, 1);
        CurrentHealth.Value = MaxHealth.Value;
        invulnerable = false;
        OnRevive?.Invoke();
    }

    private IEnumerator DoInvulnerableFrame(float time=1f)
    {
        invulnerable = true;
        yield return new WaitForSeconds(time);
        invulnerable = false;
    }

    [ClientRpc]
    public void OnDamageColorChangeClientRpc()
    {
        StartCoroutine(FlashDamage(.15f));
    }

    private IEnumerator FlashDamage(float time)
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(time);
        sr.color = Color.white;
    }


    public void OnLevelUp()
    {

    }

    public void AddXp(int amount)
    {

        CurrentXp += amount;
        CurrentTotalXp += amount;

        CheckLevelUp();
        OnXpGain?.Invoke();
        //maybe in the future this can be a coroutine that does it slowly for cool effect
    }

    public void CheckLevelUp()
    {
        if (CurrentXp > MaximumLevelXp)
        {
            int newXp = CurrentXp - MaximumLevelXp;
            CurrentXp = 0;
            LevelUp();
            AddXp(newXp);
        }
    }

    public void LevelUp()
    {
        Level++;
        MaximumLevelXp = Mathf.RoundToInt(MaximumLevelXp * 1.2f);
        //need some way for this to interact with stats.. OnLevelUp()? then it's up to the base classes to figure out what they wanna do
        OnLevelUp();
    }

    public void SetLevel(int level)
    {
        Level = level;
    }
    
    


}