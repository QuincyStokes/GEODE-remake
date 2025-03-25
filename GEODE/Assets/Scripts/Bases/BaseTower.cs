using System.Collections.Generic;
using UnityEngine;

public abstract class BaseTower : BaseObject
{
    //all towers shall inherit from this script, so we must include all things that *all* towers need
    //hold up, should towers have states? i think that doin too much, towers are simple
    #region PROPERTIES

    [Header("Tower Stats")]
    [SerializeField] private float baseFireRate; //attack rate
    [SerializeField] private float basePower; //damage/power of attack
    [SerializeField] private float baseRange; //range of attack
    [SerializeField] private float rotationSpeed;
    [SerializeField] private bool rotates;
    [SerializeField] private int maximumLevelXp; //the xp needed to level up this tower
    [SerializeField] private int currentXp; //the current xp, reset from the last levelup
    [SerializeField] private int currentTotalXp; //the total xp this tower has accumulated;
    [SerializeField] private int level = 1;

    [Header("Stat Modifiers")]
    [SerializeField] private float speed = 1.0f; //modifies attack rate
    [SerializeField] private float strength = 1.0f; //modifies power
    [SerializeField] private float size = 1.0f; //modifies range
    [SerializeField] private float sturdy = 1.0f; //modifies max health

    [Header("References")]
    [SerializeField] protected LayerMask targetLayer;
    [SerializeField] protected CircleCollider2D detectionCollider;
    [SerializeField] protected GameObject tower; //separate the tower from the "base" for towers that may rotate



    // final stats
    [HideInInspector]public float fireRate;
    [HideInInspector]public float power;
    [HideInInspector]public float range;
    [HideInInspector]public float maxHp;

    //Internal
    protected List<GameObject> targets = new List<GameObject>();
    protected Transform currentTarget;
    private Quaternion targetRotation;
    private bool isRotating;
    private float nextFireTime;
    #endregion

    #region ACCESSORS
    public float Sturdy 
    {
        get => sturdy;
        set => sturdy = value;
    }
    public float Speed 
    {
        get => speed;
        set => speed = value;
    }
    public float Size 
    {
        get => size;
        set => size = value;
    }
    public float Strength 
    {
        get => strength;
        set => strength = value;
    }

    public int CurrentXp
    {
        get => currentXp;
        private set => currentXp = value;
    }

    public int Level
    {
        get => level;
    }
    #endregion


    #region METHODS

    private void Awake()
    {
        if(detectionCollider != null)
        {
            detectionCollider.radius = baseRange;
        }

        //set stats
        power = basePower * strength;
        fireRate = baseFireRate * speed;
        range = baseRange * size;
        maxHp = MaxHealth * sturdy; //notably, MaxHealth is derived from BaseObject
    }

    private void Update()
    {
        if(Time.frameCount % 4 == 0)
        {
            currentTarget = GetNearestTarget();
            Debug.Log($"Current target: {currentTarget}");
            if(currentTarget != null)
            {
                Debug.Log("Current target isn't null!");
                if(rotates)
                {
                    Debug.Log("It rotates!");
                    SetTargetRotation();
                    RotateTowardsTarget();
                    if(Time.time >= nextFireTime && IsRotationComplete()) // 
                    {
                        Fire();
                        Debug.Log("Firing.");
                        nextFireTime = Time.time + 1f / fireRate;
                    }
                }
                else if(Time.time >= nextFireTime)
                {
                    Debug.Log("Does not rotate. Firing!");
                    Fire();
                    nextFireTime = Time.time + 1f / fireRate;
                }
                
            }
        }
    }

    public void AddXp(int amount)
    {
        CurrentXp += amount;
        currentTotalXp += amount;
        //TODO update level UI here
        if(CurrentXp >= maximumLevelXp)
        {
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentXp = 0;
        level++;
        //TODO apply some modifier to stats (increase base stats)
    }

    private Transform GetNearestTarget()
    {
        float currentClosest = range*2; //arbitrary number, fine to set it to range*2 because that will always be outside of our range
        if(targets.Count == 0)
        {
            currentTarget = null;
            return null;
        }

        foreach(GameObject target in targets)
        {
            if(target != null)
            {
                float dist = Vector3.Distance(target.transform.position, transform.position);
                if(dist < currentClosest)
                {
                    currentClosest = dist;
                    currentTarget = target.transform;
                }
            }
            
        }
        return currentTarget;
    }
    protected void AddTarget(GameObject target)
    {
        targets.Add(target);
    }

    protected void RemoveTarget(GameObject target)
    {
        targets.Remove(target);
    }

     private void SetTargetRotation()
    {
        Debug.Log("Setting target rotation!");
        Vector2 direction = (currentTarget.position - tower.transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        targetRotation = Quaternion.Euler(new Vector3(0, 0, angle));
        isRotating = true;
        
    }

    private void RotateTowardsTarget()
    {
        if(isRotating)
        {
            Debug.Log($"Tower is rotating towards {targetRotation}");
            tower.transform.rotation = Quaternion.Lerp(tower.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private bool IsRotationComplete()
    {
        if(Quaternion.Angle(tower.transform.rotation, targetRotation) < 1f)
        {
            isRotating = false;
            return true;
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(tower.transform.position, range);
    }

    public abstract void Fire();
    public abstract void OnTriggerEnter2D(Collider2D collision);
    public abstract void OnTriggerExit2D(Collider2D collision);

   
    #endregion

}
