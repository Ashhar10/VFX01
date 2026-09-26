using UnityEngine;

/// <summary>
/// ArthurSwordBloomVFX - Controls and tunes the radiant Bloom effect on King Arthur's sword VFX.
/// Manages the Glow, Particles, Trails, and optional Sword Blade mesh emission.
/// Works both in Edit Mode (via [ExecuteAlways]) and at Runtime.
/// </summary>
[ExecuteAlways]
[AddComponentMenu("VFX/King Arthur/Arthur Sword Bloom VFX")]
public class ArthurSwordBloomVFX : MonoBehaviour
{
    [Header("Bloom & Radiance Settings")]
    [Range(0.5f, 10f), Tooltip("Bloom intensity multiplier (scales colors above URP's 1.15 Bloom threshold)")]
    public float bloomIntensity = 3.5f;

    [ColorUsage(true, true), Tooltip("Color & intensity of the sword blade aura glow")]
    public Color glowColor = new Color(1.0f, 0.78f, 0.35f, 0.7f);

    [ColorUsage(true, true), Tooltip("Color & intensity of radiating sparkles")]
    public Color sparkleColor = new Color(1.0f, 0.95f, 0.65f, 1.0f);

    [ColorUsage(true, true), Tooltip("Color & intensity of the slash swing trail")]
    public Color trailColor = new Color(1.0f, 0.82f, 0.40f, 0.85f);

    [Header("Sword Blade Model Emission (Optional)")]
    [Tooltip("If enabled, the 3D sword blade mesh itself will emit an HDR bloom glow")]
    public bool enableSwordBladeBloom = false;

    [ColorUsage(true, true), Tooltip("HDR emission tint for the sword blade model")]
    public Color swordBladeColor = new Color(1.0f, 0.75f, 0.25f, 1.0f);

    [Range(0f, 5f), Tooltip("Emission intensity on the sword blade")]
    public float swordBladeBloomIntensity = 2.0f;

    [Header("Particle Systems (Auto-Linked)")]
    public ParticleSystem glowPS;
    public ParticleSystem particlesPS;
    public ParticleSystem trailsPS;
    public Renderer swordRenderer;

    [Header("Materials (Optional Overrides)")]
    public Material glowMaterial;
    public Material particlesMaterial;
    public Material trailsMaterial;

    private void Awake()
    {
        AutoLinkComponents();
        ApplyBloomSettings();
    }

    private void OnEnable()
    {
        AutoLinkComponents();
        ApplyBloomSettings();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoLinkComponents();
        ApplyBloomSettings();
    }
#endif

    [ContextMenu("Set Theme: Pink")]
    public void SetThemePink()
    {
        glowColor = new Color(1.0f, 0.35f, 0.8f, 0.7f);
        sparkleColor = new Color(1.0f, 0.65f, 0.95f, 1.0f);
        trailColor = new Color(1.0f, 0.4f, 0.85f, 0.85f);
        swordBladeColor = new Color(1.0f, 0.3f, 0.75f, 1.0f);
        ApplyBloomSettings();
    }

    [ContextMenu("Set Theme: Golden")]
    public void SetThemeGolden()
    {
        glowColor = new Color(1.0f, 0.78f, 0.35f, 0.7f);
        sparkleColor = new Color(1.0f, 0.95f, 0.65f, 1.0f);
        trailColor = new Color(1.0f, 0.82f, 0.40f, 0.85f);
        swordBladeColor = new Color(1.0f, 0.75f, 0.25f, 1.0f);
        ApplyBloomSettings();
    }

