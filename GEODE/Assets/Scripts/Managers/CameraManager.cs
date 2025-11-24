using System;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private GameObject mainMenuBackground;
    public void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(cinemachineCamera);
        }
        else
        {
            Destroy(cinemachineCamera.gameObject);
            Destroy(gameObject);
            return;
        }
    }

    public void FollowPlayer(Transform player)
    {
        Instance.gameObject.GetComponent<CinemachineBrain>().WorldUpOverride = player;
        cinemachineCamera.Follow = player;
        cinemachineCamera.LookAt = player;
        mainMenuBackground.SetActive(false);
    }

    public void UnfollowPlayer()
    {
        Instance.gameObject.GetComponent<CinemachineBrain>().WorldUpOverride = null;
        cinemachineCamera.Follow = null;
        cinemachineCamera.LookAt = null;
        transform.position = Vector3.zero;
        mainMenuBackground.SetActive(true);
    }
}
