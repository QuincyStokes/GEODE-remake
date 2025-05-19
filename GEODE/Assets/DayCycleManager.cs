using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayCycleManager : NetworkBehaviour
{
    public static DayCycleManager Instance;
    [Header("References")]
    [SerializeField] private bool isNightTime;
    [SerializeField] private Light2D sunlight;
    
    [Header("Night Cycle Settings")]
    [SerializeField] private float startTimeInSeconds;
    [SerializeField] private float dayLengthInSeconds;
    [SerializeField] private float nightLengthInSeconds;
    [SerializeField] private Gradient lightGradient;
    [SerializeField] private AnimationCurve intensityCurve;

    //EVENTS
    public event Action becameNight;
    public event Action becameDay;

    //PUBLIC TINGS
    public DayType dayType;
    public int DayNum = 1;

    //PRIVATE TINGS
    private NetworkVariable<float> currentTime = new NetworkVariable<float>(0f);
    private float timePercent;
    private float totalDayCycleLength;
    private float _t;
    


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            //Destroy(gameObject);
        }
    }

    private void Start()
    {
        sunlight.intensity = .8f;
        isNightTime = false;
        totalDayCycleLength = dayLengthInSeconds + nightLengthInSeconds;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
    }
    private void Update()
    {
        if (IsServer)
        {
            currentTime.Value += Time.deltaTime;

            if (currentTime.Value > dayLengthInSeconds && !isNightTime)
            {
                currentTime.Value = 0f;
                isNightTime = true;
                becameNight?.Invoke();
                Debug.Log("Becoming Night!");
            }
            else if (currentTime.Value > nightLengthInSeconds && isNightTime)
            {
                currentTime.Value = 0f;
                isNightTime = false;
                DayNum++;
                becameDay?.Invoke();
                Debug.Log("Becoming Day!");
            }
        }
        UpdateLighting();
    }


    public bool IsNighttime()
    {
        return isNightTime;
    }

    private void UpdateLighting()
    {
        _t = (currentTime.Value / totalDayCycleLength) % 1f;       // loops 0-1
        sunlight.color = lightGradient.Evaluate(_t);
        sunlight.intensity = intensityCurve.Evaluate(_t);

        // //timePercent = 0f;
        // float sunlightIntensity;
        // if(!isNightTime) //daytime sunlight
        // {
        //     timePercent = currentTime / dayLengthInSeconds;
        //     sunlightIntensity = Mathf.Sin(timePercent * Mathf.PI);

        //     if(sunlightIntensity < .05f) //if it would be super dark, make it not super dark
        //     {
        //         sunlightIntensity = .05f;
        //     }
        // }
        // else
        // {
        //     //sunlightIntensity = .05f;
        // }
        // //sunlight.intensity = sunlightIntensity;
        
    }


    public enum DayType
    {
        Day,
        Night,

    }
}
