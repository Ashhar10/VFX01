using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// VFXGraphLooper - Synchronizes a Visual Effect Graph with an Animation.
/// Calculates the total animation length and plays the VFX only within a user-defined
/// compartment (start to end window) of the animation.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(VisualEffect))]
public class VFXGraphLooper : MonoBehaviour
{
    [Header("=== Target References ===")]
    [Tooltip("Visual Effect Graph component. Auto-assigned from this GameObject if left empty.")]
    [SerializeField] private VisualEffect visualEffect;

    [Tooltip("Animator driving the character/weapon. Auto-searched in hierarchy if unassigned.")]
    [SerializeField] private Animator animator;

    [Tooltip("Optional: Assign a specific AnimationClip directly to calculate length and sync timing.")]
    [SerializeField] private AnimationClip specificClip;

    [Tooltip("Animator layer index (default is 0).")]
    [SerializeField] private int animatorLayer = 0;

    [Tooltip("Optional state name to filter by (e.g. 'Attack' or '01'). Leave empty to sync with any active state.")]
    [SerializeField] private string targetStateName = "";

    [Header("=== Animation Compartment (Playback Window) ===")]
    [Tooltip("Normalized start point (0.0 = 0% / start of animation, 1.0 = 100% / end).")]
    [Range(0f, 1f)]
    [SerializeField] private float startNormalizedTime = 0.25f;

    [Tooltip("Normalized end point (0.0 = 0% / start of animation, 1.0 = 100% / end).")]
    [Range(0f, 1f)]
    [SerializeField] private float endNormalizedTime = 0.75f;

    [Header("=== Calculated Timing Info (Read-Only) ===")]
    [Tooltip("Total duration of the detected animation clip in seconds.")]
    [SerializeField] private float totalAnimationDuration = 0f;

    [Tooltip("Timestamp in seconds where the VFX starts playing.")]
    [SerializeField] private float compartmentStartTimeSec = 0f;

    [Tooltip("Timestamp in seconds where the VFX stops emitting.")]
    [SerializeField] private float compartmentEndTimeSec = 0f;

    [Tooltip("Total active play window for the VFX in seconds.")]
    [SerializeField] private float compartmentDurationSec = 0f;

    [Header("=== Settings ===")]
    [Tooltip("Restart / clear old particles when entering the compartment window.")]
    [SerializeField] private bool reinitOnEnter = true;

    [Tooltip("Preview animation sync live in the Scene View during Edit Mode.")]
    [SerializeField] private bool previewInEditMode = true;

    // Internal state
    private bool isInsideCompartment = false;
    private float previousNormalizedTime = -1f;

    public float StartNormalizedTime { get => startNormalizedTime; set { startNormalizedTime = Mathf.Clamp01(value); UpdateTimingInfo(); } }
    public float EndNormalizedTime { get => endNormalizedTime; set { endNormalizedTime = Mathf.Clamp01(value); UpdateTimingInfo(); } }
    public float TotalDuration => totalAnimationDuration;
    public bool IsActiveInCompartment => isInsideCompartment;

    private void Reset()
    {
        AutoFindComponents();
        UpdateTimingInfo();
    }

    private void Awake()
    {
        AutoFindComponents();
        UpdateTimingInfo();
    }

    private void OnEnable()
    {
        AutoFindComponents();
        UpdateTimingInfo();
        isInsideCompartment = false;
        previousNormalizedTime = -1f;
    }

    private void OnDisable()
    {
        StopVFX();
        isInsideCompartment = false;
    }

    private void OnValidate()
    {
        UpdateTimingInfo();
    }

    private void AutoFindComponents()
    {
        if (visualEffect == null)
            visualEffect = GetComponent<VisualEffect>();

        if (animator == null)
        {
            // Search in parent (e.g. character root), then in self, then in children
            animator = GetComponentInParent<Animator>();
            if (animator == null)
                animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }
    }

