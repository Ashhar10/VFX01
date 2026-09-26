using UnityEngine;

/// <summary>
/// ArthurDefensiveStanceVFX - Implements King Arthur's "Defensive Stance" skill VFX.
/// Features:
/// 1. Pentagon Geodesic Dome Shield surrounding the character with glowing vertex nodes and edge facets.
/// 2. Floating golden heater shield in front with central ridge and double beveled borders.
/// 3. Orbiting ambient sparkle stars slowly drifting and twinkling around the dome perimeter.
/// 4. Concentric ground rune ring with radial ticks under feet.
/// 5. Color Theme switcher: Golden Holy (Image 1) or Pink Rose Quartz (with HDR bloom).
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("VFX/King Arthur/Arthur Defensive Stance VFX")]
public class ArthurDefensiveStanceVFX : MonoBehaviour
{
    public enum ShieldColorTheme
    {
        GoldenHoly,
        PinkRose,
        Custom
    }

    [Header("=== Color Theme & Bloom ===")]
    [Tooltip("Switch between Golden Holy (reference image) or Pink Rose Quartz energy")]
    public ShieldColorTheme colorTheme = ShieldColorTheme.GoldenHoly;

    [Range(0.5f, 10f), Tooltip("HDR Bloom intensity multiplier")]
    public float bloomIntensity = 3.2f;

    [Header("=== 1. Pentagon Geodesic Dome Shield ===")]
    public bool enableDome = true;
    [Tooltip("Radius of the spherical forcefield bubble")]
    public float domeRadius = 1.55f;
    [Tooltip("Vertical center offset (0.95m aligns directly with King Arthur's chest)")]
    public float domeHeightOffset = 0.95f;
    public float domeRotationSpeed = 8.0f;
    [ColorUsage(true, true), Tooltip("Core shield tint. Low alpha (~0.08) keeps King Arthur clearly visible")]
    public Color domeColor = new Color(1.0f, 0.82f, 0.35f, 0.08f);

    [Header("=== Dome Fresnel Rim & Hex/Pentagon Grid ===")]
    [Range(0.5f, 8f), Tooltip("Fresnel sharpness (higher = concentrated on outer rim)")]
    public float fresnelPower = 2.8f;
    [Range(0.5f, 5f), Tooltip("Intensity of the glowing golden silhouette rim")]
    public float fresnelIntensity = 2.2f;
    [Range(0.0f, 0.4f), Tooltip("Center transparency so King Arthur is never washed out")]
    public float centerAlpha = 0.06f;
    [Range(0.2f, 1f), Tooltip("Edge silhouette opacity")]
    public float rimAlpha = 0.85f;
    [Tooltip("Honeycomb / Pentagon grid density across the sphere")]
    public float gridTiling = 2.2f;
    [Range(0.5f, 5f), Tooltip("Brightness of the golden grid lines")]
    public float gridIntensity = 2.5f;
    [Tooltip("Seamless 3D projection: eliminates all seams and polar pinching")]
    public bool useTriplanar = true;

    [Header("=== 2. Floating Heater Shield ===")]
    public bool enableHeaterShield = true;
    public Vector3 shieldOffset = new Vector3(0.35f, 1.05f, 0.55f);
    public Vector2 shieldSize = new Vector2(0.95f, 1.15f);
    public float shieldBobSpeed = 2.4f;
    public float shieldBobAmount = 0.04f;
    [ColorUsage(true, true)] public Color heaterShieldColor = new Color(1.0f, 0.85f, 0.45f, 1.0f);

    [Header("=== 3. Orbiting Sparkles ===")]
    public bool enableSparkles = true;
    public int sparkleRate = 22;
    [ColorUsage(true, true)] public Color sparkleColor = new Color(1.0f, 0.95f, 0.70f, 1.0f);

    [Header("=== 4. Ground Rune Ring ===")]
    public bool enableGroundRing = true;
    public float groundRingRadius = 2.1f;
    public float groundRingRotationSpeed = -10.0f;
    [ColorUsage(true, true)] public Color groundRingColor = new Color(1.0f, 0.80f, 0.35f, 0.90f);

    [Header("=== Component References (Auto-Created) ===")]
    public MeshFilter domeMeshFilter;
    public MeshRenderer domeMeshRenderer;
    public ParticleSystem heaterShieldPS;
    public ParticleSystem sparklesPS;
    public ParticleSystem groundRingPS;

