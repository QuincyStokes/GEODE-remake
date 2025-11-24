using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Animates a UI Image by cycling through sprites from an AnimationClip at a specified framerate.
/// </summary>
public class UIAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("The AnimationClip containing the sprite frames (Editor only). Alternative: use Sprite Array below.")]
    [SerializeField] private AnimationClip animationClip;
    
    [Tooltip("Alternative: Direct sprite array assignment. If assigned, this takes priority over AnimationClip.")]
    [SerializeField] private Sprite[] spriteArray;
    
    [Tooltip("Frames per second for the animation playback")]
    [SerializeField] private float framerate = 12f;
    
    [Header("References")]
    [Tooltip("The UI Image component to animate. If not set, will try to find one on this GameObject.")]
    [SerializeField] private Image targetImage;
    
    [Header("Playback Settings")]
    [Tooltip("Whether to play the animation automatically on Start")]
    [SerializeField] private bool playOnStart = true;
    
    [Tooltip("Whether to loop the animation")]
    [SerializeField] private bool loop = true;
    
    private Sprite[] animationSprites;
    private int currentFrameIndex = 0;
    private float frameTimer = 0f;
    private float frameDuration;
    private bool isPlaying = false;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        // Find Image component if not assigned
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
        
        if (targetImage == null)
        {
            Debug.LogError($"[UIAnimator] No Image component found on {gameObject.name}. Please assign one or add an Image component.");
            return;
        }
        
        // Load sprites - prioritize sprite array over AnimationClip
        if (spriteArray != null && spriteArray.Length > 0)
        {
            SetAnimationSprites(spriteArray);
        }
        else
        {
            LoadAnimationFromClip();
        }
    }

    private void Start()
    {
        if (playOnStart && animationSprites != null && animationSprites.Length > 0)
        {
            Play();
        }
    }

    /// <summary>
    /// Loads sprite frames from the assigned AnimationClip.
    /// </summary>
    private void LoadAnimationFromClip()
    {
        if (animationClip == null)
        {
            Debug.LogWarning($"[UIAnimator] No AnimationClip assigned on {gameObject.name}.");
            return;
        }

        #if UNITY_EDITOR
        // Extract sprites from the AnimationClip (Editor only)
        // AnimationClips for sprite animations typically have ObjectReferenceCurve bindings
        var bindings = UnityEditor.AnimationUtility.GetObjectReferenceCurveBindings(animationClip);
        
        if (bindings.Length == 0)
        {
            Debug.LogWarning($"[UIAnimator] AnimationClip '{animationClip.name}' does not contain sprite keyframes. Make sure the clip is set up for sprite animation.");
            return;
        }

        // Get the first binding (usually the sprite property)
        var curve = UnityEditor.AnimationUtility.GetObjectReferenceCurve(animationClip, bindings[0]);
        
        if (curve == null || curve.Length == 0)
        {
            Debug.LogWarning($"[UIAnimator] AnimationClip '{animationClip.name}' has no keyframes.");
            return;
        }

        // Extract sprites from keyframes
        animationSprites = new Sprite[curve.Length];
        for (int i = 0; i < curve.Length; i++)
        {
            animationSprites[i] = curve[i].value as Sprite;
        }

        // Calculate frame duration based on framerate
        frameDuration = 1f / framerate;
        
        Debug.Log($"[UIAnimator] Loaded {animationSprites.Length} frames from '{animationClip.name}' at {framerate} FPS.");
        #else
        // In builds, you'll need to use SetAnimationSprites() instead
        Debug.LogWarning($"[UIAnimator] AnimationClip loading is only available in the Editor. Use SetAnimationSprites() at runtime or assign sprites directly.");
        #endif
    }

    /// <summary>
    /// Starts playing the animation.
    /// </summary>
    public void Play()
    {
        if (animationSprites == null || animationSprites.Length == 0)
        {
            Debug.LogWarning($"[UIAnimator] Cannot play animation on {gameObject.name} - no sprites loaded.");
            return;
        }

        if (isPlaying)
        {
            return; // Already playing
        }

        isPlaying = true;
        currentFrameIndex = 0;
        frameTimer = 0f;
        
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        
        animationCoroutine = StartCoroutine(AnimateCoroutine());
    }

    /// <summary>
    /// Stops the animation.
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    /// <summary>
    /// Pauses the animation at the current frame.
    /// </summary>
    public void Pause()
    {
        isPlaying = false;
    }

    /// <summary>
    /// Resumes the animation from the current frame.
    /// </summary>
    public void Resume()
    {
        if (animationSprites == null || animationSprites.Length == 0)
        {
            return;
        }

        if (!isPlaying)
        {
            isPlaying = true;
            if (animationCoroutine == null)
            {
                animationCoroutine = StartCoroutine(AnimateCoroutine());
            }
        }
    }

    /// <summary>
    /// Sets the current frame index.
    /// </summary>
    public void SetFrame(int frameIndex)
    {
        if (animationSprites == null || animationSprites.Length == 0)
        {
            return;
        }

        currentFrameIndex = Mathf.Clamp(frameIndex, 0, animationSprites.Length - 1);
        
        if (targetImage != null && animationSprites[currentFrameIndex] != null)
        {
            targetImage.sprite = animationSprites[currentFrameIndex];
        }
    }

    /// <summary>
    /// Sets the framerate of the animation.
    /// </summary>
    public void SetFramerate(float newFramerate)
    {
        framerate = Mathf.Max(0.1f, newFramerate); // Minimum 0.1 FPS
        frameDuration = 1f / framerate;
    }

    private IEnumerator AnimateCoroutine()
    {
        while (isPlaying)
        {
            // Update the sprite
            if (targetImage != null && animationSprites[currentFrameIndex] != null)
            {
                targetImage.sprite = animationSprites[currentFrameIndex];
            }

            // Wait for frame duration
            yield return new WaitForSeconds(frameDuration);

            // Advance to next frame
            currentFrameIndex++;
            
            // Handle looping
            if (currentFrameIndex >= animationSprites.Length)
            {
                if (loop)
                {
                    currentFrameIndex = 0;
                }
                else
                {
                    // Stop at last frame if not looping
                    currentFrameIndex = animationSprites.Length - 1;
                    isPlaying = false;
                    break;
                }
            }
        }
        
        animationCoroutine = null;
    }

    private void OnDestroy()
    {
        Stop();
    }

    /// <summary>
    /// Alternative method to load sprites at runtime (for builds or direct assignment).
    /// Use this if you want to assign sprites directly without using an AnimationClip.
    /// </summary>
    public void SetAnimationSprites(Sprite[] sprites)
    {
        animationSprites = sprites;
        frameDuration = 1f / framerate;
        
        if (targetImage != null && animationSprites != null && animationSprites.Length > 0)
        {
            targetImage.sprite = animationSprites[0];
            currentFrameIndex = 0;
        }
        
        Debug.Log($"[UIAnimator] Set {sprites?.Length ?? 0} animation sprites.");
    }
}

