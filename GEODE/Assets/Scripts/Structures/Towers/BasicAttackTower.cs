using System;
using System.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class BasicAttackTower : BaseTower
{
    [Header("References")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [Header("Audio")]
    [SerializeField] private SoundId fireSoundId;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private int framesToWait;
    public override IEnumerator Fire()
    {
        animator.SetTrigger("Shoot");
        //Wait one frame because animation takes 2  frames
        for(int i = 0; i < framesToWait; i++)
        {
            yield return new WaitForEndOfFrame();
        }
        GameObject bolt = Instantiate(projectilePrefab, firePoint.position, tower.transform.rotation);

        if(fireSoundId != SoundId.NONE)
        {
            AudioManager.Instance.PlayClientRpc(fireSoundId, transform.position);
        }

       

        NetworkObject no =bolt.GetComponent<NetworkObject>();
        if(no != null)
        {
            no.Spawn();
        }

        bolt.GetComponent<BaseProjectile>().Initialize(strength.Value, (currentTarget.position - tower.transform.position).normalized, this);
       
    }

    public override void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Enemy"))
        {
            if(collision.TryGetComponent<BaseEnemy>(out BaseEnemy bo))
            {
                AddTarget(bo.CenterPoint.gameObject);
            }
            else
            {
                AddTarget(collision.gameObject);
            }
            Debug.Log("Enemy entered range, adding it to targets.");
        }
    }

    public override void OnTriggerExit2D(Collider2D collision)
    {
         if(collision.gameObject.CompareTag("Enemy"))
        {
            RemoveTarget(collision.gameObject);
            Debug.Log("Enemy entered range, adding it to targets.");
        }
    }
}
