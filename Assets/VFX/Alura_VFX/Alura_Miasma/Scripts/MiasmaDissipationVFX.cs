using UnityEngine;
using System.Collections;

/// <summary>
/// MiasmaDissipationVFX - Dedicated Stage [4] Dissipation Timeline Controller.
/// 
/// User Requirement:
/// "Miasma_Fog_Dissipation when this enables it just opacity x to 0 with timeline smoothly and disappear,
/// just I want that, remove all type of axis functionality."
/// 
/// When this GameObject is enabled (or triggered):
/// - Automatically captures target Necromantic_Rotating_Smoke.
/// - Smoothly interpolates opacity from x down to 0 over a timeline duration.
/// - Once opacity reaches 0, the smoke disappears completely with zero axis distortion or spiky stretching.
/// - On Reset or Disable, restores the rotating smoke back to full opacity.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class MiasmaDissipationVFX : MonoBehaviour
{
    [Header("=== Target Smoke VFX ===")]
    [Tooltip("Reference to the Necromantic_Rotating_Smoke effect to fade out and dissolve.")]
    [SerializeField] private RotatingSmokeVFX targetMiasmaFog;

    [Header("=== Timeline Opacity Fade Settings ===")]
    [Tooltip("Total duration in seconds for the timeline fade from x to 0.")]
    [SerializeField, Range(0.5f, 6.0f)] private float fadeDuration = 2.5f;

    [Tooltip("Starting opacity x (typically 1.0 for full smoke).")]
    [SerializeField, Range(0f, 1f)] private float startOpacity = 1.0f;

    [Tooltip("Live current opacity during timeline fade (x -> 0).")]
    [SerializeField, Range(0f, 1f)] private float currentOpacity = 1.0f;

    [Tooltip("Timeline progress (0 = start at opacity x, 1 = completed fade to 0 / disappeared).")]
    [SerializeField, Range(0f, 1f)] private float timelineProgress = 0f;

    [Tooltip("Automatically start the smooth timeline fade from x to 0 when this GameObject is enabled in the hierarchy.")]
    [SerializeField] private bool autoFadeOnEnable = true;

    [Tooltip("Smooth curve easing for the fade timeline (EaseInOut by default).")]
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Coroutine fadeRoutine;

    // Public Properties
    public RotatingSmokeVFX TargetMiasmaFog
    {
        get => targetMiasmaFog;
        set => targetMiasmaFog = value;
    }

    public float FadeDuration
    {
        get => fadeDuration;
        set => fadeDuration = Mathf.Max(0.1f, value);
    }

    public float StartOpacity
    {
        get => startOpacity;
        set => startOpacity = Mathf.Clamp01(value);
    }

    public float CurrentOpacity
    {
        get => currentOpacity;
        set
        {
            currentOpacity = Mathf.Clamp01(value);
            ApplyOpacityToTarget(currentOpacity);
        }
    }

    public float TimelineProgress
    {
        get => timelineProgress;
        set
        {
            timelineProgress = Mathf.Clamp01(value);
            float curveVal = fadeCurve != null ? fadeCurve.Evaluate(timelineProgress) : (1f - timelineProgress);
            currentOpacity = Mathf.Lerp(0f, startOpacity, curveVal);
            ApplyOpacityToTarget(currentOpacity);
        }
    }

    public bool IsDisappeared => currentOpacity <= 0.002f;

    private void Awake()
    {
        AutoFindTargetFog();
        CleanChildParticles();
    }

    private void OnEnable()
    {
        AutoFindTargetFog();
        CleanChildParticles();

        if (targetMiasmaFog != null)
        {
            startOpacity = Mathf.Max(0.1f, targetMiasmaFog.MasterOpacity);
        }
        else
        {
            startOpacity = 1.0f;
        }

        currentOpacity = startOpacity;
        timelineProgress = 0f;

        if (autoFadeOnEnable)
        {
            TriggerTimelineFade();
        }
    }

    private void OnDisable()
    {
        StopAnimation();

        // Restore target smoke to full opacity when dissipation object is disabled
        if (targetMiasmaFog != null)
        {
            targetMiasmaFog.MasterOpacity = startOpacity;
            targetMiasmaFog.Play();
        }
    }

    public void AutoFindTargetFog()
    {
        if (targetMiasmaFog == null)
        {
            targetMiasmaFog = FindFirstObjectByType<RotatingSmokeVFX>();
        }
    }

    /// <summary>
    /// Removes any legacy child particle systems on this object so it remains purely the timeline fade controller.
    /// </summary>
    public void CleanChildParticles()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (child != null)
                        UnityEditor.Undo.DestroyObjectImmediate(child.gameObject);
                };
