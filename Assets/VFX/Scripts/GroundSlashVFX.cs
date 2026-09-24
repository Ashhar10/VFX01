using UnityEngine;
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
/// </summary>
[DisallowMultipleComponent]
public class GroundSlashVFX : MonoBehaviour
{
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
    [SerializeField, ColorUsage(true, true)] private Color bladeColor = new Color(8f, 4.5f, 1f, 1f);

    [Tooltip("Blinding white-hot leading cutting edge (HDR).")]
    [SerializeField, ColorUsage(true, true)] private Color bladeLeadColor = new Color(16f, 14f, 8f, 1f);

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
    [SerializeField] private Color stoneColor = new Color(0.35f, 0.32f, 0.28f, 1f);

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
    [SerializeField, Range(0.2f, 3f)] private float rockFlankOffset = 0.65f;

    [Tooltip("Rock slab scale (larger slider).")]
    [SerializeField, Range(0.1f, 1.5f)] private float rockScale = 0.45f;

    [Tooltip("Outward tilt angle of the rock slabs (degrees).")]
    [SerializeField, Range(10f, 75f)] private float rockTiltAngle = 28f;

    [Tooltip("Height rock slabs rise above the ground (meters).")]
    [SerializeField, Range(0.05f, 1.5f)] private float rockRiseHeight = 0.16f;

    [Tooltip("Molten underside emission color on rocks (HDR).")]
    [SerializeField, ColorUsage(true, true)] private Color rockMoltenEmission = new Color(1.2f, 0.5f, 0.08f, 1f);

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
    [SerializeField, Range(0f, 15f)] private float lightIntensity = 2.0f;

    [Tooltip("Light range.")]
    [SerializeField, Range(1f, 20f)] private float lightRange = 4.5f;

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
    [SerializeField, Range(0f, 6f)] private float groundLightIntensity = 1.0f;

    [Tooltip("Radius / range of the ground path lights.")]
    [SerializeField, Range(1f, 8f)] private float groundLightRange = 2.5f;

    [Tooltip("Color of the ground path illumination.")]
    [SerializeField] private Color groundLightColor = new Color(1f, 0.58f, 0.18f, 1f);

    [Tooltip("Distance spacing between dynamic ground lights along the path (meters).")]
    [SerializeField, Range(0.5f, 6f)] private float groundLightSpacing = 2.0f;

    // ==========================================
    //  RUNTIME STATE & REUSABLE ASSETS
    // ==========================================
    private bool isPlaying;
    private List<GameObject> activeInstances = new List<GameObject>();

    private Mesh verticalQuadMesh;
    private Mesh horizontalCrescentMesh;
    private Mesh horizontalQuadMesh;
    private Mesh chiseledRockMesh;

