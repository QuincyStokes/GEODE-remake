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
    public float rayDistance = 1.2f;

    [Tooltip("Seconds between steering checks (set lower for snappy).")]
    public float checkInterval = 0.1f;

    [Header("Debug")]
    public bool drawDebug = true;

    // ──────────────────────────────────────────────────────────────
    private readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[1]; // NonAlloc
    public ContactFilter2D contactFilter;
    private Vector2 _cachedDir;
    private float   _nextCheckTime;


    /// <summary>Returns a steered direction based on ray fan.</summary>
    public Vector2 GetSteeredDirection(BaseEnemy owner, Vector2 desiredDir)
    {
        if (Time.time < _nextCheckTime || desiredDir.sqrMagnitude < 1e-4f)
            return _cachedDir;                               // reuse last result

        _nextCheckTime = Time.time + checkInterval;
        _cachedDir = CalculateSteer(owner, desiredDir);

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

        contactFilter.useLayerMask = true;
        contactFilter.layerMask = owner.structureLayerMask;
        contactFilter.useTriggers = false;


        for (int i = 0; i < n; i++)
        {
            float angle = startDeg + stepDeg * i;
            Vector2 dir = Rotate(forwardNorm, angle);

            //Cast ray without GC
            // bool hit = Physics2D.RaycastNonAlloc(
            //                origin, dir, _hitBuffer, rayDistance,
            //                owner.structureLayerMask) > 0;

            //set properties for the contact filter

            //Raycast in the shape of our collider
            //This is to make sure we can fit through gaps
            int hitCount = owner.collisionHitbox.Cast(dir, contactFilter, _hitBuffer, rayDistance);

            if (drawDebug)
            {
                Color c = hitCount == 0 ? Color.green : Color.red;
                Debug.DrawLine(
                    origin, origin + dir * rayDistance,
                    c,
                    checkInterval);
            }

            //Decide which direction to move in
            //if we have 0 hits, the direction is clear. We give it a positive score
            //Anything blocked gets a negative score based on distance (closer = worse)

            float bias = 1f - (Mathf.Abs(angle) / arc) * 0.5f; // Reduced center bias for better gap alignment
            float weight;
            
            if (hitCount == 0)
            {
                // Clear path - weight by how close to desired direction and how far we can see
                weight = bias + 0.5f; // Bonus for clear paths
            }
            else
            {
                // Blocked path - weight by distance (closer hits are worse)
                float hitDistance = _hitBuffer[0].distance;
                float normalizedDistance = Mathf.Clamp01(hitDistance / rayDistance);
                // Closer obstacles get more negative weight
                weight = -1f - (1f - normalizedDistance) * 2f;
            }

            if (weight > bestWeight)
            {
                bestWeight = weight;
                if (hitCount == 0)
                {
                    bestDir = dir;
                }
            }
            
            
        }

        //here, we are handling fallback for if every direction is blocked
        //if our best weight is negative, every direction was blocked.
        if (bestWeight < 0f)
        {
            //raycast directly infront of us
            int hitCount = owner.collisionHitbox.Cast(forwardNorm, contactFilter, _hitBuffer, rayDistance);
            //if we hit something
            if (hitCount > 0)
            {
                //walk along the tangent of the blocking object
                Vector2 hitNormal = _hitBuffer[0].normal;
                Vector2 tangent = new Vector2(-hitNormal.y, hitNormal.x);

                if (Vector2.Dot(tangent, desiredDir) < 0f)
                {
                    tangent = -tangent;
                }
                return tangent;
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
