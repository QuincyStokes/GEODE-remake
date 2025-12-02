using System;
using Unity.Netcode;
using UnityEngine;

public class LowerableWall : SimpleObject, IUniqueMenu
{

    public bool isLowered;

    [Header("Unique UI")]
    [SerializeField] private GameObject uniqueUI;
    [Header("Animations")]
    [SerializeField]private Animator _animator;


    //* ------------ Events ---------
    public event Action OnWallRaised;
    public event Action OnWallLowered;
    public GameObject UniqueUI => uniqueUI;

    [ServerRpc(RequireOwnership = false)]
    public void ToggleWallServerRpc()
    {
        if(isLowered)
        {
            Raise();
        }
        else
        {
            Lower();
        }
    }

    public void Raise()
    {
        if(!IsServer) return;
        //Do the raise animation
        //Enable the collider again
        
        _animator.SetBool("isLowered", false);
        isLowered = false;
    }

    public void RaiseEffects()
    {
        CollisionHitbox.enabled = true;
        sr.sortingOrder = 0; 
        OnWallRaised?.Invoke();
    }



    public void Lower()
    {
        //Do the lower animation
        //Disable the collider.
        
        _animator.SetBool("isLowered", true);
        isLowered = true;
    }

    

    public void LowerEffects()
    {
        CollisionHitbox.enabled = false;
        sr.sortingOrder = -1; 
        OnWallLowered?.Invoke();
    }


    public void ShowMenu()
    {
        UniqueUI.SetActive(true);
    }

    public void HideMenu()
    {
        UniqueUI.SetActive(false);
    }

}
