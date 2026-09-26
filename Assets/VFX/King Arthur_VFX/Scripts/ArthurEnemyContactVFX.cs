using UnityEngine;

/// <summary>
/// ArthurEnemyContactVFX - High-impact sword contact effect for King Arthur.
/// Spawns a massive central starburst flash with HDR bloom at the contact point,
/// surrounded by a burst of small golden sparkles and sparks radiating outwards.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class ArthurEnemyContactVFX : MonoBehaviour
{
    [Header("=== Starburst Core (Radiant Starburst) ===")]
    [Tooltip("Primary starburst material (uses Radiant_Starburst_Spark.png).")]
    [SerializeField] private Material starburstMaterial;

    [Tooltip("Core starburst HDR color (bright white-gold with bloom).")]
    [ColorUsage(true, true)]
    [SerializeField] private Color starburstColor = new Color(2.5f, 2.1f, 1.2f, 1.0f);

    [Tooltip("Diameter of the central starburst flash (meters).")]
    [SerializeField] private float starburstSize = 2.4f;

    [Tooltip("Lifetime of the central starburst flash (seconds).")]
    [SerializeField] private float starburstLifetime = 0.35f;

    [Header("=== Outward Sparkles (Golden Sparks) ===")]
    [Tooltip("Sparkle particles material (uses Golden_Spark_Mote.png).")]
    [SerializeField] private Material sparkleMaterial;

    [Tooltip("Golden sparkle HDR color with intense bloom.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color sparkleColor = new Color(2.4f, 1.6f, 0.4f, 1.0f);

    [Tooltip("Secondary spark tint for chromatic richness.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color sparkleSecondaryColor = new Color(2.5f, 0.9f, 0.2f, 1.0f);

    [Tooltip("Number of sparkling motes fired upon contact.")]
    [SerializeField] private int sparkleCount = 38;

    [Tooltip("Initial burst velocity min/max (m/s).")]
    [SerializeField] private Vector2 sparkleSpeed = new Vector2(4.5f, 9.0f);

    [Tooltip("Size range of the small sparkles.")]
    [SerializeField] private Vector2 sparkleSize = new Vector2(0.06f, 0.16f);

    [Tooltip("Lifetime range of the small sparkles (seconds).")]
    [SerializeField] private Vector2 sparkleLifetime = new Vector2(0.35f, 0.65f);

    [Header("=== Impact Light Flash ===")]
    [SerializeField] private bool enablePointLight = true;
    [SerializeField] private Color lightColor = new Color(1.0f, 0.85f, 0.45f, 1.0f);
    [SerializeField] private float lightIntensity = 4.0f;
    [SerializeField] private float lightRange = 5.0f;

    [Header("=== Preview Controls ===")]
    [Tooltip("Automatically loop playback for testing in Scene view / Play mode.")]
    [SerializeField] private bool autoLoopPreview = true;
    [SerializeField] private float loopInterval = 1.2f;

    // Component references
    private ParticleSystem starburstPS;
    private ParticleSystem sparklesPS;
    private Light pointLight;
    private float previewTimer = 0f;
    private bool isInitialized = false;

    private const string STARBURST_MAT_PATH = "Assets/VFX/King Arthur_VFX/Materials/KingArthur_Contact_Starburst_Mat.mat";
    private const string SPARKS_MAT_PATH = "Assets/VFX/King Arthur_VFX/Materials/KingArthur_Contact_Sparks_Mat.mat";

    private void Awake()
    {
        InitializeComponents();
    }

    private void OnEnable()
    {
        InitializeComponents();
        if (autoLoopPreview)
        {
            Play();
        }
    }

    private void OnValidate()
    {
        if (!gameObject.scene.IsValid()) return;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            InitializeComponents();
            ApplySettings();
        };
#endif
    }

    [ContextMenu("Set Theme: Pink")]
    public void SetThemePink()
    {
        starburstColor = new Color(2.8f, 0.8f, 2.2f, 1.0f);
        sparkleColor = new Color(2.6f, 0.5f, 1.9f, 1.0f);
        sparkleSecondaryColor = new Color(2.5f, 0.3f, 1.5f, 1.0f);
        ApplySettings();
    }

    [ContextMenu("Set Theme: Golden")]
    public void SetThemeGolden()
    {
        starburstColor = new Color(2.5f, 2.1f, 1.2f, 1.0f);
        sparkleColor = new Color(2.4f, 1.6f, 0.4f, 1.0f);
        sparkleSecondaryColor = new Color(2.5f, 0.9f, 0.2f, 1.0f);
        ApplySettings();
    }

    private void Update()
    {
        if (autoLoopPreview)
        {
            float dt = Application.isPlaying ? Time.deltaTime : 0.016f;
            previewTimer += dt;
            if (previewTimer >= loopInterval)
            {
                previewTimer = 0f;
                Play();
            }
        }

        // Animate point light fadeout
        if (pointLight != null && pointLight.enabled)
        {
            float fadeSpeed = 12f;
            float dt = Application.isPlaying ? Time.deltaTime : 0.016f;
            pointLight.intensity = Mathf.MoveTowards(pointLight.intensity, 0f, lightIntensity * fadeSpeed * dt);
            if (pointLight.intensity <= 0.01f)
            {
                pointLight.enabled = false;
            }
        }
    }

    private void InitializeComponents()
    {
        LoadDefaultMaterials();

        // 1. Starburst Center Flash
        starburstPS = GetOrCreateChildParticleSystem("1_Starburst_Core");
        // 2. Outward Sparkles
        sparklesPS = GetOrCreateChildParticleSystem("2_Outward_Sparkles");
        // 3. Point Light
        pointLight = GetOrCreateChildLight("3_Impact_Light");

        ApplySettings();
        isInitialized = true;
    }

    private void LoadDefaultMaterials()
    {
#if UNITY_EDITOR
        if (starburstMaterial == null)
            starburstMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(STARBURST_MAT_PATH);
        if (sparkleMaterial == null)
            sparkleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(SPARKS_MAT_PATH);
#endif
    }

    public void ApplySettings()
    {
        ConfigureStarburst(starburstPS);
        ConfigureSparkles(sparklesPS);

        if (pointLight != null)
        {
            pointLight.color = lightColor;
            pointLight.range = lightRange;
            pointLight.intensity = 0f;
            pointLight.enabled = false;
        }
    }

    private void ConfigureStarburst(ParticleSystem ps)
    {
        if (ps == null) return;

        bool wasPlaying = ps.isPlaying;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1.0f;
        main.loop = false;
        main.startLifetime = starburstLifetime;
        main.startSpeed = 0f;
        main.startSize = starburstSize;
        main.startColor = starburstColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        // Size over lifetime: fast pop to max, subtle swell, then snap fade
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(new Keyframe(0f, 0.4f, 0f, 4f));
        sizeCurve.AddKey(new Keyframe(0.15f, 1.0f, 0.5f, 0.5f));
        sizeCurve.AddKey(new Keyframe(0.6f, 0.95f, -0.3f, -0.3f));
        sizeCurve.AddKey(new Keyframe(1.0f, 0.0f, -2.5f, 0f));
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Color over lifetime: bright core fade
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 0.4f),
                new GradientColorKey(starburstColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0f),
                new GradientAlphaKey(0.95f, 0.5f),
                new GradientAlphaKey(0.0f, 1f)
            }
        );
        col.color = grad;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (starburstMaterial != null)
                renderer.sharedMaterial = starburstMaterial;
        }

        if (wasPlaying && autoLoopPreview)
            ps.Play();
    }

    private void ConfigureSparkles(ParticleSystem ps)
    {
        if (ps == null) return;

        bool wasPlaying = ps.isPlaying;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1.0f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(sparkleLifetime.x, sparkleLifetime.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(sparkleSpeed.x, sparkleSpeed.y);
        main.startSize = new ParticleSystem.MinMaxCurve(sparkleSize.x, sparkleSize.y);
        main.startColor = new ParticleSystem.MinMaxGradient(sparkleColor, sparkleSecondaryColor);
        main.gravityModifier = 0.35f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)sparkleCount) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;

        // Velocity & Drag
        var limitVelocity = ps.limitVelocityOverLifetime;
        limitVelocity.enabled = true;
        limitVelocity.drag = 1.8f;

        // Size over lifetime: twinkle effect
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve twinkleCurve = new AnimationCurve();
        twinkleCurve.AddKey(0f, 0f);
        twinkleCurve.AddKey(0.12f, 1.2f);
        twinkleCurve.AddKey(0.45f, 0.7f);
        twinkleCurve.AddKey(0.7f, 1.1f);
        twinkleCurve.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, twinkleCurve);

        // Color over lifetime: fade
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(sparkleColor, 0.4f),
                new GradientColorKey(sparkleSecondaryColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0f),
                new GradientAlphaKey(1.0f, 0.7f),
                new GradientAlphaKey(0.0f, 1f)
            }
        );
        col.color = grad;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (sparkleMaterial != null)
                renderer.sharedMaterial = sparkleMaterial;
        }

        if (wasPlaying && autoLoopPreview)
            ps.Play();
    }

    /// <summary>
    /// Triggers the contact impact burst at the current transform position.
    /// </summary>
    [ContextMenu("Play Contact Impact")]
    public void Play()
    {
        if (!isInitialized) InitializeComponents();

        if (starburstPS != null)
        {
            starburstPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            starburstPS.Play();
        }

        if (sparklesPS != null)
        {
            sparklesPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sparklesPS.Play();
        }

        if (pointLight != null && enablePointLight)
        {
            pointLight.enabled = true;
            pointLight.intensity = lightIntensity;
        }
    }

    /// <summary>
    /// Teleports the impact to the given world position and plays the contact effect.
    /// </summary>
    public void PlayAt(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        Play();
    }

    [ContextMenu("Stop Contact Impact")]
    public void Stop()
    {
        if (starburstPS != null) starburstPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (sparklesPS != null) sparklesPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (pointLight != null) pointLight.enabled = false;
    }

    private ParticleSystem GetOrCreateChildParticleSystem(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }
        ParticleSystem ps = child.GetComponent<ParticleSystem>();
        if (ps == null) ps = child.gameObject.AddComponent<ParticleSystem>();
        return ps;
    }

    private Light GetOrCreateChildLight(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }
        Light l = child.GetComponent<Light>();
        if (l == null) l = child.gameObject.AddComponent<Light>();
        l.type = LightType.Point;
        return l;
    }
}