    [Header("=== Assets & Materials (Auto-Loaded) ===")]
    public Mesh domeMesh;
    public Material domeMaterial;
    public Material heaterShieldMaterial;
    public Material groundRingMaterial;
    public Material sparklesMaterial;

    private const string DOME_MESH_PATH = "Assets/VFX/King Arthur_VFX/Meshes/Arthur_Shield_Dome.obj";
    private const string DOME_MAT_PATH = "Assets/VFX/King Arthur_VFX/Materials/KingArthur_Defense_PentagonDome_Mat.mat";
    private const string SHIELD_MAT_PATH = "Assets/VFX/King Arthur_VFX/Materials/KingArthur_Defense_HeaterShield_Mat.mat";
    private const string RING_MAT_PATH = "Assets/VFX/King Arthur_VFX/Materials/KingArthur_Defense_GroundRing_Mat.mat";
    private const string SPARKS_MAT_PATH = "Assets/VFX/King Arthur_VFX/Materials/KingArthur_Defense_Sparkles_Mat.mat";

    [System.NonSerialized]
    private ShieldColorTheme lastTheme = (ShieldColorTheme)(-1);

    private void Reset()
    {
        InitializeVFX();
    }

    private void Awake()
    {
        InitializeVFX();
    }

    private void OnEnable()
    {
        InitializeVFX();
        Play();
    }

    private void Start()
    {
        InitializeVFX();
        Play();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (colorTheme != lastTheme)
        {
            lastTheme = colorTheme;
            if (colorTheme == ShieldColorTheme.PinkRose)
            {
                domeColor = new Color(1.0f, 0.35f, 0.75f, 0.08f);
                heaterShieldColor = new Color(1.0f, 0.45f, 0.85f, 1.0f);
                sparkleColor = new Color(1.0f, 0.70f, 0.95f, 1.0f);
                groundRingColor = new Color(0.95f, 0.30f, 0.70f, 0.90f);
            }
            else if (colorTheme == ShieldColorTheme.GoldenHoly)
            {
                domeColor = new Color(1.0f, 0.82f, 0.35f, 0.08f);
                heaterShieldColor = new Color(1.0f, 0.85f, 0.45f, 1.0f);
                sparkleColor = new Color(1.0f, 0.95f, 0.70f, 1.0f);
                groundRingColor = new Color(1.0f, 0.80f, 0.35f, 0.90f);
            }
        }

        ApplySettings();
    }
#endif

    private void Update()
    {
        float dt = Application.isPlaying ? Time.deltaTime : 0.016f;
        float time = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;

        // 1. Rotate Dome slowly
        if (domeMeshFilter != null && enableDome)
        {
            domeMeshFilter.transform.Rotate(Vector3.up, domeRotationSpeed * dt, Space.Self);
        }

        // 2. Bob Heater Shield gently
        if (heaterShieldPS != null && enableHeaterShield)
        {
            float bob = Mathf.Sin(time * shieldBobSpeed) * shieldBobAmount;
            heaterShieldPS.transform.localPosition = shieldOffset + new Vector3(0f, bob, 0f);
        }

        // 3. Rotate Ground Ring
        if (groundRingPS != null && enableGroundRing)
        {
            groundRingPS.transform.Rotate(Vector3.up, groundRingRotationSpeed * dt, Space.Self);
        }
    }

    [ContextMenu("Set Theme: Golden")]
    public void SetThemeGolden()
    {
        colorTheme = ShieldColorTheme.GoldenHoly;
        lastTheme = colorTheme;
        domeColor = new Color(1.0f, 0.82f, 0.35f, 0.08f);
        heaterShieldColor = new Color(1.0f, 0.85f, 0.45f, 1.0f);
        sparkleColor = new Color(1.0f, 0.95f, 0.70f, 1.0f);
        groundRingColor = new Color(1.0f, 0.80f, 0.35f, 0.90f);
        ApplySettings();
    }

    [ContextMenu("Set Theme: Pink")]
    public void SetThemePink()
    {
        colorTheme = ShieldColorTheme.PinkRose;
        lastTheme = colorTheme;
        domeColor = new Color(1.0f, 0.35f, 0.75f, 0.08f);
        heaterShieldColor = new Color(1.0f, 0.45f, 0.85f, 1.0f);
        sparkleColor = new Color(1.0f, 0.70f, 0.95f, 1.0f);
        groundRingColor = new Color(0.95f, 0.30f, 0.70f, 0.90f);
        ApplySettings();
    }

