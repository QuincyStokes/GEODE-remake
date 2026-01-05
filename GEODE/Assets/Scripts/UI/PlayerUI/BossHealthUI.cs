using System;
using UnityEngine;

public class BossHealthUI : MonoBehaviour
{
    [SerializeField] private BossHealthbar bossHealthbarPrefab;
    [SerializeField] private Transform healthBarParent;


    private void Awake()
    {
        if(EnemySpawningManager.Instance != null)
        {
            EnemySpawningManager.Instance.OnBossSpawned += HandleBossSpawned;
        }
    }

    private void HandleBossSpawned(BaseEnemy boss)
    {
        BossHealthbar hpBar = Instantiate(bossHealthbarPrefab, healthBarParent);

        hpBar.Initialize(boss);
        //initalize a new health bar with all the correct info
        //somehow link the boss that just spawned to THAT health bar to handle incase theres multiple bosses 
    }

    private void OnDestroy()
    {
        if(EnemySpawningManager.Instance != null)
        {
            EnemySpawningManager.Instance.OnBossSpawned -= HandleBossSpawned;
        }
    }
}