    /// <summary>
    /// Finds and connects the child ParticleSystems and parent sword mesh.
    /// </summary>
    public void AutoLinkComponents()
    {
        if (glowPS == null)
        {
            Transform t = transform.Find("Glow");
            if (t != null) glowPS = t.GetComponent<ParticleSystem>();
        }

        if (particlesPS == null)
        {
            Transform t = transform.Find("Particles");
            if (t != null) particlesPS = t.GetComponent<ParticleSystem>();
        }

        if (trailsPS == null)
        {
            Transform t = transform.Find("Trails");
            if (t != null) trailsPS = t.GetComponent<ParticleSystem>();
        }

        if (swordRenderer == null)
        {
            // Search in parent or siblings for the sword mesh renderer
            if (transform.parent != null)
            {
                Transform blade = transform.parent.Find("default");
                if (blade == null) blade = transform.parent.Find("Sword_Cylinder");
                if (blade != null) swordRenderer = blade.GetComponent<Renderer>();
                if (swordRenderer == null) swordRenderer = transform.parent.GetComponentInChildren<MeshRenderer>();
            }
        }

#if UNITY_EDITOR
        // Auto-assign default King Arthur materials if not set
        if (glowMaterial == null)
        {
            glowMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/King Arthur_VFX/Materials/KingArthur_Sword_Glow_Mat.mat");
        }
        if (particlesMaterial == null)
        {
            particlesMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/King Arthur_VFX/Materials/KingArthur_Sword_Particles_Mat.mat");
        }
        if (trailsMaterial == null)
        {
            trailsMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/King Arthur_VFX/Materials/KingArthur_Sword_Trails_Mat.mat");
        }
#endif
    }

    /// <summary>
    /// Applies HDR bloom colors and materials to all components.
    /// </summary>
    [ContextMenu("Apply Bloom Now")]
    public void ApplyBloomSettings()
    {
        // 1. Calculate HDR Colors (multiplying RGB to exceed Bloom threshold of ~1.15)
        Color hdrGlow = new Color(
            glowColor.r * bloomIntensity,
            glowColor.g * bloomIntensity,
            glowColor.b * bloomIntensity,
            glowColor.a
        );

        Color hdrSparkle = new Color(
            sparkleColor.r * bloomIntensity,
            sparkleColor.g * bloomIntensity,
            sparkleColor.b * bloomIntensity,
            sparkleColor.a
        );

        Color hdrTrail = new Color(
            trailColor.r * bloomIntensity,
            trailColor.g * bloomIntensity,
            trailColor.b * bloomIntensity,
            trailColor.a
        );

        // 2. Configure Glow Particle System
        if (glowPS != null)
        {
            var main = glowPS.main;
            main.startColor = hdrGlow;

            var psRenderer = glowPS.GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null && glowMaterial != null)
            {
                psRenderer.sharedMaterial = glowMaterial;
            }
        }

        // 3. Configure Particles (Sparkles) Particle System
        if (particlesPS != null)
        {
            var main = particlesPS.main;
            main.startColor = hdrSparkle;

            var psRenderer = particlesPS.GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null && particlesMaterial != null)
            {
                psRenderer.sharedMaterial = particlesMaterial;
            }
        }

        // 4. Configure Trails Particle System
        if (trailsPS != null)
        {
            var main = trailsPS.main;
            main.startColor = hdrTrail;

            var trailsModule = trailsPS.trails;
            if (trailsModule.enabled)
            {
                trailsModule.colorOverLifetime = hdrTrail;
            }

            var psRenderer = trailsPS.GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null)
            {
                if (trailsMaterial != null)
                {
                    psRenderer.trailMaterial = trailsMaterial;
                }
                if (glowMaterial != null)
                {
                    psRenderer.sharedMaterial = glowMaterial;
                }
            }
        }

        // 5. Update Material Emission Colors directly
        if (glowMaterial != null)
        {
            glowMaterial.SetColor("_EmissionColor", hdrGlow);
            glowMaterial.SetColor("_BaseColor", glowColor);
        }

        if (particlesMaterial != null)
        {
            particlesMaterial.SetColor("_EmissionColor", hdrSparkle);
            particlesMaterial.SetColor("_BaseColor", sparkleColor);
        }

        if (trailsMaterial != null)
        {
            trailsMaterial.SetColor("_EmissionColor", hdrTrail);
            trailsMaterial.SetColor("_BaseColor", trailColor);
        }

        // 6. Optional: Sword Blade Model Emission
        if (swordRenderer != null)
        {
            Material mat = Application.isPlaying ? swordRenderer.material : swordRenderer.sharedMaterial;
            if (mat != null)
            {
                if (enableSwordBladeBloom)
                {
                    mat.EnableKeyword("_EMISSION");
                    Color hdrBlade = new Color(
                        swordBladeColor.r * swordBladeBloomIntensity,
                        swordBladeColor.g * swordBladeBloomIntensity,
                        swordBladeColor.b * swordBladeBloomIntensity,
                        1f
                    );
                    mat.SetColor("_EmissionColor", hdrBlade);
                }
                else
                {
                    mat.SetColor("_EmissionColor", Color.black);
                }
            }
        }
    }
}