    public void InitializeVFX()
    {
        LoadDefaultAssets();
        BuildDomeGameObject();
        BuildHeaterShieldGameObject();
        BuildSparklesGameObject();
        BuildGroundRingGameObject();
        ApplySettings();
    }

    private void LoadDefaultAssets()
    {
#if UNITY_EDITOR
        if (domeMesh == null)
            domeMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(DOME_MESH_PATH);
        if (domeMaterial == null || domeMaterial.shader == null || domeMaterial.shader.name == "Hidden/InternalErrorShader")
            domeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(DOME_MAT_PATH);
        if (heaterShieldMaterial == null || heaterShieldMaterial.shader == null || heaterShieldMaterial.shader.name == "Hidden/InternalErrorShader")
            heaterShieldMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(SHIELD_MAT_PATH);
        if (groundRingMaterial == null || groundRingMaterial.shader == null || groundRingMaterial.shader.name == "Hidden/InternalErrorShader")
            groundRingMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(RING_MAT_PATH);
        if (sparklesMaterial == null || sparklesMaterial.shader == null || sparklesMaterial.shader.name == "Hidden/InternalErrorShader")
            sparklesMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(SPARKS_MAT_PATH);
#endif
    }

    private Mesh GetOrLoadDomeMesh(ref Mesh meshField, string assetPath)
    {
        if (meshField != null) return meshField;

#if UNITY_EDITOR
        meshField = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        if (meshField != null) return meshField;
#endif

        GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        meshField = tempSphere.GetComponent<MeshFilter>().sharedMesh;
        if (Application.isPlaying) Destroy(tempSphere);
        else DestroyImmediate(tempSphere);
        return meshField;
    }

