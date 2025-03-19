using System.Threading;
using UnityEngine;



public class RegisterOnChunk : MonoBehaviour
{
    public bool registerd;
    private void Start()
    {
        if(ChunkManager.Instance != null)
        {
            ChunkManager.Instance.RegisterObject(gameObject);
            registerd = true;
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Can't register, chunkmanager is null.");
        }
        gameObject.SetActive(false);
    }
}