#endif
            }
        }
    }

    public void InitializeEffect()
    {
        AutoFindTargetFog();
        CleanChildParticles();
    }

    public void Play()
    {
        TriggerTimelineFade();
    }

    public void Stop()
    {
        StopAnimation();
    }

    /// <summary>
    /// Triggers the smooth timeline fade from startOpacity (x) down to 0.0.
    /// </summary>
    public void TriggerTimelineFade() => TriggerTimelineFade(fadeDuration);

    public void TriggerTimelineFade(float duration)
    {
        AutoFindTargetFog();
        fadeDuration = Mathf.Max(0.1f, duration);
        timelineProgress = 0f;
        currentOpacity = startOpacity;

        if (targetMiasmaFog != null)
        {
            targetMiasmaFog.Play();
            targetMiasmaFog.MasterOpacity = startOpacity;
        }

        StopAnimation();

        if (Application.isPlaying)
        {
            if (gameObject.activeInHierarchy)
            {
                fadeRoutine = StartCoroutine(AnimateTimelineFade(fadeDuration));
            }
        }
        else
        {
#if UNITY_EDITOR
            TriggerEditorTimelineFade(fadeDuration);
#else
            TimelineProgress = 1.0f;
#endif
        }
    }

    /// <summary>
    /// Resets the smoke back to full opacity.
    /// </summary>
    public void ResetOpacity()
    {
        StopAnimation();
        timelineProgress = 0f;
        currentOpacity = startOpacity;

        if (targetMiasmaFog != null)
        {
            targetMiasmaFog.ResetOpacity();
        }
    }

    private void ApplyOpacityToTarget(float opacity)
    {
        if (targetMiasmaFog != null)
        {
            targetMiasmaFog.MasterOpacity = opacity;
            if (opacity <= 0.001f)
            {
                targetMiasmaFog.Stop();
            }
            else if (!targetMiasmaFog.gameObject.activeSelf)
            {
                targetMiasmaFog.gameObject.SetActive(true);
                targetMiasmaFog.Play();
            }
        }
    }

    private void StopAnimation()
    {
        if (fadeRoutine != null && Application.isPlaying)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorTimelineStep;
#endif
    }

    private IEnumerator AnimateTimelineFade(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            TimelineProgress = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        TimelineProgress = 1.0f;
        fadeRoutine = null;
    }

#if UNITY_EDITOR
    private double lastEditorTime;
    private float editorElapsed;
    private float editorDuration;

    private void TriggerEditorTimelineFade(float duration)
    {
        editorElapsed = 0f;
        editorDuration = Mathf.Max(0.05f, duration);
        lastEditorTime = UnityEditor.EditorApplication.timeSinceStartup;
        UnityEditor.EditorApplication.update -= EditorTimelineStep;
        UnityEditor.EditorApplication.update += EditorTimelineStep;
    }

    private void EditorTimelineStep()
    {
        if (this == null)
        {
            UnityEditor.EditorApplication.update -= EditorTimelineStep;
            return;
        }
        double now = UnityEditor.EditorApplication.timeSinceStartup;
        float dt = (float)(now - lastEditorTime);
        lastEditorTime = now;
        editorElapsed += dt;

        TimelineProgress = Mathf.Clamp01(editorElapsed / editorDuration);
        UnityEditor.EditorUtility.SetDirty(this);

        if (editorElapsed >= editorDuration)
        {
            UnityEditor.EditorApplication.update -= EditorTimelineStep;
            TimelineProgress = 1.0f;
        }
    }
#endif
}
