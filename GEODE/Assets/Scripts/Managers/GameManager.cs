using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance; 
    public int seed;
    [SerializeField] float noiseScale = 5f;
    [SerializeField] Vector2 offset = new Vector2(10, 10);

    private AudioListener audioListener;
    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        audioListener = Camera.main.gameObject.GetComponent<AudioListener>();
        audioListener.enabled = false;
        seed = UnityEngine.Random.Range(0, 1000000);
        
        
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
        
        //Once this scene is fully loaded, set it to be the active scene
            //This is so newly created gameobjects belong to Game, not Loading
        Scene gameplay = SceneManager.GetSceneByName("Game");
        SceneManager.SetActiveScene(gameplay);
    }


    public IEnumerator GenerateWorld()
    {
        Debug.Log("Generating world from GameManager!");

        yield return StartCoroutine(WorldGenManager.Instance.InitializeWorldGen(seed, noiseScale, offset));
        EnemySpawningManager.Instance.activated = true;

        audioListener.enabled = true;
        ConnectionManager.Instance.OnWorldReady();
    }

}
