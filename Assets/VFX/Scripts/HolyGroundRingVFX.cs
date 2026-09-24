using UnityEngine;
using System.Collections;

/// <summary>
/// Divine Right / Holy Ground Ring VFX Controller.
/// 
/// Multi-stage Level-by-Level Activation:
/// Level 1: Outer Golden Perimeter Circle expands across the floor.
/// Level 2: Sacred Inner Arena Glow materializes.
/// Level 3: Sacred concentric rings & radial geometric rune design appear between them.
/// Level 4: HIGHLY ACTIVATED! 
///          - Ring of VERTICAL SUN NEEDLES shoots UPWARD into the air like rays of sunlight (media_1789888309821.png).
///          - Radiating sunburst needles ignite on the ground.
///          - Rising holy sparks erupt upward.
///          - Blinding dynamic light flash.
/// 
/// 100% controllable in Inspector.
/// </summary>
[DisallowMultipleComponent]
public class HolyGroundRingVFX : MonoBehaviour
{
    // ==========================================
    //  RING DIMENSIONS & PLACEMENT
    // ==========================================
    [Header("=== Ring Dimensions & Placement ===")]
    [Tooltip("Maximum diameter of the ring on the floor (in meters).")]
    [SerializeField, Range(1f, 20f)] private float ringDiameter = 5.0f;

    [Tooltip("Outer ring border thickness.")]
    [SerializeField, Range(0.01f, 0.25f)] private float ringThickness = 0.05f;

    [Tooltip("Inner ring radius ratio relative to outer ring.")]
    [SerializeField, Range(0.2f, 0.85f)] private float innerRingRatio = 0.55f;

    [Tooltip("Height offset above ground to avoid Z-fighting.")]
    [SerializeField, Range(0.005f, 0.1f)] private float groundHeightOffset = 0.02f;

    // ==========================================
    //  VERTICAL SUN NEEDLES (RISING UP LIKE SUNLIGHT)
    // ==========================================
    [Header("=== Vertical Sun Needles (Rays Rising into the Air) ===")]
    [Tooltip("Enable vertical light needles shooting UPWARD from the ring perimeter like sunlight rays.")]
    [SerializeField] private bool enableVerticalSunNeedles = true;

    [Tooltip("Number of vertical sun needles standing around the ring.")]
    [SerializeField, Range(8, 64)] private int verticalNeedleCount = 32;

    [Tooltip("Height of the vertical sun needles standing in the air (meters).")]
    [SerializeField, Range(0.5f, 6.0f)] private float verticalNeedleHeight = 2.5f;

    [Tooltip("Width of each vertical sun needle at its base (meters).")]
    [SerializeField, Range(0.02f, 0.5f)] private float verticalNeedleWidth = 0.12f;

    [Tooltip("Outward flare / tilt angle of vertical sun needles (degrees). 0 = straight up, > 0 = flares like a sun crown.")]
    [SerializeField, Range(-15f, 40f)] private float verticalNeedleTilt = 12f;

    [Tooltip("Time in seconds for needles to shoot upward at Level 4 activation.")]
    [SerializeField, Range(0.05f, 1.0f)] private float needleShootDuration = 0.22f;

    [Tooltip("Golden sunbeam aura color for vertical needles (HDR).")]
    [SerializeField, ColorUsage(true, true)] private Color verticalNeedleColor = new Color(16f, 9.5f, 2f, 1f);

    [Tooltip("White-hot base core color where needles touch the floor (HDR).")]
    [SerializeField, ColorUsage(true, true)] private Color verticalNeedleCoreColor = new Color(22f, 20f, 14f, 1f);

    // ==========================================
    //  LEVEL-BY-LEVEL ACTIVATION TIMING (SECONDS)
    // ==========================================
    [Header("=== Level-by-Level Activation Timing (Seconds) ===")]
    [Tooltip("Level 1 Duration (seconds): Golden outer circle expands from center.")]
    [SerializeField, Range(0.05f, 3f)] private float stage1CircleDuration = 0.25f;

