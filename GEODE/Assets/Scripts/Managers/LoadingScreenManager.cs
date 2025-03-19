using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class LoadingScreenManager : MonoBehaviour
{
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private Slider progressBar;

    private void Awake()
    {
        audioListener = Camera.main.gameObject.GetComponent<AudioListener>();
        //NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += HandleSceneLoaded;
        audioListener.enabled = true;
       
    }

    private void HandleSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if(sceneName == "GameplayTest")
        {
            Scene gameplayScene = SceneManager.GetSceneByName("GameplayTest");
            SceneManager.SetActiveScene(gameplayScene);
            
        }
    }

    private void Start()
    {
        if(NetworkManager.Singleton.IsServer)
        {
            //load the gameplay scene in the background
            NetworkManager.Singleton.SceneManager.LoadScene("GameplayTest", UnityEngine.SceneManagement.LoadSceneMode.Additive);
            //SceneManager.LoadScene("GameplayTest", UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }   
        //StartCoroutine(LoadGameplaySceneAsync());
    }

    private IEnumerator LoadGameplaySceneAsync()
    {
        AsyncOperation loadSceneOperation = SceneManager.LoadSceneAsync("GameplayTest", LoadSceneMode.Additive);
        while(!loadSceneOperation.isDone)
        {
            float progress = Mathf.Clamp01(loadSceneOperation.progress / 1);

            if(progressBar != null)
            {
                progressBar.value = progress;
            }
            yield return null;
        }
        

        // Scene gameplayScene = SceneManager.GetSceneByName("GameplayTest");
        // if(gameplayScene.IsValid())
        // {
        //     SceneManager.SetActiveScene(gameplayScene);
        // }

        // SceneManager.UnloadSceneAsync("LoadingScreen");
    }
}
