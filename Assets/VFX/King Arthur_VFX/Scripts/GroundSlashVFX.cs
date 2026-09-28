using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gabriel Aguiar Style Traveling Ground Slash & Molten Fissure VFX.
/// 
/// Upgrades:
/// 1. Random Breakout Nodes & Vibration:
///    - Organic lateral zigzag jitter along the crack path.
///    - Random rock spread jitter, tilt vibration, and cluster bursting (1 to 3 rock chunks).
/// 2. Leading Wave (Vertical Crescent in YZ plane + Horizontal Wave in XZ plane).
/// 3. Much larger sliders for Sparkles, Fog/Vapor, and timing parameters.
/// 4. Zero black boxes / soft depth-blended shaders.
/// 5. Global & Slash Bloom sliders for real-time glow control.
/// </summary>
[DisallowMultipleComponent]
public class GroundSlashVFX : MonoBehaviour
{
    // ==========================================
    //  PLAYBACK & LOOPING
    // ==========================================
    [Header("=== Playback & Looping ===")]
    [Tooltip("Automatically play when the GameObject or component is enabled.")]
    [SerializeField] private bool playOnEnable = true;

    [Tooltip("Whether the ground slash should repeat continuously in a loop.")]
    [SerializeField] private bool loop = true;

    [Tooltip("Time interval in seconds between consecutive loops (after the slash completes).")]
    [SerializeField, Range(0f, 15f)] private float loopInterval = 1.5f;

    public bool PlayOnEnable { get => playOnEnable; set => playOnEnable = value; }
    public bool Loop { get => loop; set => loop = value; }
    public float LoopInterval { get => loopInterval; set => loopInterval = value; }

    // ==========================================
    //  BLOOM & RADIANCE CONTROLS
    // ==========================================
    [Header("=== Bloom & Radiance Controls ===")]
    [Tooltip("Master Bloom / Radiance multiplier for ground slash emissive brightness (0.2 = subtle, 0.65 = balanced holy gold, 1.5+ = high glow).")]
    [SerializeField, Range(0.1f, 3.0f)] private float slashBloomIntensity = 0.65f;

    [Tooltip("Synchronize and control the overall Scene Post-Processing Bloom directly from this Inspector.")]
    [SerializeField] private bool controlSceneGlobalBloom = true;

    [Tooltip("Overall Scene Global Volume Bloom Intensity (0 = Off, 0.4 = Subtle, 0.65 = Cinematic, 1.5+ = High).")]
    [SerializeField, Range(0f, 3f)] private float sceneBloomIntensity = 0.65f;

    [Tooltip("Overall Scene Bloom Scatter (glow diffusion radius).")]
    [SerializeField, Range(0.1f, 1f)] private float sceneBloomScatter = 0.65f;

    public float SlashBloomIntensity
    {
        get => slashBloomIntensity;
        set
        {
            slashBloomIntensity = Mathf.Clamp(value, 0.1f, 3f);
            ApplyBloomSettings();
        }
    }

    public float SceneBloomIntensity
    {
        get => sceneBloomIntensity;
        set
        {
            sceneBloomIntensity = Mathf.Clamp(value, 0f, 3f);
            ApplyBloomSettings();
        }
    }

    // ==========================================
    //  TRAVEL & TRAJECTORY (LARGE SLIDERS)
    // ==========================================
    [Header("=== Travel & Trajectory (Large Sliders) ===")]
    [Tooltip("Origin of the ground slash. If null, uses this transform's position.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Direction the slash travels. If zero, uses local forward.")]
    [SerializeField] private Vector3 slashDirection = Vector3.forward;

    [Tooltip("How far the ground slash travels forward across the ground (meters).")]
    [SerializeField, Range(2f, 40f)] private float travelDistance = 12f;

    [Tooltip("Forward traveling speed (m/s).")]
    [SerializeField, Range(2f, 40f)] private float travelSpeed = 12f;

    // ==========================================
    //  LEADING WAVE (VERTICAL & HORIZONTAL CRESCENTS)
    // ==========================================
    [Header("=== Leading Crescent Blade (Front Cutting Wave) ===")]
    [Tooltip("Enable the vertical crescent blade slicing through the ground at the front.")]
    [SerializeField] private bool enableVerticalBlade = true;

    [Tooltip("Scale / height of the vertical crescent blade (larger slider).")]
    [SerializeField, Range(0.5f, 8f)] private float verticalBladeScale = 2.0f;

    [Tooltip("Enable horizontal ground shockwave crescent skimming the floor.")]
    [SerializeField] private bool enableHorizontalWave = true;

    [Tooltip("Scale of the horizontal ground shockwave crescent (larger slider).")]
    [SerializeField, Range(0.5f, 8f)] private float horizontalWaveScale = 1.8f;

    [Tooltip("Blade body energy color (HDR).")]
    [SerializeField, ColorUsage(true, true)] private Color bladeColor = new Color(16f, 7f, 1f, 1f);

    [Tooltip("Blinding white-hot leading cutting edge (HDR).")]
    [SerializeField, ColorUsage(true, true)] private Color bladeLeadColor = new Color(20f, 16f, 8f, 1f);

    // ==========================================
    //  MOLTEN GROUND FISSURE (IMAGE 2 & 3 STYLE)
    // ==========================================
    [Header("=== Molten Fissure (Image 2 & 3: Molten Spot Chain) ===")]
    [Tooltip("Enable the molten magma fissure path left along the ground.")]
    [SerializeField] private bool enableMoltenFissure = true;

    [Tooltip("Distance between consecutive molten spots along the path (meters).")]
    [SerializeField, Range(0.2f, 3f)] private float spotSpacing = 0.65f;

    [Tooltip("Diameter of each molten spot on the ground (meters).")]
    [SerializeField, Range(0.3f, 5f)] private float spotDiameter = 1.4f;

    [Tooltip("How long the molten crack stays hot on the ground (seconds).")]
    [SerializeField, Range(0.5f, 15f)] private float fissureDuration = 3.0f;

    [Tooltip("Time to cool down from hot lava to charred rock and dissolve (seconds).")]
    [SerializeField, Range(0.2f, 6f)] private float fissureCoolDuration = 1.0f;

