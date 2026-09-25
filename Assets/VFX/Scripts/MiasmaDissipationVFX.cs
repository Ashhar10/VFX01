using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// MiasmaDissipationVFX - Dedicated Stage [4] Dissipation VFX for Alura Graves.
/// 
/// Core Behavior:
/// 1. 100% mirrors all visual settings from Necromantic_Rotating_Smoke (Bone White & Lavender
///    volumetric clouds, wisps, floor mist, soul embers, star motes, materials, radius, and speeds).
/// 2. "Traffic Circle / Roundabout Flow to +Z":
///    - Particles situated on any concentric radial row circle around the central island (Orbital Y swirl).
///    - As they orbit, they are funnelled smoothly down the exit ramp into a powerful +Z axis blowout stream.
///    - Velocity-stretched aerodynamic tails (Stretch Render Mode) deform particles along their flow vector.
///    - Smoke billows expand and fade away to 0 opacity over distance and lifetime.
/// 3. Independent & Non-Destructive:
///    - Necromantic_Rotating_Smoke is untouched and remains the continuous in-place rotating smoke.
///    - When Miasma_Fog_Dissipation is enabled (or triggered), it cleanly takes over the smoke and blows it out.
///    - On Reset or Disable, Necromantic_Rotating_Smoke is seamlessly restored.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class MiasmaDissipationVFX : MonoBehaviour
{
    // ==========================================
    //  TARGET ROTATING SMOKE & SYNC
    // ==========================================
    [Header("=== Target Rotating Smoke Reference & Sync ===")]
    [Tooltip("Reference to the main in-place Necromantic_Rotating_Smoke. If assigned, settings are synced automatically.")]
    [SerializeField] private RotatingSmokeVFX targetMiasmaFog;

    [Tooltip("Automatically copy/sync all colors, materials, dimensions, and motes from Necromantic_Rotating_Smoke on enable.")]
    [SerializeField] private bool autoSyncOnEnable = true;

    [Tooltip("Temporarily hide/deactivate the in-place Necromantic_Rotating_Smoke while the blowout is active.")]
    [SerializeField] private bool hideTargetSmokeOnEnable = true;

    // ==========================================
    //  TRAFFIC CIRCLE ROUNDABOUT +Z BLOWOUT
    // ==========================================
    [Header("=== Traffic Circle Roundabout +Z Blowout ===")]
    [Tooltip("Forward blowout speed propelling particles down the +Z axis (m/s).")]
    [SerializeField, Range(1.0f, 20.0f)] private float zBlowoutSpeed = 6.5f;

    [Tooltip("Roundabout swirl speed (degrees/sec) making particles orbit around the traffic circle before exiting down Z.")]
    [SerializeField, Range(10f, 300f)] private float roundaboutSwirlSpeed = 90f;

    public enum BlowoutDirectionMode
    {
        Forward_Z,             // Standard local/world +Z Axis
        Diagonal_Z_RedArrow,  // Diagonal forward-right (+Z / +X) matching design guide red arrow
        Custom_Direction       // Custom 3D vector
    }

    [Tooltip("Direction along which the blowout exit stream travels.")]
    [SerializeField] private BlowoutDirectionMode directionMode = BlowoutDirectionMode.Forward_Z;

    [Tooltip("Custom blowout direction vector (when Direction Mode is set to Custom).")]
    [SerializeField] private Vector3 customDirection = new Vector3(0.35f, 0.05f, 1.0f).normalized;

    [Tooltip("Coordinate space for blowout velocity (Local allows rotating the GameObject to aim the blowout).")]
    [SerializeField] private ParticleSystemSimulationSpace blowoutSpace = ParticleSystemSimulationSpace.Local;

    // ==========================================
    //  AERODYNAMIC TAIL DEFORMATION
    // ==========================================
    [Header("=== Aerodynamic Tail Deformation ===")]
    [Tooltip("Enable velocity stretching to turn cloud puffs into sleek aerodynamic trailing smoke tails.")]
    [SerializeField] private bool enableTailDeformation = true;

    [Tooltip("Velocity scale multiplier stretching particles along their motion vector (Stretch Render Mode).")]
    [SerializeField, Range(0.5f, 5.0f)] private float tailDeformationScale = 2.2f;

    [Tooltip("Base elongation factor along travel direction.")]
    [SerializeField, Range(0.5f, 4.0f)] private float tailLengthScale = 1.6f;

    [Tooltip("Lateral dispersion spreading the blowout plume slightly outward as it travels down Z.")]
    [SerializeField, Range(0.0f, 3.0f)] private float lateralDispersion = 0.8f;

    // ==========================================
    //  DISSIPATION CONTROLLER & LIFETIME
    // ==========================================
    [Header("=== Dissipation Animation & Duration ===")]
    [Tooltip("Dissipation progress (0 = start of blowout, 1 = smoke completely dissipated / board clear).")]
    [SerializeField, Range(0f, 1f)] private float dissipationProgress = 0f;

    [Tooltip("Total duration in seconds for the blowout dissipation sequence.")]
    [SerializeField, Range(0.5f, 8.0f)] private float dissipationDuration = 3.0f;

    [Tooltip("Continuous looping blowout (for visual inspection/tuning). If false, plays once and clears.")]
    [SerializeField] private bool loopBlowout = true;

    // ==========================================
    //  VORTEX GEOMETRY & SPEEDS (SYNCED)
    // ==========================================
    [Header("=== Vortex Geometry & Cloud Scale ===")]
    [SerializeField, Range(0.5f, 5.0f)] private float vortexRadius = 1.7f;
    [SerializeField, Range(0.5f, 5.0f)] private float vortexHeight = 2.2f;
    [SerializeField, Range(0.05f, 2.0f)] private float upwardSpeed = 0.35f;
    [SerializeField, Range(0.3f, 2.5f)] private float particleScale = 1.0f;
    [SerializeField, Range(0.1f, 2.5f)] private float densityMultiplier = 1.0f;

    // ==========================================
    //  COLOR PALETTE (BONE WHITE & LAVENDER)
    // ==========================================
    [Header("=== Color Palette (Bone White & Lavender) ===")]
    [SerializeField] private Color boneWhiteColor = new Color(0.96f, 0.93f, 0.88f, 1.0f);
    [SerializeField] private Color midtoneViolet = new Color(0.75f, 0.60f, 0.90f, 1.0f);
    [SerializeField] private Color deepCharcoalPurple = new Color(0.85f, 0.78f, 0.92f, 1.0f);
    [SerializeField] private Color rimHighlightLilac = new Color(0.92f, 0.86f, 0.98f, 1.0f);
    [SerializeField] private Color groundMistColor = new Color(0.90f, 0.87f, 0.85f, 1.0f);

    // ==========================================
    //  SPARKLE CONTROLS (SYNCED)
    // ==========================================
    [Header("=== Sparkling Energy Motes ===")]
    [SerializeField] private bool enableSparkles = true;
    [SerializeField, Range(0f, 20f)] private float sparkleQuantity = 5.0f;
    [SerializeField, Range(0.01f, 0.08f)] private float sparkleSize = 0.032f;
    [SerializeField, Range(0f, 6.0f)] private float sparkleBloomIntensity = 2.5f;

    // ==========================================
    //  LAYER TOGGLES (5 EXACT MATCHING LAYERS)
    // ==========================================
    [Header("=== Layer Toggles ===")]
    [SerializeField] private bool enableBackdropFog = true;
    [SerializeField] private bool enableVortexWisps = true;
    [SerializeField] private bool enableGroundMist = true;
    [SerializeField] private bool enableSoulOrbs = true;
    [SerializeField] private bool enableDiamondSparkles = true;
    [SerializeField] private bool enableCrossSparkles = true;

    // ==========================================
    //  MATERIAL REFERENCES
    // ==========================================
    [Header("=== Materials & Textures ===")]
    [SerializeField] private Material smokePlumeMaterial;
    [SerializeField] private Material smokeWispMaterial;
    [SerializeField] private Material soulOrbMaterial;
    [SerializeField] private Material diamondSparkleMaterial;
    [SerializeField] private Material crossSparkleMaterial;

    // Child Particle Systems (The 5 Matching Layers)
    private ParticleSystem backdropFogPS;
    private ParticleSystem vortexWispsPS;
    private ParticleSystem groundMistPS;
    private ParticleSystem soulOrbsPS;
    private ParticleSystem diamondSparklesPS;
    private ParticleSystem crossSparklesPS;

    // Coroutine tracking
    private Coroutine dissipationRoutine;
    private bool wasTargetActiveBeforeBlowout = false;

    // ==========================================
    //  PUBLIC PROPERTIES
    // ==========================================
    public RotatingSmokeVFX TargetMiasmaFog
    {
        get => targetMiasmaFog;
        set => targetMiasmaFog = value;
    }

    public float ZBlowoutSpeed
    {
        get => zBlowoutSpeed;
        set { zBlowoutSpeed = Mathf.Max(0.1f, value); UpdateAllLayers(); }
    }

    public float RoundaboutSwirlSpeed
    {
        get => roundaboutSwirlSpeed;
        set { roundaboutSwirlSpeed = value; UpdateAllLayers(); }
    }

    public bool EnableTailDeformation
    {
        get => enableTailDeformation;
        set { enableTailDeformation = value; UpdateAllLayers(); }
    }

    public float TailDeformationScale
    {
        get => tailDeformationScale;
        set { tailDeformationScale = Mathf.Max(0.1f, value); UpdateAllLayers(); }
    }

    public float TailLengthScale
    {
        get => tailLengthScale;
        set { tailLengthScale = Mathf.Max(0.1f, value); UpdateAllLayers(); }
    }

    public float LateralDispersion
    {
        get => lateralDispersion;
        set { lateralDispersion = Mathf.Max(0f, value); UpdateAllLayers(); }
    }

    public BlowoutDirectionMode DirectionMode
    {
        get => directionMode;
        set { directionMode = value; UpdateAllLayers(); }
    }

    public Vector3 CustomDirection
    {
        get => customDirection;
        set { customDirection = value; UpdateAllLayers(); }
    }

    public Vector3 EffectiveDirection
    {
        get
        {
            switch (directionMode)
            {
                case BlowoutDirectionMode.Forward_Z:
                    return Vector3.forward;
                case BlowoutDirectionMode.Diagonal_Z_RedArrow:
                    return new Vector3(0.35f, 0.05f, 1.0f).normalized;
                case BlowoutDirectionMode.Custom_Direction:
                    return customDirection.sqrMagnitude > 0.001f ? customDirection.normalized : Vector3.forward;
                default:
                    return Vector3.forward;
            }
        }
    }

    public ParticleSystemSimulationSpace BlowoutSpace
    {
        get => blowoutSpace;
        set { blowoutSpace = value; UpdateAllLayers(); }
    }

    public float DissipationProgress
    {
        get => dissipationProgress;
        set
        {
            dissipationProgress = Mathf.Clamp01(value);
            UpdateAllLayers();
        }
    }

    public float DissipationDuration
    {
        get => dissipationDuration;
        set => dissipationDuration = Mathf.Max(0.1f, value);
    }

    public bool LoopBlowout
    {
        get => loopBlowout;
        set { loopBlowout = value; UpdateAllLayers(); }
    }

    public bool HideTargetSmokeOnEnable
    {
        get => hideTargetSmokeOnEnable;
        set => hideTargetSmokeOnEnable = value;
    }

    // ==========================================
    //  LIFECYCLE
    // ==========================================
    private void Awake()
    {
        AutoFindMaterials();
        AutoFindTargetFog();
        InitializeEffect();
    }

    private void OnEnable()
    {
        AutoFindMaterials();
        AutoFindTargetFog();

        if (autoSyncOnEnable && targetMiasmaFog != null)
        {
            SyncFromRotatingSmoke(targetMiasmaFog);
        }
        else
        {
            InitializeEffect();
        }

        if (hideTargetSmokeOnEnable && targetMiasmaFog != null)
        {
            wasTargetActiveBeforeBlowout = targetMiasmaFog.gameObject.activeSelf;
            if (wasTargetActiveBeforeBlowout)
            {
                targetMiasmaFog.gameObject.SetActive(false);
            }
        }

        Play();
    }

    private void OnDisable()
    {
        Stop();

        // Restore target in-place smoke if it was hidden
        if (hideTargetSmokeOnEnable && targetMiasmaFog != null && wasTargetActiveBeforeBlowout)
        {
            targetMiasmaFog.gameObject.SetActive(true);
            targetMiasmaFog.Play();
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!gameObject.scene.IsValid()) return;
        UnityEditor.EditorApplication.delayCall -= DelayedUpdate;
        UnityEditor.EditorApplication.delayCall += DelayedUpdate;
#endif
    }

#if UNITY_EDITOR
    private void DelayedUpdate()
    {
        if (this == null || !gameObject.scene.IsValid()) return;
        UpdateAllLayers();
    }
#endif

    public void AutoFindTargetFog()
    {
        if (targetMiasmaFog == null)
        {
            targetMiasmaFog = FindFirstObjectByType<RotatingSmokeVFX>();
        }
    }

    public void AutoFindMaterials()
    {
#if UNITY_EDITOR
        if (smokePlumeMaterial == null)
            smokePlumeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/NecromanticSmokeMat.mat");
        if (smokeWispMaterial == null)
            smokeWispMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/NecromanticWispMat.mat");
        if (soulOrbMaterial == null)
            soulOrbMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaSoulOrbMat.mat");
        if (diamondSparkleMaterial == null)
            diamondSparkleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaSparkleDiamondMat.mat");
        if (crossSparkleMaterial == null)
            crossSparkleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaSparkleCrossMat.mat");
#endif
    }

    /// <summary>
    /// Copies all visual settings, colors, dimensions, motes, and materials from the source RotatingSmokeVFX.
    /// </summary>
    public void SyncFromRotatingSmoke(RotatingSmokeVFX source = null)
    {
        if (source == null) source = targetMiasmaFog;
        if (source == null) AutoFindTargetFog();
        if (source == null) return;

        vortexRadius = source.VortexRadius;
        vortexHeight = source.VortexHeight;
        roundaboutSwirlSpeed = Mathf.Abs(source.RotationSpeed) > 10f ? source.RotationSpeed * 1.2f : 90f;
        upwardSpeed = source.UpwardSpeed;
        particleScale = source.ParticleScale;
        densityMultiplier = source.DensityMultiplier;

        boneWhiteColor = source.BoneWhiteColor;
        midtoneViolet = source.MidtoneViolet;
        deepCharcoalPurple = source.DeepCharcoalPurple;
        rimHighlightLilac = source.RimHighlightLilac;
        groundMistColor = source.GroundMistColor;

        sparkleQuantity = source.SparkleQuantity;
        sparkleSize = source.SparkleSize;
        sparkleBloomIntensity = source.SparkleBloomIntensity;

        if (source.SmokePlumeMaterial != null) smokePlumeMaterial = source.SmokePlumeMaterial;
        if (source.SmokeWispMaterial != null) smokeWispMaterial = source.SmokeWispMaterial;
        if (source.SoulOrbMaterial != null) soulOrbMaterial = source.SoulOrbMaterial;
        if (source.DiamondSparkleMaterial != null) diamondSparkleMaterial = source.DiamondSparkleMaterial;
        if (source.CrossSparkleMaterial != null) crossSparkleMaterial = source.CrossSparkleMaterial;

        InitializeEffect();
    }

    // ==========================================
    //  INITIALIZATION
    // ==========================================
    public void InitializeEffect()
    {
        CleanLegacyChildren();
        FetchChildReferences();

        // Layer 1: Backdrop Billowing Fog
        backdropFogPS = GetOrCreateChildPS("1_Backdrop_Billowing_Fog");

        // Layer 2: Swirling Vortex Wisps
        vortexWispsPS = GetOrCreateChildPS("2_Swirling_Vortex_Wisps");

        // Layer 3: Ground Creeping Mist
        groundMistPS = GetOrCreateChildPS("3_Ground_Creeping_Mist");

        // Layer 4: Floating Soul Orbs
        soulOrbsPS = GetOrCreateChildPS("4_Floating_Soul_Orbs");

        // Layer 5A: Pure Geometric Diamond Star Sparkles
        diamondSparklesPS = GetOrCreateChildPS("5a_Diamond_Sparkles");

        // Layer 5B: Pure Geometric Greek Cross Sparkles
        crossSparklesPS = GetOrCreateChildPS("5b_Cross_Sparkles");

        UpdateAllLayers();
    }

    private void CleanLegacyChildren()
    {
        string[] legacyNames = {
            "1_Ground_Diffusion_Mist",
            "2_Evaporation_Wisps",
            "3_Cleansing_Floor_Ripple",
            "4a_Diamond_Sparkles",
            "4b_Cross_Sparkles",
            "5_Sparkling_Energy_Motes"
        };

        foreach (string legacy in legacyNames)
        {
            Transform t = transform.Find(legacy);
            if (t != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(t.gameObject);
                }
                else
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        if (t != null) UnityEditor.Undo.DestroyObjectImmediate(t.gameObject);
                    };
#endif
                }
            }
        }
    }

    private void FetchChildReferences()
    {
        if (backdropFogPS == null) backdropFogPS = transform.Find("1_Backdrop_Billowing_Fog")?.GetComponent<ParticleSystem>();
        if (vortexWispsPS == null) vortexWispsPS = transform.Find("2_Swirling_Vortex_Wisps")?.GetComponent<ParticleSystem>();
        if (groundMistPS == null) groundMistPS = transform.Find("3_Ground_Creeping_Mist")?.GetComponent<ParticleSystem>();
        if (soulOrbsPS == null) soulOrbsPS = transform.Find("4_Floating_Soul_Orbs")?.GetComponent<ParticleSystem>();
        if (diamondSparklesPS == null) diamondSparklesPS = transform.Find("5a_Diamond_Sparkles")?.GetComponent<ParticleSystem>();
        if (crossSparklesPS == null) crossSparklesPS = transform.Find("5b_Cross_Sparkles")?.GetComponent<ParticleSystem>();
    }

    private ParticleSystem GetOrCreateChildPS(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName);
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create " + childName);
#endif
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            return ps;
        }
        return child.GetComponent<ParticleSystem>();
    }

    // ==========================================
    //  UPDATE ALL LAYERS
    // ==========================================
    public void UpdateAllLayers()
    {
        FetchChildReferences();

        float alphaMult = Mathf.Clamp01(1f - dissipationProgress * 1.05f);
        bool isFullClear = dissipationProgress >= 0.99f;

        // 1. Backdrop Billowing Fog
        if (backdropFogPS != null)
        {
            bool active = enableBackdropFog && alphaMult > 0.005f && !isFullClear;
            backdropFogPS.gameObject.SetActive(active);
            if (active) ConfigureBackdropFog(backdropFogPS, alphaMult);
            else if (isFullClear) backdropFogPS.Clear();
        }

        // 2. Swirling Vortex Wisps
        if (vortexWispsPS != null)
        {
            bool active = enableVortexWisps && alphaMult > 0.005f && !isFullClear;
            vortexWispsPS.gameObject.SetActive(active);
            if (active) ConfigureVortexWisps(vortexWispsPS, alphaMult);
            else if (isFullClear) vortexWispsPS.Clear();
        }

        // 3. Ground Creeping Mist
        if (groundMistPS != null)
        {
            bool active = enableGroundMist && alphaMult > 0.005f && !isFullClear;
            groundMistPS.gameObject.SetActive(active);
            if (active) ConfigureGroundMist(groundMistPS, alphaMult);
            else if (isFullClear) groundMistPS.Clear();
        }

        // 4. Floating Soul Orbs
        if (soulOrbsPS != null)
        {
            bool active = enableSoulOrbs && alphaMult > 0.005f && !isFullClear;
            soulOrbsPS.gameObject.SetActive(active);
            if (active) ConfigureSoulOrbs(soulOrbsPS, alphaMult);
            else if (isFullClear) soulOrbsPS.Clear();
        }

        // 5a. Diamond Star Sparkles
        if (diamondSparklesPS != null)
        {
            bool active = enableSparkles && enableDiamondSparkles && sparkleQuantity > 0.001f && alphaMult > 0.005f && !isFullClear;
            diamondSparklesPS.gameObject.SetActive(active);
            if (active) ConfigureDiamondSparkles(diamondSparklesPS, alphaMult);
            else if (isFullClear) diamondSparklesPS.Clear();
        }

        // 5b. Cross Sparkles
        if (crossSparklesPS != null)
        {
            bool active = enableSparkles && enableCrossSparkles && sparkleQuantity > 0.001f && alphaMult > 0.005f && !isFullClear;
            crossSparklesPS.gameObject.SetActive(active);
            if (active) ConfigureCrossSparkles(crossSparklesPS, alphaMult);
            else if (isFullClear) crossSparklesPS.Clear();
        }
    }

    // ==========================================
    //  LAYER 1: BACKDROP BILLOWING FOG (+Z BLOWOUT)
    // ==========================================
    private void ConfigureBackdropFog(ParticleSystem ps, float alphaMult)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = loopBlowout;
        main.playOnAwake = true;
        main.simulationSpace = blowoutSpace;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(2.2f * particleScale, 3.4f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 70;

        var emission = ps.emission;
        emission.rateOverTime = dissipationProgress >= 0.85f ? 0f : (16f * densityMultiplier * alphaMult);

        // Cylinder/Cone emitter covering concentric rows
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.length = Mathf.Max(0.1f, vortexHeight * 1.1f);
        shape.radius = vortexRadius * 1.1f;
        shape.radiusThickness = 0.8f;
        shape.position = new Vector3(0f, 0.2f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        // TRAFFIC CIRCLE FLOW: Swirl around Y while blowing out along +Z
        Vector3 dir = EffectiveDirection;
        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = blowoutSpace;
        vol.x = dir.x * zBlowoutSpeed;
        vol.y = dir.y * zBlowoutSpeed + upwardSpeed * 0.4f;
        vol.z = dir.z * zBlowoutSpeed;
        vol.orbitalY = roundaboutSwirlSpeed * Mathf.Deg2Rad * 0.75f; // Traffic circle roundabout swirl
        vol.radial = lateralDispersion * 0.35f;

        // Billowing expansion as clouds travel down Z
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.6f);
        sc.AddKey(0.3f, 1.0f);
        sc.AddKey(0.7f, 1.45f);
        sc.AddKey(1.0f, 1.8f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        // Bone White into Lavender with alpha fade to 0 at the tail
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(boneWhiteColor, 0.0f),
                new GradientColorKey(midtoneViolet, 0.35f),
                new GradientColorKey(deepCharcoalPurple, 0.70f),
                new GradientColorKey(rimHighlightLilac, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.70f * densityMultiplier * alphaMult, 0.15f),
                new GradientAlphaKey(0.60f * densityMultiplier * alphaMult, 0.60f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 0.4f;
        noise.scrollSpeed = 0.3f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (enableTailDeformation)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = tailDeformationScale;
            renderer.lengthScale = tailLengthScale;
        }
        else
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
        renderer.material = smokePlumeMaterial;
        renderer.sortingOrder = 1;
    }

    // ==========================================
    //  LAYER 2: SWIRLING VORTEX WISPS (+Z BLOWOUT)
    // ==========================================
    private void ConfigureVortexWisps(ParticleSystem ps, float alphaMult)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = loopBlowout;
        main.playOnAwake = true;
        main.simulationSpace = blowoutSpace;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.4f * particleScale, 2.2f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 50;

        var emission = ps.emission;
        emission.rateOverTime = dissipationProgress >= 0.85f ? 0f : (14f * densityMultiplier * alphaMult);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = vortexRadius * 0.95f;
        shape.donutRadius = 0.4f;
        shape.position = new Vector3(0f, vortexHeight * 0.35f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        // TRAFFIC CIRCLE FLOW: Fast spiraling wisps exiting down Z
        Vector3 dir = EffectiveDirection;
        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = blowoutSpace;
        vol.x = dir.x * (zBlowoutSpeed * 1.25f);
        vol.y = dir.y * (zBlowoutSpeed * 1.25f) + upwardSpeed * 0.5f;
        vol.z = dir.z * (zBlowoutSpeed * 1.25f);
        vol.orbitalY = roundaboutSwirlSpeed * Mathf.Deg2Rad * 1.6f; // High-speed vortex swirl
        vol.radial = lateralDispersion * 0.4f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.45f);
        sc.AddKey(0.35f, 1.0f);
        sc.AddKey(0.70f, 1.30f);
        sc.AddKey(1.0f, 0.30f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(boneWhiteColor, 0.0f),
                new GradientColorKey(midtoneViolet, 0.40f),
                new GradientColorKey(rimHighlightLilac, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.65f * densityMultiplier * alphaMult, 0.18f),
                new GradientAlphaKey(0.55f * densityMultiplier * alphaMult, 0.65f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.35f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (enableTailDeformation)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = tailDeformationScale * 1.3f;
            renderer.lengthScale = tailLengthScale * 1.2f;
        }
        else
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
        renderer.material = smokeWispMaterial != null ? smokeWispMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 3;
    }

    // ==========================================
    //  LAYER 3: GROUND CREEPING MIST (+Z BLOWOUT)
    // ==========================================
    private void ConfigureGroundMist(ParticleSystem ps, float alphaMult)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = loopBlowout;
        main.playOnAwake = true;
        main.simulationSpace = blowoutSpace;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(2.0f * particleScale, 3.6f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 40;

        var emission = ps.emission;
        emission.rateOverTime = dissipationProgress >= 0.85f ? 0f : (12f * densityMultiplier * alphaMult);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = vortexRadius * 1.35f;
        shape.radiusThickness = 0.85f;
        shape.position = new Vector3(0f, 0.05f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        // Sweeping along floor tiles down +Z
        Vector3 dir = EffectiveDirection;
        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = blowoutSpace;
        vol.x = dir.x * (zBlowoutSpeed * 0.9f);
        vol.y = dir.y * (zBlowoutSpeed * 0.9f) + 0.02f;
        vol.z = dir.z * (zBlowoutSpeed * 0.9f);
        vol.orbitalY = roundaboutSwirlSpeed * Mathf.Deg2Rad * 0.5f;
        vol.radial = 0.15f + lateralDispersion * 0.45f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.5f);
        sc.AddKey(0.5f, 1.0f);
        sc.AddKey(1.0f, 1.5f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(groundMistColor, 0.0f),
                new GradientColorKey(midtoneViolet, 0.45f),
                new GradientColorKey(boneWhiteColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.60f * densityMultiplier * alphaMult, 0.20f),
                new GradientAlphaKey(0.50f * densityMultiplier * alphaMult, 0.70f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (enableTailDeformation)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = tailDeformationScale * 0.85f;
            renderer.lengthScale = tailLengthScale;
        }
        else
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
        renderer.material = smokePlumeMaterial;
        renderer.sortingOrder = 0;
    }

    // ==========================================
    //  LAYER 4: FLOATING SOUL ORBS (+Z BLOWOUT)
    // ==========================================
    private void ConfigureSoulOrbs(ParticleSystem ps, float alphaMult)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = loopBlowout;
        main.playOnAwake = true;
        main.simulationSpace = blowoutSpace;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f * particleScale, 0.9f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = Color.white;
        main.maxParticles = 8;

        var emission = ps.emission;
        emission.rateOverTime = dissipationProgress >= 0.85f ? 0f : (2.0f * alphaMult);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = vortexRadius * 0.9f;
        shape.position = new Vector3(0f, vortexHeight * 0.6f, 0f);

        Vector3 dir = EffectiveDirection;
        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = blowoutSpace;
        vol.x = dir.x * (zBlowoutSpeed * 1.1f);
        vol.y = dir.y * (zBlowoutSpeed * 1.1f) + upwardSpeed * 0.3f;
        vol.z = dir.z * (zBlowoutSpeed * 1.1f);
        vol.orbitalY = roundaboutSwirlSpeed * Mathf.Deg2Rad * 0.6f;
        vol.radial = lateralDispersion * 0.25f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.3f);
        sc.AddKey(0.3f, 1.0f);
        sc.AddKey(0.7f, 1.0f);
        sc.AddKey(1.0f, 0.1f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(boneWhiteColor, 0.0f),
                new GradientColorKey(midtoneViolet, 0.50f),
                new GradientColorKey(boneWhiteColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.85f * alphaMult, 0.20f),
                new GradientAlphaKey(0.75f * alphaMult, 0.70f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.velocityScale = 0f;
        renderer.lengthScale = 1f;
        renderer.material = soulOrbMaterial != null ? soulOrbMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 4;
    }

    // ==========================================
    //  LAYER 5A: DIAMOND STAR SPARKLES (+Z BLOWOUT)
    // ==========================================
    private void ConfigureDiamondSparkles(ParticleSystem ps, float alphaMult)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = loopBlowout;
        main.playOnAwake = true;
        main.simulationSpace = blowoutSpace;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        float minS = Mathf.Max(0.005f, sparkleSize * 0.8f * particleScale);
        float maxS = Mathf.Max(minS, sparkleSize * 1.25f * particleScale);
        main.startSize = new ParticleSystem.MinMaxCurve(minS, maxS);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        Color hdrBoneWhite = new Color(boneWhiteColor.r * sparkleBloomIntensity, boneWhiteColor.g * sparkleBloomIntensity, boneWhiteColor.b * sparkleBloomIntensity, 1f);
        Color hdrLavender = new Color(midtoneViolet.r * sparkleBloomIntensity, midtoneViolet.g * sparkleBloomIntensity, midtoneViolet.b * sparkleBloomIntensity, 1f);
        main.startColor = new ParticleSystem.MinMaxGradient(hdrBoneWhite, hdrLavender);
        main.maxParticles = Mathf.Max(4, Mathf.CeilToInt(sparkleQuantity * 3f));

        var tsa = ps.textureSheetAnimation;
        tsa.enabled = false;

        var emission = ps.emission;
        emission.rateOverTime = (sparkleQuantity <= 0.001f || !enableSparkles || dissipationProgress >= 0.85f) ? 0f : (sparkleQuantity * 0.55f * densityMultiplier * alphaMult);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.radius = vortexRadius * 0.95f;
        shape.radiusThickness = 0.85f;
        shape.length = Mathf.Max(0.1f, vortexHeight * 1.0f);
        shape.position = new Vector3(0f, 0.20f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        Vector3 dir = EffectiveDirection;
        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = blowoutSpace;
        vol.x = dir.x * (zBlowoutSpeed * 1.15f);
        vol.y = dir.y * (zBlowoutSpeed * 1.15f) + upwardSpeed * 0.8f;
        vol.z = dir.z * (zBlowoutSpeed * 1.15f);
        vol.orbitalY = roundaboutSwirlSpeed * Mathf.Deg2Rad * 1.8f; // Fast orbiting motes
        vol.radial = lateralDispersion * 0.2f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.0f);
        sc.AddKey(0.15f, 1.0f);
        sc.AddKey(0.40f, 0.4f);
        sc.AddKey(0.65f, 1.2f);
        sc.AddKey(1.0f, 0.0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-90f * Mathf.Deg2Rad, 90f * Mathf.Deg2Rad);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(hdrBoneWhite, 0.0f),
                new GradientColorKey(hdrLavender, 0.45f),
                new GradientColorKey(hdrBoneWhite, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.95f * alphaMult, 0.15f),
                new GradientAlphaKey(0.85f * alphaMult, 0.70f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.velocityScale = 0f;
        renderer.lengthScale = 1f;
        renderer.material = diamondSparkleMaterial != null ? diamondSparkleMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 5;
    }

    // ==========================================
    //  LAYER 5B: GREEK CROSS SPARKLES (+Z BLOWOUT)
    // ==========================================
    private void ConfigureCrossSparkles(ParticleSystem ps, float alphaMult)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = loopBlowout;
        main.playOnAwake = true;
        main.simulationSpace = blowoutSpace;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 2.0f);
        float minS = Mathf.Max(0.005f, sparkleSize * 0.75f * particleScale);
        float maxS = Mathf.Max(minS, sparkleSize * 1.15f * particleScale);
        main.startSize = new ParticleSystem.MinMaxCurve(minS, maxS);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        Color hdrBoneWhite = new Color(boneWhiteColor.r * sparkleBloomIntensity, boneWhiteColor.g * sparkleBloomIntensity, boneWhiteColor.b * sparkleBloomIntensity, 1f);
        Color hdrLavender = new Color(midtoneViolet.r * sparkleBloomIntensity, midtoneViolet.g * sparkleBloomIntensity, midtoneViolet.b * sparkleBloomIntensity, 1f);
        main.startColor = new ParticleSystem.MinMaxGradient(hdrBoneWhite, hdrLavender);
        main.maxParticles = Mathf.Max(4, Mathf.CeilToInt(sparkleQuantity * 3f));

        var tsa = ps.textureSheetAnimation;
        tsa.enabled = false;

        var emission = ps.emission;
        emission.rateOverTime = (sparkleQuantity <= 0.001f || !enableSparkles || dissipationProgress >= 0.85f) ? 0f : (sparkleQuantity * 0.45f * densityMultiplier * alphaMult);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.radius = vortexRadius * 0.85f;
        shape.radiusThickness = 0.85f;
        shape.length = Mathf.Max(0.1f, vortexHeight * 0.9f);
        shape.position = new Vector3(0f, 0.20f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        Vector3 dir = EffectiveDirection;
        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = blowoutSpace;
        vol.x = dir.x * (zBlowoutSpeed * 1.1f);
        vol.y = dir.y * (zBlowoutSpeed * 1.1f) + upwardSpeed * 0.7f;
        vol.z = dir.z * (zBlowoutSpeed * 1.1f);
        vol.orbitalY = roundaboutSwirlSpeed * Mathf.Deg2Rad * 1.7f;
        vol.radial = lateralDispersion * 0.2f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.0f);
        sc.AddKey(0.15f, 1.0f);
        sc.AddKey(0.45f, 0.4f);
        sc.AddKey(0.70f, 1.15f);
        sc.AddKey(1.0f, 0.0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-90f * Mathf.Deg2Rad, 90f * Mathf.Deg2Rad);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(hdrLavender, 0.0f),
                new GradientColorKey(hdrBoneWhite, 0.50f),
                new GradientColorKey(hdrLavender, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.95f * alphaMult, 0.15f),
                new GradientAlphaKey(0.85f * alphaMult, 0.70f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.velocityScale = 0f;
        renderer.lengthScale = 1f;
        renderer.material = crossSparkleMaterial != null ? crossSparkleMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 5;
    }

    // ==========================================
    //  TRANSPORT & PLAYBACK
    // ==========================================
    public void Play()
    {
        InitializeEffect();
        if (backdropFogPS != null) backdropFogPS.Play(true);
        if (vortexWispsPS != null) vortexWispsPS.Play(true);
        if (groundMistPS != null) groundMistPS.Play(true);
        if (soulOrbsPS != null) soulOrbsPS.Play(true);
        if (diamondSparklesPS != null) diamondSparklesPS.Play(true);
        if (crossSparklesPS != null) crossSparklesPS.Play(true);
    }

    public void Stop()
    {
        if (backdropFogPS != null) backdropFogPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (vortexWispsPS != null) vortexWispsPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (groundMistPS != null) groundMistPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (soulOrbsPS != null) soulOrbsPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (diamondSparklesPS != null) diamondSparklesPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (crossSparklesPS != null) crossSparklesPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    public void TriggerDissipation() => TriggerDissipation(dissipationDuration);

    public void TriggerDissipation(float duration)
    {
        Play();
        if (Application.isPlaying)
        {
            if (gameObject.activeInHierarchy)
            {
                if (dissipationRoutine != null) StopCoroutine(dissipationRoutine);
                dissipationRoutine = StartCoroutine(AnimateDissipation(duration));
            }
        }
        else
        {
#if UNITY_EDITOR
            TriggerEditorDissipation(duration);
#else
            DissipationProgress = 1.0f;
#endif
        }
    }

    public void ResetEffect()
    {
        dissipationProgress = 0f;
        if (dissipationRoutine != null && Application.isPlaying)
        {
            StopCoroutine(dissipationRoutine);
        }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorDissipationStep;
#endif

        if (hideTargetSmokeOnEnable && targetMiasmaFog != null)
        {
            targetMiasmaFog.gameObject.SetActive(true);
            targetMiasmaFog.Play();
        }

        UpdateAllLayers();
        Play();
    }

    private IEnumerator AnimateDissipation(float duration)
    {
        float elapsed = 0f;
        float startP = dissipationProgress;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            DissipationProgress = Mathf.Clamp01(Mathf.Lerp(startP, 1.0f, elapsed / duration));
            yield return null;
        }
        DissipationProgress = 1.0f;
    }

#if UNITY_EDITOR
    private double lastEditorTime;
    private float editorElapsed;
    private float editorDuration;
    private float editorStartProgress;

    private void TriggerEditorDissipation(float duration)
    {
        editorElapsed = 0f;
        editorDuration = Mathf.Max(0.1f, duration);
        editorStartProgress = dissipationProgress;
        lastEditorTime = UnityEditor.EditorApplication.timeSinceStartup;
        UnityEditor.EditorApplication.update -= EditorDissipationStep;
        UnityEditor.EditorApplication.update += EditorDissipationStep;
    }

    private void EditorDissipationStep()
    {
        if (this == null)
        {
            UnityEditor.EditorApplication.update -= EditorDissipationStep;
            return;
        }
        double now = UnityEditor.EditorApplication.timeSinceStartup;
        float dt = (float)(now - lastEditorTime);
        lastEditorTime = now;
        editorElapsed += dt;
        DissipationProgress = Mathf.Clamp01(Mathf.Lerp(editorStartProgress, 1.0f, editorElapsed / editorDuration));
        UnityEditor.EditorUtility.SetDirty(this);

        if (editorElapsed >= editorDuration)
        {
            UnityEditor.EditorApplication.update -= EditorDissipationStep;
            DissipationProgress = 1.0f;
        }
    }
#endif
}
