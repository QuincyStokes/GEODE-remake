using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Unity.VisualScripting;

public class BossHealthbar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image background;
    [SerializeField] private Image sliderBackground;
    [Header("Fade Out Settings")]
    [SerializeField] private float fadeTime;

    // INTERNAL
    private BaseEnemy boss;

    public void Initialize(BaseEnemy boss)
    {
        this.boss = boss;
        healthBar.maxValue = boss.MaxHealth.Value;
        healthBar.minValue = 0f;

        healthBar.value = boss.CurrentHealth.Value;

        nameText.text = boss.EnemyName;

        healthText.text = $"{boss.CurrentHealth.Value} / {boss.MaxHealth.Value}";

        boss.CurrentHealth.OnValueChanged += HandleBossHealthValueChanged;
    }

    private void HandleBossHealthValueChanged(float previousValue, float newValue)
    {
        int health = Mathf.Max((int)newValue, 0);
        healthBar.value = health;
        healthText.text = $"{health} / {(int)boss.MaxHealth.Value}";
        if(newValue <= 0)
        {
            StartCoroutine(BossHealthbarExit());
        }
    }

    private IEnumerator BossHealthbarExit()
    {
        float elapsed = 0f;
        Color sliderbg = sliderBackground.color;
        while (elapsed <= fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = 1 - (elapsed / fadeTime);

            nameText.color = new Color(1, 1, 1, t);
            healthText.color = new Color(1, 1, 1, t);
            background.color = new Color(1, 1, 1, t);
            sliderBackground.color = new Color(sliderbg.r, sliderbg.g, sliderbg.b, t);

            yield return null;
        }

        nameText.color = new Color(1, 1, 1, 0);
        healthText.color = new Color(1, 1, 1, 0);
        background.color = new Color(1, 1, 1, 0);
        sliderBackground.color = new Color(sliderbg.r, sliderbg.g, sliderbg.b, 0);
        
        Destroy(gameObject);
    }


    private void OnDestroy()
    {
        if(boss != null)
            boss.CurrentHealth.OnValueChanged += HandleBossHealthValueChanged;
    }
}
