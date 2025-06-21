using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject gameOverScreen; 
    private Transform coreTransform;
    private void Start()
    {
        FlowFieldManager.Instance.corePlaced += HandleCorePlaced;
    }

    private void HandleCorePlaced(Transform core)
    {
        Debug.Log("GameOver hears Core Placed!");
        coreTransform = core;
        FlowFieldManager.Instance.corePlaced -= HandleCorePlaced;
        Core.CORE.OnCoreDestroyed += HandleCoreDestroyed;
    }



    private void HandleCoreDestroyed()
    {
        Debug.Log("GameOver hears Core Destroyed!");
        //would be really sick to lerp over to the core's position.
        gameOverScreen.SetActive(true);
    }
    
    private void OnDestroy()
    {
        if(Core.CORE)
            Core.CORE.OnCoreDestroyed -= HandleCoreDestroyed;
    }
}