    [ContextMenu("Refresh Animation Timing Info")]
    public void UpdateTimingInfo()
    {
        float clipLength = 0f;

        // 1. Direct clip assigned
        if (specificClip != null)
        {
            clipLength = specificClip.length;
        }
        // 2. Query animator current state/clip info
        else if (animator != null)
        {
            if (animator.runtimeAnimatorController != null)
            {
                // Try getting active clip info
                AnimatorClipInfo[] clipInfos = animator.GetCurrentAnimatorClipInfo(animatorLayer);
                if (clipInfos != null && clipInfos.Length > 0 && clipInfos[0].clip != null)
                {
                    clipLength = clipInfos[0].clip.length;
                }
                else
                {
                    AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(animatorLayer);
                    if (stateInfo.length > 0.001f)
                    {
                        clipLength = stateInfo.length;
                    }
                    else
                    {
                        // Fallback to first clip in controller
                        AnimationClip[] allClips = animator.runtimeAnimatorController.animationClips;
                        if (allClips != null && allClips.Length > 0 && allClips[0] != null)
                        {
                            clipLength = allClips[0].length;
                        }
                    }
                }
            }
        }

        totalAnimationDuration = clipLength;

        // Calculate seconds from normalized compartment
        compartmentStartTimeSec = startNormalizedTime * totalAnimationDuration;
        compartmentEndTimeSec = endNormalizedTime * totalAnimationDuration;

        if (endNormalizedTime >= startNormalizedTime)
        {
            compartmentDurationSec = (endNormalizedTime - startNormalizedTime) * totalAnimationDuration;
        }
        else
        {
            // Wraps around loop end
            compartmentDurationSec = ((1f - startNormalizedTime) + endNormalizedTime) * totalAnimationDuration;
        }
    }

    private void Update()
    {
        if (visualEffect == null) return;
        if (!Application.isPlaying && !previewInEditMode) return;

        // Ensure animator reference is valid
        if (animator == null)
        {
            AutoFindComponents();
            if (animator == null) return;
        }

        // Check state filter if specified
        if (!string.IsNullOrEmpty(targetStateName))
        {
            AnimatorStateInfo currentInfo = animator.GetCurrentAnimatorStateInfo(animatorLayer);
            if (!currentInfo.IsName(targetStateName))
            {
                if (isInsideCompartment)
                {
                    StopVFX();
                }
                return;
            }
        }

        // Get normalized time from animator (0.0 to 1.0)
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(animatorLayer);
        if (state.length <= 0.0001f) return;

        // Keep cached total duration accurate
        if (Mathf.Abs(totalAnimationDuration - state.length) > 0.01f && specificClip == null)
        {
            UpdateTimingInfo();
        }

        float currentNormalized = state.normalizedTime % 1.0f;
        if (currentNormalized < 0f) currentNormalized += 1.0f;

        bool shouldBeInCompartment = IsInCompartment(currentNormalized);

        // Detect entry into compartment window
        if (shouldBeInCompartment && !isInsideCompartment)
        {
            PlayVFX();
        }
        // Detect exit from compartment window
        else if (!shouldBeInCompartment && isInsideCompartment)
        {
            StopVFX();
        }

        previousNormalizedTime = currentNormalized;
    }

    /// <summary>
    /// Checks whether the normalized time falls within the defined compartment.
    /// Handles both standard range [start, end] and wrapped range [start, 1] U [0, end].
    /// </summary>
    private bool IsInCompartment(float normTime)
    {
        if (startNormalizedTime <= endNormalizedTime)
        {
            return normTime >= startNormalizedTime && normTime <= endNormalizedTime;
        }
        else
        {
            // Wrapped compartment (e.g. starts at 0.85 and ends at 0.15 across loop boundary)
            return normTime >= startNormalizedTime || normTime <= endNormalizedTime;
        }
    }

    private void PlayVFX()
    {
        isInsideCompartment = true;
        if (visualEffect == null) return;

        if (reinitOnEnter)
        {
            visualEffect.Reinit();
        }
        visualEffect.Play();
    }

    private void StopVFX()
    {
        isInsideCompartment = false;
        if (visualEffect == null) return;

        visualEffect.Stop();
    }

    [ContextMenu("Force Play VFX")]
    public void ForcePlay()
    {
        PlayVFX();
    }

    [ContextMenu("Force Stop VFX")]
    public void ForceStop()
    {
        StopVFX();
    }
}