    [Tooltip("White-hot molten core color (HDR).")]
    [SerializeField, ColorUsage(true, true)] private Color moltenColor = new Color(18f, 9f, 1.5f, 1f);

    [Tooltip("Glowing magma crust color (HDR).")]
    [SerializeField, ColorUsage(true, true)] private Color crustColor = new Color(10f, 3.5f, 0.6f, 1f);

    [Tooltip("Charred rock edge color.")]
    [SerializeField] private Color stoneColor = new Color(0.12f, 0.09f, 0.07f, 1f);

    // ==========================================
    //  RANDOM BREAKOUT NODES & VIBRATION
    // ==========================================
    [Header("=== Random Breakout Nodes & Vibration ===")]
    [Tooltip("Random lateral zigzag jitter of the fissure path (creates organic fractured crack).")]
    [SerializeField, Range(0f, 0.8f)] private float pathJitter = 0.25f;

    [Tooltip("Random lateral offset jitter for rock slabs (breaks up straight zipper look).")]
    [SerializeField, Range(0f, 1f)] private float rockSpreadJitter = 0.35f;

    [Tooltip("Random rotation / tilt vibration for rock slabs (degrees).")]
    [SerializeField, Range(0f, 90f)] private float rockAngleJitter = 40f;

    [Tooltip("Chance to spawn a multi-rock cluster at breakout points (0 = single, 1 = always cluster).")]
    [SerializeField, Range(0f, 1f)] private float rockClusterChance = 0.5f;

    // ==========================================
    //  RAISED TILTED ROCK SLABS
    // ==========================================
    [Header("=== Raised Rock Slabs (Left & Right Flanks) ===")]
    [Tooltip("Enable physical 3D rock slabs erupted on both sides of the crack.")]
    [SerializeField] private bool enableRockSlabs = true;

    [Tooltip("Base sideways distance of rocks from the center crack (meters).")]
    [SerializeField, Range(0.2f, 3f)] private float rockFlankOffset = 0.55f;

    [Tooltip("Rock slab scale (larger slider).")]
    [SerializeField, Range(0.1f, 1.5f)] private float rockScale = 0.32f;

    [Tooltip("Outward tilt angle of the rock slabs (degrees).")]
    [SerializeField, Range(10f, 75f)] private float rockTiltAngle = 32f;

    [Tooltip("Height rock slabs rise above the ground (meters).")]
    [SerializeField, Range(0.05f, 1.5f)] private float rockRiseHeight = 0.25f;

    [Tooltip("Molten underside emission color on rocks (HDR).")]
    [SerializeField, ColorUsage(true, true)] private Color rockMoltenEmission = new Color(1.5f, 0.6f, 0.1f, 1f);

    // ==========================================
    //  TWINKLING DIAMOND SPARKS (LARGER SLIDERS)
    // ==========================================
    [Header("=== Twinkling Diamond Sparks (Larger Sliders) ===")]
    [Tooltip("Enable delicate 4-point diamond star sparks along the crack.")]
    [SerializeField] private bool enableSparks = true;

    [Tooltip("Spark particle size (larger slider up to 0.15).")]
    [SerializeField, Range(0.005f, 0.15f)] private float sparkSize = 0.025f;

    [Tooltip("Sparks emitted per ground node (larger slider up to 25).")]
    [SerializeField, Range(1, 25)] private int sparksPerNode = 4;

    [Tooltip("Spark lifetime (seconds).")]
    [SerializeField, Range(0.2f, 4f)] private float sparkLifetime = 1.2f;

    [Tooltip("Spark upward drift speed (m/s).")]
    [SerializeField, Range(0.2f, 6f)] private float sparkUpSpeed = 1.5f;

    [Tooltip("Spark color (HDR).")]
    [SerializeField, ColorUsage(true, true)] private Color sparkColor = new Color(15f, 10f, 2.5f, 1f);

    // ==========================================
    //  WARM HEAT VAPOR / FOG (LARGER SLIDERS)
    // ==========================================
    [Header("=== Warm Heat Smoke / Fog (Larger Sliders) ===")]
    [Tooltip("Enable soft golden heat smoke puffs when ground cracks (Zero black artifacts).")]
    [SerializeField] private bool enableHeatVapor = true;

    [Tooltip("Vapor / fog particle size (larger slider up to 5.0m).")]
    [SerializeField, Range(0.2f, 5.0f)] private float vaporSize = 1.2f;

    [Tooltip("Vapor puffs emitted per node.")]
    [SerializeField, Range(1, 8)] private int vaporPerNode = 2;

    [Tooltip("Vapor lifetime (seconds).")]
    [SerializeField, Range(0.4f, 4.0f)] private float vaporLifetime = 1.5f;

    [Tooltip("Vapor color (HDR).")]
    [SerializeField, ColorUsage(true, true)] private Color vaporColor = new Color(1.8f, 1.2f, 0.4f, 0.3f);

    // ==========================================
    //  LIGHT & CAMERA SHAKE
    // ==========================================
    [Header("=== Traveling Light & Camera Shake ===")]
    [Tooltip("Enable traveling point light at leading wave.")]
    [SerializeField] private bool enableLight = true;

    [Tooltip("Light intensity.")]
    [SerializeField, Range(1f, 30f)] private float lightIntensity = 8f;

    [Tooltip("Light range.")]
    [SerializeField, Range(1f, 25f)] private float lightRange = 7f;

    [Tooltip("Light color.")]
    [SerializeField] private Color lightColor = new Color(1f, 0.75f, 0.25f, 1f);

    [Tooltip("Enable camera shake on impact.")]
    [SerializeField] private bool enableCameraShake = true;

    [Tooltip("Camera shake intensity.")]
    [SerializeField, Range(0f, 0.5f)] private float shakeIntensity = 0.14f;

    [Tooltip("Camera shake duration (seconds).")]
    [SerializeField, Range(0.05f, 0.6f)] private float shakeDuration = 0.22f;

