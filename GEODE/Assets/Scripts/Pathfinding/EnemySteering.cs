using UnityEngine;

[System.Serializable]
public class EnemySteering
{
    [Header("Raycast Fan Settings")]
    [Tooltip("Number of rays within the arc (odd number keeps a centre ray).")]
    [Min(3)] public int rayCount = 7;

    [Tooltip("Arc (degrees) centred on desired direction.")]
    [Range(1f, 360f)] public float arc = 180f;

    [Tooltip("Max distance for each ray.")]
    public float rayDistance = 0.6f;

    [Tooltip("Seconds between steering checks (set lower for snappy).")]
    public float checkInterval = 0.15f;

    [Header("Debug")]
    public bool drawDebug = true;

    // ──────────────────────────────────────────────────────────────
    private readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[1]; // NonAlloc
    private Vector2 _cachedDir;
    private float   _nextCheckTime;

    /// <summary>Returns a steered direction based on ray fan.</summary>
    public Vector2 GetSteeredDirection(BaseEnemy owner, Vector2 desiredDir)
    {
        if (Time.time < _nextCheckTime || desiredDir.sqrMagnitude < 1e-4f)
            return _cachedDir;                               // reuse last result

        _nextCheckTime = Time.time + checkInterval;
        _cachedDir     = CalculateSteer(owner, desiredDir);

        return _cachedDir;
    }

    // ──────────────────────────────────────────────────────────────
    private Vector2 CalculateSteer(BaseEnemy owner, Vector2 desiredDir)
    {
        Vector2 origin      = owner.transform.position;
        Vector2 forwardNorm = desiredDir.normalized;

        //Build directions cache
        int    n        = Mathf.Max(3, rayCount) | 1;                 // force odd
        float  stepDeg  = arc / (n - 1);
        float  startDeg = -arc * 0.5f;

        Vector2 bestDir      = forwardNorm;   // fallback = straight
        float   bestWeight   = float.MinValue;

        for (int i = 0; i < n; i++)
        {
            float   angle   = startDeg + stepDeg * i;
            Vector2 dir     = Rotate(forwardNorm, angle);

            //Cast ray without GC
            bool hit = Physics2D.RaycastNonAlloc(
                           origin, dir, _hitBuffer, rayDistance,
                           owner.structureLayerMask) > 0;

            if (drawDebug)
            {
                Debug.DrawLine(
                    origin, origin + dir * rayDistance,
                    hit ? Color.red : Color.green,
                    checkInterval);
            }

            //Evaluate this ray (simple scoring: clear > blocked)
            float weight = hit ? -1f :               // blocked = bad
                           1f  - Mathf.Abs(angle)/arc; // favour central rays

            if (weight > bestWeight)
            {
                bestWeight = weight;
                bestDir    = hit ? bestDir : dir;     // only switch if clear
            }
        }

        return bestDir;
    }

    // Cheap 2D rotate
    private static Vector2 Rotate(Vector2 v, float deg)
    {
        float rad = deg * Mathf.Deg2Rad;
        float s   = Mathf.Sin(rad);
        float c   = Mathf.Cos(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
