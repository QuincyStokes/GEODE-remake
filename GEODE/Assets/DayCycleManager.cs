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
    [SerializeField] private float baseDayLengthInSeconds;
    [SerializeField] private float baseNightLengthInSeconds;
    [SerializeField] private float additionalDayOneLength;
    
    [Header("Transitions")]
    [Tooltip("Fraction of the night after the darkest point that counts as 'sunrise'")]
    [Range(0f,1f)] public float sunrisePercent = 0.15f;

    [Header("Curves")]
    [SerializeField] private Gradient lightGradient;
    [SerializeField] private AnimationCurve intensityCurve;

    //* ----------------Events---------------
    public event Action becameNight;
    public event Action becameDay;
    public event Action OnDay1Finished;
    public static event Action becameNightGlobal;
    public static event Action becameDayGlobal;

    //* ----------------State ---------------
    public NetworkVariable<float> timeOfDay =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone,
                                       NetworkVariableWritePermission.Server);

    public int DayNum { get; private set; } =  1;
    private float _cycleLength;
    //public getter for it
    private float _worldSizeX;

     // ------------ Internal state -----------------
    float _currentDayLength;   // daylight length for *this* cycle
    float _nightStart;         // seconds after sunrise when night begins
    private bool _isNightCached = false;


    //* ------------------------------Methods--------------------------------------
    void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            //Destroy(gameObject);
            return;
        }
        becameDay += HandleBecameDay;
    }

    void Start()
    {
        //  Day 1 is extended
        _currentDayLength = baseDayLengthInSeconds + additionalDayOneLength;
        RebuildCycleValues();

        // Begin at sunrise
        if(IsServer) timeOfDay.Value = 0f;
        
        
        sunlight.intensity = intensityCurve.Evaluate(0f);

        // After the first full cycle, revert to regular day length
        

        _worldSizeX = WorldGenManager.Instance?.WorldSizeX ?? 100f;
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
        return _isNightCached;
    }

    private void AdvanceClock()
    {
        //this adds to the clock, but autoatically resets to 0 when we reach cycleLength. 
        //This is the same as just chekcing if its greater than the max time, set it to 0.
        timeOfDay.Value = Mathf.Repeat(timeOfDay.Value + Time.deltaTime, _cycleLength);
    }

    //the percentage through the day we are.
    public float NormalizedTime => timeOfDay.Value / _cycleLength;

    void UpdateLighting()
    {
        // Colour + intensity follow the whole normalised 0-1 cycle
        float tCycle = NormalizedTime;
        sunlight.color = lightGradient.Evaluate(tCycle);
        sunlight.intensity = intensityCurve.Evaluate(tCycle);

        // Horizontal travel: rightwards during day, leftwards during night
        bool isDay = timeOfDay.Value < _nightStart;
        float x;

        if (isDay)
        {
            float t = timeOfDay.Value / _currentDayLength;              // 0-1 across daylight
            x = Mathf.Lerp(0f, _worldSizeX, t);
        }
        else
        {
            float t = (timeOfDay.Value - _nightStart) / baseNightLengthInSeconds;    // 0-1 across night
            x = Mathf.Lerp(_worldSizeX, 0f, t);
        }

        Vector3 p = sunlight.transform.position;
        sunlight.transform.position = new Vector3(x, p.y, p.z);
    }

    void CheckDayNightTransition()
    {
        // → Became night
        if (!_isNightCached && timeOfDay.Value >= _nightStart)
        {
            _isNightCached = true;
            becameNight?.Invoke();
            becameNightGlobal?.Invoke();
        }
        // → Became day
        else if (_isNightCached && timeOfDay.Value < _nightStart)
        {
            _isNightCached = false;
            becameDay?.Invoke();
            becameDayGlobal?.Invoke();
        }
    }

    private void HandleBecameDay()
    {
        DayNum++;

        // After the very first night, remove the bonus daylight for all remaining cycles
        if (DayNum == 2 && additionalDayOneLength > 0f)
        {
            OnDay1Finished?.Invoke();
            _currentDayLength = baseDayLengthInSeconds;      // back to normal
            RebuildCycleValues();
        }
    }

     void RebuildCycleValues()
    {
        _cycleLength = _currentDayLength + baseNightLengthInSeconds;
        _nightStart  = _currentDayLength;           // sunset happens after the daylight span
    }

    public enum DayType
    {
        Day,
        Night,

    }
}
