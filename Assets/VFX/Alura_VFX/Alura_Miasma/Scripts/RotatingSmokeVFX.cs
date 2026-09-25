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
/// - Emissive cloudy sparkles (no sharp slashes, no square quads, no spiky streaks)
/// 
/// 1. Backdrop Billowing Fog: Large voluminous Bone White & Lavender cumulus plumes.
/// 2. Swirling Vortex Wisps: Fast spiraling smoke streamers framing the magical rings.
/// 3. Ground Creeping Mist: Dense floor fog rolling and crawling outward.
/// 4. Floating Soul Orbs: Ethereal floating soul embers with luminous cores and lavender rims.
/// 5. Sparkling Energy Motes: Orbiting emissive smoky sparkles with additive glow.
/// 
/// Opacity Control:
/// - Master Opacity (1.0 -> 0.0): Smoothly fades all clouds and motes to 0 without any axis distortion.
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
    //  MASTER OPACITY & SMOOTH FADE
    // ==========================================
    [Header("=== Master Opacity & Fade ===")]
    [Tooltip("Master opacity multiplier (1.0 = full clouds, 0.0 = completely invisible/dissolved).")]
    [SerializeField, Range(0f, 1f)] private float masterOpacity = 1.0f;

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
    //  MASTER OPACITY PROPERTY & METHODS
    // ==========================================
    public float MasterOpacity
    {
        get => masterOpacity;
        set
        {
            masterOpacity = Mathf.Clamp01(value);
            UpdateAllLayers();
        }
    }

    public void FadeOut(float duration)
    {
        if (Application.isPlaying)
        {
            StopAllCoroutines();
            StartCoroutine(AnimateFade(duration));
        }
        else
        {
#if UNITY_EDITOR
            TriggerEditorFade(duration);
#else
            MasterOpacity = 0f;
#endif
        }
    }

    public void ResetOpacity()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorFadeStep;
#endif
        if (Application.isPlaying) StopAllCoroutines();
        masterOpacity = 1.0f;
        UpdateAllLayers();
        Play();
    }

    private IEnumerator AnimateFade(float duration)
    {
        float elapsed = 0f;
        float startOp = masterOpacity;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            MasterOpacity = Mathf.Lerp(startOp, 0f, elapsed / duration);
            yield return null;
        }
        MasterOpacity = 0f;
    }

#if UNITY_EDITOR
    private double lastFadeEditorTime;
    private float fadeEditorElapsed;
    private float fadeEditorDuration;
    private float fadeEditorStartOp;

    private void TriggerEditorFade(float duration)
    {
        fadeEditorElapsed = 0f;
        fadeEditorDuration = Mathf.Max(0.05f, duration);
        fadeEditorStartOp = masterOpacity;
        lastFadeEditorTime = UnityEditor.EditorApplication.timeSinceStartup;
        UnityEditor.EditorApplication.update -= EditorFadeStep;
        UnityEditor.EditorApplication.update += EditorFadeStep;
    }

    private void EditorFadeStep()
    {
        if (this == null)
        {
            UnityEditor.EditorApplication.update -= EditorFadeStep;
            return;
        }
        double now = UnityEditor.EditorApplication.timeSinceStartup;
        float dt = (float)(now - lastFadeEditorTime);
        lastFadeEditorTime = now;
        fadeEditorElapsed += dt;
        MasterOpacity = Mathf.Lerp(fadeEditorStartOp, 0f, fadeEditorElapsed / fadeEditorDuration);
        UnityEditor.EditorUtility.SetDirty(this);

        if (fadeEditorElapsed >= fadeEditorDuration)
        {
            UnityEditor.EditorApplication.update -= EditorFadeStep;
            MasterOpacity = 0f;
        }
    }
