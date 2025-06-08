using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

/// <summary>
/// Generates a custom mesh behind a sprite to act as a skewed shadow,
/// with the *lowest* edge of the sprite pinned in place (like the trunk base).
///
/// Attach this to the same GameObject as your SpriteRenderer.
/// Requires:
///  1) A light source transform (the shadow is cast away from it).
///  2) A sprite imported with "Mesh Type" = Tight (so alpha=0 is stripped).
///  3) A material that can render Sprites (e.g., "Sprites/Default").
///
/// It calculates each vertex's distance between minY (base) and maxY (top)
/// in local space. minY = zero offset, maxY = full shadow offset.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SkewedSpriteShadow2D : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The transform of the light source in the scene. Shadow is cast away from it.")]
    public GameObject lightSource;

    [Tooltip("The sprite to use for building the shadow mesh. Typically the same as the main SpriteRenderer.")]
    public Sprite shadowSprite;

    [Tooltip("Material used to render the shadow mesh (e.g. 'Sprites/Default').")]
    public Material shadowMaterial;

    [Header("Shadow Appearance")]
    [Tooltip("Base color for the shadow (often black or gray).")]
    public Color shadowColor = Color.black;

    [Range(0f, 1f)]
    [Tooltip("Opacity (Alpha) of the shadow. 0 = invisible, 1 = fully opaque.")]
    public float shadowAlpha = 0.5f;

    [Tooltip("How far (in world units) to extrude the topmost vertices away from the light.")]
    public float shadowLength = 1.0f;

    [Tooltip("Scales the sprite's vertical dimension (0.5 = half height, 1 = normal).")]
    public float verticalScale = 1.0f;

    [Header("Sorting / Z-Order")]
    [Tooltip("If true, automatically positions the shadow behind the main sprite's sorting order.")]
    public bool autoSetSortingLayer = true;

    [Tooltip("Optional override of the shadow's sorting layer name (e.g., 'ShadowLayer').")]
    public string shadowSortingLayerName = "";

    [Tooltip("Sorting order offset relative to the main sprite. Negative draws behind.")]
    public int shadowSortingOrderOffset = -1;

    // Internal references
    private SpriteRenderer mainSpriteRenderer;
    private MeshRenderer shadowMeshRenderer;
    private SortingGroup shadowSortingGroup;
    private MeshFilter shadowMeshFilter;
    private Mesh shadowMesh;
    private GameObject shadowObj;

    // We store the original sprite geometry
    private Vector2[] spriteVertices2D;
    private Vector2[] spriteUVs2D;
    private ushort[] spriteTriangles;


    private void Awake()
    {

        // 1. Grab the SpriteRenderer
        mainSpriteRenderer = GetComponent<SpriteRenderer>();

        

        if (shadowSprite == null)
            shadowSprite = mainSpriteRenderer.sprite;

        // Safety checks
        if (shadowSprite == null)
        {
            Debug.LogError("SkewedSpriteShadow2D: No sprite assigned and no SpriteRenderer sprite found.");
            return;
        }

        if (lightSource == null)
        {
            // Example auto-find if you have an object named "Sunlight"
            lightSource = GameObject.Find("Sunlight");
        }

        InitializeShadowMesh(mainSpriteRenderer);
    }

    private void OnEnable()
    {
        if (mainSpriteRenderer != null)
            mainSpriteRenderer.RegisterSpriteChangeCallback(InitializeShadowMesh);
    }

    private void OnDisable()
    {
        if (mainSpriteRenderer != null)
            mainSpriteRenderer.UnregisterSpriteChangeCallback(InitializeShadowMesh);
    }

    private void LateUpdate()
    {
        // Update shadow each frame, or throttle if needed
        UpdateShadowMesh();
    }

    private void InitializeShadowMesh(SpriteRenderer sr)
    {

        //Check if there's already a shadow, if so, destroy it.
        if (shadowObj != null)
        {
            Destroy(shadowObj);
        }

        // If no shadowSprite is set, default to the main sprite

        //Reassign shadow sprite to update during runtime
        shadowSprite = mainSpriteRenderer.sprite;



        // 2. Extract the sprite's TIGHT geometry (local coords, UVs, indices)
        if (shadowSprite == null) return;
            spriteVertices2D = shadowSprite.vertices;
        spriteUVs2D = shadowSprite.uv;
        spriteTriangles = shadowSprite.triangles;

        if (spriteVertices2D == null || spriteVertices2D.Length < 3)
        {
            Debug.LogError("SkewedSpriteShadow2D: Sprite has no valid geometry. Did you set Mesh Type to 'Tight'?");
            return;
        }

        // 3. Create a child for the mesh
        shadowObj = new GameObject("SkewedShadowMesh");
        shadowObj.transform.SetParent(this.transform);
        shadowObj.transform.localPosition = Vector3.zero;
        shadowObj.transform.localRotation = Quaternion.identity;
        shadowObj.transform.localScale = Vector3.one;

        if (mainSpriteRenderer.flipX)
        {
            shadowObj.transform.localScale = new Vector3(-1, 1, 1);
        }

        // 4. Add MeshFilter / MeshRenderer / Sorting Group
        shadowMeshFilter = shadowObj.AddComponent<MeshFilter>();
        shadowMeshRenderer = shadowObj.AddComponent<MeshRenderer>();
        shadowSortingGroup = shadowObj.AddComponent<SortingGroup>();

        // 5. Create the mesh
        shadowMesh = new Mesh { name = "ShadowMesh" };
        shadowMeshFilter.mesh = shadowMesh;

        // 6. Assign material
        if (shadowMaterial != null)
        {
            // Clone the assigned material so each shadow can have its own instance color
            shadowMeshRenderer.material = new Material(shadowMaterial);
        }
        else
        {
            Debug.LogWarning("No shadowMaterial assigned. Using 'Sprites/Default' as fallback.");
            shadowMeshRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        // 7. Set the shadow color/alpha
        Color finalColor = shadowColor;
        finalColor.a = shadowAlpha;
        shadowMeshRenderer.material.color = finalColor;

        // 8. Sorting layers / orders
        if (autoSetSortingLayer)
        {
            shadowMeshRenderer.sortingLayerID = mainSpriteRenderer.sortingLayerID;
            shadowMeshRenderer.sortingOrder = mainSpriteRenderer.sortingOrder + shadowSortingOrderOffset;
        }
        if (!string.IsNullOrEmpty(shadowSortingLayerName))
        {
            shadowMeshRenderer.sortingLayerName = shadowSortingLayerName;
        }

        // Generate mesh data (with positions = original sprite geometry)
        GenerateShadowMesh();
    }

    /// <summary>
    /// One-time creation of the mesh with correct vertex count, UVs, and triangles.
    /// The actual positions are skewed each frame in UpdateShadowMesh().
    /// </summary>
    private void GenerateShadowMesh()
    {
        if (shadowMesh == null || spriteVertices2D == null)
            return;

        

        // We'll create arrays for the 3D data
        Vector3[] meshVertices3D = new Vector3[spriteVertices2D.Length];
        Vector2[] meshUVs = new Vector2[spriteVertices2D.Length];
        int[] meshTriangles = new int[spriteTriangles.Length];

        // Copy the UVs
        for (int i = 0; i < spriteUVs2D.Length; i++)
            meshUVs[i] = spriteUVs2D[i];

        // Convert ushort triangles to int
        for (int i = 0; i < spriteTriangles.Length; i++)
            meshTriangles[i] = spriteTriangles[i];

        // For now, just store the local x/y in each vertex with no offset
        for (int i = 0; i < spriteVertices2D.Length; i++)
        {
            meshVertices3D[i] = new Vector3(spriteVertices2D[i].x, spriteVertices2D[i].y, 0f);
        }

        shadowMesh.Clear();
        shadowMesh.vertices = meshVertices3D;
        shadowMesh.uv = meshUVs;
        shadowMesh.triangles = meshTriangles;

        shadowMesh.RecalculateBounds();
        shadowMesh.RecalculateNormals();
    }

    /// <summary>
    /// Each frame, we skew the vertices so that the lowest edge (minY) is pinned,
    /// and the top edge (maxY) is fully offset in the direction away from the light.
    /// </summary>
    private void UpdateShadowMesh()
    {
        if (shadowMesh == null || lightSource == null || spriteVertices2D == null)
            return;

        // 1. Find minY and maxY in sprite geometry
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        for (int i = 0; i < spriteVertices2D.Length; i++)
        {
            float vy = spriteVertices2D[i].y;
            if (vy < minY) minY = vy;
            if (vy > maxY) maxY = vy;
        }

        // 2. Determine the direction in world space: from the light to this sprite
        Vector2 spritePos = transform.position;
        Vector2 lightPos  = lightSource.transform.position;
        Vector2 dirWorld  = (spritePos - lightPos).normalized;

        // 3. Convert that direction to local space of our sprite
        Quaternion invRotation = Quaternion.Inverse(transform.rotation);
        Vector3 dirLocal = invRotation * (Vector3)dirWorld;

        // 4. Offset each vertex in local space
        Vector3[] meshVerts = shadowMesh.vertices;
        for (int i = 0; i < spriteVertices2D.Length; i++)
        {
            float vx = spriteVertices2D[i].x;
            float vy = spriteVertices2D[i].y;

            // If you want to squash or stretch vertically
            vy *= verticalScale;

            // Compute how far above minY we are (0..1)
            float t = 0f;
            float denom = (maxY - minY) * verticalScale;
            if (denom != 0f)
            {
                t = (vy - (minY * verticalScale)) / denom;
                t = Mathf.Clamp01(t); // safety clamp
            }

            // Offset = t * shadowLength in local shadow direction
            Vector3 offset = dirLocal * (shadowLength * t);

            // Final local position
            meshVerts[i] = new Vector3(vx, vy, 0f) + offset;
        }

        // 5. Push these changes into the mesh
        shadowMesh.vertices = meshVerts;
        shadowMesh.RecalculateBounds();
        shadowMesh.RecalculateNormals();
    }
}
