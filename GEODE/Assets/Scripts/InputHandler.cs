using UnityEngine;
using UnityEngine.TextCore;

public class InputHandler : MonoBehaviour
{
    [HideInInspector] public float Horizontal;
    [HideInInspector] public float Vertical;

    public KeyCode spaceKeybind = KeyCode.Space;
    public KeyCode inventoryBind = KeyCode.E;

    public void Update()
    {
        Horizontal = Input.GetAxisRaw("Horizontal");
        Vertical = Input.GetAxisRaw("Vertical");
    }
    
}
