using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AOEBlastTower : BasicAttackTower
{

    //Create a pulse blast at the location of the closest target
    public override void TriggerFire()
    {
        animator.SetTrigger("Shoot");
        //Wait one frame because animation takes 2  frames
       
    }

    public override void Fire()
    {
        GameObject bolt = Instantiate(projectilePrefab, GetNearestTarget().position, tower.transform.rotation);

        if(fireSoundId != SoundId.NONE)
        {
            AudioManager.Instance.PlayClientRpc(fireSoundId, transform.position);
        }

        NetworkObject no =bolt.GetComponent<NetworkObject>();
        if(no != null)
        {
            no.Spawn();
        }

        bolt.GetComponent<BaseProjectile>().Initialize(strength.Value, (currentTarget.position - firePoint.position).normalized, this, damageType, persistent:true);
    }

}
