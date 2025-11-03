using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance; 
    public WorldGenParams worldParams;

    private AudioListener audioListener;

    public static bool IsReady = false;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        audioListener = Camera.main.gameObject.GetComponent<AudioListener>();
        audioListener.enabled = false;

        
    }

    private void Start()
    {
        Debug.Log("Checking whether GAMEMANAGER is server");  
        if(!IsServer)
        {
            Debug.Log("NOT THE SERVER. Disabling GameManager");
            enabled = false;
            return;
        }
        StartCoroutine(GenerateWorld());
       
        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            Debug.Log("Initializing WorldGenParams.");
            worldParams = new WorldGenParams
            {
                seed = UnityEngine.Random.Range(0, 1000000),
                noiseScale = 50,
                offset = new Vector2(UnityEngine.Random.Range(0, 10000), UnityEngine.Random.Range(1,10000))
            };
        }
        

        //Once this scene is fully loaded, set it to be the active scene
            //This is so newly created gameobjects belong to Game, not Loading
        Scene gameplay = SceneManager.GetSceneByName("Game");
        SceneManager.SetActiveScene(gameplay);
        IsReady = true;

    }


    public IEnumerator GenerateWorld()
    {
        Debug.Log("Generating world from GameManager!");

        yield return StartCoroutine(WorldGenManager.Instance.InitializeWorldGen(worldParams.seed, worldParams.noiseScale, worldParams.offset));
        EnemySpawningManager.Instance.activated = true;

        audioListener.enabled = true;
        ConnectionManager.Instance.OnWorldReady();
    }

    public WorldGenParams GetWorldGenParams()
    {
        return worldParams;
    }

    [System.Serializable]
    public struct WorldGenParams
    {
        public int seed;
        public float noiseScale;
        public Vector2 offset;
    }

}