    [Tooltip("Level 2 Duration (seconds): Sacred inner ground aura appears.")]
    [SerializeField, Range(0.05f, 3f)] private float stage2DarkFloorDuration = 0.20f;

    [Tooltip("Level 3 Duration (seconds): Intricate sacred concentric design forms between the rings.")]
    [SerializeField, Range(0.05f, 3f)] private float stage3DesignDuration = 0.30f;

    [Tooltip("Level 4 Duration (seconds): Blinding burst! Vertical needles shoot up & sparks erupt.")]
    [SerializeField, Range(0.05f, 3f)] private float stage4ActivatedDuration = 0.35f;

    [Tooltip("Hold Duration (seconds): Time the fully activated seal stays glowing on the ground.")]
    [SerializeField, Range(0.2f, 10f)] private float holdDuration = 2.5f;

    [Tooltip("Fade Duration (seconds): Time for the seal to gracefully dissolve away.")]
    [SerializeField, Range(0.1f, 5f)] private float fadeDuration = 0.8f;

    // ==========================================
    //  FLOOR SUNBURST RAYS
    // ==========================================
    [Header("=== Floor Sunburst Needle Rays ===")]
    [Tooltip("Number of sharp needle rays radiating across the floor.")]
    [SerializeField, Range(8, 64)] private int floorRayCount = 32;

    [Tooltip("Sharpness of the floor needle rays.")]
    [SerializeField, Range(1f, 12f)] private float floorNeedleSharpness = 6.0f;

    [Tooltip("Length of floor needle rays extending outward.")]
    [SerializeField, Range(0.05f, 0.8f)] private float floorNeedleLength = 0.35f;

    // ==========================================
    //  COLORS (HDR FOR BLOOM)
    // ==========================================
    [Header("=== Floor Seal Colors (HDR for Bloom) ===")]
    [Tooltip("Golden aura tint for design, rings, and rays (HDR).")]
    [SerializeField, ColorUsage(true, true)] private Color ringColor = new Color(14f, 8.5f, 2f, 1f);

    [Tooltip("Blinding white-hot core on ring crest and needle burst (HDR).")]
    [SerializeField, ColorUsage(true, true)] private Color coreColor = new Color(20f, 18f, 12f, 1f);

    [Tooltip("Overall brightness multiplier.")]
    [SerializeField, Range(0.5f, 15f)] private float brightness = 3.2f;

    // ==========================================
    //  RISING HOLY SPARKS
    // ==========================================
    [Header("=== Rising Holy Sparks (Erupt at Level 4) ===")]
    [Tooltip("Enable golden sparks that erupt when the ring is highly activated.")]
    [SerializeField] private bool enableRisingSparks = true;

    [Tooltip("Total sparks emitted (up to 150).")]
    [SerializeField, Range(5, 150)] private int sparkCount = 50;

    [Tooltip("Spark particle size.")]
    [SerializeField, Range(0.005f, 0.08f)] private float sparkSize = 0.022f;

    [Tooltip("Upward rise speed (m/s).")]
    [SerializeField, Range(0.5f, 8f)] private float sparkUpSpeed = 2.5f;

    [Tooltip("Spark lifetime in seconds.")]
    [SerializeField, Range(0.3f, 3f)] private float sparkLifetime = 1.0f;

    [Tooltip("Spark color (HDR).")]
    [SerializeField, ColorUsage(true, true)] private Color sparkColor = new Color(16f, 11f, 3f, 1f);

    // ==========================================
    //  ACTIVATION LIGHT FLASH
    // ==========================================
    [Header("=== Impact Light Flash ===")]
    [Tooltip("Enable dynamic point light at Level 4 activation.")]
    [SerializeField] private bool enableFlashLight = true;