    private Material GetOrLoadMaterial(ref Material matField, string assetPath, string textureName, Color baseColor, Color emissionColor, Vector2? tiling = null)
    {
        bool isDome = (assetPath == DOME_MAT_PATH);
        Shader customShieldShader = isDome ? Shader.Find("VFX/King Arthur/Defense Shield") : null;

        if (matField != null && matField.shader != null && matField.shader.name != "Hidden/InternalErrorShader")
        {
            if (isDome && customShieldShader != null && matField.shader != customShieldShader)
            {
                matField.shader = customShieldShader;
            }
            else if (!isDome && matField.shader.name == "Universal Render Pipeline/Unlit")
            {
                Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (s != null) matField.shader = s;
            }

            matField.EnableKeyword("_EMISSION");
            matField.SetColor("_BaseColor", baseColor);
            matField.SetColor("_Color", baseColor);
            matField.SetColor("_EmissionColor", emissionColor);

            if (isDome)
            {
                if (matField.HasProperty("_RimColor")) matField.SetColor("_RimColor", emissionColor);
                if (matField.HasProperty("_BloomMultiplier")) matField.SetFloat("_BloomMultiplier", bloomIntensity);
                if (matField.HasProperty("_FresnelPower")) matField.SetFloat("_FresnelPower", fresnelPower);
                if (matField.HasProperty("_FresnelIntensity")) matField.SetFloat("_FresnelIntensity", fresnelIntensity);
                if (matField.HasProperty("_CenterAlpha")) matField.SetFloat("_CenterAlpha", centerAlpha);
                if (matField.HasProperty("_RimAlpha")) matField.SetFloat("_RimAlpha", rimAlpha);
                if (matField.HasProperty("_Tiling")) matField.SetFloat("_Tiling", gridTiling);
                if (matField.HasProperty("_GridIntensity")) matField.SetFloat("_GridIntensity", gridIntensity);
                if (matField.HasProperty("_UseTriplanar")) matField.SetFloat("_UseTriplanar", useTriplanar ? 1f : 0f);
            }

            if (tiling.HasValue)
            {
                matField.SetTextureScale("_BaseMap", tiling.Value);
                matField.SetTextureScale("_MainTex", tiling.Value);
                if (matField.HasProperty("_EmissionMap"))
                {
                    matField.SetTextureScale("_EmissionMap", tiling.Value);
                }
            }
#if UNITY_EDITOR
            if (matField.HasProperty("_BaseMap") && matField.GetTexture("_BaseMap") == null)
            {
                Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VFX/King Arthur_VFX/Textures/" + textureName);
                if (tex != null) matField.SetTexture("_BaseMap", tex);
            }
            if (matField.HasProperty("_EmissionMap") && matField.GetTexture("_EmissionMap") == null)
            {
                Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VFX/King Arthur_VFX/Textures/" + textureName);
                if (tex != null) matField.SetTexture("_EmissionMap", tex);
            }
#endif
            return matField;
        }

#if UNITY_EDITOR
        matField = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (matField != null && matField.shader != null && matField.shader.name != "Hidden/InternalErrorShader")
        {
            if (isDome && customShieldShader != null && matField.shader != customShieldShader)
            {
                matField.shader = customShieldShader;
            }
            else if (!isDome && matField.shader.name == "Universal Render Pipeline/Unlit")
            {
                Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (s != null) matField.shader = s;
            }

            matField.EnableKeyword("_EMISSION");
            matField.SetColor("_BaseColor", baseColor);
            matField.SetColor("_Color", baseColor);
            matField.SetColor("_EmissionColor", emissionColor);

            if (isDome)
            {
                if (matField.HasProperty("_RimColor")) matField.SetColor("_RimColor", emissionColor);
                if (matField.HasProperty("_BloomMultiplier")) matField.SetFloat("_BloomMultiplier", bloomIntensity);
                if (matField.HasProperty("_FresnelPower")) matField.SetFloat("_FresnelPower", fresnelPower);
                if (matField.HasProperty("_FresnelIntensity")) matField.SetFloat("_FresnelIntensity", fresnelIntensity);
                if (matField.HasProperty("_CenterAlpha")) matField.SetFloat("_CenterAlpha", centerAlpha);
                if (matField.HasProperty("_RimAlpha")) matField.SetFloat("_RimAlpha", rimAlpha);
                if (matField.HasProperty("_Tiling")) matField.SetFloat("_Tiling", gridTiling);
                if (matField.HasProperty("_GridIntensity")) matField.SetFloat("_GridIntensity", gridIntensity);
                if (matField.HasProperty("_UseTriplanar")) matField.SetFloat("_UseTriplanar", useTriplanar ? 1f : 0f);
            }

            if (tiling.HasValue)
            {
                matField.SetTextureScale("_BaseMap", tiling.Value);
                matField.SetTextureScale("_MainTex", tiling.Value);
                if (matField.HasProperty("_EmissionMap"))
                {
                    matField.SetTextureScale("_EmissionMap", tiling.Value);
                }
            }
            if (matField.HasProperty("_BaseMap") && matField.GetTexture("_BaseMap") == null)
            {
                Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VFX/King Arthur_VFX/Textures/" + textureName);
                if (tex != null) matField.SetTexture("_BaseMap", tex);
            }
            if (matField.HasProperty("_EmissionMap") && matField.GetTexture("_EmissionMap") == null)
            {
                Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VFX/King Arthur_VFX/Textures/" + textureName);
                if (tex != null) matField.SetTexture("_EmissionMap", tex);
            }
            return matField;
        }
#endif

        // Failsafe dynamic material creation so pink / missing shader is impossible
        Shader shader = customShieldShader;
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");

        Material fallback = new Material(shader);
        fallback.name = "Dynamic_" + System.IO.Path.GetFileNameWithoutExtension(assetPath);
        fallback.SetColor("_BaseColor", baseColor);
        fallback.SetColor("_Color", baseColor);
        fallback.SetColor("_EmissionColor", emissionColor);
        fallback.EnableKeyword("_EMISSION");
        fallback.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        fallback.renderQueue = 3010;
        fallback.SetFloat("_Cull", 0f);

        if (isDome)
        {
            if (fallback.HasProperty("_RimColor")) fallback.SetColor("_RimColor", emissionColor);
            if (fallback.HasProperty("_BloomMultiplier")) fallback.SetFloat("_BloomMultiplier", bloomIntensity);
            if (fallback.HasProperty("_FresnelPower")) fallback.SetFloat("_FresnelPower", fresnelPower);
            if (fallback.HasProperty("_FresnelIntensity")) fallback.SetFloat("_FresnelIntensity", fresnelIntensity);
            if (fallback.HasProperty("_CenterAlpha")) fallback.SetFloat("_CenterAlpha", centerAlpha);
            if (fallback.HasProperty("_RimAlpha")) fallback.SetFloat("_RimAlpha", rimAlpha);
            if (fallback.HasProperty("_Tiling")) fallback.SetFloat("_Tiling", gridTiling);
            if (fallback.HasProperty("_GridIntensity")) fallback.SetFloat("_GridIntensity", gridIntensity);
            if (fallback.HasProperty("_UseTriplanar")) fallback.SetFloat("_UseTriplanar", useTriplanar ? 1f : 0f);
        }

#if UNITY_EDITOR
        Texture2D texFallback = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VFX/King Arthur_VFX/Textures/" + textureName);
        if (texFallback != null)
        {
            fallback.SetTexture("_BaseMap", texFallback);
            fallback.SetTexture("_MainTex", texFallback);
            if (fallback.HasProperty("_EmissionMap"))
            {
                fallback.SetTexture("_EmissionMap", texFallback);
            }
        }
#endif
        if (tiling.HasValue)
        {
            fallback.SetTextureScale("_BaseMap", tiling.Value);
            fallback.SetTextureScale("_MainTex", tiling.Value);
            if (fallback.HasProperty("_EmissionMap"))
            {
                fallback.SetTextureScale("_EmissionMap", tiling.Value);
            }
        }

        matField = fallback;
        return fallback;
    }