    // ==========================================
    //  SCREEN DISTORTION / REFRACTION RING
    // ==========================================
    [Header("=== Screen Distortion / Refraction Ring ===")]
    [Tooltip("Enable optical refraction / distortion ring along the shockwave edge.")]
    [SerializeField] private bool enableDistortionRing = true;

    [Tooltip("Maximum scale / diameter of the distortion ring (meters).")]
    [SerializeField, Range(1f, 20f)] private float distortionRingScale = 6.5f;

    [Tooltip("Expansion duration of the distortion ring in seconds.")]
    [SerializeField, Range(0.1f, 1.5f)] private float distortionDuration = 0.35f;

    [Tooltip("Optical refraction strength.")]
    [SerializeField, Range(0.01f, 0.2f)] private float distortionStrength = 0.06f;

    [Tooltip("Chromatic aberration spread across the distortion ridge.")]
    [SerializeField, Range(0f, 0.05f)] private float distortionChromaticAberration = 0.015f;

    [Tooltip("Shockwave rim glow color (HDR).")]
    [SerializeField, ColorUsage(true, true)] private Color distortionRimGlow = new Color(15f, 10f, 3f, 1f);

    // ==========================================
    //  DYNAMIC GROUND LIGHTING (BENEATH SLASH PATH)
    // ==========================================
    [Header("=== Dynamic Ground Lighting (Beneath Slash Path) ===")]
    [Tooltip("Enable subtle dynamic point lights beneath the slash path to illuminate the trench and rock faces.")]
    [SerializeField] private bool enableGroundPathLights = true;

    [Tooltip("Intensity of the ground path lights.")]
    [SerializeField, Range(0.5f, 10f)] private float groundLightIntensity = 3.5f;

    [Tooltip("Radius / range of the ground path lights.")]
    [SerializeField, Range(1f, 10f)] private float groundLightRange = 3.5f;

    [Tooltip("Color of the ground path illumination.")]
    [SerializeField] private Color groundLightColor = new Color(1f, 0.58f, 0.18f, 1f);

    [Tooltip("Distance spacing between dynamic ground lights along the path (meters).")]
    [SerializeField, Range(0.5f, 6f)] private float groundLightSpacing = 2.0f;

    // ==========================================
    //  SELF-CONTAINED MATERIALS & TEXTURES (EXPORT SAFE)
    // ==========================================
    [Header("=== Materials & Shaders (Export Safe) ===")]
    [Tooltip("Pre-configured material for vertical crescent blade.")]
    [SerializeField] private Material verticalBladeMat;

    [Tooltip("Pre-configured material for horizontal ground wave.")]
    [SerializeField] private Material horizontalWaveMat;

    [Tooltip("Pre-configured material for molten ground fissure.")]
    [SerializeField] private Material groundFissureMat;

    [Tooltip("Pre-configured material for erupted chiseled rocks.")]
    [SerializeField] private Material rockLitMat;

    [Tooltip("Pre-configured material for diamond sparks.")]
    [SerializeField] private Material sparkMat;

    [Tooltip("Pre-configured material for heat smoke / vapor.")]
    [SerializeField] private Material vaporMat;

    [Tooltip("Pre-configured material for shockwave distortion ring.")]
    [SerializeField] private Material distortionRingMat;

    [Header("=== VFX Textures (Export Safe) ===")]
    [Tooltip("Circular soft texture for smoke vapor puffs.")]
    [SerializeField] private Texture2D vaporTexture;

    [Tooltip("4-point star / diamond texture for sparks.")]
    [SerializeField] private Texture2D sparkTexture;

    [Tooltip("Crescent slash texture.")]
    [SerializeField] private Texture2D crescentTexture;

    // ==========================================
    //  RUNTIME STATE & REUSABLE ASSETS
    // ==========================================
    private bool isPlaying;
    private List<GameObject> activeInstances = new List<GameObject>();
    private Coroutine loopCoroutine;
    private Coroutine slashCoroutine;
    private Camera shakingCam;
    private Vector3 origCamPos;

    private Mesh verticalQuadMesh;
    private Mesh horizontalQuadMesh;
    private Mesh chiseledRockMesh;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        InitializeMeshes();
        InitializeMaterials();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    private void InitializeMeshes()
    {
        // 1. Vertical Quad in YZ plane
        verticalQuadMesh = new Mesh();
        verticalQuadMesh.name = "VerticalBladeQuad_YZ";
        verticalQuadMesh.vertices = new Vector3[]
        {
            new Vector3(0f, 0f, -0.5f),
            new Vector3(0f, 0f,  0.5f),
            new Vector3(0f, 1f,  0.5f),
            new Vector3(0f, 1f, -0.5f)
        };
        verticalQuadMesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };
        verticalQuadMesh.colors = new Color[]
        {
            Color.white, Color.white, Color.white, Color.white
        };
        verticalQuadMesh.triangles = new int[]
        {
            0, 2, 1, 0, 3, 2,
            0, 1, 2, 0, 2, 3
        };
        verticalQuadMesh.RecalculateNormals();
        verticalQuadMesh.RecalculateBounds();

