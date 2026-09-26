using UnityEngine;

/// <summary>
/// ArthurKingdomClaimVFX - Visual effect for King Arthur's "Kingdom Claim / Taunt" needle VFX.
/// Features:
/// 1. Up Needle VFX: Radiant vertical light needles / spikes erupting upward along the circle perimeter.
/// 2. Flat Needle VFX: Razor-sharp light needles radiating outward along the floor in an orderly radial compass burst.
/// 3. Ascending Sparks: Rising golden motes and sparkles inside the arena.
/// 4. Ground Offset & Height Controls: Freely adjust needle height, width, offset off ground, and density.
/// 5. Color Theme switcher: Golden Holy (Image 1 reference) or Pink Rose.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[AddComponentMenu("VFX/King Arthur/Arthur Kingdom Claim VFX")]
public class ArthurKingdomClaimVFX : MonoBehaviour
{
    public enum KingdomClaimColorTheme
    {
        GoldenHoly,
        PinkRose,
        Custom
    }

    [Header("=== Color Theme & Bloom ===")]
    [Tooltip("Color Theme: Golden Holy (Reference) or Pink Rose")]
    public KingdomClaimColorTheme colorTheme = KingdomClaimColorTheme.GoldenHoly;

    [Range(0.5f, 10f), Tooltip("HDR Bloom intensity multiplier")]
    public float bloomIntensity = 3.8f;

    [Header("=== Needle Placement & Height Controls ===")]
    [Tooltip("Radius of the needle perimeter circle in meters")]
    public float arenaRadius = 2.2f;

    [Tooltip("Vertical height offset above ground (in meters) to position needle bases")]
    public float needleGroundOffset = 0.0f;

    [Header("=== 1. Up Needle VFX (Vertical Spikes) ===")]
    public bool enableUpNeedles = true;
    [Tooltip("Number of upward needles around perimeter")]
    public int upNeedleCount = 36;
    [Tooltip("Min and Max height of vertical needles (in meters)")]
    public Vector2 upNeedleHeightRange = new Vector2(0.8f, 1.6f);
    [Tooltip("Width / thickness of vertical needles")]
    public float upNeedleWidth = 0.08f;
    [ColorUsage(true, true)] public Color upNeedleColor = new Color(1.0f, 0.88f, 0.45f, 1.0f);

    [Header("=== 2. Flat Needle VFX (Radial Floor Spikes) ===")]
    public bool enableFlatNeedles = true;
    [Tooltip("Number of flat needles radiating outward")]
    public int flatNeedleCount = 32;
    [Tooltip("Min and Max length of radial needles (in meters)")]
    public Vector2 flatNeedleLengthRange = new Vector2(0.9f, 1.8f);
    [Tooltip("Width / thickness of flat needles")]
    public float flatNeedleWidth = 0.06f;
    [ColorUsage(true, true)] public Color flatNeedleColor = new Color(1.0f, 0.85f, 0.40f, 1.0f);

    [Header("=== 3. Ascending Sparkles ===")]
    public bool enableAscendingSparks = true;
    public int sparkRate = 16;
    [ColorUsage(true, true)] public Color sparkColor = new Color(1.0f, 0.95f, 0.70f, 1.0f);

    [Header("=== Component References (Auto-Created) ===")]
    public ParticleSystem upNeedlesPS;
    public ParticleSystem flatNeedlesPS;
    public ParticleSystem sparksPS;

    [Header("=== Materials (Auto-Loaded) ===")]
    public Material upNeedlesMaterial;
    public Material flatNeedlesMaterial;
    public Material sparklesMaterial;

    private const string UP_MAT_PATH = "Assets/VFX/King Arthur_VFX/Materials/KingArthur_Taunt_UpNeedles_Mat.mat";
    private const string FLAT_MAT_PATH = "Assets/VFX/King Arthur_VFX/Materials/KingArthur_Taunt_FlatNeedles_Mat.mat";
    private const string SPARKS_MAT_PATH = "Assets/VFX/King Arthur_VFX/Materials/KingArthur_Defense_Sparkles_Mat.mat";

