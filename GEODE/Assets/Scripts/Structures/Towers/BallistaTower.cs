using Unity.VisualScripting;
using UnityEngine;

public class BallistaTower : BaseTower
{
    [Header("References")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    public override void Fire()
    {
       GameObject bolt = Instantiate(projectilePrefab, firePoint.position, tower.transform.rotation);
       bolt.GetComponent<BaseProjectile>().Initialize(strength, (currentTarget.position - tower.transform.position).normalized);
       
    }

    public override void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Enemy"))
        {
            AddTarget(collision.gameObject);
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
