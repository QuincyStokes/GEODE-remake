using System;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting.APIUpdating;

public class PlayerController : NetworkBehaviour
{

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float knockbackDecay;

    
    [Header("Health Settings")]
    [SerializeField] private float maxHealth;


    //EVENTS -----------------------
    public static event Action inventoryToggled;



    [Header("Internal Use")]
    private Vector2 externalVelocity;
    private float currentHealth;
    [SerializeField] private InputHandler inputHandler;


    
    //input movement, has 

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponentInChildren<Rigidbody2D>();
    }
    
    private void Update()
    {
        if(!IsOwner)
        {
            return;
        }
        HandleEvents();
    }

    private void FixedUpdate()
    {
        Move();
    }
   

    private void Move()
    {
        //movement direction input
        float horizontal = inputHandler.Horizontal;
        float vertical = inputHandler.Vertical;
        
        //normalize the direction input to keep speed consistent, then apply movemetn speed
        Vector2 inputVelocity = new Vector2(horizontal, vertical).normalized * moveSpeed;

        //add input velocity to external velocity, this ensures things like knockback aren't overwritten by inputvelocity.
        Vector2 finalVelocity = inputVelocity + externalVelocity;
        

        rb.linearVelocity = finalVelocity;
        externalVelocity = Vector2.Lerp(externalVelocity, Vector2.zero, knockbackDecay * Time.fixedDeltaTime);
    }

    //this works pretty well
    public void ApplyKnockback(Vector2 direction, float force)
    {
        // Normalize the direction to ensure consistent behavior, then add the knockback force.
        externalVelocity += direction.normalized * force;
    }

    private void HandleEvents()
    {
        if(Input.GetKeyDown(inputHandler.inventoryBind))
        {
            Debug.Log("Inventory toggled!");
            inventoryToggled?.Invoke();
        }
    }

    
}
