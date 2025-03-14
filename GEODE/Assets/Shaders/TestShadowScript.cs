using UnityEngine;

/// <summary>
/// Generates a custom mesh behind a sprite to act as a skewed shadow,
/// using the sprite's TIGHT geometry (ignoring alpha=0 pixels).
///
/// Attach this to the same GameObject as your SpriteRenderer.
/// Requires:
///  1) A light source transform (the shadow is cast away from it).
///  2) A sprite imported with "Mesh Type" = Tight (so alpha=0 is stripped).
///  3) A material that can render Sprites (e.g., "Sprites/Default").
///
/// It calculates each vertex's vertical position in local space
/// and offsets it proportionally in the direction away from the light.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SkewedSpriteShadow2D : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The transform of the light source in the scene. Shadow is cast away from it.")]
    public Transform lightSource;

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
    private MeshFilter shadowMeshFilter;
    private Mesh shadowMesh;

    // We store the original sprite geometry
    private Vector2[] spriteVertices2D;
    private Vector2[] spriteUVs2D;
    private ushort[] spriteTriangles;

    private void Awake()
    {
        // Get main sprite renderer
        mainSpriteRenderer = GetComponent<SpriteRenderer>();

        // If no specific sprite is assigned for the shadow, default to the same as the main sprite
        if (shadowSprite == null)
            shadowSprite = mainSpriteRenderer.sprite;

        // Verify we actually have geometry
        if (shadowSprite == null)
        {
            Debug.LogError("SkewedSpriteShadow2D: No sprite assigned and no SpriteRenderer sprite found.");
            return;
        }

        if(lightSource == null)
        {
            lightSource = GameObject.Find("Sunlight").transform;
        }

        // Grab the sprite's "tight" mesh data:
        //  sprite.vertices   -> array of Vector2 for each vertex (in local sprite space)
        //  sprite.uv        -> array of Vector2 for each vertex's UV
        //  sprite.triangles -> array of indices (ushort)
        spriteVertices2D = shadowSprite.vertices;   // local coords
        spriteUVs2D      = shadowSprite.uv;
        spriteTriangles  = shadowSprite.triangles;

        if (spriteVertices2D == null || spriteVertices2D.Length < 3)
        {
            Debug.LogError("SkewedSpriteShadow2D: Sprite has no valid geometry. Did you set Mesh Type to 'Tight'?");
            return;
        }

        // Create a child object to hold the shadow mesh
        GameObject shadowObj = new GameObject("SkewedShadowMesh");
        shadowObj.transform.SetParent(this.transform);
        shadowObj.transform.localPosition = Vector3.zero;
        shadowObj.transform.localRotation = Quaternion.identity;
        shadowObj.transform.localScale    = Vector3.one;

        // Add MeshFilter and MeshRenderer
        shadowMeshFilter   = shadowObj.AddComponent<MeshFilter>();
        shadowMeshRenderer = shadowObj.AddComponent<MeshRenderer>();

        // Create a new mesh
        shadowMesh = new Mesh { name = "ShadowMesh" };
        shadowMeshFilter.mesh = shadowMesh;

        // Assign material
        if (shadowMaterial != null)
        {
            shadowMeshRenderer.material = new Material(shadowMaterial);
        }
        else
        {
            // Default fallback
            Debug.LogWarning("No shadowMaterial assigned. Using 'Sprites/Default' as fallback.");
            shadowMeshRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        // Set shadow color/alpha
        Color finalColor = shadowColor;
        finalColor.a = shadowAlpha;
        shadowMeshRenderer.material.color = finalColor;

        // Sorting layer / order behind the main sprite
        if (autoSetSortingLayer)
        {
            shadowMeshRenderer.sortingLayerID = mainSpriteRenderer.sortingLayerID;
            shadowMeshRenderer.sortingOrder   = mainSpriteRenderer.sortingOrder + shadowSortingOrderOffset;
        }
        if (!string.IsNullOrEmpty(shadowSortingLayerName))
        {
            shadowMeshRenderer.sortingLayerName = shadowSortingLayerName;
        }

        // Generate the mesh data once
        GenerateShadowMesh();
    }

    private void LateUpdate()
    {
        // Update geometry each frame if light or sprite moves
        UpdateShadowMesh();
    }

    /// <summary>
    /// Sets up the mesh with the correct number of vertices, uvs, and triangles
    /// based on the sprite's geometry. The actual vertex positions (skew) are applied in UpdateShadowMesh().
    /// </summary>
    private void GenerateShadowMesh()
    {
        if (shadowMesh == null || spriteVertices2D == null)
            return;

        // We'll create arrays for 3D positions/uvs/triangles
        Vector3[] meshVertices3D = new Vector3[spriteVertices2D.Length];
        Vector2[] meshUVs        = new Vector2[spriteVertices2D.Length];
        int[] meshTriangles      = new int[spriteTriangles.Length];

        // Copy uv array directly
        for (int i = 0; i < spriteUVs2D.Length; i++)
            meshUVs[i] = spriteUVs2D[i];

        // Convert triangles from ushort to int
        for (int i = 0; i < spriteTriangles.Length; i++)
            meshTriangles[i] = spriteTriangles[i];

        // For now, just store the local x/y in each vertex with no offset
        for (int i = 0; i < spriteVertices2D.Length; i++)
        {
            meshVertices3D[i] = new Vector3(spriteVertices2D[i].x, spriteVertices2D[i].y, 0f);
        }

        // Assign to the mesh
        shadowMesh.Clear();
        shadowMesh.vertices  = meshVertices3D;
        shadowMesh.uv        = meshUVs;
        shadowMesh.triangles = meshTriangles;

        shadowMesh.RecalculateBounds();
        shadowMesh.RecalculateNormals();
    }

    /// <summary>
    /// Each frame, offset the vertices in the direction away from the light source,
    /// proportional to their vertical position. That way, higher (top) vertices get more offset,
    /// ignoring alpha=0 areas because the sprite's geometry doesn't include them.
    /// </summary>
    private void UpdateShadowMesh()
    {
        if (shadowMesh == null || lightSource == null || spriteVertices2D == null)
            return;

        // 1. Determine the minY and maxY in the sprite geometry, so we can proportionally offset
        //    each vertex from minY=0 offset up to maxY=full shadow offset.
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        for (int i = 0; i < spriteVertices2D.Length; i++)
        {
            float vy = spriteVertices2D[i].y;
            if (vy < minY) minY = vy;
            if (vy > maxY) maxY = vy;
        }

        // 2. Calculate direction in world space: from light -> sprite is (spritePos - lightPos).
        //    But we want the shadow to extend AWAY from the light, so that's dir = (spritePos - lightPos).normalized.
        Vector2 spritePos = transform.position;
        Vector2 lightPos  = lightSource.position;
        Vector2 dirWorld  = (spritePos - lightPos).normalized;

        // 3. Convert that direction to local space of the shadow's parent
        Quaternion invRotation = Quaternion.Inverse(transform.rotation);
        Vector3 dirLocal = invRotation * (Vector3)dirWorld;

        // 4. Update each vertex: 
        //    - Start with original local coords from spriteVertices2D
        //    - Scale Y if needed (verticalScale)
        //    - Compute how "high" it is from minY..maxY to get a 't' factor.
        //    - Offset by t * shadowLength in direction dirLocal.
        Vector3[] meshVerts = shadowMesh.vertices; // current positions

        for (int i = 0; i < spriteVertices2D.Length; i++)
        {
            float vx = spriteVertices2D[i].x;
            float vy = spriteVertices2D[i].y;

            // Apply vertical scale
            vy *= verticalScale;

            float t = 0f;
            if (maxY != minY)
                t = (vy - (minY * verticalScale)) / ((maxY - minY) * verticalScale);
            
            // offset by t * shadowLength in dirLocal
            Vector3 offset = dirLocal * (shadowLength * t);

            // final vertex in local space
            meshVerts[i] = new Vector3(vx, vy, 0f) + offset;
        }

        // 5. Commit these changed vertices back to the mesh
        shadowMesh.vertices = meshVerts;
        shadowMesh.RecalculateBounds();
        // Normals are not strictly important for a 2D unlit sprite,
        // but we do it for completeness.
        shadowMesh.RecalculateNormals();
    }
}
