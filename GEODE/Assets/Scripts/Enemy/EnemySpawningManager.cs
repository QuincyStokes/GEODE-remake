using System.Collections.Generic;
using NUnit.Framework.Api;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

public class EnemySpawningManager : MonoBehaviour
{
    #region Settings
    public static EnemySpawningManager Instance;
    [Header("Enemies")]

    //*dictionary where gameobject is the enemy prefab, int is the weight of it spawning.
    [SerializeField] private List<EnemySpawnWeight> forestEnemies;
    [SerializeField] private List<EnemySpawnWeight> desertEnemies;

    [Header("Settings")]
    [SerializeField] private int spawnPosWidthOffset;
    [SerializeField] private int spawnPosHeightOffset;
    [SerializeField] private int dayMaxSpawns; //maximum number of enemies during the day
    [SerializeField] private int nightMaxSpawns; //maximum number of enemies during the night
    [SerializeField] private float daySpawnRate; //chance each frame to spawn an enemy during the day
    [SerializeField] private float nightSpawnRate; //chance each frame to spawn an enemy during the night




    //! PUBLIC
    //there we should be able to modify spawn chances/conditions based on upgrades/settings/difficulties
    public float dayMaxSpawnsModifier = 1f; //default is one, we will multiply this by our maxSpawns to modify
    public float nightMaxSpawnsModifier = 1f;
    public float daySpawnRateModifier = 1f; //default is one, we will multiply this by our mxSpawns to modify
    public float nightSpawnRateModifier = 1f;


    //! INTERNAL
    private int currentMaxSpawns;
    private int currentNumSpawns;
    private float currentSpawnRate;
    private List<EnemySpawnWeight> enemies;
    private float halfHeight;
    private float halfWidth; 

    #endregion

    private void OnNetworkSpawned()
    {
        
    }
    private void OnEnable()
    {
    
    }

    private void Start()
    {

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

    private void Update()
    {
        DoEnemySpawningServerRpc();
    }

    [ServerRpc]
    private void DoEnemySpawningServerRpc()
    {
        if(currentNumSpawns < currentMaxSpawns)
        {
            float spawnRoll = Random.Range(1,(int)currentSpawnRate);
            if(spawnRoll == 1)
            {
                //pick a direction
                int directionRoll = Random.Range(1, 5);
                Vector3Int spawnPos;
                int xPos;
                int yPos;
                //! Generate direction to spawn enemy, random between up/left/right/down
                switch(directionRoll){
                    case 1: //! up
            
                        xPos = Random.Range(
                            (int)(PlayerController.Instance.transform.position.x - halfWidth - spawnPosWidthOffset), //left x boundary
                            (int)(PlayerController.Instance.transform.position.x + halfWidth + spawnPosWidthOffset) //right x boundary
                        ); 
                        yPos = Random.Range(
                            (int)(PlayerController.Instance.transform.position.y + halfHeight), //lower y boundary
                            (int)(PlayerController.Instance.transform.position.y + halfHeight + spawnPosHeightOffset) //upper y boundary
                        ); //right x boundary

                        spawnPos = new Vector3Int(xPos, yPos, 0);
                        break;
                    case 2: //! down
                        xPos = Random.Range(
                            (int)(PlayerController.Instance.transform.position.x - halfWidth - spawnPosWidthOffset), //left x boundary
                            (int)(PlayerController.Instance.transform.position.x + halfWidth + spawnPosWidthOffset) //right x boundary
                        ); 
                        yPos = Random.Range(
                            (int)(PlayerController.Instance.transform.position.y - halfHeight - spawnPosHeightOffset), //lower y boundary
                            (int)(PlayerController.Instance.transform.position.y - halfHeight) //upper y boundary
                        ); //right x boundary

                        spawnPos = new Vector3Int(xPos, yPos, 0);
                        break;
                    case 3: //! left
                        xPos = Random.Range(
                            (int)(PlayerController.Instance.transform.position.x - halfWidth - spawnPosWidthOffset), //left x boundary
                            (int)(PlayerController.Instance.transform.position.x - halfWidth) //right x boundary
                        ); 
                        yPos = Random.Range(
                            (int)(PlayerController.Instance.transform.position.y - halfHeight - spawnPosHeightOffset), //lower y boundary
                            (int)(PlayerController.Instance.transform.position.y + halfHeight + spawnPosHeightOffset) //upper y boundary
                        ); //right x boundary

                        spawnPos = new Vector3Int(xPos, yPos, 0);
                        break;
                     default: //! right
                        xPos = Random.Range(
                            (int)(PlayerController.Instance.transform.position.x + halfWidth), //left x boundary
                            (int)(PlayerController.Instance.transform.position.x + halfWidth + spawnPosWidthOffset) //right x boundary
                        ); 
                        yPos = Random.Range(
                            (int)(PlayerController.Instance.transform.position.y - halfHeight - spawnPosHeightOffset), //lower y boundary
                            (int)(PlayerController.Instance.transform.position.y + halfHeight + spawnPosHeightOffset) //upper y boundary
                        ); //right x boundary
                        
                        spawnPos = new Vector3Int(xPos, yPos, 0);
                        break;
                }
                //we have the position of the enemy spawn now
                    //check biome at that position
                    //choose an enemy based on weights
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
                GameObject enemyToSpawn = PickEnemyByWeight(enemies);
                SpawnEnemyServerRpc(enemyToSpawn, spawnPos);
                Debug.Log($"Spawning an enemy at {spawnPos} in the {biomeType}");
            }
            else
            {
                Debug.Log("Attempted to spawn enemy on null tile");
            }
            
            }
            
        }
        
    }

    private GameObject PickEnemyByWeight(List<EnemySpawnWeight> enemies)
    {
        int totalWeights = 0;
        //add up all of the weights
        foreach(EnemySpawnWeight enemy in enemies)
        {
            totalWeights += enemy.weight;
        }
        
        //now generate a random number from 1 - count, and go through the list til we find a winner
        int chosenWeight = Random.Range(1, totalWeights+1);
        int counter= 0;
        foreach(EnemySpawnWeight enemy in enemies)
        {
            counter += enemy.weight;
            if(counter >= chosenWeight)
            {
                //we found our guy. 
                return enemy.enemyPrefab;
            }
        }
        //we shouldnt technically be able to get here, but just incase we do return null
        return null;
        
    }

    [ServerRpc]
    public void SpawnEnemyServerRpc(GameObject enemy, Vector3Int pos)
    {
        GameObject spawnedEnemys = Instantiate(enemy, pos, Quaternion.identity);
        spawnedEnemys.GetComponent<NetworkObject>().Spawn();
    }

    private void ChangeToDaySettings()
    {
        currentMaxSpawns = (int)(dayMaxSpawns * dayMaxSpawnsModifier);
        currentSpawnRate = daySpawnRate * dayMaxSpawnsModifier;
    }

    private void ChangeToNightSettings()
    {
        currentMaxSpawns = (int)(nightMaxSpawns * nightMaxSpawnsModifier);
        currentSpawnRate = nightSpawnRate * nightSpawnRateModifier;
    }


    [System.Serializable]
    public struct EnemySpawnWeight{
        public GameObject enemyPrefab;
        public int weight;

    }


}
