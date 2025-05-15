using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayCycleManager : MonoBehaviour
{
    public static DayCycleManager Instance;
    [Header("References")]
    [SerializeField] private bool isNightTime;
    [SerializeField] private Light2D sunlight;
    
    [Header("Night Cycle Settings")]
    [SerializeField] private float startTimeInSeconds;
    [SerializeField] private float dayLengthInSeconds;
    [SerializeField] private float nightLengthInSeconds;

    //EVENTS
    public event Action becameNight;
    public event Action becameDay;

    //PUBLIC TINGS
    public DayType dayType;
    public int DayNum = 1;

    //PRIVATE TINGS
    private float currentTime;
    private float timePercent;
    


    private void Awake()
    {
        if(Instance == null)
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
    }

    private void Update()
    {
        currentTime += Time.deltaTime;

        if(currentTime > dayLengthInSeconds && !isNightTime)
        {
            currentTime = 0f;
            isNightTime = true;
            becameNight?.Invoke();
            Debug.Log("Becoming Night!");
        }
        else if (currentTime > nightLengthInSeconds && isNightTime)
        {
            currentTime = 0f;
            isNightTime = false;
            DayNum++;
            becameDay?.Invoke();
            Debug.Log("Becoming Day!");
        }
        UpdateLighting();
    }


    public bool IsNighttime()
    {
        return isNightTime;
    }

    private void UpdateLighting()
    {
        //timePercent = 0f;
        float sunlightIntensity;
        if(!isNightTime) //daytime sunlight
        {
            timePercent = currentTime / dayLengthInSeconds;
            sunlightIntensity = Mathf.Sin(timePercent * Mathf.PI);

            if(sunlightIntensity < .05f) //if it would be super dark, make it not super dark
            {
                sunlightIntensity = .05f;
            }
        }
        else
        {
            sunlightIntensity = .05f;
        }
        sunlight.intensity = sunlightIntensity;
        
    }


    public enum DayType
    {
        Day,
        Night,

    }
}
