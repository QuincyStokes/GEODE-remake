using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreenManager : MonoBehaviour
{
    [SerializeField] private AudioListener audioListener;

    private void Awake()
    {
        audioListener = Camera.main.gameObject.GetComponent<AudioListener>();
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += HandleSceneLoaded;
        audioListener.enabled = true;
        
    }

    private void HandleSceneLoaded(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if(sceneName == "GameplayTest")
        {
            Scene gameplayScene = SceneManager.GetSceneByName("GameplayTest");
            SceneManager.SetActiveScene(gameplayScene);
            audioListener.enabled = false;
        }
    }

    private void Start()
    {
        if(NetworkManager.Singleton.IsServer)
        {
            //load the gameplay scene in the background
            NetworkManager.Singleton.SceneManager.LoadScene("GameplayTest", UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }   
    }

}