    [System.NonSerialized]
    private KingdomClaimColorTheme lastTheme = (KingdomClaimColorTheme)(-1);

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
            if (colorTheme == KingdomClaimColorTheme.PinkRose)
            {
                upNeedleColor = new Color(1.0f, 0.40f, 0.85f, 1.0f);
                flatNeedleColor = new Color(1.0f, 0.35f, 0.80f, 1.0f);
                sparkColor = new Color(1.0f, 0.70f, 0.95f, 1.0f);
            }
            else if (colorTheme == KingdomClaimColorTheme.GoldenHoly)
            {
                upNeedleColor = new Color(1.0f, 0.88f, 0.45f, 1.0f);
                flatNeedleColor = new Color(1.0f, 0.85f, 0.40f, 1.0f);
                sparkColor = new Color(1.0f, 0.95f, 0.70f, 1.0f);
            }
        }

        ApplySettings();
    }
#endif

    [ContextMenu("Set Theme: Golden")]
    public void SetThemeGolden()
    {
        colorTheme = KingdomClaimColorTheme.GoldenHoly;
        lastTheme = colorTheme;
        upNeedleColor = new Color(1.0f, 0.88f, 0.45f, 1.0f);
        flatNeedleColor = new Color(1.0f, 0.85f, 0.40f, 1.0f);
        sparkColor = new Color(1.0f, 0.95f, 0.70f, 1.0f);
        ApplySettings();
    }

    [ContextMenu("Set Theme: Pink")]
    public void SetThemePink()
    {
        colorTheme = KingdomClaimColorTheme.PinkRose;
        lastTheme = colorTheme;
        upNeedleColor = new Color(1.0f, 0.40f, 0.85f, 1.0f);
        flatNeedleColor = new Color(1.0f, 0.35f, 0.80f, 1.0f);
        sparkColor = new Color(1.0f, 0.70f, 0.95f, 1.0f);
        ApplySettings();
    }

    public void InitializeVFX()
    {
        // Clean up any old ground ring child if present
        Transform oldRing = transform.Find("3_Ground_Arena_Ring");
        if (oldRing != null)
        {
            if (Application.isPlaying) Destroy(oldRing.gameObject);
            else DestroyImmediate(oldRing.gameObject);
        }

        LoadDefaultMaterials();
        BuildUpNeedlesGameObject();
        BuildFlatNeedlesGameObject();
        BuildSparksGameObject();
        ApplySettings();
    }

    private void LoadDefaultMaterials()
    {
#if UNITY_EDITOR
        if (upNeedlesMaterial == null || upNeedlesMaterial.shader == null || upNeedlesMaterial.shader.name == "Hidden/InternalErrorShader")
            upNeedlesMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UP_MAT_PATH);
        if (flatNeedlesMaterial == null || flatNeedlesMaterial.shader == null || flatNeedlesMaterial.shader.name == "Hidden/InternalErrorShader")
            flatNeedlesMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(FLAT_MAT_PATH);
        if (sparklesMaterial == null || sparklesMaterial.shader == null || sparklesMaterial.shader.name == "Hidden/InternalErrorShader")
            sparklesMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(SPARKS_MAT_PATH);
