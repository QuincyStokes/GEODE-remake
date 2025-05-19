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
    private NetworkVariable<float> totalDayTime = new NetworkVariable<float>(0f);
    private float timePercent;
    private float totalDayCycleLength;
    private float _t;
    private float _t2;
    private float worldSizeX;


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
        if (WorldGenManager.Instance != null)
        {
            worldSizeX = WorldGenManager.Instance.WorldSizeX;
        }
        currentTime.Value += startTimeInSeconds;
        totalDayTime.Value += startTimeInSeconds;
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
            totalDayTime.Value += Time.deltaTime;

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
                totalDayTime.Value = 0f;
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
        _t = (totalDayTime.Value / totalDayCycleLength + startTimeInSeconds) % 1f;       // loops 0-1
        sunlight.color = lightGradient.Evaluate(_t);
        sunlight.intensity = intensityCurve.Evaluate(_t);

        if (totalDayTime.Value < dayLengthInSeconds)
        {
            //percentage of dayTime complete needs to be transformed into the percentage distance to WorldSizeX
            //daytime, move left to right
            _t2 = currentTime.Value / dayLengthInSeconds;
            sunlight.transform.position = new Vector3(_t2 * worldSizeX, sunlight.transform.position.y, 0);
        }
        else
        {
            //nighttime, move right to left
            _t2 = currentTime.Value / nightLengthInSeconds;
            sunlight.transform.position = new Vector3(worldSizeX - (_t2 * worldSizeX), sunlight.transform.position.y, 0);
        }
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
