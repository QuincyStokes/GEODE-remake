using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class EnemySpawningManager : NetworkBehaviour
{
    #region Settings
    public static EnemySpawningManager Instance;
    [Header("Enemies")]

    //*List of EnemySpawnWeight. { enemyId, weight }
    [SerializeField] private List<EnemySpawnWeight> forestEnemies;
    [SerializeField] private List<EnemySpawnWeight> desertEnemies;


    [Header("Bosses")]
    [SerializeField] private List<BossSpawnPool> bossSpawns;
    [SerializeField] private int bossSpawnWeight;

    [Header("Settings")]
    [SerializeField] private int minSpawnDistanceFromPlayer;
    [SerializeField] private int maxSpawnDistanceFromPlayer;
    [SerializeField] private int dayMaxSpawns; //maximum number of enemies during the day
    [SerializeField] private int nightMaxSpawns; //maximum number of enemies during the night
    [Tooltip("(1 / SpawnRate), higher number = lower chance")] [SerializeField] private float daySpawnRate; //chance each frame to spawn an enemy during the day
    [Tooltip("(1 / SpawnRate), higher number = lower chance")][SerializeField] private float nightSpawnRate; //chance each frame to spawn an enemy during the night


    //! PUBLIC
    //there we should be able to modify spawn chances/conditions based on upgrades/settings/difficulties
    public float dayMaxSpawnsModifier = 1f; //default is one, we will multiply this by our maxSpawns to modify
    public float nightMaxSpawnsModifier = 1f;
    public float daySpawnRateModifier = 1f; //default is one, we will multiply this by our maxSpawns to modify
    public float nightSpawnRateModifier = 1f;
    public bool activated = false;

    //! INTERNAL
    private int currentMaxSpawns;
    private int currentNumSpawns;
    private float currentSpawnRate;
    private List<EnemySpawnWeight> enemies;
    private float halfHeight;
    private float halfWidth;
    private Difficulty difficulty;
    private Dictionary<int, List<int>> bossSpawnMap;
    private Dictionary<DamageType, BiomeType> crystalBiomeMap;
    private BaseEnemy currentSpawnedBoss;
    private int lastBossRound;

    //* --------- Events ----------- */
    /// <summary>
    /// Event fires when a boss spawns, int passed is the Id of the boss
    /// </summary>
    public event Action<BaseEnemy> OnBossSpawned;
    public event Action OnBossDefeated;

    #endregion

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }   
        else
        {
            //Destroy(gameObject);
        }

    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
        }
    }

    private void Start()
    {
        //Load in difficulty
        halfHeight = Camera.main.orthographicSize;
        halfWidth = halfHeight * Camera.main.aspect;
        DayCycleManager.Instance.becameDay += ChangeToDaySettings;
        DayCycleManager.Instance.becameNight += ChangeToNightSettings;

        ChangeToDaySettings();
        SeedBossMap();
        SeedBiomeCrystalMap();
    }

    private void OnDisable()
    {
        DayCycleManager.Instance.becameDay -= ChangeToDaySettings;
        DayCycleManager.Instance.becameNight -= ChangeToNightSettings;
    }

    private void FixedUpdate()
    {
        if(activated && NetworkManager.Singleton && NetworkManager.Singleton.ConnectedClientsList.Count > 0)
        {
            DoEnemySpawning();  
        }
       
    }

    private void DoEnemySpawning()
    {
        if(currentNumSpawns < currentMaxSpawns)
        {
            float spawnRoll = UnityEngine.Random.Range(1,(int)currentSpawnRate);
            if(spawnRoll == 1)
            {
                //pick a random 360 direction from the player chosen, and then a random distance
               
                Vector3Int spawnPos = GetRandomEnemySpawnPos();
            
                //! Get biome at chosen position
                BiomeType biomeType = WorldGenManager.Instance.GetBiomeAtPosition(spawnPos);
                switch(biomeType)
                {
                    case BiomeType.Desert:
                        enemies = desertEnemies;
                        break;
                    case BiomeType.Forest:
                        enemies = forestEnemies;
                        break;
                    case BiomeType.None:
                        enemies = null;
                        break;
                }

                if(enemies != null)
                {
                    int enemyToSpawnId = PickEnemyByWeight(enemies);
                    SpawnEnemyServerRpc(enemyToSpawnId, spawnPos);
                    
                }
                else
                {
                    Debug.Log("Attempted to spawn enemy on null tile");
                }
            
            }
            
        }
        
    }

    /// <summary>
    /// Picks a random player and returns their position as a Vector3Int
    /// </summary>
    /// <returns></returns>
    


    private int PickEnemyByWeight(List<EnemySpawnWeight> enemies)
    {
        int totalWeights = 0;
        //add up all of the weights
        foreach(EnemySpawnWeight enemy in enemies)
        {
            totalWeights += enemy.weight;
        }
        
        //now generate a random number from 1 - count, and go through the list til we find a winner
        int chosenWeight = UnityEngine.Random.Range(1, totalWeights+1);
        int counter = 0;
        foreach(EnemySpawnWeight enemy in enemies)
        {
            counter += enemy.weight;
            if(counter >= chosenWeight)
            {
                //we found our guy. 
                return enemy.enemyId;
            }
        }
        //we shouldnt technically be able to get here, but just incase we do return null
        return -1;
        
    }

    [ServerRpc (RequireOwnership = false)]
    public void SpawnEnemyServerRpc(int enemyId, Vector3Int pos)
    {
        if(EnemyDatabase.Instance == null)
        {
            return;
        }
        
        // Double-check we're still under the limit (race condition protection)
        if (currentNumSpawns >= currentMaxSpawns)
        {
            return;
        }
        
        GameObject enemyToSpawn = EnemyDatabase.Instance.GetEnemy(enemyId);
        if (enemyToSpawn != null)
        {
            Debug.Log($"Spawning a {enemyToSpawn.name} at {pos}.");
            GameObject spawnedEnemy = Instantiate(enemyToSpawn, pos, Quaternion.identity);
            spawnedEnemy.GetComponent<NetworkObject>().Spawn();
            currentNumSpawns++;
            BaseEnemy enemy = spawnedEnemy.GetComponent<BaseEnemy>();

            // Track enemy through death event - this is our sole tracking mechanism
            enemy.OnDeath += HandleEnemyDied;
            //! HERE can maybe pass a modifier depending on amount of players in the world.
            enemy.InitializeBaseStats(difficulty);
            enemy.AddLevels(DayCycleManager.Instance.DayNum);
        }
        else
        {
            Debug.Log($"Error spawning enemy {enemyId}. Check EnemyDatabase and enemy IDs");
        }
       
    }

    [ServerRpc (RequireOwnership = false)]
    public void SpawnBossServerRpc(int enemyId, Vector3Int pos)
    {
        if(EnemyDatabase.Instance == null)
        {
            return;
        }
        
        GameObject enemyToSpawn = EnemyDatabase.Instance.GetEnemy(enemyId);
        if (enemyToSpawn != null)
        {
            Debug.Log($"Spawning a {enemyToSpawn.name} at {pos}.");
            GameObject spawnedEnemy = Instantiate(enemyToSpawn, pos, Quaternion.identity);
            spawnedEnemy.GetComponent<NetworkObject>().Spawn();
            
            BaseEnemy enemy = spawnedEnemy.GetComponent<BaseEnemy>();
            enemy.isBoss = true;

            // Track enemy through death event - this is our sole tracking mechanism
            enemy.OnDeath += HandleEnemyDied;
            enemy.OnDeath += HandleBossDied;
            currentSpawnedBoss = enemy;
            //! HERE can maybe pass a modifier depending on amount of players in the world.
            enemy.InitializeBaseStats(difficulty);
            enemy.AddLevels(DayCycleManager.Instance.DayNum);

            OnBossSpawned?.Invoke(enemy);
        }
        else
        {
            Debug.Log($"Error spawning enemy {enemyId}. Check EnemyDatabase and enemy IDs");
        }
       
    }

    private void HandleBossDied(IDamageable damageable)
    {
        // add the boss to the spawn pool, 
        OnBossDefeated?.Invoke();
        //with the bosses id, we need to access all of the other kinds of bosses. W
        if(bossSpawnMap.ContainsKey(lastBossRound))
        {
            for(int i = 0; i < bossSpawnMap[lastBossRound].Count; i++)
            {
                EnemySpawnWeight esw = new();
                esw.weight = bossSpawnWeight;
                esw.enemyId = bossSpawnMap[lastBossRound][i];
                GameObject go = EnemyDatabase.Instance.GetEnemy(esw.enemyId);
                BaseEnemy be = go.GetComponent<BaseEnemy>();
                switch(be.enemyCrystalType)
                {
                    //! HTHHESE WILL NEED TO CHANGE WHEN NEW BIOMES ARE ADDED
                    case(DamageType.Renthite):
                        desertEnemies.Add(esw);
                        break;
                    case(DamageType.Gelthite):
                        forestEnemies.Add(esw);
                        break;
                    case(DamageType.Bizite):
                        forestEnemies.Add(esw);
                        break;
                    case(DamageType.Yeedrite):
                        desertEnemies.Add(esw);
                        break;
                    default:
                        break; //this means if we have a "non-typed" boss, it won't spawn after the boss night. sure.   
                }
            }
        }

        currentSpawnedBoss = null;

    }

    private void ChangeToDaySettings()
    {
        currentMaxSpawns = (int)(dayMaxSpawns * dayMaxSpawnsModifier);
        currentSpawnRate = daySpawnRate * daySpawnRateModifier;
    }

    private void ChangeToNightSettings()
    {
        //Here can we spawn a boss if it's a certain night, since this gets fired once when it turns nighttime? Sounds kinda perfect. 

       
        
            //get this night's boss enemyId and spawn it with some special conditions (near the crystal basically)
            //We should find a way to do this "procedurally". So basically nights can keep going, alternating between boss types or something like that
            // Their stats should just scale up, the hardest part will be getting the numbers right though.

            //how do boss?
            //assign have a dict? <nightnum, Boss>? Then how do the procedural part.. 
            //I like the idea of having maybe the first 5-10 set, but after that it'll be a random boss. Their stats will be "balanced",but
                //the type might mess with some people. I like it. 

        if(bossSpawnMap.ContainsKey(DayCycleManager.Instance.DayNum))
        {
            List<int> bossSpawnList = bossSpawnMap[DayCycleManager.Instance.DayNum];
            SpawnBoss(bossSpawnList[UnityEngine.Random.Range(0, bossSpawnList.Count)]);
            lastBossRound = DayCycleManager.Instance.DayNum;
        }

        nightSpawnRateModifier *= .9f;
        currentMaxSpawns = (int)(nightMaxSpawns * nightMaxSpawnsModifier);
        currentSpawnRate = Mathf.Clamp(nightSpawnRate * nightSpawnRateModifier, 25, 1000);
    }

    /// <summary>
    /// Handles enemy death - decrements spawn count and unsubscribes from events.
    /// This is our sole tracking mechanism for enemy lifecycle (spawn increments, death decrements).
    /// </summary>
    private void HandleEnemyDied(IDamageable damageable)
    {
        currentNumSpawns = Mathf.Max(0, currentNumSpawns - 1); // Prevent going negative
        damageable.OnDeath -= HandleEnemyDied;
    }

    public void SetDifficulty(Difficulty newDifficulty)
    {
        difficulty = newDifficulty;
    }

    /// <summary>
    /// Spawns boss of a given Id, essentilly just a wrapper for SpawnEnemy with an added event, and with some positioning logic.
    /// </summary>
    /// <param name="id">Id of the boss to spawn</param>
    private void SpawnBoss(int id)
    {           
        //Choose a location near the core and off the player's screen
        Vector3 corePos = GetCorePos();
        Vector3 spawnPos = (Vector2)corePos + GetSpawnOffset(minSpawnDistanceFromPlayer, maxSpawnDistanceFromPlayer);

        SpawnBossServerRpc(id, new Vector3Int((int)spawnPos.x, (int)spawnPos.y));
        
        
    }


    //* ------------------------- Helpers ----------------------- *//

    internal Vector3Int GetRandomPlayerPosition()
    {
        //Pick a random player connected to the game
        int randomPlayer = UnityEngine.Random.Range(0, NetworkManager.Singleton.ConnectedClientsList.Count);
        var client = NetworkManager.Singleton.ConnectedClientsList[randomPlayer];
        if(client.PlayerObject == null || client.PlayerObject.transform == null)
        {
            return Vector3Int.zero;
        }

        //Get that randmo player's position
        Vector3 randomPlayerPos = NetworkManager.Singleton.ConnectedClientsList[randomPlayer].PlayerObject.transform.position;
        Vector3Int randomPlayerPosInt = new Vector3Int((int)randomPlayerPos.x, (int)randomPlayerPos.y, 0);
        return randomPlayerPosInt;
    }

    internal Vector3Int GetRandomEnemySpawnPos()
    {
        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = UnityEngine.Random.Range(minSpawnDistanceFromPlayer, maxSpawnDistanceFromPlayer);
        Vector2 offset2D = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

        Vector3Int spawnPos = GetRandomPlayerPosition() + Vector3Int.RoundToInt((Vector3)offset2D);


        //we have the position of the enemy spawn now
        //check biome at that position
        //choose an enemy based on weights
        //but first, make sure it isnt within a certain range from any of the players
        //Notably with more players, spawn checks may fail more often, resulting in less spawns on high population games
        if(IsPointAwayFromPlayer(spawnPos, 10))
        {
            return spawnPos;
        }
        else
        {
            return Vector3Int.zero;
        }
    }

    internal Vector2 GetSpawnOffset(int minDist, int maxDist)
    {
        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float dist = UnityEngine.Random.Range(minDist, maxDist);
        Vector2 offset2D = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
        return offset2D;
        
    }

    internal Vector3 GetCorePos()
    {
        return Core.CORE.CenterPoint.position;
    }

    internal bool IsPointAwayFromPlayer(Vector3Int pos, float distance)
    {
        foreach (NetworkClient networkClient in NetworkManager.Singleton.ConnectedClientsList)
        {
            //if the distance to any player is under a ertain range
            if (Vector3.Distance(networkClient.PlayerObject.transform.position, pos) < distance)
            {
                return false;    //don't spawn an enemy here, its too close to a player.
            }
        }
        return true;
    }

    internal void SeedBossMap()
    {
        bossSpawnMap = new();
        foreach(BossSpawnPool bsp in bossSpawns)
        {
            bossSpawnMap.Add(bsp.nightNum, bsp.possibleEnemySpawnIds);
        }
    }
    internal void SeedBiomeCrystalMap()
    {
        crystalBiomeMap = new();
        crystalBiomeMap.Add(DamageType.Renthite, BiomeType.Desert); //! THIS IS TO CHANGE WHEN THE LAST TWO BIOMES ARE ADDED
        crystalBiomeMap.Add(DamageType.Gelthite, BiomeType.Forest);
        crystalBiomeMap.Add(DamageType.Bizite, BiomeType.Forest);  //! TO CHANGE AS WELL
        crystalBiomeMap.Add(DamageType.Yeedrite, BiomeType.Desert);
    }


    [System.Serializable]
    public struct EnemySpawnWeight{
        public int enemyId;
        public int weight;

    }

    [System.Serializable]
    public struct BossSpawnPool
    {
        public int nightNum; //this will be the key in our dict
        public List<int> possibleEnemySpawnIds;

    }


}
