using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance;
    public Slider progressBar;
    [SerializeField] private AudioListener audioListener;
    

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
        //NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += HandleSceneLoaded;
        audioListener.enabled = true;

        WorldGenManager.OnWorldGenerationProgressChanged += HandleWorldGenerationProgressChanged;
    }

    private void HandleWorldGenerationProgressChanged(float value)
    {
        progressBar.value = value;
    }
}
