using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager Instance;
    [SerializeField] private AudioListener audioListener;
    [SerializeField] private Slider progressBar;

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

    }

}