    [Tooltip("Flash light color.")]
    [SerializeField] private Color lightColor = new Color(1f, 0.85f, 0.35f, 1f);

    [Header("=== Silhouette Protection (Prevents Character Washout) ===")]
    [Tooltip("Dampens center brightness under character feet so stance and dark armor remain crisp and readable.")]
    [SerializeField, Range(0f, 1f)] private float centerGlowDampening = 0.75f;

    [Tooltip("Flash light intensity (kept balanced to avoid washing out the character mesh).")]
    [SerializeField, Range(0.5f, 15f)] private float lightIntensity = 4.5f;

    [Tooltip("Flash light range.")]
    [SerializeField, Range(2f, 20f)] private float lightRange = 7f;

    // ==========================================
    //  RUNTIME STATE
    // ==========================================
    private Mesh quadMesh;
    private Mesh verticalNeedlesMesh;
    private Coroutine activeRoutine;
    private bool isPlaying;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        quadMesh = CreateHorizontalQuad();
    }

    /// <summary>
    /// Trigger the Holy Ground Ring at the current position.
    /// </summary>
    public void TriggerRing()
    {
        TriggerRing(transform.position);
    }

    /// <summary>
    /// Trigger the Holy Ground Ring at a specific world position.
    /// </summary>
    public void TriggerRing(Vector3 worldPosition)
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(PlayRingRoutine(worldPosition));
    }

    /// <summary>
    /// Stop and clean up immediately.
    /// </summary>
    public void StopRing()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
        isPlaying = false;
    }

    private IEnumerator PlayRingRoutine(Vector3 worldPosition)
    {
        isPlaying = true;

        if (quadMesh == null)
            quadMesh = CreateHorizontalQuad();

        // Container in world space
        GameObject ringInstance = new GameObject("HolyGroundRing_Instance");
        ringInstance.transform.position = worldPosition + Vector3.up * groundHeightOffset;
        ringInstance.transform.rotation = Quaternion.identity;

        // 1. FLOOR SEAL (Horizontal Quad)
        GameObject floorGO = new GameObject("FloorSeal");
        floorGO.transform.SetParent(ringInstance.transform);
        floorGO.transform.localPosition = Vector3.zero;
        floorGO.transform.localRotation = Quaternion.identity;
        floorGO.transform.localScale = Vector3.one * ringDiameter;

        MeshFilter mf = floorGO.AddComponent<MeshFilter>();
        mf.mesh = quadMesh;

        MeshRenderer mr = floorGO.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        Shader ringShader = Shader.Find("VFX/HolyGroundRing");
        if (ringShader == null) ringShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (ringShader == null) ringShader = Shader.Find("Particles/Standard Unlit");
        if (ringShader == null) ringShader = Shader.Find("Mobile/Particles/Additive");

        Material floorMat = new Material(ringShader);
        floorMat.SetColor("_Color", ringColor);
        floorMat.SetColor("_BaseColor", ringColor);
        floorMat.SetColor("_CoreColor", coreColor);
        floorMat.SetFloat("_Radius", 0.85f);
        floorMat.SetFloat("_Thickness", ringThickness);
        floorMat.SetFloat("_InnerRingRadius", 0.85f * innerRingRatio);
        floorMat.SetFloat("_RaysCount", floorRayCount);
        floorMat.SetFloat("_NeedleSharpness", floorNeedleSharpness);
        floorMat.SetFloat("_NeedleLength", floorNeedleLength);
        floorMat.SetFloat("_CenterDampening", centerGlowDampening);
        floorMat.SetFloat("_Brightness", brightness);
        floorMat.SetFloat("_ActivationProgress", 0.05f); // Instantly visible on spawn
        floorMat.SetFloat("_Dissolve", 0f);
        ConfigureAdditiveFallback(floorMat, ringColor);
        mr.material = floorMat;

        // 2. VERTICAL SUN NEEDLES (Rising into the air like sunbeams)
        GameObject vertNeedlesGO = null;
        Material vertNeedlesMat = null;
        if (enableVerticalSunNeedles)
        {
            vertNeedlesGO = new GameObject("VerticalSunNeedles_Cage");
            vertNeedlesGO.transform.SetParent(ringInstance.transform);
            vertNeedlesGO.transform.localPosition = Vector3.zero;
            vertNeedlesGO.transform.localRotation = Quaternion.identity;
            vertNeedlesGO.transform.localScale = Vector3.one;

            MeshFilter vmf = vertNeedlesGO.AddComponent<MeshFilter>();
            vmf.mesh = CreateVerticalSunNeedlesMesh(ringDiameter * 0.42f, verticalNeedleCount, verticalNeedleHeight, verticalNeedleWidth, verticalNeedleTilt);

            MeshRenderer vmr = vertNeedlesGO.AddComponent<MeshRenderer>();
            vmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            vmr.receiveShadows = false;

            Shader needleShader = Shader.Find("VFX/VerticalSunNeedles");
            if (needleShader == null) needleShader = ringShader;

            vertNeedlesMat = new Material(needleShader);
            vertNeedlesMat.SetColor("_Color", verticalNeedleColor);
            vertNeedlesMat.SetColor("_BaseColor", verticalNeedleColor);
            vertNeedlesMat.SetColor("_CoreColor", verticalNeedleCoreColor);
            vertNeedlesMat.SetFloat("_Brightness", brightness * 1.1f);
            vertNeedlesMat.SetFloat("_NeedleSharpness", 3.0f);
            vertNeedlesMat.SetFloat("_HeightProgress", 0f); // Hidden until Level 4
            vertNeedlesMat.SetFloat("_Dissolve", 0f);
            ConfigureAdditiveFallback(vertNeedlesMat, verticalNeedleColor);
            vmr.material = vertNeedlesMat;
        }

        // 3. RISING SPARKS
        ParticleSystem sparks = null;
        if (enableRisingSparks)
            sparks = CreateRisingSparks(ringInstance.transform);

        // 4. FLASH LIGHT
        Light flashLight = null;
        if (enableFlashLight)
            flashLight = CreateImpactLight(ringInstance.transform);

        // ==========================================
        // LEVEL 1: CREATE CIRCLE (Progress 0.05 -> 0.25)
        // ==========================================
        float elapsed = 0f;
        while (elapsed < stage1CircleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / stage1CircleDuration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            floorMat.SetFloat("_ActivationProgress", Mathf.Lerp(0.05f, 0.25f, ease));
            yield return null;
        }
        floorMat.SetFloat("_ActivationProgress", 0.25f);

        // ==========================================
        // LEVEL 2: SACRED INNER ARENA GLOW (Progress 0.25 -> 0.45)
        // ==========================================
        elapsed = 0f;
        while (elapsed < stage2DarkFloorDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / stage2DarkFloorDuration);
            floorMat.SetFloat("_ActivationProgress", Mathf.Lerp(0.25f, 0.45f, t));
            yield return null;
        }
        floorMat.SetFloat("_ActivationProgress", 0.45f);

        // ==========================================
        // LEVEL 3: DESIGN BETWEEN THEM (Progress 0.45 -> 0.70)
        // ==========================================
        elapsed = 0f;
        while (elapsed < stage3DesignDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / stage3DesignDuration);
            floorMat.SetFloat("_ActivationProgress", Mathf.Lerp(0.45f, 0.70f, t));
            yield return null;
        }
        floorMat.SetFloat("_ActivationProgress", 0.70f);

        // ==========================================
        // LEVEL 4: HIGHLY ACTIVATED!
        // VERTICAL SUN NEEDLES SHOOT UPWARD + SPARKS ERUPT
        // ==========================================
        if (sparks != null) sparks.Play();

        // Shoot up vertical sun needles
        StartCoroutine(AnimateVerticalNeedlesShoot(vertNeedlesMat));

        elapsed = 0f;
        while (elapsed < stage4ActivatedDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / stage4ActivatedDuration);
            float ease = 1f - Mathf.Pow(1f - t, 4f);
            floorMat.SetFloat("_ActivationProgress", Mathf.Lerp(0.70f, 1.0f, ease));

            if (flashLight != null)
                flashLight.intensity = Mathf.Lerp(0f, lightIntensity, ease);

            yield return null;
        }
        floorMat.SetFloat("_ActivationProgress", 1.0f);

        // ==========================================
        // HOLD PHASE (Radiates in full glory)
        // ==========================================
        elapsed = 0f;
        while (elapsed < holdDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / holdDuration;

            if (flashLight != null)
                flashLight.intensity = Mathf.Lerp(lightIntensity, lightIntensity * 0.3f, t);

            yield return null;
        }

        if (flashLight != null)
            flashLight.enabled = false;

        // ==========================================
        // FADE / DISSOLVE PHASE
        // ==========================================
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            floorMat.SetFloat("_Dissolve", t);
            if (vertNeedlesMat != null)
                vertNeedlesMat.SetFloat("_Dissolve", t);

            yield return null;
        }

        // Cleanup
        isPlaying = false;
        activeRoutine = null;
        Destroy(ringInstance);
    }

    private IEnumerator AnimateVerticalNeedlesShoot(Material needleMat)
    {
        if (needleMat == null) yield break;
        float elapsed = 0f;
        while (elapsed < needleShootDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / needleShootDuration);
            // Powerful explosive shoot-up with slight overshoot
            float ease = 1f - Mathf.Pow(1f - t, 3f);
            needleMat.SetFloat("_HeightProgress", ease);
            yield return null;
        }
        needleMat.SetFloat("_HeightProgress", 1.0f);
    }

    /// <summary>
    /// Procedurally builds a circular crown/cage of vertical light needles standing in the air.
    /// Exactly matches the user's drawing in media_1789888309821.png.
    /// </summary>
    private Mesh CreateVerticalSunNeedlesMesh(float radius, int count, float height, float baseWidth, float outwardTilt)
    {
        Mesh mesh = new Mesh();
        mesh.name = "VerticalSunNeedles_RingMesh";

        int vertCount = count * 4;
        int triCount = count * 12; // Double-sided quads

        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs = new Vector2[vertCount];
        Color[] colors = new Color[vertCount];
        int[] tris = new int[triCount];

        float tiltRad = outwardTilt * Mathf.Deg2Rad;
        float tiltOffset = Mathf.Sin(tiltRad) * height;
        float vertHeight = Mathf.Cos(tiltRad) * height;

        int vertIdx = 0;
        int triIdx = 0;

        for (int i = 0; i < count; i++)
        {
            float angle = (float)i / count * Mathf.PI * 2f;
            Vector3 center = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            Vector3 outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));

            // Base left & right
            Vector3 bLeft = center - tangent * (baseWidth * 0.5f);
            Vector3 bRight = center + tangent * (baseWidth * 0.5f);

            // Top tip tilted outward like sun rays
            Vector3 tipCenter = center + Vector3.up * vertHeight + outward * tiltOffset;
            Vector3 tLeft = tipCenter - tangent * (baseWidth * 0.1f);
            Vector3 tRight = tipCenter + tangent * (baseWidth * 0.1f);

            int b0 = vertIdx;
            verts[vertIdx] = bLeft;   uvs[vertIdx] = new Vector2(0f, 0f); colors[vertIdx] = Color.white; vertIdx++;
            verts[vertIdx] = bRight;  uvs[vertIdx] = new Vector2(1f, 0f); colors[vertIdx] = Color.white; vertIdx++;
            verts[vertIdx] = tRight;  uvs[vertIdx] = new Vector2(1f, 1f); colors[vertIdx] = Color.white; vertIdx++;
            verts[vertIdx] = tLeft;   uvs[vertIdx] = new Vector2(0f, 1f); colors[vertIdx] = Color.white; vertIdx++;

            // Front
            tris[triIdx++] = b0;
            tris[triIdx++] = b0 + 2;
            tris[triIdx++] = b0 + 1;

            tris[triIdx++] = b0;
            tris[triIdx++] = b0 + 3;
            tris[triIdx++] = b0 + 2;

            // Back (double-sided)
            tris[triIdx++] = b0;
            tris[triIdx++] = b0 + 1;
            tris[triIdx++] = b0 + 2;

            tris[triIdx++] = b0;
            tris[triIdx++] = b0 + 2;
            tris[triIdx++] = b0 + 3;
        }

        mesh.vertices = verts;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private ParticleSystem CreateRisingSparks(Transform parent)
    {
        GameObject sparksGO = new GameObject("RisingSparks");
        sparksGO.transform.SetParent(parent);
        sparksGO.transform.localPosition = Vector3.zero;
        sparksGO.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        sparksGO.transform.localScale = Vector3.one;

        ParticleSystem ps = sparksGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(sparkLifetime * 0.6f, sparkLifetime);
        main.startSize = sparkSize;
        main.startSpeed = new ParticleSystem.MinMaxCurve(sparkUpSpeed * 0.5f, sparkUpSpeed);
        main.startColor = sparkColor;
        main.maxParticles = sparkCount * 2;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.gravityModifier = -0.1f;
        main.playOnAwake = false;
        main.loop = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, sparkCount)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = ringDiameter * 0.42f;
        shape.radiusThickness = 0.25f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0f, 0.4f);
        sc.AddKey(0.2f, 1f);
        sc.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.8f, 0.2f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = g;

        var renderer = sparksGO.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Shader sparkShader = Shader.Find("VFX/AdditiveParticle");
        if (sparkShader == null) sparkShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sparkShader == null) sparkShader = Shader.Find("Particles/Standard Unlit");
        if (sparkShader == null) sparkShader = Shader.Find("Mobile/Particles/Additive");

        Material sparkMat = new Material(sparkShader);
        sparkMat.SetColor("_Color", sparkColor);
        sparkMat.SetColor("_BaseColor", sparkColor);
        sparkMat.SetFloat("_Brightness", 3.5f);
        sparkMat.SetFloat("_StarIntensity", 2.0f);
        sparkMat.SetFloat("_CoreSharpness", 5.0f);
        ConfigureAdditiveFallback(sparkMat, sparkColor);
        renderer.material = sparkMat;

        return ps;
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

    private Light CreateImpactLight(Transform parent)
    {
        GameObject lightGO = new GameObject("RingLightFlash");
        lightGO.transform.SetParent(parent);
        lightGO.transform.localPosition = Vector3.up * 0.15f; // Ground level to avoid blinding character torso

        Light l = lightGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = lightColor;
        l.intensity = 0f;
        l.range = lightRange;
        l.shadows = LightShadows.None;
        return l;
    }

    private Mesh CreateHorizontalQuad()
    {
        Mesh mesh = new Mesh();
        mesh.name = "FloorRingQuad";

        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3( 0.5f, 0f, -0.5f),
            new Vector3( 0.5f, 0f,  0.5f),
            new Vector3(-0.5f, 0f,  0.5f)
        };

        mesh.uv = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f)
        };

        mesh.colors = new Color[]
        {
            Color.white,
            Color.white,
            Color.white,
            Color.white
        };

        mesh.triangles = new int[]
        {
            0, 2, 1, 0, 3, 2,
            0, 1, 2, 0, 2, 3
        };

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, ringDiameter * 0.5f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * (verticalNeedleHeight * 0.5f), new Vector3(ringDiameter, verticalNeedleHeight, ringDiameter));
    }
}
