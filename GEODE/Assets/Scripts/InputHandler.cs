using Unity.Netcode;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TextCore;

public class InputHandler : NetworkBehaviour
{
    [HideInInspector] public float Horizontal;
    [HideInInspector] public float Vertical;
    [HideInInspector] public string InputString;
    [HideInInspector] public float ScrollY;
    [HideInInspector] public Vector3 MousePosition;

    public KeyCode useKeybind = KeyCode.Mouse0;
    public KeyCode spaceKeybind = KeyCode.Space;
    public KeyCode inventoryKeybind = KeyCode.E;

    public override void OnNetworkSpawn()
    {
        if(!IsOwner)
        {
            enabled = false;
        }
    }

    public void Update()
    {
        
        Horizontal = Input.GetAxisRaw("Horizontal");
        Vertical = Input.GetAxisRaw("Vertical");
        InputString = Input.inputString;
        ScrollY = Input.mouseScrollDelta.y;
        MousePosition = Input.mousePosition;
    }
    
}
