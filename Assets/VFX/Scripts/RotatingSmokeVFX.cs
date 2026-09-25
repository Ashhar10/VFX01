using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Rotating Necromantic Smoke & Miasma VFX.
/// Faithful implementation of Alura Graves' "3. MIASMA (Ultimate)" Wind-up Design Specification:
/// 
/// High-power volumetric cloudy aesthetic:
/// - Bone White (cream / off-white clouds)
/// - Lavender (soft mystical purple accent & rim catch)
/// - Emissive cloudy sparkles (no sharp slashes, no square quads)
/// 
/// 1. Backdrop Billowing Fog: Large voluminous Bone White & Lavender cumulus plumes.
/// 2. Swirling Vortex Wisps: Fast spiraling smoke streamers framing the magical rings.
/// 3. Ground Creeping Mist: Dense floor fog rolling and crawling outward.
/// 4. Floating Soul Orbs: Ethereal floating soul embers with luminous cores and lavender rims.
/// 5. Sparkling Energy Motes: Orbiting emissive smoky sparkles with additive glow.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class RotatingSmokeVFX : MonoBehaviour
{
    // ==========================================
    //  VORTEX GEOMETRY & SPEEDS
    // ==========================================
    [Header("=== Vortex Geometry & Dynamics ===")]
    [Tooltip("Radius of the swirling miasma vortex around the character (meters).")]
    [SerializeField, Range(0.5f, 5.0f)] private float vortexRadius = 1.7f;

    [Tooltip("Height the miasma rises around the character (meters).")]
    [SerializeField, Range(0.5f, 5.0f)] private float vortexHeight = 2.4f;

    [Tooltip("Vortex rotation / swirl speed (degrees/sec).")]
    [SerializeField, Range(-200f, 200f)] private float rotationSpeed = 60f;

    [Tooltip("Upward drift speed of the smoke (m/s).")]
    [SerializeField, Range(0.05f, 2.0f)] private float upwardSpeed = 0.40f;

    [Tooltip("Overall scale multiplier for smoke particles.")]
    [SerializeField, Range(0.3f, 2.5f)] private float particleScale = 1.0f;

    [Tooltip("Density / Opacity multiplier for the smoke.")]
    [SerializeField, Range(0.1f, 2.5f)] private float densityMultiplier = 1.0f;

    // ==========================================
    //  COLOR PALETTE (BONE WHITE & LAVENDER)
    // ==========================================
    [Header("=== Color Palette (Bone White & Lavender) ===")]
    [Tooltip("Primary cloud body color: Bone White (cream / off-white).")]
    [SerializeField] private Color boneWhiteColor = new Color(0.96f, 0.93f, 0.88f, 1.0f);

    [Tooltip("Accent billow color: Lavender (soft purple).")]
    [SerializeField] private Color midtoneViolet = new Color(0.75f, 0.60f, 0.90f, 1.0f);

    [Tooltip("Deep cloud base color: Mystical Lilac / Soft Purple.")]
    [SerializeField] private Color deepCharcoalPurple = new Color(0.85f, 0.78f, 0.92f, 1.0f);

    [Tooltip("Bright rim highlight: Eerie Lilac / Bright White rim catch.")]
    [SerializeField] private Color rimHighlightLilac = new Color(0.92f, 0.86f, 0.98f, 1.0f);

    [Tooltip("Ground creeping mist color: Bone White with subtle lavender.")]
    [SerializeField] private Color groundMistColor = new Color(0.90f, 0.87f, 0.85f, 1.0f);

    [Tooltip("Energy Motes Color: Luminous Bone White with warm glow (NOT green!).")]
    [SerializeField] private Color necroticGreenColor = new Color(0.98f, 0.95f, 0.90f, 1.0f);

    // ==========================================
    //  LAYER TOGGLES
    // ==========================================
    [Header("=== Layer Toggles ===")]
    [Tooltip("Layer 1: Backdrop Billowing Fog (atmospheric volume).")]
    [SerializeField] private bool enableBackdropFog = true;

    [Tooltip("Layer 2: Swirling Vortex Wisps (curved smoke ribbons).")]
    [SerializeField] private bool enableVortexWisps = true;

    [Tooltip("Layer 3: Ground Creeping Mist (low floor miasma).")]
    [SerializeField] private bool enableGroundMist = true;

    [Tooltip("Layer 4: Floating Soul Orbs / Skull Motes.")]
    [SerializeField] private bool enableSoulOrbs = true;

    [Tooltip("Layer 5 Master Toggle: Sparkling Energy Motes & Stars.")]
    [SerializeField] private bool enableSparkles = true;

    [Tooltip("Layer 5A: Pure Geometric Diamond Star Sparkles (Image 2 ◆).")]
    [SerializeField] private bool enableDiamondSparkles = true;

    [Tooltip("Layer 5B: Pure Geometric Greek Cross Sparkles (Image 3 +).")]
    [SerializeField] private bool enableCrossSparkles = true;

    public bool enableEnergyMotes
    {
        get => enableSparkles && (enableDiamondSparkles || enableCrossSparkles);
        set { enableSparkles = value; enableDiamondSparkles = value; enableCrossSparkles = value; UpdateAllLayers(); }
    }

    public bool EnableSparkles
    {
        get => enableEnergyMotes;
        set => enableEnergyMotes = value;
    }

    // ==========================================
    //  LAYER 5: USER-FRIENDLY SPARKLE CONTROLS
    // ==========================================
    [Header("=== Layer 5: Sparkling Energy Motes (User Friendly) ===")]
    [Tooltip("Sparkle Quantity: Single clean slider (0 = OFF, 1-20 = subtle delicate motes). Controls BOTH Diamond ◆ and Cross + shapes.")]
    [SerializeField, Range(0f, 20f)] private float sparkleQuantity = 5.0f;

    [Tooltip("Sparkle Size: controls the size of the twinkling star motes (small and delicate).")]
    [SerializeField, Range(0.01f, 0.08f)] private float sparkleSize = 0.032f;

    [Tooltip("Sparkle Glow / Bloom intensity multiplier.")]
    [SerializeField, Range(0f, 6.0f)] private float sparkleBloomIntensity = 2.5f;

    [Tooltip("Orbital swirl speed multiplier for energy motes.")]
    [SerializeField, Range(0f, 5.0f)] private float moteOrbitMultiplier = 1.8f;

    [Tooltip("Twinkle rotation speed (degrees/sec) so star points sparkle as they spin.")]
    [SerializeField, Range(0f, 360f)] private float moteTwinkleSpeed = 90f;

    public float SparkleQuantity
    {
        get => sparkleQuantity;
        set { sparkleQuantity = Mathf.Clamp(value, 0f, 20f); UpdateAllLayers(); }
    }

    public float SparkleSize
    {
        get => sparkleSize;
        set { sparkleSize = Mathf.Clamp(value, 0.01f, 0.08f); UpdateAllLayers(); }
    }

    public float SparkleBloomIntensity
    {
        get => sparkleBloomIntensity;
        set { sparkleBloomIntensity = Mathf.Max(0f, value); UpdateAllLayers(); }
    }

    public float SparkleRate
    {
        get => sparkleQuantity;
        set => SparkleQuantity = value;
    }

    public float SmallSparkleSize
    {
        get => sparkleSize * 0.8f;
        set => SparkleSize = value / 0.8f;
    }

    public float BigSparkleSize
    {
        get => sparkleSize * 1.2f;
        set => SparkleSize = value / 1.2f;
    }

    public float SparkleScale
    {
        get => 1.0f;
        set { }
    }

    public float DiamondSparkleRate
    {
        get => sparkleQuantity * 0.55f;
        set => SparkleQuantity = value / 0.55f;
    }

    public float CrossSparkleRate
    {
        get => sparkleQuantity * 0.45f;
        set => SparkleQuantity = value / 0.45f;
    }

    // ==========================================
    //  STAGE 4: FOG DISSIPATION & CLOUD ELIMINATION (IMAGE 4)
    // ==========================================
    [Header("=== Stage 4: Fog Dissipation & Cloud Elimination (Image 4) ===")]
    [Tooltip("Immediately eliminate the tall billowing clouds and vortex wisps (leaving only ground mist/particles).")]
    [SerializeField] private bool eliminateClouds = false;

    [Tooltip("Enable Stage 4 Fog Dissipation mode (clouds fade out, ground mist expands outwards along XZ grid and fades).")]
    [SerializeField] private bool enableDissipation = false;

    public enum DissipationAxisType
    {
        Vertical_Y,                     // Eliminates clouds from top to bottom along the vertical Y axis
        Radial_XZ,                      // Expands and dissipates fog radially along the ground grid plane
        Both_Y_and_XZ,                  // Combines vertical height cutoff with ground radial expansion fade
        Z_Axis_Forward,                 // Sweeps all particles along +Z axis, deforms into tails, disperses and vanishes
        Combined_Z_Sweep_And_Radial    // Full Stage 4 combination: Z-sweep forward tail deformation + radial spread + complete fade
    }

    [Tooltip("The axis along which the fog and clouds are eliminated/dissipated.")]
    [SerializeField] private DissipationAxisType dissipationAxis = DissipationAxisType.Combined_Z_Sweep_And_Radial;

    [Tooltip("Dissipation progress along the axis (0 = full vortex & clouds present, 1 = clouds completely eliminated).")]
    [SerializeField, Range(0f, 1f)] private float dissipationProgress = 0f;

    [Tooltip("Maximum height cutoff on the Y axis above which clouds/wisps are eliminated (0 = eliminate all tall clouds).")]
    [SerializeField, Range(0f, 5f)] private float cloudHeightCutoff = 2.4f;

    [Tooltip("Target radial distance on the ground plane (XZ axis) that mist expands to during dissipation.")]
    [SerializeField, Range(0f, 8f)] private float dissipationRadialSpread = 4.5f;

    [Tooltip("Speed/duration in seconds for smooth dissipation animation when triggered.")]
    [SerializeField, Range(0f, 10f)] private float dissipationDuration = 3.0f;

    [Tooltip("Direction along which particles sweep and deform into tails during dissipation.")]
    [SerializeField] private Vector3 dissipationDirection = new Vector3(0.35f, 0.05f, 1.0f).normalized;

    [Tooltip("Speed/force at which particles are swept along the Z axis during dissipation.")]
    [SerializeField, Range(0f, 15f)] private float zSweepSpeed = 4.5f;

    [Tooltip("Enable aerodynamic tail deformation (stretches particles into elongated wispy smoke tails along motion vector).")]
    [SerializeField] private bool enableTailDeformation = true;

    [Tooltip("Multiplier for particle velocity stretching when deforming into tails (Stretch Render Mode velocityScale).")]
    [SerializeField, Range(0.5f, 6.0f)] private float tailDeformationScale = 2.2f;

    [Tooltip("Base elongation factor along heading (Stretch Render Mode lengthScale).")]
    [SerializeField, Range(0.5f, 5.0f)] private float tailLengthScale = 1.8f;

    [Tooltip("Lateral dispersion pushing particles outward as they rush along the Z axis.")]
    [SerializeField, Range(0f, 5.0f)] private float lateralDispersion = 1.2f;

    [Tooltip("Coordinate space for directional sweep velocity.")]
    [SerializeField] private ParticleSystemSimulationSpace sweepSpace = ParticleSystemSimulationSpace.World;

    public bool EliminateClouds
    {
        get => eliminateClouds;
        set { eliminateClouds = value; UpdateAllLayers(); }
    }

    public bool EnableDissipation
    {
        get => enableDissipation;
        set { enableDissipation = value; UpdateAllLayers(); }
    }

    public DissipationAxisType DissipationAxis
    {
        get => dissipationAxis;
        set { dissipationAxis = value; UpdateAllLayers(); }
    }

    public float DissipationProgress
    {
        get => dissipationProgress;
        set { dissipationProgress = Mathf.Clamp01(value); UpdateAllLayers(); }
    }

    public float CloudHeightCutoff
    {
        get => cloudHeightCutoff;
        set { cloudHeightCutoff = Mathf.Max(0f, value); UpdateAllLayers(); }
    }

    public float DissipationRadialSpread
    {
        get => dissipationRadialSpread;
        set { dissipationRadialSpread = Mathf.Max(0f, value); UpdateAllLayers(); }
    }

    public float DissipationDuration
    {
        get => dissipationDuration;
        set { dissipationDuration = Mathf.Max(0f, value); }
    }

    public Vector3 DissipationDirection
    {
        get => dissipationDirection;
        set { dissipationDirection = value.sqrMagnitude > 0.001f ? value.normalized : Vector3.forward; UpdateAllLayers(); }
    }

    public float ZSweepSpeed
    {
        get => zSweepSpeed;
        set { zSweepSpeed = Mathf.Max(0f, value); UpdateAllLayers(); }
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

    public ParticleSystemSimulationSpace SweepSpace
    {
        get => sweepSpace;
        set { sweepSpace = value; UpdateAllLayers(); }
    }

    public void TriggerDissipation() => TriggerDissipation(dissipationDuration);

    public void TriggerDissipation(float duration)
    {
        enableDissipation = true;
        if (Application.isPlaying)
        {
            if (gameObject.activeInHierarchy)
            {
                StopAllCoroutines();
                StartCoroutine(AnimateDissipation(duration));
            }
            else
            {
                dissipationProgress = 1.0f;
                UpdateAllLayers();
            }
        }
        else
        {
#if UNITY_EDITOR
            TriggerEditorDissipation(duration);
#else
            dissipationProgress = 1.0f;
            UpdateAllLayers();
#endif
        }
    }

    private IEnumerator AnimateDissipation(float duration)
    {
        float elapsed = 0f;
        float startP = dissipationProgress;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            dissipationProgress = Mathf.Clamp01(Mathf.Lerp(startP, 1.0f, elapsed / duration));
            UpdateAllLayers();
            yield return null;
        }
        dissipationProgress = 1.0f;
        UpdateAllLayers();
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
        dissipationProgress = Mathf.Clamp01(Mathf.Lerp(editorStartProgress, 1.0f, editorElapsed / editorDuration));
        UpdateAllLayers();
        UnityEditor.EditorUtility.SetDirty(this);

        if (editorElapsed >= editorDuration)
        {
            UnityEditor.EditorApplication.update -= EditorDissipationStep;
            dissipationProgress = 1.0f;
            UpdateAllLayers();
        }
    }
#endif

    public void ResetDissipation()
    {
        enableDissipation = false;
        eliminateClouds = false;
        dissipationProgress = 0f;
        cloudHeightCutoff = vortexHeight;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorDissipationStep;
#endif
        if (Application.isPlaying) StopAllCoroutines();
        UpdateAllLayers();
        Play();
    }

    // ==========================================
    //  MATERIAL REFERENCES
    // ==========================================
    [Header("=== Materials & Textures ===")]
    [SerializeField] private Material smokePlumeMaterial;
    [SerializeField] private Material smokeWispMaterial;
    [SerializeField] private Material soulOrbMaterial;
    [SerializeField] private Material diamondSparkleMaterial;
    [SerializeField] private Material crossSparkleMaterial;
    [SerializeField] private Material energyMoteMaterial;

    // ==========================================
    //  CHILD PARTICLE SYSTEMS
    // ==========================================
    private ParticleSystem backdropFogPS;
    private ParticleSystem vortexWispsPS;
    private ParticleSystem groundMistPS;
    private ParticleSystem soulOrbsPS;
    private ParticleSystem diamondSparklesPS;
    private ParticleSystem crossSparklesPS;

    public float VortexRadius => vortexRadius;
    public float RotationSpeed => rotationSpeed;

    private void Awake()
    {
        InitializeEffect();
    }

    private void OnEnable()
    {
        LoadDefaultAssets();
        InitializeEffect();
        Play();
    }

    private void OnDisable()
    {
        Stop();
    }

    private void Update()
    {
        // Smoothly rotate the root container to add physical vortex swirl
        if (Mathf.Abs(rotationSpeed) > 0.01f)
        {
            float delta = Application.isPlaying ? Time.deltaTime : 0.016f;
            transform.Rotate(Vector3.up, rotationSpeed * 0.25f * delta, Space.Self);
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        // Never modify hierarchy on Prefab Assets on disk or during OnValidate callbacks
        if (!gameObject.scene.IsValid()) return;

        LoadDefaultAssets();
        FetchChildReferences();

        UnityEditor.EditorApplication.delayCall -= DelayedUpdate;
        UnityEditor.EditorApplication.delayCall += DelayedUpdate;
#endif
    }

#if UNITY_EDITOR
    private void DelayedUpdate()
    {
        if (this == null || !gameObject.scene.IsValid()) return;
        if (HasAllRequiredChildren())
        {
            UpdateAllLayers();
        }
        else
        {
            DelayedRebuild();
        }
    }

    private void DelayedRebuild()
    {
        if (this == null || !gameObject.scene.IsValid()) return;
        InitializeEffect();
    }
#endif

    private void LoadDefaultAssets()
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
        if (energyMoteMaterial == null)
            energyMoteMaterial = diamondSparkleMaterial;
#endif
    }

    private void FetchChildReferences()
    {
        if (backdropFogPS == null) backdropFogPS = FindChildPS("1_Backdrop_Billowing_Fog");
        if (vortexWispsPS == null) vortexWispsPS = FindChildPS("2_Swirling_Vortex_Wisps");
        if (groundMistPS == null) groundMistPS = FindChildPS("3_Ground_Creeping_Mist");
        if (soulOrbsPS == null) soulOrbsPS = FindChildPS("4_Floating_Soul_Orbs");
        if (diamondSparklesPS == null) diamondSparklesPS = FindChildPS("5a_Diamond_Sparkles");
        if (crossSparklesPS == null) crossSparklesPS = FindChildPS("5b_Cross_Sparkles");
    }

    private ParticleSystem FindChildPS(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<ParticleSystem>() : null;
    }

    private bool HasAllRequiredChildren()
    {
        return backdropFogPS != null &&
               vortexWispsPS != null &&
               groundMistPS != null &&
               soulOrbsPS != null &&
               diamondSparklesPS != null &&
               crossSparklesPS != null &&
               transform.Find("5_Sparkling_Energy_Motes") == null;
    }

    /// <summary>
    /// Set up all particle systems and configure their parameters.
    /// </summary>
    public void InitializeEffect()
    {
        LoadDefaultAssets();

        // Layer 1: Backdrop Billowing Fog
        backdropFogPS = GetOrCreateChildPS("1_Backdrop_Billowing_Fog");

        // Layer 2: Swirling Vortex Wisps
        vortexWispsPS = GetOrCreateChildPS("2_Swirling_Vortex_Wisps");

        // Layer 3: Ground Creeping Mist
        groundMistPS = GetOrCreateChildPS("3_Ground_Creeping_Mist");

        // Layer 4: Floating Soul Orbs
        soulOrbsPS = GetOrCreateChildPS("4_Floating_Soul_Orbs");

        // Clean up legacy motes child if present
        Transform legacyMotes = transform.Find("5_Sparkling_Energy_Motes");
        if (legacyMotes != null)
        {
            if (Application.isPlaying)
            {
                Destroy(legacyMotes.gameObject);
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (legacyMotes != null)
                        UnityEditor.Undo.DestroyObjectImmediate(legacyMotes.gameObject);
                };
#endif
            }
        }

        // Layer 5A: Pure Geometric Diamond Star Sparkles (Image 2 ◆)
        diamondSparklesPS = GetOrCreateChildPS("5a_Diamond_Sparkles");

        // Layer 5B: Pure Geometric Greek Cross Sparkles (Image 3 +)
        crossSparklesPS = GetOrCreateChildPS("5b_Cross_Sparkles");

        UpdateAllLayers();
    }

    public void UpdateAllLayers()
    {
        FetchChildReferences();

        float effectiveProgress = dissipationProgress;
        bool isFullClear = effectiveProgress >= 0.99f;

        // 1. Calculate Y-axis height cutoff (clouds and wisps shrink downwards)
        float effectiveHeight = vortexHeight;
        if (dissipationAxis == DissipationAxisType.Vertical_Y || dissipationAxis == DissipationAxisType.Both_Y_and_XZ)
        {
            float progressHeight = Mathf.Lerp(vortexHeight, 0f, effectiveProgress);
            effectiveHeight = Mathf.Min(cloudHeightCutoff, progressHeight);
        }
        else
        {
            effectiveHeight = Mathf.Min(cloudHeightCutoff, vortexHeight);
        }

        // 2. Calculate Cloud Alpha Multiplier (instant eliminateClouds toggle or fade)
        float cloudAlphaMult = 1f;
        if (eliminateClouds || isFullClear)
        {
            cloudAlphaMult = 0f;
        }
        else if (dissipationAxis == DissipationAxisType.Vertical_Y || dissipationAxis == DissipationAxisType.Both_Y_and_XZ || dissipationAxis == DissipationAxisType.Z_Axis_Forward || dissipationAxis == DissipationAxisType.Combined_Z_Sweep_And_Radial)
        {
            cloudAlphaMult = Mathf.Clamp01(1f - effectiveProgress * 1.05f);
            if (effectiveHeight <= 0.05f && (dissipationAxis == DissipationAxisType.Vertical_Y || dissipationAxis == DissipationAxisType.Both_Y_and_XZ)) cloudAlphaMult = 0f;
        }

        // 3. Calculate Ground Radius & Mist Alpha Multiplier (XZ plane radial spread)
        float currentGroundRadius = vortexRadius * 1.35f;
        float groundAlphaMult = 1f;
        if (isFullClear)
        {
            groundAlphaMult = 0f;
        }
        else if (dissipationAxis == DissipationAxisType.Radial_XZ || dissipationAxis == DissipationAxisType.Both_Y_and_XZ || dissipationAxis == DissipationAxisType.Combined_Z_Sweep_And_Radial)
        {
            currentGroundRadius = Mathf.Lerp(vortexRadius * 1.35f, dissipationRadialSpread, effectiveProgress);
            groundAlphaMult = Mathf.Clamp01(1f - effectiveProgress * 1.05f);
        }
        else if (dissipationAxis == DissipationAxisType.Z_Axis_Forward)
        {
            groundAlphaMult = Mathf.Clamp01(1f - effectiveProgress * 1.05f);
        }

        // 4. Sparkle Alpha Multiplier & Height
        float sparkleAlphaMult = isFullClear ? 0f : Mathf.Clamp01(1f - effectiveProgress * 1.05f);
        float sparkleHeight = Mathf.Max(0.1f, effectiveHeight);

        // Apply to Layer 1: Backdrop Billowing Fog
        if (backdropFogPS != null)
        {
            bool active = enableBackdropFog && !eliminateClouds && cloudAlphaMult > 0.005f && !isFullClear;
            backdropFogPS.gameObject.SetActive(active);
            if (active) ConfigureBackdropFog(backdropFogPS, effectiveHeight, cloudAlphaMult);
            else if (isFullClear) backdropFogPS.Clear();
        }

        // Apply to Layer 2: Swirling Vortex Wisps
        if (vortexWispsPS != null)
        {
            bool active = enableVortexWisps && !eliminateClouds && cloudAlphaMult > 0.005f && !isFullClear;
            vortexWispsPS.gameObject.SetActive(active);
            if (active) ConfigureVortexWisps(vortexWispsPS, effectiveHeight, cloudAlphaMult);
            else if (isFullClear) vortexWispsPS.Clear();
        }

        // Apply to Layer 3: Ground Creeping Mist
        if (groundMistPS != null)
        {
            bool active = enableGroundMist && groundAlphaMult > 0.005f && !isFullClear;
            groundMistPS.gameObject.SetActive(active);
            if (active) ConfigureGroundMist(groundMistPS, currentGroundRadius, groundAlphaMult);
            else if (isFullClear) groundMistPS.Clear();
        }

        // Apply to Layer 4: Floating Soul Orbs
        if (soulOrbsPS != null)
        {
            bool active = enableSoulOrbs && !eliminateClouds && cloudAlphaMult > 0.005f && !isFullClear;
            soulOrbsPS.gameObject.SetActive(active);
            if (active) ConfigureSoulOrbs(soulOrbsPS, effectiveHeight, cloudAlphaMult);
            else if (isFullClear) soulOrbsPS.Clear();
        }

        // Apply to Layer 5A: Diamond Star Sparkles
        if (diamondSparklesPS != null)
        {
            bool active = enableSparkles && enableDiamondSparkles && sparkleQuantity > 0.001f && sparkleAlphaMult > 0.005f && !isFullClear;
            diamondSparklesPS.gameObject.SetActive(active);
            if (active) ConfigureDiamondSparkles(diamondSparklesPS, sparkleHeight, sparkleAlphaMult);
            else if (isFullClear) diamondSparklesPS.Clear();
        }

        // Apply to Layer 5B: Cross Sparkles
        if (crossSparklesPS != null)
        {
            bool active = enableSparkles && enableCrossSparkles && sparkleQuantity > 0.001f && sparkleAlphaMult > 0.005f && !isFullClear;
            crossSparklesPS.gameObject.SetActive(active);
            if (active) ConfigureCrossSparkles(crossSparklesPS, sparkleHeight, sparkleAlphaMult);
            else if (isFullClear) crossSparklesPS.Clear();
        }
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
            child = go.transform;
        }

        ParticleSystem ps = child.GetComponent<ParticleSystem>();
        if (ps == null)
            ps = child.gameObject.AddComponent<ParticleSystem>();

        return ps;
    }

    // ==========================================
    //  LAYER 1: BACKDROP BILLOWING FOG (MASSIVE CLOUDS)
    // ==========================================
    private void ConfigureBackdropFog(ParticleSystem ps, float currentHeight = -1f, float alphaMult = 1f)
    {
        if (ps == null) return;
        if (currentHeight <= 0f) currentHeight = vortexHeight;
        if (alphaMult < 0f) alphaMult = 1f;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 5.2f);
        // Voluminous, grand cloud billows: 2.2m to 3.4m!
        main.startSize = new ParticleSystem.MinMaxCurve(2.2f * particleScale, 3.4f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 65;

        var emission = ps.emission;
        emission.rateOverTime = dissipationProgress >= 0.75f ? 0f : ((16f * densityMultiplier) * alphaMult);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f; // Cylinder
        shape.length = Mathf.Max(0.1f, currentHeight * 1.1f);
        shape.radius = vortexRadius * 1.1f;
        shape.radiusThickness = 0.65f;
        shape.position = new Vector3(0f, 0.2f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = dissipationProgress > 0.01f ? sweepSpace : ParticleSystemSimulationSpace.Local;
        if (dissipationProgress > 0.01f)
        {
            Vector3 dir = dissipationDirection.normalized;
            float sweep = zSweepSpeed * Mathf.Clamp01(dissipationProgress * 1.5f);
            vol.x = dir.x * sweep;
            vol.y = dir.y * sweep + upwardSpeed * 0.75f * (1f - dissipationProgress);
            vol.z = dir.z * sweep;
            vol.radial = 0.04f + lateralDispersion * 0.35f * dissipationProgress;
            vol.orbitalY = (rotationSpeed * Mathf.Deg2Rad * 0.5f) * (1f - dissipationProgress);
        }
        else
        {
            vol.x = 0f;
            vol.y = upwardSpeed * 0.75f;
            vol.z = 0f;
            vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 0.5f;
            vol.radial = 0.04f;
        }

        // Cloud expansion over lifetime: billows grow as they rise
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.55f);
        sc.AddKey(0.35f, 1.0f);
        sc.AddKey(0.75f, 1.35f);
        sc.AddKey(1.0f, 1.50f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        // Rich, visible cloud color gradient: Bone White transitioning into Lavender
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
                new GradientAlphaKey(0.65f * densityMultiplier * alphaMult, 0.18f),
                new GradientAlphaKey(0.60f * densityMultiplier * alphaMult, 0.65f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        // Rolling cloud billow rotation (disabled during dissipation tail stretch so smoke trails stay aligned with Z vector)
        var rol = ps.rotationOverLifetime;
        rol.enabled = dissipationProgress <= 0.02f;
        rol.z = new ParticleSystem.MinMaxCurve(-25f * Mathf.Deg2Rad, 25f * Mathf.Deg2Rad);

        // Soft turbulent cloud churning
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.32f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.18f + dissipationProgress * 0.25f;
        noise.damping = true;

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
    //  LAYER 2: SWIRLING VORTEX WISPS (CLOUD STREAMERS)
    // ==========================================
    private void ConfigureVortexWisps(ParticleSystem ps, float currentHeight = -1f, float alphaMult = 1f)
    {
        if (ps == null) return;
        if (currentHeight <= 0f) currentHeight = vortexHeight;
        if (alphaMult < 0f) alphaMult = 1f;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.4f * particleScale, 2.2f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 45;

        var emission = ps.emission;
        emission.rateOverTime = dissipationProgress >= 0.75f ? 0f : ((14f * densityMultiplier) * alphaMult);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = vortexRadius * 0.95f;
        shape.donutRadius = 0.35f;
        shape.position = new Vector3(0f, currentHeight * 0.35f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = dissipationProgress > 0.01f ? sweepSpace : ParticleSystemSimulationSpace.Local;
        if (dissipationProgress > 0.01f)
        {
            Vector3 dir = dissipationDirection.normalized;
            float sweep = zSweepSpeed * Mathf.Clamp01(dissipationProgress * 1.5f);
            vol.x = dir.x * sweep;
            vol.y = dir.y * sweep + upwardSpeed * 1.25f * (1f - dissipationProgress);
            vol.z = dir.z * sweep;
            vol.radial = 0.03f + lateralDispersion * 0.35f * dissipationProgress;
            vol.orbitalY = (rotationSpeed * Mathf.Deg2Rad * 1.5f) * (1f - dissipationProgress);
        }
        else
        {
            vol.x = 0f;
            vol.y = upwardSpeed * 1.25f;
            vol.z = 0f;
            vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 1.5f; // Fast swirling cloud streamers
            vol.radial = 0.03f;
        }

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.45f);
        sc.AddKey(0.35f, 1.0f);
        sc.AddKey(0.70f, 1.25f);
        sc.AddKey(1.0f, 0.40f);
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
                new GradientAlphaKey(0.60f * densityMultiplier * alphaMult, 0.18f),
                new GradientAlphaKey(0.55f * densityMultiplier * alphaMult, 0.65f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var rol = ps.rotationOverLifetime;
        rol.enabled = dissipationProgress <= 0.02f;
        rol.z = new ParticleSystem.MinMaxCurve(-40f * Mathf.Deg2Rad, 40f * Mathf.Deg2Rad);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.45f;
        noise.scrollSpeed = 0.25f + dissipationProgress * 0.25f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (enableTailDeformation && dissipationProgress > 0.02f)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = Mathf.Lerp(0.3f, tailDeformationScale * 1.2f, Mathf.Clamp01(dissipationProgress * 1.5f));
            renderer.lengthScale = Mathf.Lerp(1.0f, tailLengthScale * 1.1f, Mathf.Clamp01(dissipationProgress * 1.5f));
        }
        else
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
        renderer.material = smokeWispMaterial != null ? smokeWispMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 3;
    }

    // ==========================================
    //  LAYER 3: GROUND CREEPING MIST (ROLLING FLOOR FOG)
    // ==========================================
    private void ConfigureGroundMist(ParticleSystem ps, float currentRadius = -1f, float alphaMult = 1f)
    {
        if (ps == null) return;
        if (currentRadius <= 0f) currentRadius = vortexRadius * 1.35f;
        if (alphaMult < 0f) alphaMult = 1f;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.2f, 4.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(2.2f * particleScale, 3.8f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 35;

        var emission = ps.emission;
        emission.rateOverTime = dissipationProgress >= 0.75f ? 0f : ((10f * densityMultiplier) * alphaMult);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = currentRadius;
        shape.radiusThickness = 0.8f;
        shape.position = new Vector3(0f, 0.05f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = dissipationProgress > 0.01f ? sweepSpace : ParticleSystemSimulationSpace.Local;
        if (dissipationProgress > 0.01f)
        {
            Vector3 dir = dissipationDirection.normalized;
            float sweep = zSweepSpeed * Mathf.Clamp01(dissipationProgress * 1.5f);
            vol.x = dir.x * sweep;
            vol.y = dir.y * sweep + 0.02f;
            vol.z = dir.z * sweep;
            vol.radial = 0.08f + lateralDispersion * 0.4f * dissipationProgress;
            vol.orbitalY = (rotationSpeed * Mathf.Deg2Rad * 0.4f) * (1f - dissipationProgress);
        }
        else
        {
            vol.x = 0f;
            vol.y = 0.02f;
            vol.z = 0f;
            vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 0.4f;
            vol.radial = 0.08f; // Sprawling outward floor crawl
        }

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.5f);
        sc.AddKey(0.5f, 1.0f);
        sc.AddKey(1.0f, 1.45f);
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
                new GradientAlphaKey(0.55f * densityMultiplier * alphaMult, 0.20f),
                new GradientAlphaKey(0.50f * densityMultiplier * alphaMult, 0.70f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (enableTailDeformation && dissipationProgress > 0.02f)
        {
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = Mathf.Lerp(0.2f, tailDeformationScale * 0.9f, Mathf.Clamp01(dissipationProgress * 1.5f));
            renderer.lengthScale = Mathf.Lerp(1.0f, tailLengthScale, Mathf.Clamp01(dissipationProgress * 1.5f));
        }
        else
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }
        renderer.material = smokePlumeMaterial;
        renderer.sortingOrder = 0;
    }

    // ==========================================
    //  LAYER 4: FLOATING SOUL ORBS (ETHEREAL EMBERS)
    // ==========================================
    private void ConfigureSoulOrbs(ParticleSystem ps, float currentHeight = -1f, float alphaMult = 1f)
    {
        if (ps == null) return;
        if (currentHeight <= 0f) currentHeight = vortexHeight;
        if (alphaMult < 0f) alphaMult = 1f;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 5.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f * particleScale, 0.9f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.10f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-20f * Mathf.Deg2Rad, 20f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 6;

        var emission = ps.emission;
        emission.rateOverTime = dissipationProgress >= 0.75f ? 0f : (1.5f * alphaMult);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = vortexRadius * 0.9f;
        shape.position = new Vector3(0f, currentHeight * 0.65f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = dissipationProgress > 0.01f ? sweepSpace : ParticleSystemSimulationSpace.Local;
        if (dissipationProgress > 0.01f)
        {
            Vector3 dir = dissipationDirection.normalized;
            float sweep = zSweepSpeed * Mathf.Clamp01(dissipationProgress * 1.5f);
            vol.x = dir.x * sweep;
            vol.y = dir.y * sweep + upwardSpeed * 0.4f * (1f - dissipationProgress);
            vol.z = dir.z * sweep;
            vol.orbitalY = (rotationSpeed * Mathf.Deg2Rad * 0.35f) * (1f - dissipationProgress);
        }
        else
        {
            vol.x = 0f;
            vol.y = upwardSpeed * 0.4f;
            vol.z = 0f;
            vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 0.35f;
        }

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.3f);
        sc.AddKey(0.25f, 1.0f);
        sc.AddKey(0.75f, 1.0f);
        sc.AddKey(1.0f, 0.2f);
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
                new GradientAlphaKey(0.80f * alphaMult, 0.20f),
                new GradientAlphaKey(0.75f * alphaMult, 0.75f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.15f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.1f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.velocityScale = 0f;
        renderer.lengthScale = 1f;
        renderer.material = soulOrbMaterial != null ? soulOrbMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 4;
    }

    // ==========================================
    //  LAYER 5A: DIAMOND STAR SPARKLES (IMAGE 2 ◆)
    // ==========================================
    private void ConfigureDiamondSparkles(ParticleSystem ps, float currentHeight = -1f, float alphaMult = 1f)
    {
        if (ps == null) return;
        if (currentHeight <= 0f) currentHeight = vortexHeight;
        if (alphaMult < 0f) alphaMult = 1f;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
        float minS = Mathf.Max(0.005f, sparkleSize * 0.8f * particleScale);
        float maxS = Mathf.Max(minS, sparkleSize * 1.25f * particleScale);
        main.startSize = new ParticleSystem.MinMaxCurve(minS, maxS);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        // Emissive dual-color selection: Random between HDR Bone White and Lavender (Image 1 palette)
        Color hdrBoneWhite = new Color(boneWhiteColor.r * sparkleBloomIntensity, boneWhiteColor.g * sparkleBloomIntensity, boneWhiteColor.b * sparkleBloomIntensity, 1f);
        Color hdrLavender = new Color(midtoneViolet.r * sparkleBloomIntensity, midtoneViolet.g * sparkleBloomIntensity, midtoneViolet.b * sparkleBloomIntensity, 1f);
        main.startColor = new ParticleSystem.MinMaxGradient(hdrBoneWhite, hdrLavender);
        main.maxParticles = Mathf.Max(4, Mathf.CeilToInt(sparkleQuantity * 2.5f));

        // Pure geometric shape: NO texture sheet animation, zero circular disc halo
        var tsa = ps.textureSheetAnimation;
        tsa.enabled = false;

        var emission = ps.emission;
        emission.rateOverTime = (sparkleQuantity <= 0.001f || !enableSparkles || dissipationProgress >= 0.75f) ? 0f : (sparkleQuantity * 0.55f * densityMultiplier * alphaMult);

        // Volumetric 3D cylinder distribution across the full height and radius of the vortex
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f; // Perfect cylinder in Unity 6
        shape.radius = vortexRadius * 0.95f;
        shape.radiusThickness = 0.85f; // Fills the entire interior volume
        shape.length = Mathf.Max(0.1f, currentHeight * 1.0f);
        shape.position = new Vector3(0f, 0.20f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = dissipationProgress > 0.01f ? sweepSpace : ParticleSystemSimulationSpace.Local;
        if (dissipationProgress > 0.01f)
        {
            Vector3 dir = dissipationDirection.normalized;
            float sweep = zSweepSpeed * Mathf.Clamp01(dissipationProgress * 1.5f) * 0.6f;
            vol.x = dir.x * sweep;
            vol.y = dir.y * sweep + upwardSpeed * 1.15f * (1f - dissipationProgress);
            vol.z = dir.z * sweep;
            vol.orbitalY = (rotationSpeed * Mathf.Deg2Rad * moteOrbitMultiplier) * (1f - dissipationProgress);
        }
        else
        {
            vol.x = 0f;
            vol.y = upwardSpeed * 1.15f;
            vol.z = 0f;
            vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * moteOrbitMultiplier; // Fast orbiting sparkle swirl
            vol.radial = 0.02f;
        }

        // Sparkling Twinkle Size Curve: multi-pulse shimmer before dissolving
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.0f);
        sc.AddKey(0.12f, 1.0f);
        sc.AddKey(0.32f, 0.40f);
        sc.AddKey(0.52f, 1.20f);
        sc.AddKey(0.72f, 0.50f);
        sc.AddKey(0.88f, 1.05f);
        sc.AddKey(1.0f, 0.0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        // Star-point Twinkle Rotation over life
        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-moteTwinkleSpeed * Mathf.Deg2Rad, moteTwinkleSpeed * Mathf.Deg2Rad);

        // Luminous Bone White to Soft Lavender star sparkle gradient with HDR bloom
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
                new GradientAlphaKey(0.90f * alphaMult, 0.75f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.18f;
        noise.frequency = 0.65f;
        noise.scrollSpeed = 0.30f;
        noise.damping = true;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.velocityScale = 0f;
        renderer.lengthScale = 1f;
        renderer.material = diamondSparkleMaterial != null ? diamondSparkleMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 5;
    }

    // ==========================================
    //  LAYER 5B: GREEK CROSS SPARKLES (IMAGE 3 +)
    // ==========================================
    private void ConfigureCrossSparkles(ParticleSystem ps, float currentHeight = -1f, float alphaMult = 1f)
    {
        if (ps == null) return;
        if (currentHeight <= 0f) currentHeight = vortexHeight;
        if (alphaMult < 0f) alphaMult = 1f;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.9f);
        float minS = Mathf.Max(0.005f, sparkleSize * 0.75f * particleScale);
        float maxS = Mathf.Max(minS, sparkleSize * 1.15f * particleScale);
        main.startSize = new ParticleSystem.MinMaxCurve(minS, maxS);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        Color hdrBoneWhite = new Color(boneWhiteColor.r * sparkleBloomIntensity, boneWhiteColor.g * sparkleBloomIntensity, boneWhiteColor.b * sparkleBloomIntensity, 1f);
        Color hdrLavender = new Color(midtoneViolet.r * sparkleBloomIntensity, midtoneViolet.g * sparkleBloomIntensity, midtoneViolet.b * sparkleBloomIntensity, 1f);
        main.startColor = new ParticleSystem.MinMaxGradient(hdrBoneWhite, hdrLavender);
        main.maxParticles = Mathf.Max(4, Mathf.CeilToInt(sparkleQuantity * 2.5f));

        // Pure geometric shape: NO texture sheet animation, zero circular disc halo
        var tsa = ps.textureSheetAnimation;
        tsa.enabled = false;

        var emission = ps.emission;
        emission.rateOverTime = (sparkleQuantity <= 0.001f || !enableSparkles || dissipationProgress >= 0.75f) ? 0f : (sparkleQuantity * 0.45f * densityMultiplier * alphaMult);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.radius = vortexRadius * 0.92f;
        shape.radiusThickness = 0.85f;
        shape.length = Mathf.Max(0.1f, currentHeight * 1.0f);
        shape.position = new Vector3(0f, 0.20f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = dissipationProgress > 0.01f ? sweepSpace : ParticleSystemSimulationSpace.Local;
        if (dissipationProgress > 0.01f)
        {
            Vector3 dir = dissipationDirection.normalized;
            float sweep = zSweepSpeed * Mathf.Clamp01(dissipationProgress * 1.5f) * 0.6f;
            vol.x = dir.x * sweep;
            vol.y = dir.y * sweep + upwardSpeed * 1.10f * (1f - dissipationProgress);
            vol.z = dir.z * sweep;
            vol.orbitalY = (rotationSpeed * Mathf.Deg2Rad * (moteOrbitMultiplier * 0.95f)) * (1f - dissipationProgress);
        }
        else
        {
            vol.x = 0f;
            vol.y = upwardSpeed * 1.10f;
            vol.z = 0f;
            vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * (moteOrbitMultiplier * 0.95f);
            vol.radial = 0.02f;
        }

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.0f);
        sc.AddKey(0.15f, 1.0f);
        sc.AddKey(0.35f, 0.45f);
        sc.AddKey(0.55f, 1.15f);
        sc.AddKey(0.75f, 0.50f);
        sc.AddKey(0.90f, 1.0f);
        sc.AddKey(1.0f, 0.0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-moteTwinkleSpeed * 0.75f * Mathf.Deg2Rad, moteTwinkleSpeed * 0.75f * Mathf.Deg2Rad);

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
                new GradientAlphaKey(0.90f * alphaMult, 0.75f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.16f;
        noise.frequency = 0.60f;
        noise.scrollSpeed = 0.25f;
        noise.damping = true;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.velocityScale = 0f;
        renderer.lengthScale = 1f;
        renderer.material = crossSparkleMaterial != null ? crossSparkleMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 6;
    }

    /// <summary>
    /// Play all miasma systems.
    /// </summary>
    public void Play()
    {
        if (backdropFogPS != null && enableBackdropFog) backdropFogPS.Play();
        if (vortexWispsPS != null && enableVortexWisps) vortexWispsPS.Play();
        if (groundMistPS != null && enableGroundMist) groundMistPS.Play();
        if (soulOrbsPS != null && enableSoulOrbs) soulOrbsPS.Play();
        if (diamondSparklesPS != null && enableDiamondSparkles) diamondSparklesPS.Play();
        if (crossSparklesPS != null && enableCrossSparkles) crossSparklesPS.Play();
    }

    /// <summary>
    /// Stop all miasma systems gracefully.
    /// </summary>
    public void Stop()
    {
        if (backdropFogPS != null) backdropFogPS.Stop();
        if (vortexWispsPS != null) vortexWispsPS.Stop();
        if (groundMistPS != null) groundMistPS.Stop();
        if (soulOrbsPS != null) soulOrbsPS.Stop();
        if (diamondSparklesPS != null) diamondSparklesPS.Stop();
        if (crossSparklesPS != null) crossSparklesPS.Stop();
    }

    /// <summary>
    /// Immediately clear all particles.
    /// </summary>
    public void Clear()
    {
        if (backdropFogPS != null) backdropFogPS.Clear();
        if (vortexWispsPS != null) vortexWispsPS.Clear();
        if (groundMistPS != null) groundMistPS.Clear();
        if (soulOrbsPS != null) soulOrbsPS.Clear();
        if (diamondSparklesPS != null) diamondSparklesPS.Clear();
        if (crossSparklesPS != null) crossSparklesPS.Clear();
    }
}
