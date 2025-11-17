using System.Collections;
using Unity.Netcode;
using UnityEngine;



public class RegisterOnChunk : NetworkBehaviour
{
    [HideInInspector] public bool registered;
    private const float RETRY_DELAY = 0.1f;
    private const int MAX_RETRIES = 50; // 5 seconds max wait time
    private bool hasTriedRegistering = false;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Start registration attempt
        TryRegister();
    }
    
    private void Start()
    {
        // Fallback for non-network objects or if OnNetworkSpawn wasn't called
        // Only try if we haven't already registered via OnNetworkSpawn
        if (!hasTriedRegistering)
        {
            TryRegister();
        }
    }
    
    private void TryRegister()
    {
        if (!hasTriedRegistering)
        {
            hasTriedRegistering = true;
            StartCoroutine(TryRegisterObject());
        }
    }
    
    private IEnumerator TryRegisterObject()
    {
        int attempts = 0;
        
        // Wait for ChunkManager to be ready
        while (ChunkManager.Instance == null && attempts < MAX_RETRIES)
        {
            attempts++;
            yield return new WaitForSeconds(RETRY_DELAY);
        }
        
        // Try to register
        if (ChunkManager.Instance != null)
        {
            ChunkManager.Instance.RegisterObject(gameObject);
            registered = true;
            gameObject.SetActive(false);
        }
    }
    
    public override void OnNetworkDespawn()
    {
        // Deregister when object is despawned
        if (registered && ChunkManager.Instance != null)
        {
            ChunkManager.Instance.DeregisterObject(gameObject);
            registered = false;
        }
        
        base.OnNetworkDespawn();
    }
    
    private void OnDestroy()
    {
        // Safety check: deregister if object is destroyed
        if (registered && ChunkManager.Instance != null)
        {
            ChunkManager.Instance.DeregisterObject(gameObject);
        }
    }
}
