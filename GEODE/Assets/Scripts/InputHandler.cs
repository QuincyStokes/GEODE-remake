using Unity.Netcode;
using UnityEngine;
using UnityEngine.TextCore;

public class InputHandler : NetworkBehaviour
{
    [HideInInspector] public float Horizontal;
    [HideInInspector] public float Vertical;

    public KeyCode spaceKeybind = KeyCode.Space;
    public KeyCode inventoryBind = KeyCode.E;

    public void Update()
    {
        
        if(!IsOwner)
        {
            return;
        }
        Horizontal = Input.GetAxisRaw("Horizontal");
        Vertical = Input.GetAxisRaw("Vertical");
        
        
    }
    
}
