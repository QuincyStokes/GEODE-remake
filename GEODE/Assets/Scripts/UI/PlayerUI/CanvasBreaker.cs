using Unity.Netcode;
using UnityEngine;

public class CanvasBreaker : NetworkBehaviour
{
    private void Start()
    {
        if(!IsOwner) gameObject.SetActive(false);
        transform.SetParent(null);
    }
}