#endif
    }

    private Material GetOrLoadMaterial(ref Material matField, string assetPath, string textureName, Color baseColor, Color emissionColor)
    {
        if (matField != null && matField.shader != null && matField.shader.name != "Hidden/InternalErrorShader")
        {
            matField.SetColor("_BaseColor", baseColor);
            matField.SetColor("_Color", baseColor);
            matField.SetColor("_EmissionColor", emissionColor);
            return matField;
        }

#if UNITY_EDITOR
        matField = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (matField != null && matField.shader != null && matField.shader.name != "Hidden/InternalErrorShader")
        {
            matField.SetColor("_BaseColor", baseColor);
            matField.SetColor("_Color", baseColor);
            matField.SetColor("_EmissionColor", emissionColor);
            return matField;
        }
#endif

        // Failsafe dynamic material creation so pink / missing shader is impossible
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
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
        fallback.renderQueue = 3000;

#if UNITY_EDITOR
        Texture2D tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VFX/King Arthur_VFX/Textures/" + textureName);
        if (tex != null)
        {
            fallback.SetTexture("_BaseMap", tex);
            fallback.SetTexture("_MainTex", tex);
        }
#endif
        matField = fallback;
        return fallback;
    }

    private void BuildUpNeedlesGameObject()
    {
        Transform child = transform.Find("1_Up_Needles");
        if (child == null)
        {
            GameObject go = new GameObject("1_Up_Needles");
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        upNeedlesPS = child.GetComponent<ParticleSystem>();
        if (upNeedlesPS == null) upNeedlesPS = child.gameObject.AddComponent<ParticleSystem>();
    }

    private void BuildFlatNeedlesGameObject()
    {
        Transform child = transform.Find("2_Flat_Needles");
        if (child == null)
        {
            GameObject go = new GameObject("2_Flat_Needles");
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        flatNeedlesPS = child.GetComponent<ParticleSystem>();
        if (flatNeedlesPS == null) flatNeedlesPS = child.gameObject.AddComponent<ParticleSystem>();
    }

    private void BuildSparksGameObject()
    {
        Transform child = transform.Find("3_Ascending_Sparks");
        if (child == null)
        {
            GameObject go = new GameObject("3_Ascending_Sparks");
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        sparksPS = child.GetComponent<ParticleSystem>();
        if (sparksPS == null) sparksPS = child.gameObject.AddComponent<ParticleSystem>();
    }

    [ContextMenu("Apply Settings")]
    public void ApplySettings()
    {
        LoadDefaultMaterials();
        if (upNeedlesPS != null) ConfigureUpNeedles(upNeedlesPS);
        if (flatNeedlesPS != null) ConfigureFlatNeedles(flatNeedlesPS);
        if (sparksPS != null) ConfigureSparks(sparksPS);
    }

    private void ConfigureUpNeedles(ParticleSystem ps)
    {
        ps.gameObject.SetActive(enableUpNeedles);
        if (!enableUpNeedles) return;

        bool wasPlaying = ps.isPlaying;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);
        main.startSpeed = 0f; // Stationary in place along circle perimeter
        main.startSize3D = true;
        main.startSizeX = upNeedleWidth;
        main.startSizeY = new ParticleSystem.MinMaxCurve(upNeedleHeightRange.x, upNeedleHeightRange.y);
        main.startSizeZ = 1f;

        Color hdrUp = new Color(
            upNeedleColor.r * bloomIntensity,
            upNeedleColor.g * bloomIntensity,
            upNeedleColor.b * bloomIntensity,
            upNeedleColor.a
        );
        main.startColor = hdrUp;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = false;
        main.maxParticles = upNeedleCount * 2;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = upNeedleCount;

        // Circular perimeter emission at ground level
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = arenaRadius;
        shape.radiusThickness = 0.02f; // Clean sharp ring perimeter
        shape.rotation = new Vector3(90f, 0f, 0f);

        // Size over lifetime (sharp eruption and shimmer)
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0f, 0f);
        sc.AddKey(0.12f, 1.1f);
        sc.AddKey(0.5f, 0.95f);
        sc.AddKey(0.85f, 1.02f);
        sc.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        // Vertical Billboard renderer with pivot at needle base (Y = -0.5)
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.VerticalBillboard;
            renderer.pivot = new Vector3(0f, -0.5f, 0f); // Pivot at needle base: standing ABOVE ground!

            Material targetMat = GetOrLoadMaterial(ref upNeedlesMaterial, UP_MAT_PATH, "Light_Needle_Spike.png", upNeedleColor, hdrUp);
            renderer.sharedMaterial = targetMat;
            renderer.sharedMaterials = new Material[] { targetMat };
            if (Application.isPlaying)
            {
                renderer.material = targetMat;
            }
        }

        // Position base exactly at needleGroundOffset
        ps.transform.localPosition = new Vector3(0f, needleGroundOffset, 0f);

        if (wasPlaying || !Application.isPlaying)
        {
            ps.Play();
        }
    }

    private void ConfigureFlatNeedles(ParticleSystem ps)
    {
        ps.gameObject.SetActive(enableFlatNeedles);
        if (!enableFlatNeedles) return;

        bool wasPlaying = ps.isPlaying;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        main.startSpeed = 0f; // Stationary in place radiating outward!
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(flatNeedleLengthRange.x, flatNeedleLengthRange.y);
        main.startSizeY = flatNeedleWidth;
        main.startSizeZ = 1f;

        Color hdrFlat = new Color(
            flatNeedleColor.r * bloomIntensity,
            flatNeedleColor.g * bloomIntensity,
            flatNeedleColor.b * bloomIntensity,
            flatNeedleColor.a
        );
        main.startColor = hdrFlat;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = false;
        main.maxParticles = flatNeedleCount * 2;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = flatNeedleCount;

        // Radial circle alignment
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = arenaRadius;
        shape.radiusThickness = 0.02f;
        shape.rotation = new Vector3(90f, 0f, 0f);
        shape.alignToDirection = true; // Aligns horizontal needle pointing radially outward!

        // Size over lifetime (pulsing radial rays)
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0f, 0.2f);
        sc.AddKey(0.15f, 1.08f);
        sc.AddKey(0.7f, 0.95f);
        sc.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        // Horizontal Billboard facing outward
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            renderer.pivot = new Vector3(-0.5f, 0f, 0f); // Pivot at base, pointing OUTWARD!

            Material targetMat = GetOrLoadMaterial(ref flatNeedlesMaterial, FLAT_MAT_PATH, "Flat_Needle_Ray.png", flatNeedleColor, hdrFlat);
            renderer.sharedMaterial = targetMat;
            renderer.sharedMaterials = new Material[] { targetMat };
            if (Application.isPlaying)
            {
                renderer.material = targetMat;
            }
        }

        // Just slightly above ground to prevent Z-fighting
        ps.transform.localPosition = new Vector3(0f, needleGroundOffset + 0.015f, 0f);

        if (wasPlaying || !Application.isPlaying)
        {
            ps.Play();
        }
    }

    private void ConfigureSparks(ParticleSystem ps)
    {
        ps.gameObject.SetActive(enableAscendingSparks);
        if (!enableAscendingSparks) return;

        bool wasPlaying = ps.isPlaying;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 2.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);

        Color hdrSpark = new Color(
            sparkColor.r * bloomIntensity,
            sparkColor.g * bloomIntensity,
            sparkColor.b * bloomIntensity,
            sparkColor.a
        );
        main.startColor = hdrSpark;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = false;
        main.maxParticles = 24;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = sparkRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = arenaRadius * 0.9f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = 0.5f;
        velocity.z = 0f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0f, 0f);
        sc.AddKey(0.2f, 1.2f);
        sc.AddKey(0.7f, 0.7f);
        sc.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.pivot = Vector3.zero;

            Material targetMat = GetOrLoadMaterial(ref sparklesMaterial, SPARKS_MAT_PATH, "Arthur_Sparkle_Star.png", sparkColor, hdrSpark);
            renderer.sharedMaterial = targetMat;
            renderer.sharedMaterials = new Material[] { targetMat };
            if (Application.isPlaying)
            {
                renderer.material = targetMat;
            }
        }

        ps.transform.localPosition = new Vector3(0f, needleGroundOffset + 0.05f, 0f);

        if (wasPlaying || !Application.isPlaying)
        {
            ps.Play();
        }
    }

    [ContextMenu("Play Taunt")]
    public void Play()
    {
        if (upNeedlesPS != null) upNeedlesPS.Play();
        if (flatNeedlesPS != null) flatNeedlesPS.Play();
        if (sparksPS != null) sparksPS.Play();
    }

    [ContextMenu("Stop Taunt")]
    public void Stop()
    {
        if (upNeedlesPS != null) upNeedlesPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (flatNeedlesPS != null) flatNeedlesPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (sparksPS != null) sparksPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
