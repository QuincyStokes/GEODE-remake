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
            
            // Wait for initialization to complete before deactivating
            // This ensures BaseObject.OnNetworkSpawn() and health initialization complete
            yield return null; // Wait one frame for all OnNetworkSpawn calls to complete
            yield return null; // Wait one more frame to ensure NetworkVariable updates propagate
            
            // Check if this is a BaseObject and verify health was initialized
            BaseObject baseObj = GetComponent<BaseObject>();
            if (baseObj != null && baseObj.BASE_HEALTH > 1f)
            {
                // Verify health was initialized (should match BASE_HEALTH if properly initialized)
                // If still at default value, wait a bit more for NetworkVariable sync
                int healthCheckAttempts = 0;
                while (baseObj.MaxHealth.Value <= 1f && healthCheckAttempts < 10)
                {
                    healthCheckAttempts++;
                    yield return null;
                }
            }
            
            // Only deactivate if the object is still valid
            if (gameObject != null)
            {
                gameObject.SetActive(false);
            }
        }
    }
    
    public override void OnNetworkDespawn()
    {
        // Deregistration happens in OnDestroy when the object is truly gone.
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
