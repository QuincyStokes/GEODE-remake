using System;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;
    [SerializeField] private CinemachineCamera cinemachineCamera;
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
    }
}