#endif

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

    // Public Getters for external mirroring
    public float VortexRadius => vortexRadius;
    public float VortexHeight => vortexHeight;
    public float RotationSpeed => rotationSpeed;
    public float UpwardSpeed => upwardSpeed;
    public float ParticleScale => particleScale;
    public float DensityMultiplier => densityMultiplier;
    public Color BoneWhiteColor => boneWhiteColor;
    public Color MidtoneViolet => midtoneViolet;
    public Color DeepCharcoalPurple => deepCharcoalPurple;
    public Color RimHighlightLilac => rimHighlightLilac;
    public Color GroundMistColor => groundMistColor;
    public Color NecroticGreenColor => necroticGreenColor;
    public float MoteOrbitMultiplier => moteOrbitMultiplier;
    public float MoteTwinkleSpeed => moteTwinkleSpeed;
    public Material SmokePlumeMaterial => smokePlumeMaterial;
    public Material SmokeWispMaterial => smokeWispMaterial;
    public Material SoulOrbMaterial => soulOrbMaterial;
    public Material DiamondSparkleMaterial => diamondSparkleMaterial;
    public Material CrossSparkleMaterial => crossSparkleMaterial;

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
        if (Mathf.Abs(rotationSpeed) > 0.01f && masterOpacity > 0.005f)
        {
            float delta = Application.isPlaying ? Time.deltaTime : 0.016f;
            transform.Rotate(Vector3.up, rotationSpeed * 0.25f * delta, Space.Self);
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
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
            smokePlumeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Miasma/Materials/NecromanticSmokeMat.mat");
        if (smokeWispMaterial == null)
            smokeWispMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Miasma/Materials/NecromanticWispMat.mat");
        if (soulOrbMaterial == null)
            soulOrbMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Miasma/Materials/MiasmaSoulOrbMat.mat");
        if (diamondSparkleMaterial == null)
            diamondSparkleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Miasma/Materials/MiasmaSparkleDiamondMat.mat");
        if (crossSparkleMaterial == null)
            crossSparkleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Miasma/Materials/MiasmaSparkleCrossMat.mat");
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

    public void InitializeEffect()
    {
        LoadDefaultAssets();

        backdropFogPS = GetOrCreateChildPS("1_Backdrop_Billowing_Fog");
        vortexWispsPS = GetOrCreateChildPS("2_Swirling_Vortex_Wisps");
        groundMistPS = GetOrCreateChildPS("3_Ground_Creeping_Mist");
        soulOrbsPS = GetOrCreateChildPS("4_Floating_Soul_Orbs");

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

        diamondSparklesPS = GetOrCreateChildPS("5a_Diamond_Sparkles");
        crossSparklesPS = GetOrCreateChildPS("5b_Cross_Sparkles");

        UpdateAllLayers();
    }

    public void UpdateAllLayers()
    {
        FetchChildReferences();

        bool isVisible = masterOpacity > 0.005f;

        // Layer 1: Backdrop Billowing Fog
        if (backdropFogPS != null)
        {
            bool active = enableBackdropFog && isVisible;
            backdropFogPS.gameObject.SetActive(active);
            if (active) ConfigureBackdropFog(backdropFogPS, masterOpacity);
            else backdropFogPS.Clear();
        }

        // Layer 2: Swirling Vortex Wisps
        if (vortexWispsPS != null)
        {
            bool active = enableVortexWisps && isVisible;
            vortexWispsPS.gameObject.SetActive(active);
            if (active) ConfigureVortexWisps(vortexWispsPS, masterOpacity);
            else vortexWispsPS.Clear();
        }

        // Layer 3: Ground Creeping Mist
        if (groundMistPS != null)
        {
            bool active = enableGroundMist && isVisible;
            groundMistPS.gameObject.SetActive(active);
            if (active) ConfigureGroundMist(groundMistPS, masterOpacity);
            else groundMistPS.Clear();
        }

        // Layer 4: Floating Soul Orbs
        if (soulOrbsPS != null)
        {
            bool active = enableSoulOrbs && isVisible;
            soulOrbsPS.gameObject.SetActive(active);
            if (active) ConfigureSoulOrbs(soulOrbsPS, masterOpacity);
            else soulOrbsPS.Clear();
        }

        // Layer 5A: Diamond Star Sparkles
        if (diamondSparklesPS != null)
        {
            bool active = enableSparkles && enableDiamondSparkles && sparkleQuantity > 0.001f && isVisible;
            diamondSparklesPS.gameObject.SetActive(active);
            if (active) ConfigureDiamondSparkles(diamondSparklesPS, masterOpacity);
            else diamondSparklesPS.Clear();
        }

        // Layer 5B: Cross Sparkles
        if (crossSparklesPS != null)
        {
            bool active = enableSparkles && enableCrossSparkles && sparkleQuantity > 0.001f && isVisible;
            crossSparklesPS.gameObject.SetActive(active);
            if (active) ConfigureCrossSparkles(crossSparklesPS, masterOpacity);
            else crossSparklesPS.Clear();
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
    //  LAYER 1: BACKDROP BILLOWING FOG
    // ==========================================
    private void ConfigureBackdropFog(ParticleSystem ps, float opacity)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.5f, 5.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(2.2f * particleScale, 3.4f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 65;

        var emission = ps.emission;
        emission.rateOverTime = (16f * densityMultiplier) * opacity;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.length = Mathf.Max(0.1f, vortexHeight * 1.1f);
        shape.radius = vortexRadius * 1.1f;
        shape.radiusThickness = 0.65f;
        shape.position = new Vector3(0f, 0.2f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.x = 0f;
        vol.y = upwardSpeed * 0.75f;
        vol.z = 0f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 0.5f;
        vol.radial = 0.04f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.55f);
        sc.AddKey(0.35f, 1.0f);
        sc.AddKey(0.75f, 1.35f);
        sc.AddKey(1.0f, 1.50f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

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
                new GradientAlphaKey(0.65f * densityMultiplier * opacity, 0.18f),
                new GradientAlphaKey(0.60f * densityMultiplier * opacity, 0.65f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-25f * Mathf.Deg2Rad, 25f * Mathf.Deg2Rad);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.32f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.18f;
        noise.damping = true;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.velocityScale = 0f;
        renderer.lengthScale = 1f;
        renderer.material = smokePlumeMaterial;
        renderer.sortingOrder = 1;
    }

    // ==========================================
    //  LAYER 2: SWIRLING VORTEX WISPS
    // ==========================================
    private void ConfigureVortexWisps(ParticleSystem ps, float opacity)
    {
        if (ps == null) return;

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
        emission.rateOverTime = (14f * densityMultiplier) * opacity;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = vortexRadius * 0.95f;
        shape.donutRadius = 0.35f;
        shape.position = new Vector3(0f, vortexHeight * 0.35f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.x = 0f;
        vol.y = upwardSpeed * 1.25f;
        vol.z = 0f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 1.5f;
        vol.radial = 0.03f;

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
                new GradientAlphaKey(0.60f * densityMultiplier * opacity, 0.18f),
                new GradientAlphaKey(0.55f * densityMultiplier * opacity, 0.65f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-40f * Mathf.Deg2Rad, 40f * Mathf.Deg2Rad);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.45f;
        noise.scrollSpeed = 0.25f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.velocityScale = 0f;
        renderer.lengthScale = 1f;
        renderer.material = smokeWispMaterial != null ? smokeWispMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 3;
    }

    // ==========================================
    //  LAYER 3: GROUND CREEPING MIST
    // ==========================================
    private void ConfigureGroundMist(ParticleSystem ps, float opacity)
    {
        if (ps == null) return;

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
        emission.rateOverTime = (10f * densityMultiplier) * opacity;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = vortexRadius * 1.35f;
        shape.radiusThickness = 0.8f;
        shape.position = new Vector3(0f, 0.05f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.x = 0f;
        vol.y = 0.02f;
        vol.z = 0f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 0.4f;
        vol.radial = 0.08f;

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
                new GradientAlphaKey(0.55f * densityMultiplier * opacity, 0.20f),
                new GradientAlphaKey(0.50f * densityMultiplier * opacity, 0.70f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.velocityScale = 0f;
        renderer.lengthScale = 1f;
        renderer.material = smokePlumeMaterial;
        renderer.sortingOrder = 0;
    }

    // ==========================================
    //  LAYER 4: FLOATING SOUL ORBS
    // ==========================================
    private void ConfigureSoulOrbs(ParticleSystem ps, float opacity)
    {
        if (ps == null) return;

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
        emission.rateOverTime = 1.5f * opacity;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = vortexRadius * 0.9f;
        shape.position = new Vector3(0f, vortexHeight * 0.65f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.x = 0f;
        vol.y = upwardSpeed * 0.4f;
        vol.z = 0f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 0.35f;

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
                new GradientAlphaKey(0.80f * opacity, 0.20f),
                new GradientAlphaKey(0.75f * opacity, 0.75f),
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
    //  LAYER 5A: DIAMOND STAR SPARKLES
    // ==========================================
    private void ConfigureDiamondSparkles(ParticleSystem ps, float opacity)
    {
        if (ps == null) return;

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

        Color hdrBoneWhite = new Color(boneWhiteColor.r * sparkleBloomIntensity, boneWhiteColor.g * sparkleBloomIntensity, boneWhiteColor.b * sparkleBloomIntensity, 1f);
        Color hdrLavender = new Color(midtoneViolet.r * sparkleBloomIntensity, midtoneViolet.g * sparkleBloomIntensity, midtoneViolet.b * sparkleBloomIntensity, 1f);
        main.startColor = new ParticleSystem.MinMaxGradient(hdrBoneWhite, hdrLavender);
        main.maxParticles = Mathf.Max(4, Mathf.CeilToInt(sparkleQuantity * 2.5f));

        var tsa = ps.textureSheetAnimation;
        tsa.enabled = false;

        var emission = ps.emission;
        emission.rateOverTime = (sparkleQuantity <= 0.001f || !enableSparkles) ? 0f : (sparkleQuantity * 0.55f * densityMultiplier * opacity);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.radius = vortexRadius * 0.95f;
        shape.radiusThickness = 0.85f;
        shape.length = Mathf.Max(0.1f, vortexHeight * 1.0f);
        shape.position = new Vector3(0f, 0.20f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.x = 0f;
        vol.y = upwardSpeed * 1.15f;
        vol.z = 0f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * moteOrbitMultiplier;
        vol.radial = 0.02f;

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

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-moteTwinkleSpeed * Mathf.Deg2Rad, moteTwinkleSpeed * Mathf.Deg2Rad);

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
                new GradientAlphaKey(0.95f * opacity, 0.15f),
                new GradientAlphaKey(0.90f * opacity, 0.75f),
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
    //  LAYER 5B: GREEK CROSS SPARKLES
    // ==========================================
    private void ConfigureCrossSparkles(ParticleSystem ps, float opacity)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.9f);
        float minS = Mathf.Max(0.005f, sparkleSize * 0.75f * particleScale);
        float maxS = Mathf.Max(minS, sparkleSize * 1.15f * particleScale);
        main.startSize = new ParticleSystem.MinMaxCurve(minS, maxS);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        Color hdrBoneWhite = new Color(boneWhiteColor.r * sparkleBloomIntensity, boneWhiteColor.g * sparkleBloomIntensity, boneWhiteColor.b * sparkleBloomIntensity, 1f);
        Color hdrLavender = new Color(midtoneViolet.r * sparkleBloomIntensity, midtoneViolet.g * sparkleBloomIntensity, midtoneViolet.b * sparkleBloomIntensity, 1f);
        main.startColor = new ParticleSystem.MinMaxGradient(hdrBoneWhite, hdrLavender);
        main.maxParticles = Mathf.Max(4, Mathf.CeilToInt(sparkleQuantity * 2.5f));

        var tsa = ps.textureSheetAnimation;
        tsa.enabled = false;

        var emission = ps.emission;
        emission.rateOverTime = (sparkleQuantity <= 0.001f || !enableSparkles) ? 0f : (sparkleQuantity * 0.45f * densityMultiplier * opacity);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.radius = vortexRadius * 0.85f;
        shape.radiusThickness = 0.85f;
        shape.length = Mathf.Max(0.1f, vortexHeight * 0.9f);
        shape.position = new Vector3(0f, 0.20f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.x = 0f;
        vol.y = upwardSpeed * 1.05f;
        vol.z = 0f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * (moteOrbitMultiplier * 0.9f);
        vol.radial = 0.02f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.0f);
        sc.AddKey(0.14f, 1.0f);
        sc.AddKey(0.35f, 0.35f);
        sc.AddKey(0.55f, 1.15f);
        sc.AddKey(0.75f, 0.45f);
        sc.AddKey(0.90f, 1.0f);
        sc.AddKey(1.0f, 0.0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-moteTwinkleSpeed * Mathf.Deg2Rad, moteTwinkleSpeed * Mathf.Deg2Rad);

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
                new GradientAlphaKey(0.95f * opacity, 0.15f),
                new GradientAlphaKey(0.90f * opacity, 0.75f),
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
}
