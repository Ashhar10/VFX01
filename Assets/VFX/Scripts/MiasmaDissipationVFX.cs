using UnityEngine;
using System.Collections;

/// <summary>
/// MiasmaDissipationVFX - Dedicated Stage [4] Fog Dissipation & Board Return VFX.
/// 
/// Corresponds directly to Stage [4] in the design guide (media_1790291226710.png):
/// "After 3 turns the fog dissipates. Particles fade. The board returns to normal."
/// 
/// Key Features:
/// 1. Low Ground Diffusion Mist: Spills across the grid tiles, creeping radially outward and dissolving.
/// 2. Ascending Evaporation Wisps: Ethereal plumes that lift off the floor and disperse into thin air.
/// 3. Cleansing Floor Wave: Gentle expanding perimeter ripple clearing the floor tiles.
/// 4. Delicate Lingering Sparkles: Subtle Diamond (◆) and Cross (+) motes that blink out as the magic fades.
/// 5. 3-Turn Controller: Step through Turn 1, Turn 2, Turn 3 or animate continuously over duration.
/// 6. Optional Fog Sync: Link to a RotatingSmokeVFX instance to automatically dissolve the vortex while diffusing.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class MiasmaDissipationVFX : MonoBehaviour
{
    // ==========================================
    //  STAGE 4 DISSIPATION CONTROLLER
    // ==========================================
    [Header("=== Stage 4: Fog Dissipation & Board Return (Image 4) ===")]
    [Tooltip("Dissipation Progress (0 = start of dissipation, 1 = fog completely cleared / board normal).")]
    [SerializeField, Range(0f, 1f)] private float dissipationProgress = 0f;

    [Tooltip("Turn step (1 = Fog thinning, 2 = Spreading across floor tiles, 3 = Board returned to normal).")]
    [SerializeField, Range(1, 3)] private int currentTurn = 1;

    [Tooltip("Total duration in seconds for automated dissipation animation.")]
    [SerializeField, Range(0.5f, 8.0f)] private float dissipationDuration = 3.0f;

    [Tooltip("Maximum radial distance on the ground plane (meters) that mist expands across the board.")]
    [SerializeField, Range(1.5f, 8.0f)] private float diffusionSpreadRadius = 4.5f;

    [Tooltip("Base radius where the fog started before diffusing.")]
    [SerializeField, Range(0.5f, 4.0f)] private float startRadius = 1.7f;

    [Tooltip("Optional reference to the main Miasma Fog (RotatingSmokeVFX). If assigned, it will automatically sync and fade!")]
    [SerializeField] private RotatingSmokeVFX targetMiasmaFog;

    // ==========================================
    //  Z-AXIS DIRECTIONAL SWEEP & TAIL DEFORMATION
    // ==========================================
    public enum DissipationDirectionMode
    {
        Forward_Z,             // Standard +Z Axis (Vector3.forward)
        Diagonal_Z_RedArrow,  // Diagonal forward-right (+Z / +X) matching user's red arrow in Image 4
        Custom_Direction       // Custom 3D vector
    }

    [Header("=== Z-Axis Directional Sweep & Tail Deformation (Image 4) ===")]
    [Tooltip("Direction along which particles are swept when dissipation triggers.")]
    [SerializeField] private DissipationDirectionMode directionMode = DissipationDirectionMode.Diagonal_Z_RedArrow;

    [Tooltip("Custom direction vector (used when Direction Mode is set to Custom).")]
    [SerializeField] private Vector3 customDirection = new Vector3(0.35f, 0.05f, 1.0f).normalized;

    [Tooltip("Coordinate space for directional sweep velocity.")]
    [SerializeField] private ParticleSystemSimulationSpace sweepSpace = ParticleSystemSimulationSpace.World;

    [Tooltip("Speed/force at which particles are swept along the Z axis (meters/sec).")]
    [SerializeField, Range(0f, 15f)] private float zSweepSpeed = 4.5f;

    [Tooltip("Enable aerodynamic tail deformation (stretches particles into elongated wispy smoke tails along their motion vector).")]
    [SerializeField] private bool enableTailDeformation = true;

    [Tooltip("Multiplier for particle velocity stretching when deforming into tails (Stretch Render Mode velocityScale).")]
    [SerializeField, Range(0.5f, 6.0f)] private float tailDeformationScale = 2.2f;

    [Tooltip("Base elongation factor along heading (Stretch Render Mode lengthScale).")]
    [SerializeField, Range(0.5f, 5.0f)] private float tailLengthScale = 1.8f;

    [Tooltip("Lateral dispersion force pushing particles outward as they rush along the Z axis.")]
    [SerializeField, Range(0f, 5.0f)] private float lateralDispersion = 1.2f;

    // ==========================================
    //  LAYER TOGGLES
    // ==========================================
    [Header("=== Layer Toggles ===")]
    [Tooltip("Layer 1: Ground diffusion mist creeping outward across floor tiles.")]
    [SerializeField] private bool enableGroundDiffusion = true;

    [Tooltip("Layer 2: Ascending evaporation wisps lifting off the floor and dissolving.")]
    [SerializeField] private bool enableEvaporationWisps = true;

    [Tooltip("Layer 3: Ethereal cleansing shockwave ripple expanding across the floor.")]
    [SerializeField] private bool enableCleansingRipple = true;

    [Tooltip("Layer 4: Delicate lingering star sparkles (Diamond ◆ & Cross +) fading away.")]
    [SerializeField] private bool enableLingeringSparkles = true;

    // ==========================================
    //  SPARKLE CONTROLS (USER FRIENDLY)
    // ==========================================
    [Header("=== Subtle Sparkles (User Friendly) ===")]
    [Tooltip("Sparkle Quantity: Single clean slider (0 = OFF, 1-10 = subtle delicate motes).")]
    [SerializeField, Range(0f, 10f)] private float sparkleQuantity = 3.0f;

    [Tooltip("Sparkle Size: controls the size of the lingering star motes.")]
    [SerializeField, Range(0.01f, 0.06f)] private float sparkleSize = 0.028f;

    [Tooltip("Sparkle Bloom / Glow intensity multiplier.")]
    [SerializeField, Range(0f, 5.0f)] private float sparkleBloomIntensity = 2.5f;

    // ==========================================
    //  COLOR PALETTE (ALURA GRAVES PALETTE)
    // ==========================================
    [Header("=== Palette (Bone White, Lilac & Charcoal) ===")]
    [SerializeField] private Color boneWhiteColor = new Color(0.96f, 0.93f, 0.88f, 1.0f);
    [SerializeField] private Color midtoneViolet = new Color(0.75f, 0.60f, 0.90f, 1.0f);
    [SerializeField] private Color groundMistColor = new Color(0.88f, 0.84f, 0.92f, 1.0f);
    [SerializeField] private Color spectralTint = new Color(0.80f, 0.95f, 0.85f, 1.0f);

    // ==========================================
    //  MATERIAL REFERENCES
    // ==========================================
    [Header("=== Materials & Textures ===")]
    [SerializeField] private Material smokePlumeMaterial;
    [SerializeField] private Material smokeWispMaterial;
    [SerializeField] private Material diamondSparkleMaterial;
    [SerializeField] private Material crossSparkleMaterial;

    // Child Particle Systems
    private ParticleSystem groundDiffusionPS;
    private ParticleSystem evaporationWispsPS;
    private ParticleSystem cleansingRipplePS;
    private ParticleSystem diamondSparklesPS;
    private ParticleSystem crossSparklesPS;

    // Coroutine tracking
    private Coroutine dissipationRoutine;

    // ==========================================
    //  PUBLIC PROPERTIES
    // ==========================================
    public float DissipationProgress
    {
        get => dissipationProgress;
        set
        {
            dissipationProgress = Mathf.Clamp01(value);
            currentTurn = dissipationProgress < 0.33f ? 1 : (dissipationProgress < 0.66f ? 2 : 3);
            UpdateAllLayers();
            SyncTargetFog();
        }
    }

    public int CurrentTurn
    {
        get => currentTurn;
        set
        {
            currentTurn = Mathf.Clamp(value, 1, 3);
            dissipationProgress = (currentTurn - 1) / 2.0f;
            UpdateAllLayers();
            SyncTargetFog();
        }
    }

    public float DissipationDuration
    {
        get => dissipationDuration;
        set => dissipationDuration = Mathf.Max(0.1f, value);
    }

    public float DiffusionSpreadRadius
    {
        get => diffusionSpreadRadius;
        set { diffusionSpreadRadius = Mathf.Max(0.5f, value); UpdateAllLayers(); }
    }

    public float SparkleQuantity
    {
        get => sparkleQuantity;
        set { sparkleQuantity = Mathf.Clamp(value, 0f, 10f); UpdateAllLayers(); }
    }

    public float SparkleSize
    {
        get => sparkleSize;
        set { sparkleSize = Mathf.Clamp(value, 0.01f, 0.06f); UpdateAllLayers(); }
    }

    public RotatingSmokeVFX TargetMiasmaFog
    {
        get => targetMiasmaFog;
        set => targetMiasmaFog = value;
    }

    public bool IsDissipated => dissipationProgress >= 0.99f;

    public DissipationDirectionMode DirectionMode
    {
        get => directionMode;
        set { directionMode = value; UpdateAllLayers(); SyncTargetFog(); }
    }

    public Vector3 CustomDirection
    {
        get => customDirection;
        set { customDirection = value; UpdateAllLayers(); SyncTargetFog(); }
    }

    public Vector3 EffectiveDirection
    {
        get
        {
            switch (directionMode)
            {
                case DissipationDirectionMode.Forward_Z:
                    return Vector3.forward;
                case DissipationDirectionMode.Diagonal_Z_RedArrow:
                    return new Vector3(0.35f, 0.05f, 1.0f).normalized;
                case DissipationDirectionMode.Custom_Direction:
                    return customDirection.sqrMagnitude > 0.001f ? customDirection.normalized : Vector3.forward;
                default:
                    return Vector3.forward;
            }
        }
    }

    public ParticleSystemSimulationSpace SweepSpace
    {
        get => sweepSpace;
        set { sweepSpace = value; UpdateAllLayers(); }
    }

    public float ZSweepSpeed
    {
        get => zSweepSpeed;
        set { zSweepSpeed = Mathf.Max(0f, value); UpdateAllLayers(); SyncTargetFog(); }
    }

    public bool EnableTailDeformation
    {
        get => enableTailDeformation;
        set { enableTailDeformation = value; UpdateAllLayers(); SyncTargetFog(); }
    }

    public float TailDeformationScale
    {
        get => tailDeformationScale;
        set { tailDeformationScale = Mathf.Max(0.1f, value); UpdateAllLayers(); SyncTargetFog(); }
    }

    public float TailLengthScale
    {
        get => tailLengthScale;
        set { tailLengthScale = Mathf.Max(0.1f, value); UpdateAllLayers(); SyncTargetFog(); }
    }

    public float LateralDispersion
    {
        get => lateralDispersion;
        set { lateralDispersion = Mathf.Max(0f, value); UpdateAllLayers(); SyncTargetFog(); }
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
        InitializeEffect();
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
        SyncTargetFog();
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
        if (diamondSparkleMaterial == null)
            diamondSparkleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaSparkleDiamondMat.mat");
        if (crossSparkleMaterial == null)
            crossSparkleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaSparkleCrossMat.mat");
#endif
    }

    // ==========================================
    //  INITIALIZATION
    // ==========================================
    public void InitializeEffect()
    {
        FetchChildReferences();

        // 1. Ground Diffusion Mist
        groundDiffusionPS = GetOrCreateChildPS("1_Ground_Diffusion_Mist");

        // 2. Ascending Evaporation Wisps
        evaporationWispsPS = GetOrCreateChildPS("2_Evaporation_Wisps");

        // 3. Cleansing Floor Ripple
        cleansingRipplePS = GetOrCreateChildPS("3_Cleansing_Floor_Ripple");

        // 4a. Lingering Diamond Sparkles
        diamondSparklesPS = GetOrCreateChildPS("4a_Diamond_Sparkles");

        // 4b. Lingering Cross Sparkles
        crossSparklesPS = GetOrCreateChildPS("4b_Cross_Sparkles");

        UpdateAllLayers();
    }

    private void FetchChildReferences()
    {
        if (groundDiffusionPS == null) groundDiffusionPS = transform.Find("1_Ground_Diffusion_Mist")?.GetComponent<ParticleSystem>();
        if (evaporationWispsPS == null) evaporationWispsPS = transform.Find("2_Evaporation_Wisps")?.GetComponent<ParticleSystem>();
        if (cleansingRipplePS == null) cleansingRipplePS = transform.Find("3_Cleansing_Floor_Ripple")?.GetComponent<ParticleSystem>();
        if (diamondSparklesPS == null) diamondSparklesPS = transform.Find("4a_Diamond_Sparkles")?.GetComponent<ParticleSystem>();
        if (crossSparklesPS == null) crossSparklesPS = transform.Find("4b_Cross_Sparkles")?.GetComponent<ParticleSystem>();
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

        // Calculate dynamic properties along progress
        // As progress goes from 0 -> 1:
        // - Ground radius expands from startRadius -> diffusionSpreadRadius
        // - Floor opacity starts strong, peaks mid-spread (Turn 2), then fades to 0 (Turn 3)
        float currentRadius = Mathf.Lerp(startRadius, diffusionSpreadRadius, dissipationProgress);
        
        // Alpha curve for diffusion: rises during expansion, then fades completely out
        float diffusionAlpha = 0f;
        if (dissipationProgress < 0.65f)
        {
            diffusionAlpha = Mathf.Lerp(1.0f, 0.8f, dissipationProgress / 0.65f);
        }
        else
        {
            diffusionAlpha = Mathf.Lerp(0.8f, 0.0f, (dissipationProgress - 0.65f) / 0.35f);
        }

        // Evaporation wisps fade out as progress increases
        float evaporationAlpha = Mathf.Clamp01(1.0f - dissipationProgress * 1.1f);

        // Sparkle alpha fades out
        float sparkleAlpha = Mathf.Clamp01(1.0f - dissipationProgress * 1.2f);

        // When dissipation is complete (>= 0.99), completely vanish all particles
        bool boardReturnedToNormal = dissipationProgress >= 0.99f;

        // 1. Ground Diffusion Mist
        if (groundDiffusionPS != null)
        {
            bool active = enableGroundDiffusion && diffusionAlpha > 0.005f && !boardReturnedToNormal;
            groundDiffusionPS.gameObject.SetActive(active);
            if (active) ConfigureGroundDiffusion(groundDiffusionPS, currentRadius, diffusionAlpha);
            else if (boardReturnedToNormal) groundDiffusionPS.Clear();
        }

        // 2. Evaporation Wisps
        if (evaporationWispsPS != null)
        {
            bool active = enableEvaporationWisps && evaporationAlpha > 0.005f && !boardReturnedToNormal;
            evaporationWispsPS.gameObject.SetActive(active);
            if (active) ConfigureEvaporationWisps(evaporationWispsPS, currentRadius, evaporationAlpha);
            else if (boardReturnedToNormal) evaporationWispsPS.Clear();
        }

        // 3. Cleansing Floor Ripple
        if (cleansingRipplePS != null)
        {
            bool active = enableCleansingRipple && diffusionAlpha > 0.01f && !boardReturnedToNormal;
            cleansingRipplePS.gameObject.SetActive(active);
            if (active) ConfigureCleansingRipple(cleansingRipplePS, currentRadius, diffusionAlpha);
            else if (boardReturnedToNormal) cleansingRipplePS.Clear();
        }

        // 4a. Lingering Diamond Sparkles
        if (diamondSparklesPS != null)
        {
            bool active = enableLingeringSparkles && sparkleQuantity > 0.001f && sparkleAlpha > 0.005f && !boardReturnedToNormal;
            diamondSparklesPS.gameObject.SetActive(active);
            if (active) ConfigureDiamondSparkles(diamondSparklesPS, currentRadius, sparkleAlpha);
            else if (boardReturnedToNormal) diamondSparklesPS.Clear();
        }

        // 4b. Lingering Cross Sparkles
        if (crossSparklesPS != null)
        {
            bool active = enableLingeringSparkles && sparkleQuantity > 0.001f && sparkleAlpha > 0.005f && !boardReturnedToNormal;
            crossSparklesPS.gameObject.SetActive(active);
            if (active) ConfigureCrossSparkles(crossSparklesPS, currentRadius, sparkleAlpha);
            else if (boardReturnedToNormal) crossSparklesPS.Clear();
        }
    }

    private void SyncTargetFog()
    {
        if (targetMiasmaFog != null)
        {
            targetMiasmaFog.DissipationDirection = EffectiveDirection;
            targetMiasmaFog.ZSweepSpeed = zSweepSpeed;
            targetMiasmaFog.EnableTailDeformation = enableTailDeformation;
            targetMiasmaFog.TailDeformationScale = tailDeformationScale;
            targetMiasmaFog.TailLengthScale = tailLengthScale;
            targetMiasmaFog.LateralDispersion = lateralDispersion;
            targetMiasmaFog.DissipationProgress = dissipationProgress;
            if (dissipationProgress >= 0.98f)
            {
                targetMiasmaFog.EliminateClouds = true;
            }
            else
            {
                targetMiasmaFog.EliminateClouds = false;
            }
        }
    }

    // ==========================================
    //  LAYER 1: GROUND DIFFUSION MIST (IMAGE 4 FLOOR SPREAD)
    // ==========================================
    private void ConfigureGroundDiffusion(ParticleSystem ps, float radius, float alphaMult)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.0f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 40;

        var emission = ps.emission;
        emission.rateOverTime = dissipationProgress >= 0.75f ? 0f : (14f * alphaMult);

        // Donut / Circle expanding along the ground plane (XZ)
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = Mathf.Max(0.5f, radius * 0.9f);
        shape.donutRadius = Mathf.Max(0.3f, radius * 0.45f);
        shape.position = new Vector3(0f, 0.04f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        // Forward Z-Sweep & Floor Crawl
        Vector3 dir = EffectiveDirection;
        float currentSweep = zSweepSpeed * Mathf.Clamp01(dissipationProgress * 1.5f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = sweepSpace;
        vol.x = dir.x * currentSweep;
        vol.y = dir.y * currentSweep + 0.02f;
        vol.z = dir.z * currentSweep;
        vol.radial = Mathf.Lerp(0.35f, 0.35f + lateralDispersion * 0.4f, dissipationProgress); // Creeps outward away from center & disperses laterally
        vol.orbitalY = (15f * Mathf.Deg2Rad) * (1f - dissipationProgress);

        // Size grows as it diffuses across the ground tiles
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.4f);
        sc.AddKey(0.5f, 1.1f);
        sc.AddKey(1.0f, 1.4f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        // Color: Pale bone white rim with dark charcoal violet floor crawl
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(boneWhiteColor, 0.0f),
                new GradientColorKey(groundMistColor, 0.35f),
                new GradientColorKey(midtoneViolet, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.65f * alphaMult, 0.20f),
                new GradientAlphaKey(0.50f * alphaMult, 0.70f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 0.4f;
        noise.scrollSpeed = 0.15f + dissipationProgress * 0.2f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (enableTailDeformation && dissipationProgress > 0.02f)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = Mathf.Lerp(0.3f, tailDeformationScale, Mathf.Clamp01(dissipationProgress * 1.5f));
            renderer.lengthScale = Mathf.Lerp(1.0f, tailLengthScale, Mathf.Clamp01(dissipationProgress * 1.5f));
        }
        else
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
        renderer.material = smokePlumeMaterial;
        renderer.sortingOrder = 1;
    }

    // ==========================================
    //  LAYER 2: ASCENDING EVAPORATION WISPS
    // ==========================================
    private void ConfigureEvaporationWisps(ParticleSystem ps, float radius, float alphaMult)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.25f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 25;

        var emission = ps.emission;
        emission.rateOverTime = dissipationProgress >= 0.75f ? 0f : (8f * alphaMult);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius * 0.85f;
        shape.position = new Vector3(0f, 0.1f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        // Upward gentle evaporation drift + Forward Z-Sweep
        Vector3 dir = EffectiveDirection;
        float currentSweep = zSweepSpeed * Mathf.Clamp01(dissipationProgress * 1.5f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = sweepSpace;
        vol.x = dir.x * currentSweep;
        vol.y = dir.y * currentSweep + 0.65f * (1f - dissipationProgress * 0.5f);
        vol.z = dir.z * currentSweep;
        vol.orbitalY = (25f * Mathf.Deg2Rad) * (1f - dissipationProgress);
        vol.radial = Mathf.Lerp(0.15f, 0.15f + lateralDispersion * 0.35f, dissipationProgress);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.3f);
        sc.AddKey(0.35f, 1.0f);
        sc.AddKey(1.0f, 0.1f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(spectralTint, 0.0f),
                new GradientColorKey(midtoneViolet, 0.5f),
                new GradientColorKey(boneWhiteColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.55f * alphaMult, 0.25f),
                new GradientAlphaKey(0.40f * alphaMult, 0.70f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.2f + dissipationProgress * 0.3f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (enableTailDeformation && dissipationProgress > 0.02f)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = Mathf.Lerp(0.3f, tailDeformationScale * 1.25f, Mathf.Clamp01(dissipationProgress * 1.5f));
            renderer.lengthScale = Mathf.Lerp(1.0f, tailLengthScale * 1.15f, Mathf.Clamp01(dissipationProgress * 1.5f));
        }
        else
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
        renderer.material = smokeWispMaterial != null ? smokeWispMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 2;
    }

    // ==========================================
    //  LAYER 3: CLEANSING FLOOR RIPPLE
    // ==========================================
    private void ConfigureCleansingRipple(ParticleSystem ps, float radius, float alphaMult)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startColor = Color.white;
        main.maxParticles = 20;

        var emission = ps.emission;
        emission.rateOverTime = dissipationProgress >= 0.75f ? 0f : (6f * alphaMult);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = Mathf.Max(0.5f, radius);
        shape.radiusThickness = 0.15f; // Thin boundary ring
        shape.position = new Vector3(0f, 0.03f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        Vector3 dir = EffectiveDirection;
        float currentSweep = zSweepSpeed * Mathf.Clamp01(dissipationProgress * 1.5f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = sweepSpace;
        vol.x = dir.x * currentSweep;
        vol.y = dir.y * currentSweep;
        vol.z = dir.z * currentSweep;
        vol.radial = Mathf.Lerp(0.5f, 0.5f + lateralDispersion * 0.5f, dissipationProgress); // Expands outward along floor

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(boneWhiteColor, 0.0f),
                new GradientColorKey(spectralTint, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.50f * alphaMult, 0.30f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (enableTailDeformation && dissipationProgress > 0.02f)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = Mathf.Lerp(0.2f, tailDeformationScale * 0.8f, Mathf.Clamp01(dissipationProgress * 1.5f));
            renderer.lengthScale = Mathf.Lerp(1.0f, tailLengthScale, Mathf.Clamp01(dissipationProgress * 1.5f));
        }
        else
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
        renderer.material = smokeWispMaterial != null ? smokeWispMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 0;
    }

    // ==========================================
    //  LAYER 4A: DELICATE DIAMOND SPARKLES (IMAGE 2 ◆)
    // ==========================================
    private void ConfigureDiamondSparkles(ParticleSystem ps, float radius, float alphaMult)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.6f);
        float minS = Mathf.Max(0.005f, sparkleSize * 0.8f);
        float maxS = Mathf.Max(minS, sparkleSize * 1.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(minS, maxS);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.10f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        Color hdrBoneWhite = new Color(boneWhiteColor.r * sparkleBloomIntensity, boneWhiteColor.g * sparkleBloomIntensity, boneWhiteColor.b * sparkleBloomIntensity, 1f);
        Color hdrLavender = new Color(midtoneViolet.r * sparkleBloomIntensity, midtoneViolet.g * sparkleBloomIntensity, midtoneViolet.b * sparkleBloomIntensity, 1f);
        main.startColor = new ParticleSystem.MinMaxGradient(hdrBoneWhite, hdrLavender);
        main.maxParticles = Mathf.Max(2, Mathf.CeilToInt(sparkleQuantity * 2f));

        var tsa = ps.textureSheetAnimation;
        tsa.enabled = false;

        var emission = ps.emission;
        emission.rateOverTime = (sparkleQuantity <= 0.001f || !enableLingeringSparkles || dissipationProgress >= 0.75f) ? 0f : (sparkleQuantity * 0.55f * alphaMult);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius * 0.9f;
        shape.position = new Vector3(0f, 0.12f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        Vector3 dir = EffectiveDirection;
        float currentSweep = zSweepSpeed * Mathf.Clamp01(dissipationProgress * 1.5f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = sweepSpace;
        vol.x = dir.x * currentSweep;
        vol.y = dir.y * currentSweep + 0.25f * (1f - dissipationProgress);
        vol.z = dir.z * currentSweep;
        vol.radial = 0.15f + lateralDispersion * 0.2f * dissipationProgress;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.0f);
        sc.AddKey(0.2f, 1.0f);
        sc.AddKey(0.6f, 0.8f);
        sc.AddKey(1.0f, 0.0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(hdrBoneWhite, 0.0f),
                new GradientColorKey(hdrLavender, 0.5f),
                new GradientColorKey(hdrBoneWhite, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.9f * alphaMult, 0.2f),
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
    //  LAYER 4B: DELICATE CROSS SPARKLES (IMAGE 3 +)
    // ==========================================
    private void ConfigureCrossSparkles(ParticleSystem ps, float radius, float alphaMult)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        float minS = Mathf.Max(0.005f, sparkleSize * 0.75f);
        float maxS = Mathf.Max(minS, sparkleSize * 1.15f);
        main.startSize = new ParticleSystem.MinMaxCurve(minS, maxS);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.09f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        Color hdrBoneWhite = new Color(boneWhiteColor.r * sparkleBloomIntensity, boneWhiteColor.g * sparkleBloomIntensity, boneWhiteColor.b * sparkleBloomIntensity, 1f);
        Color hdrLavender = new Color(midtoneViolet.r * sparkleBloomIntensity, midtoneViolet.g * sparkleBloomIntensity, midtoneViolet.b * sparkleBloomIntensity, 1f);
        main.startColor = new ParticleSystem.MinMaxGradient(hdrBoneWhite, hdrLavender);
        main.maxParticles = Mathf.Max(2, Mathf.CeilToInt(sparkleQuantity * 2f));

        var tsa = ps.textureSheetAnimation;
        tsa.enabled = false;

        var emission = ps.emission;
        emission.rateOverTime = (sparkleQuantity <= 0.001f || !enableLingeringSparkles || dissipationProgress >= 0.75f) ? 0f : (sparkleQuantity * 0.45f * alphaMult);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius * 0.85f;
        shape.position = new Vector3(0f, 0.12f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        Vector3 dir = EffectiveDirection;
        float currentSweep = zSweepSpeed * Mathf.Clamp01(dissipationProgress * 1.5f) * 0.6f;

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = sweepSpace;
        vol.x = dir.x * currentSweep;
        vol.y = dir.y * currentSweep + 0.20f * (1f - dissipationProgress);
        vol.z = dir.z * currentSweep;
        vol.radial = 0.10f + lateralDispersion * 0.15f * dissipationProgress;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.0f);
        sc.AddKey(0.2f, 1.0f);
        sc.AddKey(0.6f, 0.7f);
        sc.AddKey(1.0f, 0.0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(hdrLavender, 0.0f),
                new GradientColorKey(hdrBoneWhite, 0.5f),
                new GradientColorKey(hdrLavender, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.9f * alphaMult, 0.2f),
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
        if (groundDiffusionPS != null) groundDiffusionPS.Play(true);
        if (evaporationWispsPS != null) evaporationWispsPS.Play(true);
        if (cleansingRipplePS != null) cleansingRipplePS.Play(true);
        if (diamondSparklesPS != null) diamondSparklesPS.Play(true);
        if (crossSparklesPS != null) crossSparklesPS.Play(true);
    }

    public void Stop()
    {
        if (groundDiffusionPS != null) groundDiffusionPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (evaporationWispsPS != null) evaporationWispsPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (cleansingRipplePS != null) cleansingRipplePS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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

    public void AdvanceTurn()
    {
        CurrentTurn = currentTurn >= 3 ? 1 : currentTurn + 1;
    }

    public void ResetBoard()
    {
        dissipationProgress = 0f;
        currentTurn = 1;
        if (dissipationRoutine != null && Application.isPlaying)
        {
            StopCoroutine(dissipationRoutine);
        }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorDissipationStep;
#endif
        if (targetMiasmaFog != null)
        {
            targetMiasmaFog.ResetDissipation();
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
