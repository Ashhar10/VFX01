using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Rotating Necromantic Smoke & Miasma VFX.
/// Alura Graves' "3. MIASMA (Ultimate)" Wind-up effect.
/// 
/// Color palette: Bone White (cream/off-white clouds) with Lavender (soft purple) accents.
/// Aesthetic: Soft, cloudy, volumetric smoke with ethereal glowing sparkles.
/// 
/// 1. Backdrop Billowing Fog: Bone White clouds with Lavender highlights.
/// 2. Swirling Vortex Wisps: Lavender-tinted smoke ribbons framing the magical rings.
/// 3. Ground Creeping Mist: Bone White floor fog with soft Lavender tint.
/// 4. Floating Soul Orbs: Ethereal bone-white orbs with lavender rim glow.
/// 5. Sparkling Energy Motes: Soft glowing cloud embers with emissive bone-white/lavender glow.
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
    [SerializeField, Range(0.5f, 5.0f)] private float vortexHeight = 2.2f;

    [Tooltip("Vortex rotation / swirl speed (degrees/sec).")]
    [SerializeField, Range(-200f, 200f)] private float rotationSpeed = 55f;

    [Tooltip("Upward drift speed of the smoke (m/s).")]
    [SerializeField, Range(0.05f, 2.0f)] private float upwardSpeed = 0.35f;

    [Tooltip("Overall scale multiplier for smoke particles.")]
    [SerializeField, Range(0.3f, 2.5f)] private float particleScale = 1.0f;

    [Tooltip("Density / Opacity multiplier for the smoke.")]
    [SerializeField, Range(0.1f, 2.5f)] private float densityMultiplier = 1.0f;

    // ==========================================
    //  COLOR PALETTE (BONE WHITE & LAVENDER)
    // ==========================================
    [Header("=== Color Palette (Bone White & Lavender) ===")]
    [Tooltip("Primary cloud color: Bone White (cream / off-white).")]
    [SerializeField] private Color deepCharcoalPurple = new Color(0.90f, 0.86f, 0.80f, 0.30f);

    [Tooltip("Accent cloud highlight: Lavender (soft purple).")]
    [SerializeField] private Color midtoneViolet = new Color(0.62f, 0.48f, 0.78f, 0.35f);

    [Tooltip("Bright rim highlight: Light Lavender / White rim catch.")]
    [SerializeField] private Color rimHighlightLilac = new Color(0.80f, 0.72f, 0.90f, 0.50f);

    [Tooltip("Ground creeping mist color: Bone White with slight lavender tint.")]
    [SerializeField] private Color groundMistColor = new Color(0.85f, 0.82f, 0.80f, 0.25f);

    [Tooltip("Sparks Color: Bone White with warm glow.")]
    [SerializeField] private Color necroticGreenColor = new Color(0.95f, 0.90f, 0.85f, 1.0f);

    [Tooltip("Healing / Life Energy Sparks Color (Bone White ✚).")]
    [SerializeField] private Color boneWhiteColor = new Color(0.92f, 0.88f, 0.82f, 1.0f);

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

    [Tooltip("Layer 5: Sparkling Energy Motes (✚ and ◆ stars).")]
    [SerializeField] private bool enableEnergyMotes = true;

    // ==========================================
    //  MATERIAL REFERENCES
    // ==========================================
    [Header("=== Materials & Textures ===")]
    [SerializeField] private Material smokePlumeMaterial;
    [SerializeField] private Material smokeWispMaterial;
    [SerializeField] private Material soulOrbMaterial;
    [SerializeField] private Material energyMoteMaterial;

    // ==========================================
    //  CHILD PARTICLE SYSTEMS
    // ==========================================
    private ParticleSystem backdropFogPS;
    private ParticleSystem vortexWispsPS;
    private ParticleSystem groundMistPS;
    private ParticleSystem soulOrbsPS;
    private ParticleSystem energyMotesPS;

    private bool isInitialized;

    public float VortexRadius => vortexRadius;
    public float RotationSpeed => rotationSpeed;

    private void Awake()
    {
        InitializeEffect();
    }

    private void OnEnable()
    {
        LoadDefaultAssets();
        if (!isInitialized || transform.childCount == 0)
        {
            InitializeEffect();
        }
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
        LoadDefaultAssets();
        if (!isInitialized || transform.childCount == 0)
        {
            InitializeEffect();
            return;
        }
        UpdateAllLayers();
    }

    private void LoadDefaultAssets()
    {
#if UNITY_EDITOR
        if (smokePlumeMaterial == null)
            smokePlumeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/NecromanticSmokeMat.mat");
        if (smokeWispMaterial == null)
            smokeWispMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/NecromanticWispMat.mat");
        if (soulOrbMaterial == null)
            soulOrbMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaSoulOrbMat.mat");
        if (energyMoteMaterial == null)
            energyMoteMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Materials/MiasmaEnergyMoteMat.mat");
#endif
    }

    /// <summary>
    /// Set up all 5 particle systems and configure their parameters.
    /// </summary>
    public void InitializeEffect()
    {
        LoadDefaultAssets();

        // Layer 1: Backdrop Billowing Fog
        backdropFogPS = GetOrCreateChildPS("1_Backdrop_Billowing_Fog");
        ConfigureBackdropFog(backdropFogPS);

        // Layer 2: Swirling Vortex Wisps
        vortexWispsPS = GetOrCreateChildPS("2_Swirling_Vortex_Wisps");
        ConfigureVortexWisps(vortexWispsPS);

        // Layer 3: Ground Creeping Mist
        groundMistPS = GetOrCreateChildPS("3_Ground_Creeping_Mist");
        ConfigureGroundMist(groundMistPS);

        // Layer 4: Floating Soul Orbs
        soulOrbsPS = GetOrCreateChildPS("4_Floating_Soul_Orbs");
        ConfigureSoulOrbs(soulOrbsPS);

        // Layer 5: Sparkling Energy Motes
        energyMotesPS = GetOrCreateChildPS("5_Sparkling_Energy_Motes");
        ConfigureEnergyMotes(energyMotesPS);

        isInitialized = true;
    }

    public void UpdateAllLayers()
    {
        if (backdropFogPS != null)
        {
            backdropFogPS.gameObject.SetActive(enableBackdropFog);
            ConfigureBackdropFog(backdropFogPS);
        }
        if (vortexWispsPS != null)
        {
            vortexWispsPS.gameObject.SetActive(enableVortexWisps);
            ConfigureVortexWisps(vortexWispsPS);
        }
        if (groundMistPS != null)
        {
            groundMistPS.gameObject.SetActive(enableGroundMist);
            ConfigureGroundMist(groundMistPS);
        }
        if (soulOrbsPS != null)
        {
            soulOrbsPS.gameObject.SetActive(enableSoulOrbs);
            ConfigureSoulOrbs(soulOrbsPS);
        }
        if (energyMotesPS != null)
        {
            energyMotesPS.gameObject.SetActive(enableEnergyMotes);
            ConfigureEnergyMotes(energyMotesPS);
        }
    }

    private ParticleSystem GetOrCreateChildPS(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName);
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
    private void ConfigureBackdropFog(ParticleSystem ps)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.0f, 4.5f);
        // Tuned size: 1.1m to 1.7m (gracefully frames the character without overwhelming)
        main.startSize = new ParticleSystem.MinMaxCurve(1.1f * particleScale, 1.7f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = deepCharcoalPurple;
        main.maxParticles = 35;

        var emission = ps.emission;
        emission.rateOverTime = 8f * densityMultiplier;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f; // Cylinder
        shape.length = vortexHeight * 0.9f;
        shape.radius = vortexRadius * 1.1f;
        shape.radiusThickness = 0.5f;
        shape.position = new Vector3(0f, 0.15f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.y = upwardSpeed * 0.8f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 0.5f;
        vol.radial = 0.04f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.6f);
        sc.AddKey(0.35f, 1.0f);
        sc.AddKey(0.80f, 1.25f);
        sc.AddKey(1.0f, 1.35f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        // Soft cloudy alpha curve: max ~0.40 for visible bone-white volumetric clouds
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.95f, 0.92f, 0.88f), 0.0f),
                new GradientColorKey(new Color(0.80f, 0.72f, 0.88f), 0.35f),
                new GradientColorKey(new Color(0.92f, 0.88f, 0.84f), 0.7f),
                new GradientColorKey(new Color(0.85f, 0.80f, 0.90f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.40f * densityMultiplier, 0.20f),
                new GradientAlphaKey(0.38f * densityMultiplier, 0.60f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-20f * Mathf.Deg2Rad, 20f * Mathf.Deg2Rad);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.15f;
        noise.damping = true;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = smokePlumeMaterial;
        renderer.sortingOrder = 1;
    }

    // ==========================================
    //  LAYER 2: SWIRLING VORTEX WISPS
    // ==========================================
    private void ConfigureVortexWisps(ParticleSystem ps)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f * particleScale, 1.15f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = midtoneViolet;
        main.maxParticles = 28;

        var emission = ps.emission;
        emission.rateOverTime = 11f * densityMultiplier;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = vortexRadius * 0.85f;
        shape.donutRadius = 0.25f;
        shape.position = new Vector3(0f, vortexHeight * 0.35f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.y = upwardSpeed * 1.3f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 1.6f; // Fast orbiting wisps
        vol.radial = 0.02f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.4f);
        sc.AddKey(0.3f, 1.0f);
        sc.AddKey(0.7f, 1.1f);
        sc.AddKey(1.0f, 0.3f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.92f, 0.88f, 0.95f), 0.0f),
                new GradientColorKey(new Color(0.70f, 0.58f, 0.85f), 0.35f),
                new GradientColorKey(new Color(0.88f, 0.82f, 0.92f), 0.7f),
                new GradientColorKey(new Color(0.80f, 0.75f, 0.88f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.48f * densityMultiplier, 0.15f),
                new GradientAlphaKey(0.45f * densityMultiplier, 0.60f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-45f * Mathf.Deg2Rad, 45f * Mathf.Deg2Rad);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.45f;
        noise.scrollSpeed = 0.25f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = smokeWispMaterial != null ? smokeWispMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 3;
    }

    // ==========================================
    //  LAYER 3: GROUND CREEPING MIST
    // ==========================================
    private void ConfigureGroundMist(ParticleSystem ps)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 4.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.2f * particleScale, 2.0f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = groundMistColor;
        main.maxParticles = 22;

        var emission = ps.emission;
        emission.rateOverTime = 6f * densityMultiplier;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = vortexRadius * 1.25f;
        shape.radiusThickness = 0.6f;
        shape.position = new Vector3(0f, 0.05f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.y = 0.02f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 0.45f;
        vol.radial = 0.06f; // Gentle outward crawl

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.5f);
        sc.AddKey(0.5f, 1.0f);
        sc.AddKey(1.0f, 1.35f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.90f, 0.87f, 0.84f), 0.0f),
                new GradientColorKey(new Color(0.82f, 0.76f, 0.86f), 0.4f),
                new GradientColorKey(new Color(0.88f, 0.84f, 0.82f), 0.7f),
                new GradientColorKey(new Color(0.85f, 0.80f, 0.84f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.35f * densityMultiplier, 0.20f),
                new GradientAlphaKey(0.32f * densityMultiplier, 0.65f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = smokePlumeMaterial;
        renderer.sortingOrder = 0;
    }

    // ==========================================
    //  LAYER 4: FLOATING SOUL ORBS (SKULL MOTES)
    // ==========================================
    private void ConfigureSoulOrbs(ParticleSystem ps)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.8f, 5.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.45f * particleScale, 0.75f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-15f * Mathf.Deg2Rad, 15f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 5;

        var emission = ps.emission;
        emission.rateOverTime = 1.0f; // Only 3 to 5 floating orbs active at any time

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = vortexRadius * 0.9f;
        shape.position = new Vector3(0f, vortexHeight * 0.65f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.y = upwardSpeed * 0.35f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 0.35f; // Slow floating orbit

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
                new GradientColorKey(new Color(0.92f, 0.88f, 0.84f), 0.0f),
                new GradientColorKey(new Color(0.78f, 0.70f, 0.88f), 0.4f),
                new GradientColorKey(new Color(0.88f, 0.84f, 0.90f), 0.7f),
                new GradientColorKey(new Color(0.85f, 0.80f, 0.86f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.65f, 0.2f),
                new GradientAlphaKey(0.60f, 0.75f),
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
        renderer.material = soulOrbMaterial != null ? soulOrbMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 4;
    }

    // ==========================================
    //  LAYER 5: SPARKLING ENERGY MOTES (✚ & ◆)
    // ==========================================
    private void ConfigureEnergyMotes(ParticleSystem ps)
    {
        if (ps == null) return;

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f * particleScale, 0.40f * particleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.10f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        
        // Two-color random gradient between Pale Necrotic Green and Bone White
        Gradient startGrad = new Gradient();
        startGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(necroticGreenColor, 0.0f),
                new GradientColorKey(boneWhiteColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 1.0f)
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(startGrad);
        main.maxParticles = 35;

        var emission = ps.emission;
        emission.rateOverTime = 16f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = vortexRadius * 0.85f;
        shape.donutRadius = 0.35f;
        shape.position = new Vector3(0f, vortexHeight * 0.45f, 0f);
        shape.rotation = new Vector3(90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.y = upwardSpeed * 0.9f;
        vol.orbitalY = rotationSpeed * Mathf.Deg2Rad * 1.8f; // Fast energetic orbit
        vol.radial = 0.01f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.2f);
        sc.AddKey(0.3f, 1.0f);
        sc.AddKey(0.7f, 0.9f);
        sc.AddKey(1.0f, 0.0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.95f, 0.92f, 0.88f), 0.0f),
                new GradientColorKey(new Color(0.85f, 0.78f, 0.92f), 0.5f),
                new GradientColorKey(new Color(0.90f, 0.85f, 0.82f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.85f, 0.15f),
                new GradientAlphaKey(0.80f, 0.65f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.2f;
        noise.frequency = 0.8f;
        noise.scrollSpeed = 0.4f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = energyMoteMaterial != null ? energyMoteMaterial : smokePlumeMaterial;
        renderer.sortingOrder = 5;
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
        if (energyMotesPS != null && enableEnergyMotes) energyMotesPS.Play();
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
        if (energyMotesPS != null) energyMotesPS.Stop();
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
        if (energyMotesPS != null) energyMotesPS.Clear();
    }
}
