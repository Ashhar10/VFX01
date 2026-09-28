using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// VFXGlobalBloomController - Provides an easy-to-use Inspector slider to control
/// overall scene Bloom and prevent VFX from blowing out into blinding white light.
/// Works in both Edit mode and Play mode in real-time.
/// </summary>
[ExecuteAlways]
[AddComponentMenu("VFX/King Arthur/VFX Global Bloom Controller")]
public class VFXGlobalBloomController : MonoBehaviour
{
    [Header("=== Master Bloom Controls ===")]
    [Tooltip("Master Bloom Intensity slider. 0 = Off, 0.4 = Subtle, 0.65 = Cinematic (Recommended), 1.5+ = Blinding.")]
    [Range(0f, 3f)]
    [SerializeField] private float bloomIntensity = 0.65f;

    [Tooltip("How far the bloom glow scatters around glowing surfaces.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float bloomScatter = 0.65f;

    [Tooltip("HDR luminance threshold before an object starts blooming. Standard is 1.15.")]
    [Range(0.5f, 2.5f)]
    [SerializeField] private float bloomThreshold = 1.15f;

    [Header("=== Target Post-Processing Volume ===")]
    [Tooltip("Target Volume component. If left empty, will automatically locate the scene's Global Volume.")]
    [SerializeField] private Volume targetVolume;

    public float BloomIntensity
    {
        get => bloomIntensity;
        set
        {
            bloomIntensity = Mathf.Clamp(value, 0f, 3f);
            ApplyBloom();
        }
    }

    public float BloomScatter
    {
        get => bloomScatter;
        set
        {
            bloomScatter = Mathf.Clamp01(value);
            ApplyBloom();
        }
    }

    public float BloomThreshold
    {
        get => bloomThreshold;
        set
        {
            bloomThreshold = Mathf.Clamp(value, 0.5f, 2.5f);
            ApplyBloom();
        }
    }

    private void OnEnable()
    {
        FindVolumeIfNull();
        ApplyBloom();
    }

    private void Update()
    {
        // Keep synced in editor when tweaking
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ApplyBloom();
        }
        #endif
    }

    private void OnValidate()
    {
        FindVolumeIfNull();
        ApplyBloom();
    }

    private void FindVolumeIfNull()
    {
        if (targetVolume == null)
        {
            targetVolume = GetComponent<Volume>();
            if (targetVolume == null)
            {
                var volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
                foreach (var v in volumes)
                {
                    if (v.isGlobal)
                    {
                        targetVolume = v;
                        break;
                    }
                }
                if (targetVolume == null && volumes.Length > 0)
                {
                    targetVolume = volumes[0];
                }
            }
        }
    }

    /// <summary>
    /// Applies the current slider values to the URP Bloom component on the Volume Profile.
    /// </summary>
    [ContextMenu("Apply Bloom Now")]
    public void ApplyBloom()
    {
        if (targetVolume == null)
            FindVolumeIfNull();

        if (targetVolume == null) return;

        VolumeProfile profile = targetVolume.sharedProfile != null ? targetVolume.sharedProfile : targetVolume.profile;
        if (profile == null) return;

        if (profile.TryGet<Bloom>(out var bloom))
        {
            bloom.intensity.overrideState = true;
            bloom.intensity.value = bloomIntensity;

            bloom.scatter.overrideState = true;
            bloom.scatter.value = bloomScatter;

            bloom.threshold.overrideState = true;
            bloom.threshold.value = bloomThreshold;
        }
    }

    // ==========================================
    //  QUICK PRESET CONTEXT MENUS
    // ==========================================
    [ContextMenu("Preset: Subtle Bloom (0.35)")]
    public void SetSubtleBloom()
    {
        bloomIntensity = 0.35f;
        bloomScatter = 0.55f;
        ApplyBloom();
    }

    [ContextMenu("Preset: Cinematic Balanced Bloom (0.65)")]
    public void SetCinematicBloom()
    {
        bloomIntensity = 0.65f;
        bloomScatter = 0.65f;
        ApplyBloom();
    }

    [ContextMenu("Preset: Intense Radiance (1.2)")]
    public void SetIntenseBloom()
    {
        bloomIntensity = 1.2f;
        bloomScatter = 0.75f;
        ApplyBloom();
    }

    [ContextMenu("Preset: Turn Off Bloom (0.0)")]
    public void TurnOffBloom()
    {
        bloomIntensity = 0f;
        ApplyBloom();
    }
}