    private Material verticalBladeMat;
    private Material horizontalWaveMat;
    private Material groundFissureMat;
    private Material rockLitMat;
    private Material sparkMat;
    private Material vaporMat;
    private Material distortionRingMat;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        InitializeMeshes();
        InitializeMaterials();
    }

    private void InitializeMeshes()
    {
        // 1. Vertical Quad in YZ plane (width along Z, height along Y)
        // Convex outer cutting edge faces forward (+Z), tips curve back (-Z) and up/down
        verticalQuadMesh = new Mesh();
        verticalQuadMesh.name = "VerticalBladeQuad_YZ";
        verticalQuadMesh.vertices = new Vector3[]
        {
            new Vector3(0f, 0f, -0.6f),
            new Vector3(0f, 0f,  0.6f),
            new Vector3(0f, 1.4f,  0.6f),
            new Vector3(0f, 1.4f, -0.6f)
        };
        verticalQuadMesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };
        verticalQuadMesh.triangles = new int[]
        {
            0, 2, 1, 0, 3, 2,
            0, 1, 2, 0, 2, 3
        };
        verticalQuadMesh.RecalculateNormals();
        verticalQuadMesh.RecalculateBounds();

        // 2. Horizontal Crescent Mesh in XZ plane (Forward-cutting crescent flat on the ground)
        // Convex outer cutting edge faces forward (+Z), tips spread left (-X) and right (+X)
        horizontalCrescentMesh = new Mesh();
        horizontalCrescentMesh.name = "HorizontalCrescent_XZ";
        horizontalCrescentMesh.vertices = new Vector3[]
        {
            new Vector3(-1.0f, 0f, -0.6f),
            new Vector3( 1.0f, 0f, -0.6f),
            new Vector3( 1.0f, 0f,  0.6f),
            new Vector3(-1.0f, 0f,  0.6f)
        };
        horizontalCrescentMesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f)
        };
        horizontalCrescentMesh.triangles = new int[]
        {
            0, 2, 1, 0, 3, 2,
            0, 1, 2, 0, 2, 3
        };
        horizontalCrescentMesh.RecalculateNormals();
        horizontalCrescentMesh.RecalculateBounds();

        // 3. Horizontal Quad in XZ plane (For circular ground fissure nodes and distortion ring)
        horizontalQuadMesh = new Mesh();
        horizontalQuadMesh.name = "HorizontalQuad_XZ";
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
        horizontalQuadMesh.triangles = new int[]
        {
            0, 2, 1, 0, 3, 2,
            0, 1, 2, 0, 2, 3
        };
        horizontalQuadMesh.RecalculateNormals();
        horizontalQuadMesh.RecalculateBounds();

        // 4. Chiseled 3D Rock Mesh
        chiseledRockMesh = CreateChiseledRockMesh();
    }

    private void InitializeMaterials()
    {
        Shader vertShader = Shader.Find("VFX/VerticalCrescent");
        if (vertShader == null) vertShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (vertShader == null) vertShader = Shader.Find("Particles/Standard Unlit");
        if (vertShader == null) vertShader = Shader.Find("Mobile/Particles/Additive");

        Color haloCol = new Color(bladeColor.r * 0.5f, bladeColor.g * 0.4f, bladeColor.b * 0.25f, 1f);

        verticalBladeMat = new Material(vertShader);
        verticalBladeMat.SetColor("_Color", bladeColor);
        verticalBladeMat.SetColor("_BaseColor", bladeColor);
        verticalBladeMat.SetColor("_CoreColor", bladeLeadColor);
        verticalBladeMat.SetColor("_HaloColor", haloCol);
        verticalBladeMat.SetFloat("_Brightness", 1.8f);
        verticalBladeMat.SetFloat("_Sharpness", 3.0f);
        ConfigureAdditiveFallback(verticalBladeMat, bladeColor);

        horizontalWaveMat = new Material(vertShader);
        horizontalWaveMat.SetColor("_Color", bladeColor);
        horizontalWaveMat.SetColor("_BaseColor", bladeColor);
        horizontalWaveMat.SetColor("_CoreColor", bladeLeadColor);
        horizontalWaveMat.SetColor("_HaloColor", haloCol);
        horizontalWaveMat.SetFloat("_Brightness", 1.6f);
        horizontalWaveMat.SetFloat("_Sharpness", 2.5f);
        ConfigureAdditiveFallback(horizontalWaveMat, bladeColor);

        Shader fissureShader = Shader.Find("VFX/GroundFissure");
        if (fissureShader == null) fissureShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (fissureShader == null) fissureShader = Shader.Find("Particles/Standard Unlit");
        if (fissureShader == null) fissureShader = Shader.Find("Mobile/Particles/Additive");

        groundFissureMat = new Material(fissureShader);
        groundFissureMat.SetColor("_MoltenColor", moltenColor);
        groundFissureMat.SetColor("_BaseColor", moltenColor);
        groundFissureMat.SetColor("_CrustColor", crustColor);
        groundFissureMat.SetColor("_RockColor", stoneColor);
        groundFissureMat.SetFloat("_CrackIntensity", 5f);
        groundFissureMat.SetFloat("_CoreRadius", 0.35f);
        groundFissureMat.SetFloat("_Brightness", 3.0f);
        ConfigureAdditiveFallback(groundFissureMat, moltenColor);

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        rockLitMat = new Material(litShader);
        rockLitMat.SetColor("_BaseColor", stoneColor);
        rockLitMat.SetColor("_Color", stoneColor);
        rockLitMat.SetFloat("_Smoothness", 0.1f);
        rockLitMat.SetColor("_EmissionColor", rockMoltenEmission);
        rockLitMat.EnableKeyword("_EMISSION");

        Shader sparkShader = Shader.Find("VFX/AdditiveParticle");
        if (sparkShader == null) sparkShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sparkShader == null) sparkShader = Shader.Find("Particles/Standard Unlit");
        if (sparkShader == null) sparkShader = Shader.Find("Mobile/Particles/Additive");

        sparkMat = new Material(sparkShader);
        sparkMat.SetColor("_Color", sparkColor);
        sparkMat.SetColor("_BaseColor", sparkColor);
        sparkMat.SetFloat("_Brightness", 3.5f);
        sparkMat.SetFloat("_StarIntensity", 2.5f);
        sparkMat.SetFloat("_CoreSharpness", 5f);
        ConfigureAdditiveFallback(sparkMat, sparkColor);

        Shader vaporShader = Shader.Find("VFX/SoftHeatSmoke");
        if (vaporShader == null) vaporShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (vaporShader == null) vaporShader = Shader.Find("Particles/Standard Unlit");
        if (vaporShader == null) vaporShader = Shader.Find("Mobile/Particles/Additive");

        vaporMat = new Material(vaporShader);
        vaporMat.SetColor("_Color", vaporColor);
        vaporMat.SetColor("_BaseColor", vaporColor);
        vaporMat.SetFloat("_CoreBrightness", 1.8f);
        vaporMat.SetFloat("_Softness", 0.6f);
        ConfigureAdditiveFallback(vaporMat, vaporColor);

        // Screen Distortion Ring Material
        Shader distortShader = Shader.Find("VFX/ScreenDistortionRing");
        if (distortShader == null) distortShader = vertShader;
        if (distortShader != null)
        {
            distortionRingMat = new Material(distortShader);
            distortionRingMat.SetFloat("_DistortionStrength", distortionStrength);
            distortionRingMat.SetFloat("_ChromaticAberration", distortionChromaticAberration);
            distortionRingMat.SetColor("_GlowColor", distortionRimGlow);
            distortionRingMat.SetColor("_BaseColor", distortionRimGlow);
            distortionRingMat.SetFloat("_GlowIntensity", 1.5f);
            distortionRingMat.SetFloat("_RingWidth", 0.08f);
            ConfigureAdditiveFallback(distortionRingMat, distortionRimGlow);
        }
    }

    private void ConfigureAdditiveFallback(Material mat, Color col)
    {
        if (mat == null) return;
        mat.SetColor("_Color", col);
        mat.SetColor("_BaseColor", col);
        if (mat.shader != null && !mat.shader.name.StartsWith("VFX/"))
        {
            mat.SetFloat("_Surface", 1.0f); // Transparent
            mat.SetFloat("_Blend", 1.0f);   // Additive
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
    }

    /// <summary>
    /// Trigger ground slash at configured origin and direction.
    /// </summary>
    public void TriggerSlash()
    {
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Vector3 dir = GetSlashDirection();
        TriggerSlash(pos, dir);
    }

    /// <summary>
    /// Trigger ground slash at specific world position and direction.
    /// </summary>
    public void TriggerSlash(Vector3 origin, Vector3 direction)
    {
        StartCoroutine(ExecuteGroundSlashRoutine(origin, direction));
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
            vertBladeGO.transform.localPosition = new Vector3(0f, -0.05f * verticalBladeScale, 0f);
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
            horizWaveGO.transform.localScale = new Vector3(horizontalWaveScale * 1.2f, 1f, horizontalWaveScale);

            MeshFilter mf = horizWaveGO.AddComponent<MeshFilter>();
            mf.mesh = horizontalCrescentMesh;
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

        Vector3[] vertices = new Vector3[]
        {
            // Bottom face (sitting near ground)
            new Vector3(-0.35f, 0.0f, -0.45f),
            new Vector3( 0.35f, 0.0f, -0.45f),
            new Vector3( 0.35f, 0.0f,  0.45f),
            new Vector3(-0.35f, 0.0f,  0.45f),

            // Top face (slightly inset for chiseled bevel edge)
            new Vector3(-0.30f, 0.08f, -0.40f),
            new Vector3( 0.30f, 0.08f, -0.40f),
            new Vector3( 0.30f, 0.08f,  0.40f),
            new Vector3(-0.30f, 0.08f,  0.40f)
        };

        int[] triangles = new int[]
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            3, 6, 2, 3, 7, 6,
            0, 1, 5, 0, 5, 4,
            1, 2, 6, 1, 6, 5,
            0, 4, 7, 0, 7, 3
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private IEnumerator DoCameraShake()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 orig = cam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / shakeDuration);
            float ox = Random.Range(-shakeIntensity, shakeIntensity) * t;
            float oy = Random.Range(-shakeIntensity, shakeIntensity) * t;
            cam.transform.localPosition = orig + new Vector3(ox, oy, 0);
            yield return null;
        }

        cam.transform.localPosition = orig;
    }

    private Vector3 GetSlashDirection()
    {
        if (slashDirection != Vector3.zero) return transform.TransformDirection(slashDirection);
        return transform.forward;
    }

    public void CleanupAll()
    {
        StopAllCoroutines();
        foreach (var go in activeInstances)
        {
            if (go != null) Destroy(go);
        }
        activeInstances.Clear();
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
