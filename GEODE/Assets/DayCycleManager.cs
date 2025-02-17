using UnityEngine;

public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance;

    [SerializeField] private bool isNightTime;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public bool IsNighttime()
    {
        return isNightTime;
    }
}
