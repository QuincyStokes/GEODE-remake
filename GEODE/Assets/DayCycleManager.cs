using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayCycleManager : NetworkBehaviour
{
    //*--------------Singleton--------------
    public static DayCycleManager Instance { get; private set; }

    //* ------------ Inspector -----------
    [Header("Light")]
    [SerializeField] private Light2D sunlight;

    [Header("Lengths (sec)")]
    [SerializeField] private float dayLengthInSeconds;
    [SerializeField] private float nightLengthInSeconds;

    
    [Header("Transitions")]
    [Tooltip("Fraction of the night after the darkest point that counts as 'sunrise'")]
    [Range(0f,1f)] public float sunrisePercent = 0.15f;

    [Header("Curves")]
    [SerializeField] private Gradient lightGradient;
    [SerializeField] private AnimationCurve intensityCurve;

    //* ----------------Events---------------
    public event Action becameNight;
    public event Action becameDay;

    //* ----------------State ---------------
    private NetworkVariable<float> timeOfDay =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone,
                                       NetworkVariableWritePermission.Server);

    public int DayNum { get; private set; } =  1;
    private bool isNightCached;

    private float _cycleLength;
    private float _worldSizeX;

    // since we're using *one* clock, the time at which night starts will be the amount of seconds that dayLength is.
    float nightStart; 
    float sunriseTime;


    //* ------------------------------Methods--------------------------------------
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            //Destroy
        }
    }

    private void Start()
    {
        _cycleLength = dayLengthInSeconds + nightLengthInSeconds;
        _worldSizeX = WorldGenManager.Instance?.WorldSizeX ?? 100f;   // fallback

        sunlight.intensity = intensityCurve.Evaluate(0f);

        // since we're using *one* clock, the time at which night starts will be the amount of seconds that dayLength is.
        nightStart = dayLengthInSeconds;
        sunriseTime = nightStart + nightLengthInSeconds * sunrisePercent;

        timeOfDay.Value = sunriseTime;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) { return; }        
    }
    private void Update()
    {
        if (IsServer)
        {
            AdvanceClock();
        }
        UpdateLighting();
        CheckDayNightTransition();
    }


    //* --------------------------- HELPERS ---------------------------
    public bool IsNighttime()
    {
        return isNightCached;
    }

    private void AdvanceClock()
    {
        //this adds to the clock, but autoatically resets to 0 when we reach cycleLength. 
        //This is the same as just chekcing if its greater than the max time, set it to 0.
        timeOfDay.Value = Mathf.Repeat(timeOfDay.Value + Time.deltaTime, _cycleLength);
    }

    //the percentage through the day we are.
    private float NormalizedTime => timeOfDay.Value / _cycleLength;

    private void UpdateLighting()
    {
        float t = NormalizedTime;

        //Update sunlight color and position
        sunlight.color = lightGradient.Evaluate(t);
        sunlight.intensity = intensityCurve.Evaluate(t);

        float xPos;
        //if the time is less than the daylength, it's daytime! move the sun right
        if (timeOfDay.Value < dayLengthInSeconds)
        {
            xPos = Mathf.Lerp(0, _worldSizeX, timeOfDay.Value / dayLengthInSeconds);
        }
        else //its nightime! move the sun left
        {
            float nightT = (timeOfDay.Value - dayLengthInSeconds) / nightLengthInSeconds;
            xPos = Mathf.Lerp(_worldSizeX, 0, nightT);
        }

        //set the suns position from above
        Vector3 p = sunlight.transform.position;
        sunlight.transform.position = new Vector3(xPos, p.y, p.z);
    }

    private void CheckDayNightTransition()
    {
        //helper variable to see if its night now or not
        bool nowNight = timeOfDay.Value >= nightStart &&
                timeOfDay.Value <  sunriseTime;

        //if it's not night, but its NowNight, that means we should change to nighttime
        if (!isNightCached && nowNight)
        {
            isNightCached = true;
            becameNight?.Invoke();
        }
        //else if its night but the time of day is daytime, this means we need to switch to daytime.
        else if (isNightCached && timeOfDay.Value >= sunriseTime && becameDay != null)
        {
            isNightCached = false;
            DayNum++;
            becameDay?.Invoke();
        }
    }

    public enum DayType
    {
        Day,
        Night,

    }
}