    private void BuildDomeGameObject()
    {
        Transform child = transform.Find("1_Pentagon_Shield_Dome");
        if (child == null)
        {
            GameObject go = new GameObject("1_Pentagon_Shield_Dome");
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        domeMeshFilter = child.GetComponent<MeshFilter>();
        if (domeMeshFilter == null) domeMeshFilter = child.gameObject.AddComponent<MeshFilter>();

        domeMeshRenderer = child.GetComponent<MeshRenderer>();
        if (domeMeshRenderer == null) domeMeshRenderer = child.gameObject.AddComponent<MeshRenderer>();

        if (domeMesh != null) domeMeshFilter.sharedMesh = domeMesh;
        if (domeMaterial != null) domeMeshRenderer.sharedMaterial = domeMaterial;

        domeMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        domeMeshRenderer.receiveShadows = false;
    }

    private void BuildHeaterShieldGameObject()
    {
        Transform child = transform.Find("2_Heater_Shield_Face");
        if (child == null)
        {
            GameObject go = new GameObject("2_Heater_Shield_Face");
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        heaterShieldPS = child.GetComponent<ParticleSystem>();
        if (heaterShieldPS == null) heaterShieldPS = child.gameObject.AddComponent<ParticleSystem>();
    }

    private void BuildSparklesGameObject()
    {
        Transform child = transform.Find("3_Orbiting_Sparkles");
        if (child == null)
        {
            GameObject go = new GameObject("3_Orbiting_Sparkles");
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        sparklesPS = child.GetComponent<ParticleSystem>();
        if (sparklesPS == null) sparklesPS = child.gameObject.AddComponent<ParticleSystem>();
    }

    private void BuildGroundRingGameObject()
    {
        Transform child = transform.Find("4_Ground_Rune_Ring");
        if (child == null)
        {
            GameObject go = new GameObject("4_Ground_Rune_Ring");
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        groundRingPS = child.GetComponent<ParticleSystem>();
        if (groundRingPS == null) groundRingPS = child.gameObject.AddComponent<ParticleSystem>();
    }

    [ContextMenu("Apply Settings")]
    public void ApplySettings()
    {
        LoadDefaultAssets();

        // 1. Pentagon Dome
        if (domeMeshRenderer != null)
        {
            domeMeshRenderer.enabled = enableDome;
            if (domeMeshFilter != null)
            {
                domeMeshFilter.transform.localPosition = new Vector3(0f, domeHeightOffset, 0f);
                domeMeshFilter.transform.localScale = Vector3.one * domeRadius;
                domeMeshFilter.sharedMesh = GetOrLoadDomeMesh(ref domeMesh, DOME_MESH_PATH);
            }

            Color hdrDome = new Color(
                domeColor.r * bloomIntensity,
                domeColor.g * bloomIntensity,
                domeColor.b * bloomIntensity,
                domeColor.a
            );
            Material targetDomeMat = GetOrLoadMaterial(ref domeMaterial, DOME_MAT_PATH, "Arthur_Pentagon_Grid.png", domeColor, hdrDome, new Vector2(gridTiling, gridTiling));
            domeMeshRenderer.sharedMaterial = targetDomeMat;
            domeMeshRenderer.sharedMaterials = new Material[] { targetDomeMat };
            if (Application.isPlaying)
            {
                domeMeshRenderer.material = targetDomeMat;
            }
        }

        // 2. Floating Heater Shield Particle
        if (heaterShieldPS != null)
        {
            ConfigureHeaterShield(heaterShieldPS);
        }

        // 3. Orbiting Sparkles
        if (sparklesPS != null)
        {
            ConfigureSparkles(sparklesPS);
        }

        // 4. Ground Rune Ring
        if (groundRingPS != null)
        {
            ConfigureGroundRing(groundRingPS);
        }
    }

    private void ConfigureHeaterShield(ParticleSystem ps)
    {
        ps.gameObject.SetActive(enableHeaterShield);
        if (!enableHeaterShield) return;

        bool wasPlaying = ps.isPlaying;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = Mathf.Infinity;
        main.startSpeed = 0f;
        main.startSize3D = true;
        main.startSizeX = shieldSize.x;
        main.startSizeY = shieldSize.y;
        main.startSizeZ = 1f;

        Color hdrShield = new Color(
            heaterShieldColor.r * bloomIntensity,
            heaterShieldColor.g * bloomIntensity,
            heaterShieldColor.b * bloomIntensity,
            heaterShieldColor.a
        );
        main.startColor = hdrShield;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = false;
        main.maxParticles = 1;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.VerticalBillboard;
            Material targetMat = GetOrLoadMaterial(ref heaterShieldMaterial, SHIELD_MAT_PATH, "Arthur_Heater_Shield.png", heaterShieldColor, hdrShield);
            renderer.sharedMaterial = targetMat;
            renderer.sharedMaterials = new Material[] { targetMat };
            if (Application.isPlaying)
            {
                renderer.material = targetMat;
            }
        }

        if (wasPlaying || !Application.isPlaying)
        {
            ps.Play();
        }
    }

    private void ConfigureSparkles(ParticleSystem ps)
    {
        ps.gameObject.SetActive(enableSparkles);
        if (!enableSparkles) return;

        bool wasPlaying = ps.isPlaying;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 2.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);

        Color hdrSpark = new Color(
            sparkleColor.r * bloomIntensity,
            sparkleColor.g * bloomIntensity,
            sparkleColor.b * bloomIntensity,
            sparkleColor.a
        );
        main.startColor = hdrSpark;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = false;
        main.maxParticles = 36;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = sparkleRate;

        // Spherical shell shape matching the dome surface
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = domeRadius * 0.98f;
        shape.radiusThickness = 0.2f;
        shape.position = new Vector3(0f, 0.8f, 0f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = 0.35f;
        velocity.z = 0f;
        velocity.orbitalY = 15f * Mathf.Deg2Rad;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0f, 0f);
        sc.AddKey(0.2f, 1.3f);
        sc.AddKey(0.7f, 0.7f);
        sc.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            Material targetMat = GetOrLoadMaterial(ref sparklesMaterial, SPARKS_MAT_PATH, "Arthur_Sparkle_Star.png", sparkleColor, hdrSpark);
            renderer.sharedMaterial = targetMat;
            renderer.sharedMaterials = new Material[] { targetMat };
            if (Application.isPlaying)
            {
                renderer.material = targetMat;
            }
        }

        if (wasPlaying || !Application.isPlaying)
        {
            ps.Play();
        }
    }

    private void ConfigureGroundRing(ParticleSystem ps)
    {
        ps.gameObject.SetActive(enableGroundRing);
        if (!enableGroundRing) return;

        bool wasPlaying = ps.isPlaying;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = Mathf.Infinity;
        main.startSpeed = 0f;
        main.startSize = groundRingRadius * 2.0f;

        Color hdrRing = new Color(
            groundRingColor.r * bloomIntensity,
            groundRingColor.g * bloomIntensity,
            groundRingColor.b * bloomIntensity,
            groundRingColor.a
        );
        main.startColor = hdrRing;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = false;
        main.maxParticles = 1;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            Material targetMat = GetOrLoadMaterial(ref groundRingMaterial, RING_MAT_PATH, "Arthur_Ground_Rune_Ring.png", groundRingColor, hdrRing);
            renderer.sharedMaterial = targetMat;
            renderer.sharedMaterials = new Material[] { targetMat };
            if (Application.isPlaying)
            {
                renderer.material = targetMat;
            }
        }

        ps.transform.localPosition = new Vector3(0f, 0.02f, 0f);

        if (wasPlaying || !Application.isPlaying)
        {
            ps.Play();
        }
    }

    [ContextMenu("Play Defence Shield")]
    public void Play()
    {
        if (heaterShieldPS != null) heaterShieldPS.Play();
        if (sparklesPS != null) sparklesPS.Play();
        if (groundRingPS != null) groundRingPS.Play();
    }

    [ContextMenu("Stop Defence Shield")]
    public void Stop()
    {
        if (heaterShieldPS != null) heaterShieldPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (sparklesPS != null) sparklesPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (groundRingPS != null) groundRingPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
