using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Alura Graves "3. MIASMA" Affected Enemy Status Cloud VFX (Images 4 & 5).
/// 
/// Applies a lingering necromantic debuff / curse cloud around an enemy:
/// 1. Spectral Skull Icon: Stylized glowing necromantic skull hovering over the face/chest (Image 4).
/// 2. Body Mist Shroud: Volumetric necromantic fog cylinder wrapping around the enemy's torso (Image 4 & 5).
/// 3. Waist Orbit Ring: Ethereal spectral ring revolving around the waist/hips (Image 5).
/// 4. Status Sparkles: Tiny orbiting star sparkles (◆ Diamond Stars & + Greek Crosses from Images 2 & 3).
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class MiasmaEnemyEffectVFX : MonoBehaviour
{
    // ==========================================
    //  TARGET & GEOMETRY
    // ==========================================
    [Header("=== Enemy Body Bounds & Dynamics ===")]
    [Tooltip("Radius of the mist shroud around the enemy (meters).")]
    [SerializeField, Range(0.3f, 2.5f)] private float bodyRadius = 0.65f;

    [Tooltip("Total height of the affected enemy shroud (meters).")]
    [SerializeField, Range(0.8f, 3.5f)] private float bodyHeight = 1.8f;

    [Tooltip("Swirl rotation speed around the enemy (degrees/sec).")]
    [SerializeField, Range(-180f, 180f)] private float swirlSpeed = 50f;

    [Tooltip("Upward smoke drift speed (m/s).")]
    [SerializeField, Range(0.05f, 1.5f)] private float upwardSpeed = 0.35f;

    // ==========================================
    //  LAYER 1: SPECTRAL SKULL MASK (IMAGE 4)
    // ==========================================
    [Header("=== Layer 1: Spectral Skull Mask (Image 4) ===")]
    [Tooltip("Enable the glowing spectral skull mask over the enemy's head/chest.")]
    [SerializeField] private bool enableSkullMask = true;

    [Tooltip("Height offset of the skull mask (meters above feet).")]
    [SerializeField, Range(0.5f, 2.5f)] private float skullHeightOffset = 1.35f;

    [Tooltip("Size/scale of the floating skull icon.")]
    [SerializeField, Range(0.2f, 1.5f)] private float skullScale = 0.65f;

    [Tooltip("Rhythmic breathing/pulsing speed of the skull icon.")]
    [SerializeField, Range(0.5f, 8.0f)] private float skullPulseSpeed = 2.5f;

    [Tooltip("Skull color tint (Bone White core with eerie spectral rim).")]
    [SerializeField] private Color skullColor = new Color(0.96f, 0.98f, 0.92f, 0.95f);

    // ==========================================
    //  LAYER 2: BODY MIST SHROUD (IMAGE 4 & 5)
    // ==========================================
    [Header("=== Layer 2: Body Mist Shroud (Torso Fog) ===")]
    [Tooltip("Enable the volumetric body mist shroud.")]
    [SerializeField] private bool enableBodyMist = true;

    [Tooltip("Density/opacity multiplier for the body fog.")]
    [SerializeField, Range(0.1f, 2.5f)] private float mistDensity = 1.0f;

    [Tooltip("Scale multiplier for the smoke billow puffs.")]
    [SerializeField, Range(0.3f, 2.0f)] private float mistParticleScale = 0.9f;

    // ==========================================
    //  LAYER 3: WAIST ORBIT RING (IMAGE 5)
    // ==========================================
    [Header("=== Layer 3: Waist Orbit Ring (Image 5) ===")]
    [Tooltip("Enable the spectral ring revolving around the waist.")]
    [SerializeField] private bool enableWaistRing = true;

    [Tooltip("Height of the waist ring above the ground (meters).")]
    [SerializeField, Range(0.2f, 2.0f)] private float waistRingHeight = 0.85f;

    [Tooltip("Radius of the waist ring (meters).")]
    [SerializeField, Range(0.3f, 2.0f)] private float waistRingRadius = 0.75f;

    // ==========================================
    // ==========================================
    //  LAYER 4: USER-FRIENDLY STATUS SPARKLES
    // ==========================================
    [Header("=== Layer 4: Status Sparkles (User Friendly) ===")]
    [Tooltip("Master toggle for all status sparkles around the enemy.")]
    [SerializeField] private bool enableSparkles = true;

    [Tooltip("Enable Diamond Star sparkles (Image 2 ◆).")]
    [SerializeField] private bool enableDiamondSparkles = true;

    [Tooltip("Enable Greek Cross sparkles (Image 3 +).")]
    [SerializeField] private bool enableCrossSparkles = true;

    [Tooltip("Sparkle Quantity: Single clean slider (0 = OFF, 1-15 = subtle delicate twinkles). Controls BOTH Diamond ◆ and Cross + shapes.")]
    [SerializeField, Range(0f, 15f)] private float sparkleQuantity = 4.0f;

    [Tooltip("Sparkle Size: controls the size of the twinkling star motes around the enemy (small and delicate).")]
    [SerializeField, Range(0.01f, 0.06f)] private float sparkleSize = 0.028f;

    [Tooltip("Sparkle Glow / Bloom intensity multiplier.")]
    [SerializeField, Range(0f, 6.0f)] private float sparkleBloomIntensity = 2.5f;

    public bool EnableSparkles
    {
        get => enableSparkles && (enableDiamondSparkles || enableCrossSparkles);
        set { enableSparkles = value; enableDiamondSparkles = value; enableCrossSparkles = value; UpdateAllLayers(); }
    }

    public float SparkleQuantity
    {
        get => sparkleQuantity;
        set { sparkleQuantity = Mathf.Clamp(value, 0f, 15f); UpdateAllLayers(); }
    }

    public float SparkleSize
    {
        get => sparkleSize;
        set { sparkleSize = Mathf.Clamp(value, 0.01f, 0.06f); UpdateAllLayers(); }
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
    //  SKULL BLOOM & EMISSION INTENSITY
    // ==========================================
    [Header("=== Skull Bloom & Emission Intensity ===")]
    [Tooltip("HDR Bloom & Emission multiplier for the spectral skull mask.")]
    [SerializeField, Range(0f, 10.0f)] private float skullBloomIntensity = 3.5f;

    public float SkullBloomIntensity
    {
        get => skullBloomIntensity;
        set { skullBloomIntensity = Mathf.Max(0f, value); UpdateAllLayers(); }
    }

    // ==========================================
    //  COLOR PALETTE (IMAGE 1)
    // ==========================================
    [Header("=== Color Palette (Image 1 Swatches) ===")]
    [Tooltip("Bone White (Heal/Core Life Energy) - Hex #F6F3EC")]
    [SerializeField] private Color boneWhiteColor = new Color(0.96f, 0.93f, 0.88f, 1.0f);

    [Tooltip("Lavender (Mystical Accent) - Hex #BFA3E6")]
    [SerializeField] private Color lavenderColor = new Color(0.75f, 0.60f, 0.90f, 1.0f);

    [Tooltip("Spectral Eerie Tint (Image 4 Spectral Aura)")]
    [SerializeField] private Color spectralTint = new Color(0.78f, 0.95f, 0.82f, 1.0f);

    // ==========================================
    //  MATERIALS
    // ==========================================
    [Header("=== Materials & Textures ===")]
    [SerializeField] private Material skullMaterial;
    [SerializeField] private Material smokePlumeMaterial;
    [SerializeField] private Material smokeWispMaterial;
    [SerializeField] private Material diamondSparkleMaterial;
    [SerializeField] private Material crossSparkleMaterial;
    [SerializeField] private Material energyMoteMaterial;

    // Child Particle Systems
    private ParticleSystem skullPS;
    private ParticleSystem bodyMistPS;
    private ParticleSystem waistRingPS;
    private ParticleSystem diamondSparklesPS;
    private ParticleSystem crossSparklesPS;

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
        if (Mathf.Abs(swirlSpeed) > 0.01f)
        {
            float delta = Application.isPlaying ? Time.deltaTime : 0.016f;
            transform.Rotate(Vector3.up, swirlSpeed * 0.3f * delta, Space.Self);
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

    public void LoadDefaultAssets()
    {
#if UNITY_EDITOR
        if (skullMaterial == null)
            skullMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Miasma/Materials/MiasmaSpectralSkullMat.mat");
        if (smokePlumeMaterial == null)
            smokePlumeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Miasma/Materials/NecromanticSmokeMat.mat");
        if (smokeWispMaterial == null)
            smokeWispMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Miasma/Materials/NecromanticWispMat.mat");
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
        if (skullPS == null) skullPS = FindChildPS("1_Spectral_Skull_Icon");
        if (bodyMistPS == null) bodyMistPS = FindChildPS("2_Body_Mist_Shroud");
        if (waistRingPS == null) waistRingPS = FindChildPS("3_Waist_Orbit_Ring");
        if (diamondSparklesPS == null) diamondSparklesPS = FindChildPS("4a_Diamond_Sparkles");
        if (crossSparklesPS == null) crossSparklesPS = FindChildPS("4b_Cross_Sparkles");
    }

    private ParticleSystem FindChildPS(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<ParticleSystem>() : null;
    }

    private bool HasAllRequiredChildren()
    {
        return skullPS != null &&
               bodyMistPS != null &&
               waistRingPS != null &&
               diamondSparklesPS != null &&
               crossSparklesPS != null &&
               transform.Find("4_Status_Sparkles") == null;
    }

    public void InitializeEffect()
    {
        LoadDefaultAssets();

        // 1. Spectral Skull Icon (Image 4)
        skullPS = GetOrCreateChildPS("1_Spectral_Skull_Icon");
        ConfigureSkullMask(skullPS);

        // 2. Body Mist Shroud (Image 4 & 5)
        bodyMistPS = GetOrCreateChildPS("2_Body_Mist_Shroud");
        ConfigureBodyMist(bodyMistPS);

        // 3. Waist Orbit Ring (Image 5)
        waistRingPS = GetOrCreateChildPS("3_Waist_Orbit_Ring");
        ConfigureWaistRing(waistRingPS);

        // Remove legacy combined sparkles child if present
        Transform legacySparkles = transform.Find("4_Status_Sparkles");
        if (legacySparkles != null)
        {
            if (Application.isPlaying)
            {
                Destroy(legacySparkles.gameObject);
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (legacySparkles != null)
                        UnityEditor.Undo.DestroyObjectImmediate(legacySparkles.gameObject);
                };
#endif
            }
        }

        // 4a. Diamond Star Sparkles (Image 2 ◆)
        diamondSparklesPS = GetOrCreateChildPS("4a_Diamond_Sparkles");
        ConfigureDiamondSparkles(diamondSparklesPS);

        // 4b. Greek Cross Sparkles (Image 3 +)
        crossSparklesPS = GetOrCreateChildPS("4b_Cross_Sparkles");
        ConfigureCrossSparkles(crossSparklesPS);
    }

    public void UpdateAllLayers()
    {
        FetchChildReferences();

        if (skullPS != null)
        {
            skullPS.gameObject.SetActive(enableSkullMask);
            ConfigureSkullMask(skullPS);
        }
        if (bodyMistPS != null)
        {
            bodyMistPS.gameObject.SetActive(enableBodyMist);
            ConfigureBodyMist(bodyMistPS);
        }
        if (waistRingPS != null)
        {
            waistRingPS.gameObject.SetActive(enableWaistRing);
            ConfigureWaistRing(waistRingPS);
        }
        if (diamondSparklesPS != null)
        {
            bool active = enableSparkles && enableDiamondSparkles && sparkleQuantity > 0.001f;
            diamondSparklesPS.gameObject.SetActive(active);
            if (active) ConfigureDiamondSparkles(diamondSparklesPS);
        }
        if (crossSparklesPS != null)
        {
            bool active = enableSparkles && enableCrossSparkles && sparkleQuantity > 0.001f;
            crossSparklesPS.gameObject.SetActive(active);
            if (active) ConfigureCrossSparkles(crossSparklesPS);
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
        {
            ps = child.gameObject.AddComponent<ParticleSystem>();
        }
        return ps;
    }

    // ==========================================
    //  LAYER 1: SPECTRAL SKULL MASK
    // ==========================================
    private void ConfigureSkullMask(ParticleSystem ps)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = Mathf.Max(0.5f, 6.0f / Mathf.Max(0.1f, skullPulseSpeed));
        main.startSize = skullScale;
        main.startSpeed = 0f;
        main.startRotation = 0f;
        Color hdrSkull = new Color(skullColor.r * skullBloomIntensity, skullColor.g * skullBloomIntensity, skullColor.b * skullBloomIntensity, skullColor.a);
        Color hdrSpectral = new Color(spectralTint.r * skullBloomIntensity, spectralTint.g * skullBloomIntensity, spectralTint.b * skullBloomIntensity, spectralTint.a);
        main.startColor = hdrSkull;
        main.maxParticles = 1;

        var emission = ps.emission;
        emission.rateOverTime = 1f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.01f;
        shape.position = new Vector3(0f, skullHeightOffset, 0.12f); // Slightly in front of face

        // Rhythmic breathing / pulsing scale curve
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.90f);
        sc.AddKey(0.25f, 1.10f);
        sc.AddKey(0.50f, 0.95f);
        sc.AddKey(0.75f, 1.08f);
        sc.AddKey(1.0f, 0.90f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        // Alpha & Color with HDR Bloom
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(hdrSkull, 0.0f),
                new GradientColorKey(hdrSpectral, 0.5f),
                new GradientColorKey(hdrSkull, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.95f, 0.15f),
                new GradientAlphaKey(0.90f, 0.85f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = skullMaterial != null ? skullMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 6;
    }

    // ==========================================
    //  LAYER 2: BODY MIST SHROUD
    // ==========================================
    private void ConfigureBodyMist(ParticleSystem ps)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.9f * mistParticleScale, 1.5f * mistParticleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 35;

        var emission = ps.emission;
        emission.rateOverTime = 12f * mistDensity;

        // Upright cylinder enclosing the enemy body
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f; // Perfect cylinder in Unity 6
        shape.radius = bodyRadius;
        shape.radiusThickness = 0.75f;
        shape.length = bodyHeight;
        shape.position = new Vector3(0f, 0.1f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.x = 0f;
        vol.y = upwardSpeed;
        vol.z = 0f;
        vol.orbitalY = swirlSpeed * Mathf.Deg2Rad * 0.7f;
        vol.radial = 0.02f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.5f);
        sc.AddKey(0.4f, 1.0f);
        sc.AddKey(1.0f, 1.35f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(boneWhiteColor, 0.0f),
                new GradientColorKey(spectralTint, 0.45f),
                new GradientColorKey(lavenderColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.60f * mistDensity, 0.20f),
                new GradientAlphaKey(0.55f * mistDensity, 0.70f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.22f;
        noise.frequency = 0.40f;
        noise.scrollSpeed = 0.20f;
        noise.damping = true;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = smokeWispMaterial != null ? smokeWispMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 2;
    }

    // ==========================================
    //  LAYER 3: WAIST ORBIT RING
    // ==========================================
    private void ConfigureWaistRing(ParticleSystem ps)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 45;

        var emission = ps.emission;
        emission.rateOverTime = 25f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = waistRingRadius;
        shape.donutRadius = 0.08f;
        shape.position = new Vector3(0f, waistRingHeight, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.x = 0f;
        vol.y = 0.02f;
        vol.z = 0f;
        vol.orbitalY = swirlSpeed * Mathf.Deg2Rad * 2.0f; // Fast sparkle orbit

        // Twinkle size curve (grow -> shrink -> grow for sparkle feel)
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.2f);
        sc.AddKey(0.15f, 1.0f);
        sc.AddKey(0.4f, 0.3f);
        sc.AddKey(0.65f, 0.9f);
        sc.AddKey(1.0f, 0.0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        // HDR sparkle colors: bone white core -> spectral green -> lavender fade
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(boneWhiteColor, 0.0f),
                new GradientColorKey(spectralTint, 0.3f),
                new GradientColorKey(lavenderColor, 0.7f),
                new GradientColorKey(spectralTint, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.15f),
                new GradientAlphaKey(0.85f, 0.75f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        // Use sparkle material instead of smoke wisp for twinkling star orbit
        renderer.material = diamondSparkleMaterial != null ? diamondSparkleMaterial : (energyMoteMaterial != null ? energyMoteMaterial : smokePlumeMaterial);
        renderer.sortingOrder = 3;
    }

    // ==========================================
    //  LAYER 4A: STATUS DIAMOND SPARKLES (IMAGE 2 ◆)
    // ==========================================
    private void ConfigureDiamondSparkles(ParticleSystem ps)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.8f);
        float minS = Mathf.Max(0.005f, sparkleSize * 0.8f);
        float maxS = Mathf.Max(minS, sparkleSize * 1.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(minS, maxS);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        // Dual-color with HDR Bloom: Random between Bone White and Lavender (Image 1 palette)
        Color hdrBoneWhite = new Color(boneWhiteColor.r * sparkleBloomIntensity, boneWhiteColor.g * sparkleBloomIntensity, boneWhiteColor.b * sparkleBloomIntensity, 1f);
        Color hdrLavender = new Color(lavenderColor.r * sparkleBloomIntensity, lavenderColor.g * sparkleBloomIntensity, lavenderColor.b * sparkleBloomIntensity, 1f);
        main.startColor = new ParticleSystem.MinMaxGradient(hdrBoneWhite, hdrLavender);
        main.maxParticles = Mathf.Max(3, Mathf.CeilToInt(sparkleQuantity * 2.5f));

        // Pure geometric shape: NO texture sheet animation, zero circular disc halo
        var tsa = ps.textureSheetAnimation;
        tsa.enabled = false;

        var emission = ps.emission;
        emission.rateOverTime = (sparkleQuantity <= 0.001f || !enableSparkles) ? 0f : (sparkleQuantity * 0.55f);

        // Cylinder shape around enemy body
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f; // Perfect cylinder in Unity 6
        shape.radius = bodyRadius * 1.15f;
        shape.radiusThickness = 0.85f;
        shape.length = bodyHeight * 0.95f;
        shape.position = new Vector3(0f, 0.2f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.x = 0f;
        vol.y = upwardSpeed * 0.8f;
        vol.z = 0f;
        vol.orbitalY = swirlSpeed * Mathf.Deg2Rad * 1.8f; // Fast orbiting sparkles
        vol.radial = 0.02f;

        // Twinkle shimmer curve
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.0f);
        sc.AddKey(0.15f, 1.0f);
        sc.AddKey(0.40f, 0.4f);
        sc.AddKey(0.65f, 1.2f);
        sc.AddKey(1.0f, 0.0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        // Star-point spin
        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-120f * Mathf.Deg2Rad, 120f * Mathf.Deg2Rad);

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
                new GradientAlphaKey(0.95f, 0.15f),
                new GradientAlphaKey(0.85f, 0.75f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = diamondSparkleMaterial != null ? diamondSparkleMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 5;
    }

    // ==========================================
    //  LAYER 4B: STATUS GREEK CROSS SPARKLES (IMAGE 3 +)
    // ==========================================
    private void ConfigureCrossSparkles(ParticleSystem ps)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.7f);
        float minS = Mathf.Max(0.005f, sparkleSize * 0.75f);
        float maxS = Mathf.Max(minS, sparkleSize * 1.15f);
        main.startSize = new ParticleSystem.MinMaxCurve(minS, maxS);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

        Color hdrBoneWhite = new Color(boneWhiteColor.r * sparkleBloomIntensity, boneWhiteColor.g * sparkleBloomIntensity, boneWhiteColor.b * sparkleBloomIntensity, 1f);
        Color hdrLavender = new Color(lavenderColor.r * sparkleBloomIntensity, lavenderColor.g * sparkleBloomIntensity, lavenderColor.b * sparkleBloomIntensity, 1f);
        main.startColor = new ParticleSystem.MinMaxGradient(hdrBoneWhite, hdrLavender);
        main.maxParticles = Mathf.Max(3, Mathf.CeilToInt(sparkleQuantity * 2.5f));

        // Pure geometric shape: NO texture sheet animation, zero circular disc halo
        var tsa = ps.textureSheetAnimation;
        tsa.enabled = false;

        var emission = ps.emission;
        emission.rateOverTime = (sparkleQuantity <= 0.001f || !enableSparkles) ? 0f : (sparkleQuantity * 0.45f);

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.radius = bodyRadius * 1.10f;
        shape.radiusThickness = 0.85f;
        shape.length = bodyHeight * 0.95f;
        shape.position = new Vector3(0f, 0.2f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.x = 0f;
        vol.y = upwardSpeed * 0.75f;
        vol.z = 0f;
        vol.orbitalY = swirlSpeed * Mathf.Deg2Rad * 1.7f;
        vol.radial = 0.02f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.0f);
        sc.AddKey(0.15f, 1.0f);
        sc.AddKey(0.40f, 0.45f);
        sc.AddKey(0.65f, 1.15f);
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
                new GradientAlphaKey(0.95f, 0.15f),
                new GradientAlphaKey(0.85f, 0.75f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = crossSparkleMaterial != null ? crossSparkleMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 6;
    }

    public void Play()
    {
        if (skullPS != null && enableSkullMask) skullPS.Play();
        if (bodyMistPS != null && enableBodyMist) bodyMistPS.Play();
        if (waistRingPS != null && enableWaistRing) waistRingPS.Play();
        if (diamondSparklesPS != null && enableDiamondSparkles) diamondSparklesPS.Play();
        if (crossSparklesPS != null && enableCrossSparkles) crossSparklesPS.Play();
    }

    public void Stop()
    {
        if (skullPS != null) skullPS.Stop();
        if (bodyMistPS != null) bodyMistPS.Stop();
        if (waistRingPS != null) waistRingPS.Stop();
        if (diamondSparklesPS != null) diamondSparklesPS.Stop();
        if (crossSparklesPS != null) crossSparklesPS.Stop();
    }

    public void Clear()
    {
        if (skullPS != null) skullPS.Clear();
        if (bodyMistPS != null) bodyMistPS.Clear();
        if (waistRingPS != null) waistRingPS.Clear();
        if (diamondSparklesPS != null) diamondSparklesPS.Clear();
        if (crossSparklesPS != null) crossSparklesPS.Clear();
    }
}
