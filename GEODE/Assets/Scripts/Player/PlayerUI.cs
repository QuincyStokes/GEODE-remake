using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerUI : NetworkBehaviour
{
    [Tooltip("List of all objects that should only be active on local player.")]
    [SerializeField] private List<GameObject> LocalUI = new List<GameObject>();

    [Tooltip("List of all UI objects that should be active when the game starts.")]
    [SerializeField] private List<GameObject> ActiveOnStart = new List<GameObject>();


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        foreach (GameObject go in LocalUI)
        {
            go.SetActive(false);
        }

        if (IsOwner)
        {
            foreach (GameObject go in ActiveOnStart)
            {
                go.SetActive(true);
            }
        }
        
    }
}
