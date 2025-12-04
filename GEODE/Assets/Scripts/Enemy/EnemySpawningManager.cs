using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawningManager : NetworkBehaviour
{
    #region Settings
    public static EnemySpawningManager Instance;
    [Header("Enemies")]

    //*List of EnemySpawnWeight. { enemyId, weight }
    [SerializeField] private List<EnemySpawnWeight> forestEnemies;
    [SerializeField] private List<EnemySpawnWeight> desertEnemies;

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
            float spawnRoll = Random.Range(1,(int)currentSpawnRate);
            if(spawnRoll == 1)
            {
                
                Vector3Int spawnPos;
                //Pick a random player connected to the game
                int randomPlayer = Random.Range(0, NetworkManager.Singleton.ConnectedClientsList.Count);
                var client = NetworkManager.Singleton.ConnectedClientsList[randomPlayer];
                if(client.PlayerObject == null || client.PlayerObject.transform == null)
                {
                    return;
                }

                //Get that randmo player's position
                Vector3 randomPlayerPos = NetworkManager.Singleton.ConnectedClientsList[randomPlayer].PlayerObject.transform.position;
                Vector3Int randomPlayerPosInt = new Vector3Int((int)randomPlayerPos.x, (int)randomPlayerPos.y, 0);

                //pick a random 360 direction from the player chosen, and then a random distance
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float dist = Random.Range(minSpawnDistanceFromPlayer, maxSpawnDistanceFromPlayer);
                Vector2 offset2D = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;
                spawnPos = randomPlayerPosInt + Vector3Int.RoundToInt((Vector3)offset2D);


                //we have the position of the enemy spawn now
                //check biome at that position
                //choose an enemy based on weights
                //but first, make sure it isnt within a certain range from any of the players
                //Notably with more players, spawn checks may fail more often, resulting in less spawns on high population games
                foreach (NetworkClient networkClient in NetworkManager.Singleton.ConnectedClientsList)
                {
                    //if the distance to any player is under a ertain range
                    if (Vector3.Distance(networkClient.PlayerObject.transform.position, spawnPos) < 10)
                    {
                        return; //don't spawn an enemy here, its too close to a player.
                    }
                }
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

    private int PickEnemyByWeight(List<EnemySpawnWeight> enemies)
    {
        int totalWeights = 0;
        //add up all of the weights
        foreach(EnemySpawnWeight enemy in enemies)
        {
            totalWeights += enemy.weight;
        }
        
        //now generate a random number from 1 - count, and go through the list til we find a winner
        int chosenWeight = Random.Range(1, totalWeights+1);
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
            GameObject spawnedEnemy = Instantiate(EnemyDatabase.Instance.GetEnemy(enemyId), pos, Quaternion.identity);
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

    private void ChangeToDaySettings()
    {
        currentMaxSpawns = (int)(dayMaxSpawns * dayMaxSpawnsModifier);
        currentSpawnRate = daySpawnRate * daySpawnRateModifier;
    }

    private void ChangeToNightSettings()
    {
        //Here can we spawn a boss if it's a certain night, since this gets fired once when it turns nighttime? Sounds kinda perfect. 

        if (DayCycleManager.Instance.DayNum % 5 == 0)
        {
            //get this night's boss enemyId and spawn it with some special conditions (near the crystal basically)
            //We should find a way to do this "procedurally". So basically nights can keep going, alternating between boss types or something like that
            // Their stats should just scale up, the hardest part will be getting the numbers right though.

            //how do boss?
            //assign have a dict? <nightnum, Boss>? Then how do the procedural part.. 
            //I like the idea of having maybe the first 5-10 set, but after that it'll be a random boss. Their stats will be "balanced",but
                //the type might mess with some people. I like it. 
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

    [System.Serializable]
    public struct EnemySpawnWeight{
        public int enemyId;
        public int weight;

    }


}
