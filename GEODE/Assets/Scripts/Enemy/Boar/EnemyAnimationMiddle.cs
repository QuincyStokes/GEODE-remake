using System.Security.Cryptography;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;



public class EnemyAnimationMiddle : NetworkBehaviour
{
    //This script is just a middleman between the animation events and the actual enemy brain since they exist on different objects.

    private BaseEnemy enemy;

    private void Awake()
    {
        enemy = GetComponentInParent<BaseEnemy>();
    }

    public void DoAttack()
    { 
        if(!IsServer) return; //i think
        if(enemy != null) 
            enemy.Attack();
    }

    public void DoDeath()
    {
        if(!IsServer) return; //i think
        if(enemy != null) 
            enemy.DestroyThisServerRpc(true);
    }
}