        // 2. Horizontal Quad in XZ plane
        horizontalQuadMesh = new Mesh();
        horizontalQuadMesh.name = "HorizontalWaveQuad_XZ";
        horizontalQuadMesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3( 0.5f, 0f, -0.5f),
            new Vector3( 0.5f, 0f,  0.5f),
            new Vector3(-0.5f, 0f,  0.5f)
        };
        horizontalQuadMesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };
        horizontalQuadMesh.colors = new Color[]
        {
            Color.white, Color.white, Color.white, Color.white
        };
        horizontalQuadMesh.triangles = new int[]
        {
            0, 2, 1, 0, 3, 2,
            0, 1, 2, 0, 2, 3
        };
        horizontalQuadMesh.RecalculateNormals();
        horizontalQuadMesh.RecalculateBounds();

        // 3. Chiseled 3D Rock Mesh
        chiseledRockMesh = CreateChiseledRockMesh();
    }

    private void InitializeMaterials()
    {
        // 1. Vertical Blade Material
        if (verticalBladeMat == null)
        {
            Shader vertShader = Shader.Find("VFX/VerticalCrescent");
            if (vertShader == null) vertShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            verticalBladeMat = new Material(vertShader);
        }
        else
        {
            verticalBladeMat = new Material(verticalBladeMat);
        }
        verticalBladeMat.SetColor("_Color", bladeColor);
        verticalBladeMat.SetColor("_CoreColor", bladeLeadColor);
        verticalBladeMat.SetFloat("_Brightness", 3.2f);
        verticalBladeMat.SetFloat("_Sharpness", 3.5f);
        if (crescentTexture != null)
        {
            verticalBladeMat.SetTexture("_MainTex", crescentTexture);
            verticalBladeMat.SetTexture("_BaseMap", crescentTexture);
        }

        // 2. Horizontal Wave Material
        if (horizontalWaveMat == null)
        {
            Shader vertShader = Shader.Find("VFX/VerticalCrescent");
            if (vertShader == null) vertShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            horizontalWaveMat = new Material(vertShader);
        }
        else
        {
            horizontalWaveMat = new Material(horizontalWaveMat);
        }
        horizontalWaveMat.SetColor("_Color", bladeColor);
        horizontalWaveMat.SetColor("_CoreColor", bladeLeadColor);
        horizontalWaveMat.SetFloat("_Brightness", 2.8f);
        horizontalWaveMat.SetFloat("_Sharpness", 2.5f);
        if (crescentTexture != null)
        {
            horizontalWaveMat.SetTexture("_MainTex", crescentTexture);
            horizontalWaveMat.SetTexture("_BaseMap", crescentTexture);
        }

        // 3. Ground Fissure Material
        if (groundFissureMat == null)
        {
            Shader fissureShader = Shader.Find("VFX/GroundFissure");
            if (fissureShader == null) fissureShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            groundFissureMat = new Material(fissureShader);
        }
        else
        {
            groundFissureMat = new Material(groundFissureMat);
        }
        groundFissureMat.SetColor("_MoltenColor", moltenColor);
        groundFissureMat.SetColor("_CrustColor", crustColor);
        groundFissureMat.SetColor("_RockColor", stoneColor);
        groundFissureMat.SetFloat("_CrackIntensity", 5f);
        groundFissureMat.SetFloat("_CoreRadius", 0.35f);
        groundFissureMat.SetFloat("_Brightness", 3.0f);

        // 4. Erupted Rock Slab Material
        if (rockLitMat == null)
        {
            Shader rockShader = Shader.Find("VFX/MoltenRockSlab");
            if (rockShader == null) rockShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            rockLitMat = new Material(rockShader);
        }
        else
        {
            rockLitMat = new Material(rockLitMat);
        }
        rockLitMat.SetColor("_BaseColor", stoneColor);
        rockLitMat.SetFloat("_Smoothness", 0.15f);
        if (rockLitMat.HasProperty("_MoltenColor"))
        {
            rockLitMat.SetColor("_MoltenColor", moltenColor * 1.5f);
        }
        else
        {
            rockLitMat.SetColor("_EmissionColor", Color.black);
            rockLitMat.DisableKeyword("_EMISSION");
        }

        // 5. Spark Material
        if (sparkMat == null)
        {
            Shader sparkShader = Shader.Find("VFX/AdditiveParticle");
            if (sparkShader == null) sparkShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            sparkMat = new Material(sparkShader);
        }
        else
        {
            sparkMat = new Material(sparkMat);
        }
        sparkMat.SetColor("_Color", sparkColor);
        sparkMat.SetFloat("_Brightness", 3.5f);
        sparkMat.SetFloat("_StarIntensity", 2.5f);
        sparkMat.SetFloat("_CoreSharpness", 5f);
        if (sparkTexture != null)
        {
            sparkMat.SetTexture("_MainTex", sparkTexture);
            sparkMat.SetTexture("_BaseMap", sparkTexture);
        }

        // 6. Heat Smoke / Vapor Material
        if (vaporMat == null)
        {
            Shader vaporShader = Shader.Find("VFX/SoftHeatSmoke");
            if (vaporShader == null) vaporShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            vaporMat = new Material(vaporShader);
        }
        else
        {
            vaporMat = new Material(vaporMat);
        }
        vaporMat.SetColor("_Color", vaporColor);
        vaporMat.SetFloat("_CoreBrightness", 1.8f);
        vaporMat.SetFloat("_Softness", 0.6f);
        if (vaporTexture != null)
        {
            vaporMat.SetTexture("_MainTex", vaporTexture);
            vaporMat.SetTexture("_BaseMap", vaporTexture);
        }

        // 7. Screen Distortion Ring Material
        if (distortionRingMat == null)
        {
            Shader distortShader = Shader.Find("VFX/ScreenDistortionRing");
            if (distortShader != null)
            {
                distortionRingMat = new Material(distortShader);
            }
        }
        else
        {
            distortionRingMat = new Material(distortionRingMat);
        }
        if (distortionRingMat != null)
        {
            distortionRingMat.SetFloat("_DistortionStrength", distortionStrength);
            distortionRingMat.SetFloat("_ChromaticAberration", distortionChromaticAberration);
            distortionRingMat.SetColor("_GlowColor", distortionRimGlow);
            distortionRingMat.SetFloat("_GlowIntensity", 1.5f);
            distortionRingMat.SetFloat("_RingWidth", 0.08f);
        }

        ApplyBloomSettings();
    }

    private void OnValidate()
    {
        ApplyBloomSettings();
    }

    /// <summary>
    /// Updates all material brightness / bloom radiance multipliers and optionally synchronizes scene Global Volume Bloom.
    /// </summary>
    public void ApplyBloomSettings()
    {
        if (verticalBladeMat != null && verticalBladeMat.HasProperty("_Brightness"))
        {
            verticalBladeMat.SetFloat("_Brightness", 3.2f * slashBloomIntensity);
        }

        if (horizontalWaveMat != null && horizontalWaveMat.HasProperty("_Brightness"))
        {
            horizontalWaveMat.SetFloat("_Brightness", 2.8f * slashBloomIntensity);
        }

        if (groundFissureMat != null && groundFissureMat.HasProperty("_Brightness"))
        {
            groundFissureMat.SetFloat("_Brightness", 3.0f * slashBloomIntensity);
        }

        if (sparkMat != null && sparkMat.HasProperty("_Brightness"))
        {
            sparkMat.SetFloat("_Brightness", 3.5f * slashBloomIntensity);
        }

        if (vaporMat != null && vaporMat.HasProperty("_CoreBrightness"))
        {
            vaporMat.SetFloat("_CoreBrightness", 1.8f * slashBloomIntensity);
        }

        if (distortionRingMat != null && distortionRingMat.HasProperty("_GlowIntensity"))
        {
            distortionRingMat.SetFloat("_GlowIntensity", 1.5f * slashBloomIntensity);
        }

        if (controlSceneGlobalBloom)
        {
            ApplySceneBloom();
        }
    }

    private void ApplySceneBloom()
    {
        Volume[] volumes = FindObjectsByType<Volume>(FindObjectsSortMode.None);
        if (volumes == null || volumes.Length == 0) return;

        foreach (Volume vol in volumes)
        {
            if (vol == null) continue;
            VolumeProfile profile = vol.sharedProfile;
            if (Application.isPlaying && vol.profile != null)
            {
                profile = vol.profile;
            }

            if (profile != null && profile.TryGet(out Bloom bloom))
            {
                bloom.intensity.overrideState = true;
                bloom.intensity.value = sceneBloomIntensity;
                bloom.scatter.overrideState = true;
                bloom.scatter.value = sceneBloomScatter;
            }
        }
    }

    /// <summary>
    /// Starts ground slash playback. If loop is enabled, continuously repeats with loopInterval delay.
    /// </summary>
    [ContextMenu("Play Slash")]
    public void Play()
    {
        Stop();
        if (!gameObject.activeInHierarchy || !enabled) return;

        if (loop)
        {
            loopCoroutine = StartCoroutine(PlayLoopRoutine());
        }
        else
        {
            TriggerSlash();
        }
    }

    /// <summary>
    /// Stops the loop and active slashes, cleaning up all instantiated objects immediately.
    /// </summary>
    [ContextMenu("Stop Slash")]
    public void Stop()
    {
        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
            loopCoroutine = null;
        }
        if (slashCoroutine != null)
        {
            StopCoroutine(slashCoroutine);
            slashCoroutine = null;
        }
        CleanupAll();
    }

    private IEnumerator PlayLoopRoutine()
    {
        while (enabled && gameObject.activeInHierarchy && loop)
        {
            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            Vector3 dir = GetSlashDirection();

            // Run slash and wait for complete journey, hold, and dissolution
            yield return StartCoroutine(ExecuteGroundSlashRoutine(pos, dir));

            // Time interval between loops
            if (loopInterval > 0f)
            {
                yield return new WaitForSeconds(loopInterval);
            }
            else
            {
                yield return null;
            }
        }
        loopCoroutine = null;
    }

    /// <summary>
    /// Trigger ground slash once at configured origin and direction.
    /// </summary>
    public void TriggerSlash()
    {
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector3 dir = GetSlashDirection();
        TriggerSlash(pos, dir);
    }

    /// <summary>
    /// Trigger ground slash once at specific world position and direction.
    /// </summary>
    public void TriggerSlash(Vector3 origin, Vector3 direction)
    {
        if (slashCoroutine != null)
        {
            StopCoroutine(slashCoroutine);
        }
        slashCoroutine = StartCoroutine(ExecuteGroundSlashRoutine(origin, direction));
    }

    private IEnumerator ExecuteGroundSlashRoutine(Vector3 startPos, Vector3 direction)
    {
        isPlaying = true;
        direction = direction.normalized;
        if (direction == Vector3.zero) direction = transform.forward;

        if (verticalQuadMesh == null) InitializeMeshes();
        if (verticalBladeMat == null) InitializeMaterials();

        // World Container
        GameObject root = new GameObject("GroundSlash_RunInstance");
        root.transform.position = startPos;
        root.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        activeInstances.Add(root);

        // Camera Shake on strike
        if (enableCameraShake)
            StartCoroutine(DoCameraShake());

        // Screen Distortion / Refraction Ring at impact point
        if (enableDistortionRing && distortionRingMat != null)
            StartCoroutine(SpawnDistortionRingRoutine(root.transform, startPos));

        // 1. Setup Traveling Leading Wave (Vertical Blade + Horizontal Wave)
        GameObject waveRoot = new GameObject("LeadingWave");
        waveRoot.transform.SetParent(root.transform);
        waveRoot.transform.localPosition = Vector3.zero;
        waveRoot.transform.localRotation = Quaternion.identity;

        if (enableVerticalBlade)
        {
            GameObject vertBladeGO = new GameObject("VerticalBlade_YZ");
            vertBladeGO.transform.SetParent(waveRoot.transform);
            vertBladeGO.transform.localPosition = Vector3.up * (verticalBladeScale * 0.4f);
            vertBladeGO.transform.localRotation = Quaternion.identity;
            vertBladeGO.transform.localScale = Vector3.one * verticalBladeScale;

            MeshFilter mf = vertBladeGO.AddComponent<MeshFilter>();
            mf.mesh = verticalQuadMesh;
            MeshRenderer mr = vertBladeGO.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.material = verticalBladeMat;
        }

        if (enableHorizontalWave)
        {
            GameObject horizWaveGO = new GameObject("HorizontalGroundWave_XZ");
            horizWaveGO.transform.SetParent(waveRoot.transform);
            horizWaveGO.transform.localPosition = Vector3.up * 0.02f;
            horizWaveGO.transform.localRotation = Quaternion.identity;
            horizWaveGO.transform.localScale = new Vector3(horizontalWaveScale * 1.3f, 1f, horizontalWaveScale);

            MeshFilter mf = horizWaveGO.AddComponent<MeshFilter>();
            mf.mesh = horizontalQuadMesh;
            MeshRenderer mr = horizWaveGO.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.material = horizontalWaveMat;
        }

        Light travelLight = null;
        if (enableLight)
        {
            GameObject lGO = new GameObject("WaveLight");
            lGO.transform.SetParent(waveRoot.transform);
            lGO.transform.localPosition = Vector3.up * 0.4f;
            travelLight = lGO.AddComponent<Light>();
            travelLight.type = LightType.Point;
            travelLight.color = lightColor;
            travelLight.intensity = lightIntensity;
            travelLight.range = lightRange;
            travelLight.shadows = LightShadows.None;
        }

        // 2. Setup Particle Systems for Sparks & Heat Vapor
        ParticleSystem sparkPS = null;
        if (enableSparks)
            sparkPS = CreateSparkParticleSystem(root.transform);

        ParticleSystem vaporPS = null;
        if (enableHeatVapor)
            vaporPS = CreateVaporParticleSystem(root.transform);

        // 3. TRACKING & SPAWNING NODES ALONG THE PATH (Organic Breakout with Jitter)
        List<GameObject> spawnedNodes = new List<GameObject>();
        List<GameObject> spawnedRocks = new List<GameObject>();
        List<Light> groundPathLights = new List<Light>();

        float travelDuration = travelDistance / travelSpeed;
        float elapsed = 0f;
        float lastSpawnDist = 0f;
        float lastLightDist = 0f;
        Vector3 rightVec = Vector3.Cross(Vector3.up, direction).normalized;

        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / travelDuration);
            float currentDist = travelDistance * t;
            Vector3 currentPos = startPos + direction * currentDist;

            // Move leading wave
            waveRoot.transform.position = currentPos;

            // Spawn molten ground node and flanking rock slabs at intervals
            if (currentDist - lastSpawnDist >= spotSpacing)
            {
                lastSpawnDist = currentDist;

                // Organic lateral zigzag jitter on the path
                float lateralOffset = Random.Range(-pathJitter, pathJitter);
                Vector3 nodePos = currentPos + rightVec * lateralOffset;

                // A. Spawn Molten Magma Node with SEQUENTIAL TEARING ANIMATION
                if (enableMoltenFissure)
                {
                    float randomizedDiameter = spotDiameter * Random.Range(0.85f, 1.3f);
                    GameObject node = SpawnMoltenGroundNode(root.transform, nodePos, randomizedDiameter);
                    spawnedNodes.Add(node);
                    // Sequential tearing: node starts at scale 0 and violently tears open
                    StartCoroutine(TearOpenNodeRoutine(node, randomizedDiameter));
                }

                // B. Spawn Raised Rock Slabs with Organic Vibration
                if (enableRockSlabs)
                {
                    // Left Rock Slab(s)
                    float leftDist = rockFlankOffset + Random.Range(-rockSpreadJitter * 0.5f, rockSpreadJitter);
                    Vector3 leftPos = nodePos - rightVec * leftDist;
                    SpawnRockBreakout(root.transform, leftPos, direction, isLeft: true, spawnedRocks);

                    // Right Rock Slab(s)
                    float rightDist = rockFlankOffset + Random.Range(-rockSpreadJitter * 0.5f, rockSpreadJitter);
                    Vector3 rightPos = nodePos + rightVec * rightDist;
                    SpawnRockBreakout(root.transform, rightPos, direction, isLeft: false, spawnedRocks);
                }

                // C. Emit twinkling diamond sparks
                if (sparkPS != null)
                {
                    sparkPS.transform.position = nodePos + Vector3.up * 0.1f;
                    sparkPS.Emit(sparksPerNode);
                }

                // D. Emit soft golden heat smoke / fog
                if (vaporPS != null)
                {
                    vaporPS.transform.position = nodePos + Vector3.up * 0.1f;
                    vaporPS.Emit(vaporPerNode);
                }
            }

            // E. Dynamic Ground Path Lights (spaced along the slash)
            if (enableGroundPathLights && currentDist - lastLightDist >= groundLightSpacing)
            {
                lastLightDist = currentDist;
                Vector3 lightPos = currentPos + Vector3.up * 0.12f;
                Light gl = SpawnGroundPathLight(root.transform, lightPos);
                groundPathLights.Add(gl);
            }

            yield return null;
        }

        // 4. Fade out leading wave
        StartCoroutine(FadeOutLeadingWave(waveRoot, travelLight));

        // 5. Hold glowing fissure on ground
        yield return new WaitForSeconds(fissureDuration);

        // 6. Dissolve phase: Molten nodes cool, rock slabs sink, ground lights dim
        float coolElapsed = 0f;
        while (coolElapsed < fissureCoolDuration)
        {
            coolElapsed += Time.deltaTime;
            float dt = Mathf.Clamp01(coolElapsed / fissureCoolDuration);

            foreach (var node in spawnedNodes)
            {
                if (node != null)
                {
                    var mr = node.GetComponent<MeshRenderer>();
                    if (mr != null && mr.material != null)
                        mr.material.SetFloat("_Dissolve", dt);
                }
            }

            foreach (var rock in spawnedRocks)
            {
                if (rock != null)
                {
                    rock.transform.position -= Vector3.up * (rockRiseHeight * (Time.deltaTime / fissureCoolDuration));
                    rock.transform.localScale = Vector3.Lerp(rock.transform.localScale, Vector3.zero, dt);
                }
            }

            // Dim ground path lights in sync with fissure cooling
            foreach (var gl in groundPathLights)
            {
                if (gl != null)
                    gl.intensity = Mathf.Lerp(groundLightIntensity, 0f, dt);
            }

            yield return null;
        }

        // Final cleanup
        isPlaying = false;
        slashCoroutine = null;
        if (root != null)
        {
            activeInstances.Remove(root);
            Destroy(root);
        }
    }

    // ==========================================
    //  SPAWN HELPERS WITH ORGANIC VIBRATION
    // ==========================================

    private GameObject SpawnMoltenGroundNode(Transform parent, Vector3 worldPos, float diameter)
    {
        GameObject node = new GameObject("MoltenNode");
        node.transform.SetParent(parent);
        node.transform.position = worldPos + Vector3.up * 0.015f;
        float randomRot = Random.Range(0f, 360f);
        node.transform.rotation = Quaternion.Euler(0f, randomRot, 0f);
        float stretch = Random.Range(0.85f, 1.35f);
        node.transform.localScale = new Vector3(diameter, 1f, diameter * stretch);

        MeshFilter mf = node.AddComponent<MeshFilter>();
        mf.mesh = horizontalQuadMesh;

        MeshRenderer mr = node.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.material = new Material(groundFissureMat);

        return node;
    }

    private void SpawnRockBreakout(Transform parent, Vector3 worldPos, Vector3 forwardDir, bool isLeft, List<GameObject> collector)
    {
        // Primary rock slab
        GameObject mainRock = SpawnSingleRockSlab(parent, worldPos, forwardDir, isLeft, rockScale);
        collector.Add(mainRock);

        // Chance to spawn an extra cluster chunk for violent organic breakout
        if (Random.value < rockClusterChance)
        {
            Vector3 secondaryOffset = Random.insideUnitSphere * (rockScale * 0.6f);
            secondaryOffset.y = 0f;
            GameObject subRock = SpawnSingleRockSlab(parent, worldPos + secondaryOffset, forwardDir, isLeft, rockScale * Random.Range(0.45f, 0.75f));
            collector.Add(subRock);
        }
    }

    private GameObject SpawnSingleRockSlab(Transform parent, Vector3 worldPos, Vector3 forwardDir, bool isLeft, float scale)
    {
        GameObject rock = new GameObject(isLeft ? "RockSlab_Left" : "RockSlab_Right");
        rock.transform.SetParent(parent);

        // Randomized pop-up height
        float randomizedHeight = rockRiseHeight * Random.Range(0.8f, 1.3f);
        Vector3 finalPos = worldPos + Vector3.up * randomizedHeight;
        rock.transform.position = worldPos;

        // Base rotation facing forward, plus outward tilt and random angle jitter
        Quaternion baseRot = Quaternion.LookRotation(forwardDir, Vector3.up);
        float tiltBase = isLeft ? rockTiltAngle : -rockTiltAngle;
        float tiltJitter = Random.Range(-rockAngleJitter * 0.4f, rockAngleJitter * 0.4f);
        float yawJitter = Random.Range(-rockAngleJitter, rockAngleJitter);
        float pitchJitter = Random.Range(-15f, 15f);

        Quaternion targetRot = baseRot * Quaternion.Euler(pitchJitter, yawJitter, tiltBase + tiltJitter);
        rock.transform.rotation = targetRot;

        float sizeVar = Random.Range(0.8f, 1.3f) * scale;
        rock.transform.localScale = new Vector3(sizeVar * 1.2f, sizeVar * 0.8f, sizeVar * 1.5f);

        MeshFilter mf = rock.AddComponent<MeshFilter>();
        mf.mesh = chiseledRockMesh;

        MeshRenderer mr = rock.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;
        mr.material = rockLitMat;

        StartCoroutine(PopUpRockRoutine(rock, finalPos));

        return rock;
    }

    private IEnumerator PopUpRockRoutine(GameObject rock, Vector3 targetPos)
    {
        if (rock == null) yield break;
        Vector3 startPos = rock.transform.position;
        float duration = 0.12f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (rock == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f);
            rock.transform.position = Vector3.LerpUnclamped(startPos, targetPos, ease);
            yield return null;
        }

        if (rock != null)
            rock.transform.position = targetPos;
    }

    /// <summary>
    /// Sequential Tearing: Molten node tears open from scale 0 to full size with violent ease-out.
    /// Gives the propagation wave motion instead of static instant appearance.
    /// </summary>
    private IEnumerator TearOpenNodeRoutine(GameObject node, float targetDiameter)
    {
        if (node == null) yield break;
        float stretch = node.transform.localScale.z / Mathf.Max(node.transform.localScale.x, 0.01f);
        node.transform.localScale = Vector3.zero;

        float duration = 0.1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (node == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Explosive ease-out with slight overshoot for violent tearing feel
            float ease = 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f);
            float s = targetDiameter * ease;
            node.transform.localScale = new Vector3(s, 1f, s * stretch);
            yield return null;
        }

        if (node != null)
            node.transform.localScale = new Vector3(targetDiameter, 1f, targetDiameter * stretch);
    }

    /// <summary>
    /// Spawn screen distortion / refraction ring at the impact point, expanding outward.
    /// Uses ScreenDistortionRing shader sampling _CameraOpaqueTexture.
    /// </summary>
    private IEnumerator SpawnDistortionRingRoutine(Transform parent, Vector3 worldPos)
    {
        if (horizontalQuadMesh == null || distortionRingMat == null) yield break;

        GameObject ringGO = new GameObject("DistortionRing_Shockwave");
        ringGO.transform.SetParent(parent);
        ringGO.transform.position = worldPos + Vector3.up * 0.05f;
        ringGO.transform.localRotation = Quaternion.identity;
        ringGO.transform.localScale = Vector3.one * 0.1f;

        MeshFilter mf = ringGO.AddComponent<MeshFilter>();
        mf.mesh = horizontalQuadMesh;

        MeshRenderer mr = ringGO.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        Material instanceMat = new Material(distortionRingMat);
        mr.material = instanceMat;

        float elapsed = 0f;
        while (elapsed < distortionDuration)
        {
            if (ringGO == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / distortionDuration);

            // Expand ring scale
            float currentScale = Mathf.Lerp(0.3f, distortionRingScale, t);
            ringGO.transform.localScale = Vector3.one * currentScale;

            // Animate shader parameters
            instanceMat.SetFloat("_Radius", Mathf.Lerp(0.1f, 0.95f, t));
            instanceMat.SetFloat("_Dissolve", Mathf.Pow(t, 1.5f));

            yield return null;
        }

        if (ringGO != null)
            Destroy(ringGO);
    }

    /// <summary>
    /// Spawn a low-lying dynamic point light beneath the slash path.
    /// Illuminates the trench, rock faces, and fissure from below.
    /// </summary>
    private Light SpawnGroundPathLight(Transform parent, Vector3 worldPos)
    {
        GameObject lGO = new GameObject("GroundPathLight");
        lGO.transform.SetParent(parent);
        lGO.transform.position = worldPos;

        Light l = lGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = groundLightColor;
        l.intensity = groundLightIntensity;
        l.range = groundLightRange;
        l.shadows = LightShadows.None;
        return l;
    }

    private IEnumerator FadeOutLeadingWave(GameObject waveRoot, Light lightComp)
    {
        if (waveRoot == null) yield break;
        float elapsed = 0f;
        float duration = 0.2f;
        Vector3 startScale = waveRoot.transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (waveRoot != null)
                waveRoot.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            if (lightComp != null)
                lightComp.intensity = Mathf.Lerp(lightIntensity, 0f, t);
            yield return null;
        }

        if (waveRoot != null)
            Destroy(waveRoot);
    }

    private ParticleSystem CreateSparkParticleSystem(Transform parent)
    {
        GameObject sparkGO = new GameObject("Sparks");
        sparkGO.transform.SetParent(parent);
        sparkGO.transform.localPosition = Vector3.zero;

        ParticleSystem ps = sparkGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(sparkLifetime * 0.6f, sparkLifetime);
        main.startSize = sparkSize;
        main.startSpeed = new ParticleSystem.MinMaxCurve(sparkUpSpeed * 0.5f, sparkUpSpeed);
        main.startColor = sparkColor;
        main.maxParticles = 150;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.gravityModifier = -0.1f;
        main.playOnAwake = false;
        main.loop = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0f, 0.4f);
        sc.AddKey(0.2f, 1f);
        sc.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var renderer = sparkGO.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = sparkMat;

        return ps;
    }

    private ParticleSystem CreateVaporParticleSystem(Transform parent)
    {
        GameObject vaporGO = new GameObject("HeatVapor");
        vaporGO.transform.SetParent(parent);
        vaporGO.transform.localPosition = Vector3.zero;

        ParticleSystem ps = vaporGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(vaporLifetime * 0.6f, vaporLifetime);
        main.startSize = new ParticleSystem.MinMaxCurve(vaporSize * 0.7f, vaporSize);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startColor = vaporColor;
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.gravityModifier = -0.15f;
        main.playOnAwake = false;
        main.loop = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.4f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0f, 0.5f);
        sc.AddKey(0.4f, 1f);
        sc.AddKey(1f, 1.5f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = g;

        var renderer = vaporGO.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = vaporMat;

        return ps;
    }

    private Mesh CreateChiseledRockMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "ChiseledRockSlab";

        // 8 Corner Vertices:
        // Bottom 4 (wider base)
        Vector3 b0 = new Vector3(-0.5f, 0.0f, -0.6f);
        Vector3 b1 = new Vector3( 0.5f, 0.0f, -0.5f);
        Vector3 b2 = new Vector3( 0.6f, 0.0f,  0.5f);
        Vector3 b3 = new Vector3(-0.4f, 0.0f,  0.6f);

        // Top 4 (faceted, narrower)
        Vector3 t0 = new Vector3(-0.35f, 0.5f, -0.35f);
        Vector3 t1 = new Vector3( 0.35f, 0.45f, -0.3f);
        Vector3 t2 = new Vector3( 0.4f,  0.55f,  0.35f);
        Vector3 t3 = new Vector3(-0.3f,  0.5f,   0.4f);

        // Separate vertices for each face to ensure sharp, flat-shaded chiseled normals
        List<Vector3> verts = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<Color> cols = new List<Color>();
        List<int> tris = new List<int>();

        void AddQuad(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Color c0, Color c1, Color c2, Color c3)
        {
            int idx = verts.Count;
            verts.Add(v0); verts.Add(v1); verts.Add(v2); verts.Add(v3);
            uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(0f, 1f));
            cols.Add(c0); cols.Add(c1); cols.Add(c2); cols.Add(c3);
            tris.Add(idx); tris.Add(idx + 1); tris.Add(idx + 2);
            tris.Add(idx); tris.Add(idx + 2); tris.Add(idx + 3);
        }

        Color molten = new Color(1f, 0.6f, 0.1f, 1f);
        Color stone = new Color(0f, 0f, 0f, 1f);

        // Bottom face (molten underside)
        AddQuad(b0, b3, b2, b1, molten, molten, molten, molten);

        // Top face (dark charred stone)
        AddQuad(t0, t1, t2, t3, stone, stone, stone, stone);

        // 4 Side Facets (gradient from bottom molten to top stone)
        AddQuad(b0, b1, t1, t0, molten, molten, stone, stone); // Front
        AddQuad(b1, b2, t2, t1, molten, molten, stone, stone); // Right
        AddQuad(b2, b3, t3, t2, molten, molten, stone, stone); // Back
        AddQuad(b3, b0, t0, t3, molten, molten, stone, stone); // Left

        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(cols);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    private IEnumerator DoCameraShake()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        shakingCam = cam;
        origCamPos = cam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / shakeDuration);
            float ox = Random.Range(-shakeIntensity, shakeIntensity) * t;
            float oy = Random.Range(-shakeIntensity, shakeIntensity) * t;
            cam.transform.localPosition = origCamPos + new Vector3(ox, oy, 0);
            yield return null;
        }

        cam.transform.localPosition = origCamPos;
        shakingCam = null;
    }

    private Vector3 GetSlashDirection()
    {
        if (slashDirection != Vector3.zero) return transform.TransformDirection(slashDirection);
        return transform.forward;
    }

    public void CleanupAll()
    {
        StopAllCoroutines();
        loopCoroutine = null;
        slashCoroutine = null;

        for (int i = activeInstances.Count - 1; i >= 0; i--)
        {
            GameObject go = activeInstances[i];
            if (go != null)
            {
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
        }
        activeInstances.Clear();

        if (shakingCam != null)
        {
            shakingCam.transform.localPosition = origCamPos;
            shakingCam = null;
        }

        isPlaying = false;
    }

    private void OnDestroy()
    {
        CleanupAll();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector3 dir = GetSlashDirection();

        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
        Gizmos.DrawWireSphere(pos, 0.3f);
        Gizmos.DrawRay(pos, dir * travelDistance);
    }
}
