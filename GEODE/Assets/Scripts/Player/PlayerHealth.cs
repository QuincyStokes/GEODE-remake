using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using System.Collections;


public class PlayerHealthAndXP : MonoBehaviour, IDamageable, IExperienceGain
{
    [Header("Health")]
    [SerializeField] private float maxHealth;
    [SerializeField] private float currentHealth;

    [Header("XP")]
    [SerializeField] private int maxLevelXp;
    [SerializeField] private int currentLevelXp;
    [SerializeField] private int totalXp;
    [SerializeField] private int level;

    [Header("Settings")]
    [SerializeField] private List<DroppedItem> droppedItems;

    [Header("References")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Transform objectTransform;
    [SerializeField] private string objectName;

    [SerializeField] private Slider healthbarSlider;
    [SerializeField] private TMP_Text healthbarText;

    [SerializeField] private Slider xpbarSlider;
    [SerializeField] private TMP_Text xpbarText;
    [SerializeField] private TMP_Text levelText;


    public float MaxHealth
    {
        get => maxHealth;
        set => maxHealth = value;
    }

    public float CurrentHealth
    {
        get => currentHealth;
        set => currentHealth = value;
    }

    public Transform ObjectTransform
    {
        get => objectTransform;
    }

    public string ObjectName
    {
        get => objectName;
        set => objectName = value;
    }

    [SerializeField] private Transform centerPoint;
    public Transform CenterPoint
    {
        get => centerPoint;
    }
    public List<DroppedItem> DroppedItems
    {
        get => droppedItems;
    }
    public int MaximumLevelXp { get => maxLevelXp; set => maxLevelXp = value; }
    public int CurrentXp { get => currentLevelXp; set => currentLevelXp = value; }
    public int CurrentTotalXp { get => totalXp; set => totalXp = value; }
    public int Level { get => level; set => level = value; }

    public void DestroyThisServerRpc(bool dropItems)
    {
        //not sure if this'll ever be called, have to look into the player "dying", my guess is we don't actually want to destroy it
    }

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

    public void DropItemsServerRpc()
    {
        foreach (DroppedItem item in DroppedItems)
        {
            //If the item has something other than 100% drop chance
            if (item.chance < 100)
            {
                //roll the dice to see if we should spawn this item
                float rolledChance = Random.Range(0f, 100f);
                if (rolledChance <= item.chance)
                {
                    LootManager.Instance.SpawnLootServerRpc(transform.position, item.Id, Random.Range(item.minAmount, item.maxAmount + 1));
                }
            }
            else
            {
                LootManager.Instance.SpawnLootServerRpc(transform.position, item.Id, Random.Range(item.minAmount, item.maxAmount + 1));
            }


        }
    }

    public void OnTakeDamage(float amount, Vector2 sourceDirection)
    {
        DisplayDamageFloaterClientRpc(amount);
        OnDamageColorChangeClientRpc();
        UpdateHealthbar();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RestoreHealthServerRpc(float amount)
    {
        CurrentHealth += amount;
        if (CurrentHealth > MaxHealth)
        {
            CurrentHealth = MaxHealth;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float amount, Vector2 sourceDirection, bool dropItems = false)
    {
        CurrentHealth -= amount;
        OnTakeDamage(amount, sourceDirection);
        if (CurrentHealth <= 0)
        {
            //not sure how to handle player death yet, current thought is similar to terraria?
            //drop some portion of items or lose xp? or maybe nothing
            //wait x amount of time, respawn at core
        }
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

    public void UpdateHealthbar()
    {
        healthbarSlider.maxValue = MaxHealth;
        healthbarSlider.value = CurrentHealth;

        healthbarText.text = $"{CurrentHealth}/{MaxHealth}";
    }

    public void OnXpGain()
    {
        UpdateXpbar();
    }

    public void UpdateXpbar()
    {
        xpbarSlider.maxValue = MaximumLevelXp;
        xpbarSlider.value = CurrentXp;

        xpbarText.text = $"{CurrentXp}/{MaximumLevelXp}";

        levelText.text = Level.ToString();
    }

    public void OnLevelUp()
    {
        
    }
    
    public void AddXp(int amount)
    {
        
        CurrentXp += amount;
        
        CheckLevelUp();
        OnXpGain();
        //maybe in the future this can be a coroutine that does it slowly for cool effect
    }

    public void CheckLevelUp()
    {
        if(CurrentXp > MaximumLevelXp)
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