using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private PlayerHealthAndXP playerHp;
    
    [SerializeField] private Slider xpbarSlider;
    [SerializeField] private TMP_Text xpbarText;
    [SerializeField] private TMP_Text levelText;
    
    [SerializeField] private Slider healthbarSlider;
    [SerializeField] private TMP_Text healthbarText;

    private void Start()
    {
        if (!playerHp.IsOwner)
        {
            enabled = false;
            gameObject.SetActive(false);
            return;
        }   
        playerHp.OnDeath += DeathScreen;
        playerHp.OnRevive += EndDeathScreen;
        playerHp.CurrentHealth.OnValueChanged += UpdateHealthbar;
        playerHp.OnXpGain += UpdateXpbar;

        UpdateHealthbar(0, 0);
        UpdateXpbar();
    }

    private void OnDestroy()
    {
        playerHp.OnDeath -= DeathScreen;
        playerHp.OnRevive -= EndDeathScreen;
        playerHp.CurrentHealth.OnValueChanged -= UpdateHealthbar;
        playerHp.OnXpGain -= UpdateXpbar;

    }

    private void DeathScreen(IDamageable damageable)
    {
        StartCoroutine(DeathScreenCountdown());
    }

    private void EndDeathScreen()
    {

    }
    
    public void UpdateHealthbar(float old, float current)
    {
        healthbarSlider.maxValue = playerHp.MaxHealth.Value;
        healthbarSlider.value = playerHp.CurrentHealth.Value;

        healthbarText.text = $"{playerHp.CurrentHealth.Value}/{playerHp.MaxHealth.Value}";
    }

    public void UpdateXpbar()
    {
        xpbarSlider.maxValue = playerHp.MaximumLevelXp;
        xpbarSlider.value = playerHp.CurrentXp;

        xpbarText.text = $"{playerHp.CurrentXp}/{playerHp.MaximumLevelXp}";

        levelText.text = playerHp.Level.ToString();
    }

    private IEnumerator DeathScreenCountdown()
    {
        //make the text appear, count down the text every second
        //at the end disable
        float elapsed = playerHp.deathTimer;
        countdownText.gameObject.SetActive(true);
        while (elapsed >= 0)
        {
            elapsed -= Time.deltaTime;
            countdownText.text = $"Respawning in {Mathf.RoundToInt(elapsed)}..";
            yield return null;
        }
        countdownText.gameObject.SetActive(false);
    }
}
