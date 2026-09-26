using UnityEngine;

/// <summary>
/// ArthurKingdomClaimVFX - Visual effect for King Arthur's "Kingdom Claim / Taunt" needle VFX.
/// 
/// Key Features:
/// 1. Up Needle VFX (Towering Holy Spikes):
///    - Erected ABOVE ground (base at Y=0), standing majestically upright (Y >= 0).
///    - Zero inward tilt: Stands 100% perpendicular to ground, forming a sacred light circle.
///    - BurstSpread circle distribution: Needles are spaced evenly around the 360-degree perimeter,
///      eliminating wide random gaps and clumping.
///    - Vertical Billboard mode ensures needles always face the camera with full width and HDR bloom.
///    - Synchronized 4-Phase Eruption Lifecycle (No "music equalizer" beat):
///      * Phase 1 (Emergence): High-speed thrust piercing up out of the floor.
///      * Phase 2 (Hold): Rock-solid dignified hold at full height.
///      * Phase 3 (Blink): Celestial double-blink flash & HDR bloom pulse.
///      * Phase 4 (Plunge): Swift retraction back down into the earth.
/// 2. Multi-Tier Solar Orbit Layering:
///    - Needles scatter across 3 concentric solar orbit layers:
///      * Outer Orbit (Tier 3): Dense boundary perimeter circle (0.9 - 1.8m spikes).
///      * Mid Orbit (Tier 2): Intermediate solar corona halo (0.7 - 1.4m spikes).
///      * Inner Orbit (Tier 1): Sacred inner crown closely encircling King Arthur (0.5 - 1.0m spikes).
///    - Outward shockwave wave ripple (0.00s inner -> 0.03s mid -> 0.06s outer).
/// 3. Ascending Sparkles:
///    - Gentle, subtle golden motes drifting upward inside the arena.
/// 4. Live Inspector Preview:
///    - Deferred preview simulation via EditorApplication.delayCall keeps needles live and visible
///      without disappearing when adjusting settings, with zero SendMessage/Prefab errors.
/// 5. Clean, Pure VFX:
///    - Flat needles and ground rings completely removed as requested.
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

    public enum NeedleRenderStyle
    {
        Mesh3D,
        VerticalBillboard
    }

    public enum NeedleMotionStyle
    {
        MusicBarEqualizer,
        SacredEruption
    }

    [Header("=== Color Theme & Bloom ===")]
    [Tooltip("Color Theme: Golden Holy (Reference) or Pink Rose")]
    public KingdomClaimColorTheme colorTheme = KingdomClaimColorTheme.GoldenHoly;

    [Range(0.5f, 10f), Tooltip("HDR Bloom intensity multiplier")]
    public float bloomIntensity = 3.8f;

    [Header("=== Solar Orbit Arena Settings ===")]
    [Tooltip("Radius of the outermost solar orbit in meters (increase to expand wider around the character)")]
    [Range(1.0f, 10.0f)]
    public float arenaRadius = 3.2f;

    [Range(1, 3), Tooltip("Number of concentric sun orbit layers (1 to 3)")]
    public int orbitLayers = 3;

    [Tooltip("Total cycle duration in seconds (Erupt -> Hold -> Blink -> Retract -> Rest)")]
    [Range(1.5f, 6.0f)]
    public float cycleDuration = 3.0f;

    [Tooltip("Whether the taunt effect loops periodically")]
    public bool loop = true;

    [Tooltip("Vertical height offset above ground (in meters) to position needle bases")]
    public float needleGroundOffset = 0.0f;

    [Header("=== Needle Geometry & Tilt ===")]
    [Tooltip("Needle tilt angle in degrees (-60° to +60°). Positive (+1° to +60°) = radiant outward tilt away from center. 0° = straight UP (90° vertical). Negative (-1° to -60°) = defensive inward tilt leaning towards center.")]
    [Range(-60f, 60f)]
    public float needleTiltAngle = 20f;

    [Tooltip("Whether the thin pointed tip is at the top (True = sharp tip at sky, thick base at ground)")]
    public bool thinTipAtTop = true;

    [Tooltip("Render style: Mesh3D (true 3D cross-quad spikes with radial tilt) or VerticalBillboard (2D camera-facing perpendicular to ground)")]
    public NeedleRenderStyle needleRenderStyle = NeedleRenderStyle.Mesh3D;

    [Header("=== Orbit Layer Counter-Rotation ===")]
    [Tooltip("Enable continuous counter-rotation of the concentric needle circles")]
    public bool enableOrbitRotation = true;

    [Tooltip("Global rotation speed multiplier. Increase to supercharge or maximize spinning speed!")]
    public float rotationSpeedMultiplier = 1.0f;

    [Tooltip("Outer orbit rotation speed in degrees per second (positive = clockwise, negative = counter-clockwise)")]
    public float outerRotationSpeed = 15f;

    [Tooltip("Mid orbit rotation speed in degrees per second (positive = clockwise, negative = counter-clockwise)")]
    public float midRotationSpeed = -22f;

    [Tooltip("Inner orbit rotation speed in degrees per second (positive = clockwise, negative = counter-clockwise)")]
    public float innerRotationSpeed = 30f;

    [Header("=== Needle Motion & Animation Style ===")]
    [Tooltip("Continuous running mode: needles never disappear, constantly pulsating to beats and rotating seamlessly without any dead duration or vanishing.")]
    public bool continuousRunning = true;

    [Tooltip("Animation style:\n• MusicBarEqualizer: Down-to-top audio visualizer bouncing up and down to rhythmic beats.\n• SacredEruption: Emerge, hold solid at full height with divine blink pulses, plunge down.")]
    public NeedleMotionStyle motionStyle = NeedleMotionStyle.MusicBarEqualizer;

    [Tooltip("Rhythmic beat / bounce pulses per cycle in MusicBarEqualizer mode")]
    [Range(2, 16)]
    public int musicBarBeats = 6;

    [Tooltip("Lowest dip ratio between beats (e.g. 0.12 = dips to 12% height before next beat punch; 0 = dips all the way to floor)")]
    [Range(0f, 0.45f)]
    public float musicBarMinDip = 0.12f;

    [Tooltip("Bounce punchiness / attack sharpness for music beats (higher = snappier drum beat punch)")]
    [Range(1f, 4f)]
    public float musicBarPunchiness = 2.0f;

    [Header("=== Up Needle VFX (Vertical / Radiant Spikes) ===")]
    public bool enableUpNeedles = true;
    [Tooltip("Total upward needles distributed across orbit layers")]
    [Range(16, 120)]
    public int upNeedleCount = 60;
    [Tooltip("Min and Max height of vertical needles on the outer orbit (in meters)")]
    public Vector2 upNeedleHeightRange = new Vector2(0.9f, 1.8f);
    [Tooltip("Width / thickness of vertical needles")]
    public float upNeedleWidth = 0.08f;
    [ColorUsage(true, true)] public Color upNeedleColor = new Color(1.0f, 0.88f, 0.45f, 1.0f);

    [Header("=== Ascending Sparkles ===")]
    public bool enableAscendingSparks = true;
    [Range(4, 50), Tooltip("Subtle sparkle emission rate")]
    public int sparkRate = 12;
    [Tooltip("Min and Max base size of ascending sparkle particles (in meters)")]
    public Vector2 sparkSize = new Vector2(0.06f, 0.14f);
    [Range(0.1f, 5.0f), Tooltip("Sparkle particle size multiplier to easily scale sparkles larger or smaller")]
    public float sparkSizeMultiplier = 1.0f;
    [ColorUsage(true, true)] public Color sparkColor = new Color(1.0f, 0.95f, 0.70f, 1.0f);

    [Header("=== Component References (Auto-Created) ===")]
    public ParticleSystem upNeedlesPS;
    public ParticleSystem upNeedlesMidPS;
    public ParticleSystem upNeedlesInnerPS;
    public ParticleSystem sparksPS;

    [Header("=== Materials & Meshes (Auto-Loaded) ===")]
    public Material upNeedlesMaterial;
    public Material sparklesMaterial;
    public Mesh spikeMesh;

    private const string UP_MAT_PATH = "Assets/VFX/King Arthur_VFX/Materials/KingArthur_Taunt_UpNeedles_Mat.mat";
    private const string SPARKS_MAT_PATH = "Assets/VFX/King Arthur_VFX/Materials/KingArthur_Defense_Sparkles_Mat.mat";
    private const string SPIKE_MESH_PATH = "Assets/VFX/King Arthur_VFX/Meshes/Needle_Spike_Mesh.obj";

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
#if UNITY_EDITOR
        lastEditorTime = UnityEditor.EditorApplication.timeSinceStartup;
        UnityEditor.EditorApplication.update -= EditorUpdate;
        UnityEditor.EditorApplication.update += EditorUpdate;
#endif
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorUpdate;
#endif
    }

    private void Start()
    {
        InitializeVFX();
        Play();
    }

    private void Update()
    {
        if (enableOrbitRotation && Application.isPlaying)
        {
            RotateOrbitLayers(Time.deltaTime);
        }
    }

    public void RotateOrbitLayers(float dt)
    {
        if (dt <= 0f) return;
        float mult = rotationSpeedMultiplier;

        // 1. Outer Orbit (1_Up_Needles) - rotated in Space.World, strictly locking Z to 180
        if (upNeedlesPS != null)
        {
            upNeedlesPS.transform.Rotate(0f, outerRotationSpeed * mult * dt, 0f, Space.World);
            Vector3 euler = upNeedlesPS.transform.localEulerAngles;
            euler.x = 0f;
            euler.z = 180f; // strictly locked to 180 on Z axis as requested
            upNeedlesPS.transform.localEulerAngles = euler;
        }

        // 2. Mid Orbit (Up_Needles_Mid)
        if (upNeedlesMidPS != null && upNeedlesMidPS.gameObject.activeInHierarchy)
        {
            float midSpeed = (upNeedlesPS != null && upNeedlesMidPS.transform.parent == upNeedlesPS.transform)
                ? (midRotationSpeed - outerRotationSpeed)
                : midRotationSpeed;
            upNeedlesMidPS.transform.Rotate(0f, midSpeed * mult * dt, 0f, Space.World);
        }

        // 3. Inner Orbit (Up_Needles_Inner)
        if (upNeedlesInnerPS != null && upNeedlesInnerPS.gameObject.activeInHierarchy)
        {
            float innerSpeed = (upNeedlesPS != null && upNeedlesInnerPS.transform.parent == upNeedlesPS.transform)
                ? (innerRotationSpeed - outerRotationSpeed)
                : innerRotationSpeed;
            upNeedlesInnerPS.transform.Rotate(0f, innerSpeed * mult * dt, 0f, Space.World);
        }
    }

#if UNITY_EDITOR
    private double lastEditorTime;
    private void EditorUpdate()
    {
        if (!Application.isPlaying && enableOrbitRotation && this != null && gameObject != null && gameObject.scene.IsValid())
        {
            double now = UnityEditor.EditorApplication.timeSinceStartup;
            float dt = (float)(now - lastEditorTime);
            lastEditorTime = now;
            if (dt > 0.0001f && dt < 0.1f)
            {
                RotateOrbitLayers(dt);
            }
        }
    }
#endif

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!gameObject.scene.IsValid() || UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;

        if (colorTheme != lastTheme)
        {
            lastTheme = colorTheme;
            if (colorTheme == KingdomClaimColorTheme.PinkRose)
            {
                upNeedleColor = new Color(1.0f, 0.40f, 0.85f, 1.0f);
                sparkColor = new Color(1.0f, 0.70f, 0.95f, 1.0f);
            }
            else if (colorTheme == KingdomClaimColorTheme.GoldenHoly)
            {
                upNeedleColor = new Color(1.0f, 0.88f, 0.45f, 1.0f);
                sparkColor = new Color(1.0f, 0.95f, 0.70f, 1.0f);
            }
        }

        spikeMesh = null;
        ApplySettings();
    }
#endif

    [ContextMenu("Set Theme: Golden")]
    public void SetThemeGolden()
    {
        colorTheme = KingdomClaimColorTheme.GoldenHoly;
        lastTheme = colorTheme;
        upNeedleColor = new Color(1.0f, 0.88f, 0.45f, 1.0f);
        sparkColor = new Color(1.0f, 0.95f, 0.70f, 1.0f);
        ApplySettings();
    }

    [ContextMenu("Set Theme: Pink")]
    public void SetThemePink()
    {
        colorTheme = KingdomClaimColorTheme.PinkRose;
        lastTheme = colorTheme;
        upNeedleColor = new Color(1.0f, 0.40f, 0.85f, 1.0f);
        sparkColor = new Color(1.0f, 0.70f, 0.95f, 1.0f);
        ApplySettings();
    }

    public void InitializeVFX()
    {
        // Deactivate legacy flat needles if present
        Transform oldFlat = transform.Find("2_Flat_Needles");
        if (oldFlat != null)
        {
            if (Application.isPlaying) Destroy(oldFlat.gameObject);
            else oldFlat.gameObject.SetActive(false);
        }

        // Deactivate legacy ground ring if leftover
        Transform oldRing = transform.Find("3_Ground_Arena_Ring");
        if (oldRing != null)
        {
            if (Application.isPlaying) Destroy(oldRing.gameObject);
            else oldRing.gameObject.SetActive(false);
        }

        LoadDefaultMaterials();
        ResolveReferences();
        ApplySettings();
    }

    private void LoadDefaultMaterials()
    {
#if UNITY_EDITOR
        if (upNeedlesMaterial == null || upNeedlesMaterial.shader == null || upNeedlesMaterial.shader.name == "Hidden/InternalErrorShader")
            upNeedlesMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UP_MAT_PATH);
        if (sparklesMaterial == null || sparklesMaterial.shader == null || sparklesMaterial.shader.name == "Hidden/InternalErrorShader")
            sparklesMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(SPARKS_MAT_PATH);
        if (spikeMesh == null)
            spikeMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(SPIKE_MESH_PATH);
#endif
    }

    public Mesh GetOrCreateSpikeMesh()
    {
        string expectedName = thinTipAtTop ? "Needle_Spike_TipAtTop" : "Needle_Spike_TipAtBottom";
        if (spikeMesh != null && spikeMesh.name == expectedName)
            return spikeMesh;

        // Build guaranteed perfectly oriented cross-quad spike mesh:
        // Base at Z=0 (thick, V=1), Tip at Z=1 (sharp apex point, V=0)
        Mesh mesh = new Mesh();
        mesh.name = expectedName;

        float baseWidth = 0.5f;
        float tipWidth = 0.03f; // physically tapered apex tip

        float vBase = thinTipAtTop ? 1f : 0f; // V=1 is thick base in Unity DirectX texture sampling
        float vTip = thinTipAtTop ? 0f : 1f;  // V=0 is thin pointed tip in Unity DirectX texture sampling

        Vector3[] vertices = new Vector3[]
        {
            // Quad 1: X-Z plane (width along X, height along Z)
            new Vector3(-baseWidth, 0f, 0f),     // 0: Base left
            new Vector3( baseWidth, 0f, 0f),     // 1: Base right
            new Vector3( tipWidth,  0f, 1f),     // 2: Tip right (tapered apex)
            new Vector3(-tipWidth,  0f, 1f),     // 3: Tip left (tapered apex)

            // Quad 2: Y-Z plane (thickness along Y, height along Z)
            new Vector3(0f, -baseWidth, 0f),     // 4: Base back
            new Vector3(0f,  baseWidth, 0f),     // 5: Base front
            new Vector3(0f,  tipWidth,  1f),     // 6: Tip front (tapered apex)
            new Vector3(0f, -tipWidth,  1f)      // 7: Tip back (tapered apex)
        };

        Vector2[] uvs = new Vector2[]
        {
            // Quad 1
            new Vector2(0f, vBase),
            new Vector2(1f, vBase),
            new Vector2(1f, vTip),
            new Vector2(0f, vTip),

            // Quad 2
            new Vector2(0f, vBase),
            new Vector2(1f, vBase),
            new Vector2(1f, vTip),
            new Vector2(0f, vTip)
        };

        int[] triangles = new int[]
        {
            // Quad 1 front & back
            0, 1, 2,  0, 2, 3,
            0, 2, 1,  0, 3, 2,
            // Quad 2 front & back
            4, 5, 6,  4, 6, 7,
            4, 6, 5,  4, 7, 6
        };

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        spikeMesh = mesh;
        return mesh;
    }

    [ContextMenu("Toggle Tip Orientation")]
    public void ToggleTipOrientation()
    {
        thinTipAtTop = !thinTipAtTop;
        spikeMesh = null;
        ApplySettings();
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

    public void ResolveReferences()
    {
        // 1. Outer Orbit (1_Up_Needles)
        if (upNeedlesPS == null)
        {
            Transform outerChild = transform.Find("1_Up_Needles");
            if (outerChild != null) upNeedlesPS = outerChild.GetComponent<ParticleSystem>();
        }

        // 2. Mid Orbit (Up_Needles_Mid) - can be child of 1_Up_Needles or direct child of VFX root
        if (upNeedlesMidPS == null)
        {
            Transform midChild = (upNeedlesPS != null) ? upNeedlesPS.transform.Find("Up_Needles_Mid") : null;
            if (midChild == null) midChild = transform.Find("1_Up_Needles/Up_Needles_Mid");
            if (midChild == null) midChild = transform.Find("Up_Needles_Mid");
            if (midChild != null) upNeedlesMidPS = midChild.GetComponent<ParticleSystem>();
        }

        // 3. Inner Orbit (Up_Needles_Inner) - can be child of 1_Up_Needles or direct child of VFX root
        if (upNeedlesInnerPS == null)
        {
            Transform innerChild = (upNeedlesPS != null) ? upNeedlesPS.transform.Find("Up_Needles_Inner") : null;
            if (innerChild == null) innerChild = transform.Find("1_Up_Needles/Up_Needles_Inner");
            if (innerChild == null) innerChild = transform.Find("Up_Needles_Inner");
            if (innerChild != null) upNeedlesInnerPS = innerChild.GetComponent<ParticleSystem>();
        }

        // 4. Ascending Sparks (3_Ascending_Sparks)
        if (sparksPS == null)
        {
            Transform sparksChild = transform.Find("3_Ascending_Sparks");
            if (sparksChild != null) sparksPS = sparksChild.GetComponent<ParticleSystem>();
        }

        // Fallback: search all child ParticleSystems by name
        if (upNeedlesPS == null || upNeedlesMidPS == null || upNeedlesInnerPS == null || sparksPS == null)
        {
            ParticleSystem[] allPS = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < allPS.Length; i++)
            {
                if (allPS[i] == null) continue;
                string pName = allPS[i].name;
                if (upNeedlesPS == null && pName == "1_Up_Needles") upNeedlesPS = allPS[i];
                else if (upNeedlesMidPS == null && pName == "Up_Needles_Mid") upNeedlesMidPS = allPS[i];
                else if (upNeedlesInnerPS == null && pName == "Up_Needles_Inner") upNeedlesInnerPS = allPS[i];
                else if (sparksPS == null && pName == "3_Ascending_Sparks") sparksPS = allPS[i];
            }
        }
    }

    [ContextMenu("Rebuild Hierarchy (Safe)")]
    public void RebuildHierarchySafe()
    {
#if UNITY_EDITOR
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
#endif
        ResolveReferences();

        if (upNeedlesPS == null)
        {
            Transform outerChild = transform.Find("1_Up_Needles");
            if (outerChild == null)
            {
                GameObject go = new GameObject("1_Up_Needles");
                go.transform.SetParent(transform, false);
                outerChild = go.transform;
            }
            upNeedlesPS = outerChild.GetComponent<ParticleSystem>();
            if (upNeedlesPS == null) upNeedlesPS = outerChild.gameObject.AddComponent<ParticleSystem>();
        }

        Transform midParent = (upNeedlesPS != null) ? upNeedlesPS.transform : transform;
        if (upNeedlesMidPS == null)
        {
            Transform midChild = midParent.Find("Up_Needles_Mid");
            if (midChild == null) midChild = transform.Find("Up_Needles_Mid");
            if (midChild == null)
            {
                GameObject go = new GameObject("Up_Needles_Mid");
                go.transform.SetParent(midParent, false);
                midChild = go.transform;
            }
            upNeedlesMidPS = midChild.GetComponent<ParticleSystem>();
            if (upNeedlesMidPS == null) upNeedlesMidPS = midChild.gameObject.AddComponent<ParticleSystem>();
        }

        Transform innerParent = (upNeedlesPS != null) ? upNeedlesPS.transform : transform;
        if (upNeedlesInnerPS == null)
        {
            Transform innerChild = innerParent.Find("Up_Needles_Inner");
            if (innerChild == null) innerChild = transform.Find("Up_Needles_Inner");
            if (innerChild == null)
            {
                GameObject go = new GameObject("Up_Needles_Inner");
                go.transform.SetParent(innerParent, false);
                innerChild = go.transform;
            }
            upNeedlesInnerPS = innerChild.GetComponent<ParticleSystem>();
            if (upNeedlesInnerPS == null) upNeedlesInnerPS = innerChild.gameObject.AddComponent<ParticleSystem>();
        }

        if (sparksPS == null)
        {
            Transform sparksChild = transform.Find("3_Ascending_Sparks");
            if (sparksChild == null)
            {
                GameObject go = new GameObject("3_Ascending_Sparks");
                go.transform.SetParent(transform, false);
                sparksChild = go.transform;
            }
            sparksPS = sparksChild.GetComponent<ParticleSystem>();
            if (sparksPS == null) sparksPS = sparksChild.gameObject.AddComponent<ParticleSystem>();
        }

        ApplySettings();
    }

    [ContextMenu("Apply Settings")]
    public void ApplySettings()
    {
#if UNITY_EDITOR
        if (!gameObject.scene.IsValid() || UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
#endif
        LoadDefaultMaterials();
        ResolveReferences();

        ConfigureUpNeedles();
        if (sparksPS != null) ConfigureSparks(sparksPS);

        if (Application.isPlaying)
        {
            Play();
        }
#if UNITY_EDITOR
        else
        {
            SchedulePreviewSimulation();
        }
#endif
    }

#if UNITY_EDITOR
    private void SchedulePreviewSimulation()
    {
        if (Application.isPlaying) return;
        if (!gameObject.scene.IsValid() || UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;

        UnityEditor.EditorApplication.delayCall -= DoSimulatePreview;
        UnityEditor.EditorApplication.delayCall += DoSimulatePreview;
    }

    private void DoSimulatePreview()
    {
        if (this == null || gameObject == null || Application.isPlaying) return;
        if (!gameObject.scene.IsValid() || UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;

        float previewTime = Mathf.Clamp(cycleDuration * 0.35f, 0.4f, 1.2f);
        if (upNeedlesPS != null && upNeedlesPS.gameObject.activeInHierarchy)
        {
            upNeedlesPS.Simulate(previewTime, true, true, true);
        }
        if (upNeedlesMidPS != null && upNeedlesMidPS.gameObject.activeInHierarchy)
        {
            upNeedlesMidPS.Simulate(previewTime, true, true, true);
        }
        if (upNeedlesInnerPS != null && upNeedlesInnerPS.gameObject.activeInHierarchy)
        {
            upNeedlesInnerPS.Simulate(previewTime, true, true, true);
        }
        if (sparksPS != null && sparksPS.gameObject.activeInHierarchy)
        {
            sparksPS.Simulate(previewTime, true, true, true);
        }
    }
#endif

    private AnimationCurve CreateNeedleEruptionSizeCurve()
    {
        // 4-Phase Lifecycle:
        // Phase 1 (0.00 -> 0.10): Rapid explosive eruption up out of the floor
        // Phase 2 (0.10 -> 0.68): Regal, rock-solid hold at full height (no music equalizer bounce!)
        // Phase 3 (0.68 -> 0.85): Divine celestial double-blink pulse
        // Phase 4 (0.85 -> 1.00): Swift plunge down back into the floor
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(new Keyframe(0.00f, 0.00f, 0f, 16.0f));
        sc.AddKey(new Keyframe(0.10f, 1.12f, 0f, 0f));     // Impact overshoot
        sc.AddKey(new Keyframe(0.16f, 1.00f, 0f, 0f));     // Solid height
        sc.AddKey(new Keyframe(0.68f, 1.00f, 0f, 0f));     // Rock-solid hold
        sc.AddKey(new Keyframe(0.73f, 1.10f, 0f, 0f));     // Blink 1 pulse
        sc.AddKey(new Keyframe(0.77f, 0.94f, 0f, 0f));     // Blink 1 dip
        sc.AddKey(new Keyframe(0.81f, 1.08f, 0f, 0f));     // Blink 2 pulse
        sc.AddKey(new Keyframe(0.85f, 1.00f, 0f, 0f));     // Pre-retract settle
        sc.AddKey(new Keyframe(1.00f, 0.00f, -12.0f, 0f)); // Plunge down into earth
        return sc;
    }

    private AnimationCurve CreateMusicBarEqualizerCurve(int tier)
    {
        // Down-to-top audio visualizer bars pulsating and bouncing up and down to rhythmic beats
        AnimationCurve curve = new AnimationCurve();
        int beats = Mathf.Clamp(musicBarBeats, 2, 16);
        float minDip = Mathf.Clamp(musicBarMinDip, 0.0f, 0.45f);

        // Musical dynamic peak energies for each tier
        float[] energyWeights;
        if (tier == 3) // Outer (Sub-bass / Kick drum punches)
            energyWeights = new float[] { 1.15f, 0.78f, 1.25f, 0.82f, 1.18f, 0.75f, 1.28f, 0.85f };
        else if (tier == 2) // Mid (Syncopated chord / melody pulses)
            energyWeights = new float[] { 0.82f, 1.18f, 0.88f, 1.22f, 0.85f, 1.15f, 0.90f, 1.25f };
        else // Inner (High-frequency arpeggio & crown beats)
            energyWeights = new float[] { 1.05f, 1.20f, 0.95f, 1.12f, 1.25f, 0.92f, 1.15f, 1.08f };

        float dt = 1.0f / beats;

        if (continuousRunning)
        {
            // Continuous running: Never plunge to 0; loop seamlessly at minDip
            curve.AddKey(new Keyframe(0.00f, minDip, 0f, 0f));

            for (int i = 0; i < beats; i++)
            {
                float tStart = i * dt;
                float peakEnergy = energyWeights[i % energyWeights.Length];
                float tPeak = tStart + dt * 0.40f;
                float tEnd = (i + 1) * dt;

                curve.AddKey(new Keyframe(tPeak, peakEnergy, 0f, 0f));

                if (i < beats - 1)
                {
                    float dipVal = Mathf.Lerp(minDip, minDip * 1.35f, (i % 2 == 0) ? 0f : 1f);
                    curve.AddKey(new Keyframe(tEnd, dipVal, 0f, 0f));
                }
            }

            curve.AddKey(new Keyframe(1.00f, minDip, 0f, 0f));

            for (int k = 0; k < curve.length; k++)
            {
                curve.SmoothTangents(k, 0f);
            }

            // Ensure start and end tangents are perfectly level for seamless looping
            Keyframe firstKey = curve[0];
            firstKey.inTangent = 0f;
            firstKey.outTangent = 0f;
            curve.MoveKey(0, firstKey);

            Keyframe lastKey = curve[curve.length - 1];
            lastKey.inTangent = 0f;
            lastKey.outTangent = 0f;
            curve.MoveKey(curve.length - 1, lastKey);
        }
        else
        {
            // Starts down on the floor (Y = 0)
            curve.AddKey(new Keyframe(0.00f, 0.00f, 0f, 10f * musicBarPunchiness));

            for (int i = 0; i < beats; i++)
            {
                float tStart = i * dt;
                float peakEnergy = energyWeights[i % energyWeights.Length];
                float tPeak = tStart + dt * 0.38f;
                float tDip = tStart + dt * 0.85f;

                curve.AddKey(new Keyframe(tPeak, peakEnergy, 0f, 0f));

                if (i < beats - 1)
                {
                    float dipVal = Mathf.Lerp(minDip, minDip * 1.4f, (i % 2 == 0) ? 0f : 1f);
                    curve.AddKey(new Keyframe(tDip, dipVal, 0f, 0f));
                }
            }

            // Final plunge down to earth at cycle end so loop resets clean
            curve.AddKey(new Keyframe(1.00f, 0.00f, -8f * musicBarPunchiness, 0f));

            for (int k = 0; k < curve.length; k++)
            {
                curve.SmoothTangents(k, 0f);
            }
        }

        return curve;
    }

    private Gradient CreateNeedleColorGradient(Color baseColor)
    {
        Gradient grad = new Gradient();
        GradientAlphaKey[] aKeys = new GradientAlphaKey[]
        {
            new GradientAlphaKey(0.0f, 0.00f),
            new GradientAlphaKey(1.0f, 0.06f),
            new GradientAlphaKey(1.0f, 0.68f),
            new GradientAlphaKey(1.0f, 0.73f),
            new GradientAlphaKey(0.35f, 0.77f), // Blink dip
            new GradientAlphaKey(1.0f, 0.81f),  // Blink on
            new GradientAlphaKey(1.0f, 0.85f),
            new GradientAlphaKey(0.0f, 1.00f)   // Fade into ground
        };

        GradientColorKey[] cKeys = new GradientColorKey[]
        {
            new GradientColorKey(Color.white, 0.00f), // Flash on emergence
            new GradientColorKey(baseColor, 0.10f),
            new GradientColorKey(baseColor, 0.68f),
            new GradientColorKey(Color.white, 0.73f), // Blink flash 1
            new GradientColorKey(baseColor, 0.77f),
            new GradientColorKey(Color.white, 0.81f), // Blink flash 2
            new GradientColorKey(baseColor, 0.85f),
            new GradientColorKey(baseColor, 1.00f)
        };

        grad.SetKeys(cKeys, aKeys);
        return grad;
    }

    private Gradient CreateMusicBarColorGradient(Color baseColor)
    {
        Gradient grad = new Gradient();
        GradientAlphaKey[] aKeys;

        if (continuousRunning)
        {
            // Continuous running: 100% visible throughout cycle, no disappearing
            aKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1.0f, 0.00f),
                new GradientAlphaKey(1.0f, 1.00f)
            };
        }
        else
        {
            aKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.0f, 0.00f),
                new GradientAlphaKey(1.0f, 0.03f),
                new GradientAlphaKey(1.0f, 0.95f),
                new GradientAlphaKey(0.0f, 1.00f)
            };
        }

        GradientColorKey[] cKeys = new GradientColorKey[]
        {
            new GradientColorKey(baseColor, 0.00f),
            new GradientColorKey(Color.white, 0.16f), // Beat 1 highlight flash
            new GradientColorKey(baseColor, 0.33f),
            new GradientColorKey(Color.white, 0.50f), // Beat 2 highlight flash
            new GradientColorKey(baseColor, 0.67f),
            new GradientColorKey(Color.white, 0.83f), // Beat 3 highlight flash
            new GradientColorKey(baseColor, 1.00f)
        };

        grad.SetKeys(cKeys, aKeys);
        return grad;
    }

    private void ConfigureUpNeedles()
    {
        if (upNeedlesPS == null) return;
        upNeedlesPS.gameObject.SetActive(enableUpNeedles);
        if (!enableUpNeedles) return;

        Color hdrUp = new Color(
            upNeedleColor.r * bloomIntensity,
            upNeedleColor.g * bloomIntensity,
            upNeedleColor.b * bloomIntensity,
            upNeedleColor.a
        );
        Material mat = GetOrLoadMaterial(ref upNeedlesMaterial, UP_MAT_PATH, "Light_Needle_Spike.png", upNeedleColor, hdrUp);

        upNeedlesPS.transform.localPosition = new Vector3(0f, needleGroundOffset, 0f);
        upNeedlesPS.transform.localRotation = Quaternion.Euler(0f, upNeedlesPS.transform.localEulerAngles.y, 180f);
        upNeedlesPS.transform.localScale = Vector3.one;

        float activeDuration = continuousRunning ? cycleDuration : Mathf.Max(1.0f, cycleDuration * 0.72f);
        float delayOuter = continuousRunning ? 0f : 0.06f;
        float delayMid = continuousRunning ? 0f : 0.03f;
        float delayInner = 0f;

        AnimationCurve sizeCurveOuter, sizeCurveMid, sizeCurveInner;
        Gradient colorGrad;

        if (motionStyle == NeedleMotionStyle.MusicBarEqualizer)
        {
            sizeCurveOuter = CreateMusicBarEqualizerCurve(3);
            sizeCurveMid = CreateMusicBarEqualizerCurve(2);
            sizeCurveInner = CreateMusicBarEqualizerCurve(1);
            colorGrad = CreateMusicBarColorGradient(upNeedleColor);
        }
        else
        {
            sizeCurveOuter = CreateNeedleEruptionSizeCurve();
            sizeCurveMid = sizeCurveOuter;
            sizeCurveInner = sizeCurveOuter;
            colorGrad = CreateNeedleColorGradient(upNeedleColor);
        }

        // Distribute count across concentric circles:
        // Outer perimeter gets the dominant share to form a dense, gap-free sacred circle!
        int countOuter, countMid, countInner;
        if (orbitLayers == 1 || upNeedlesMidPS == null)
        {
            countOuter = upNeedleCount;
            countMid = 0;
            countInner = 0;
        }
        else if (orbitLayers == 2 || upNeedlesInnerPS == null)
        {
            countOuter = Mathf.RoundToInt(upNeedleCount * 0.70f);
            countMid = Mathf.Max(4, upNeedleCount - countOuter);
            countInner = 0;
        }
        else // 3 layers
        {
            countOuter = Mathf.RoundToInt(upNeedleCount * 0.60f);
            countMid = Mathf.RoundToInt(upNeedleCount * 0.26f);
            countInner = Mathf.Max(4, upNeedleCount - countOuter - countMid);
        }

        // Tier 3: Outermost Perimeter Orbit (Dense, evenly distributed circle without gaps)
        ConfigureSingleUpNeedlePS(
            upNeedlesPS,
            radius: arenaRadius,
            radiusScatter: 0.10f,
            minH: upNeedleHeightRange.x,
            maxH: upNeedleHeightRange.y,
            count: countOuter,
            startDelay: delayOuter,
            activeDuration: activeDuration,
            sizeCurve: sizeCurveOuter,
            colorGrad: colorGrad,
            mat: mat,
            hdrCol: hdrUp,
            tiltAngle: needleTiltAngle
        );

        // Tier 2: Mid Solar Halo Orbit
        if (upNeedlesMidPS != null)
        {
            bool midActive = orbitLayers >= 2;
            upNeedlesMidPS.gameObject.SetActive(midActive);
            if (midActive)
            {
                upNeedlesMidPS.transform.localPosition = new Vector3(0f, needleGroundOffset, 0f);
                upNeedlesMidPS.transform.localRotation = Quaternion.identity;
                upNeedlesMidPS.transform.localScale = Vector3.one;
                ConfigureSingleUpNeedlePS(
                    upNeedlesMidPS,
                    radius: arenaRadius * 0.74f,
                    radiusScatter: 0.12f,
                    minH: upNeedleHeightRange.x * 0.80f,
                    maxH: upNeedleHeightRange.y * 0.80f,
                    count: countMid,
                    startDelay: delayMid,
                    activeDuration: activeDuration,
                    sizeCurve: sizeCurveMid,
                    colorGrad: colorGrad,
                    mat: mat,
                    hdrCol: hdrUp,
                    tiltAngle: needleTiltAngle * 0.82f
                );
            }
        }

        // Tier 1: Inner Solar Crown Orbit
        if (upNeedlesInnerPS != null)
        {
            bool innerActive = orbitLayers >= 3;
            upNeedlesInnerPS.gameObject.SetActive(innerActive);
            if (innerActive)
            {
                upNeedlesInnerPS.transform.localPosition = new Vector3(0f, needleGroundOffset, 0f);
                upNeedlesInnerPS.transform.localRotation = Quaternion.identity;
                upNeedlesInnerPS.transform.localScale = Vector3.one;
                ConfigureSingleUpNeedlePS(
                    upNeedlesInnerPS,
                    radius: arenaRadius * 0.52f,
                    radiusScatter: 0.15f,
                    minH: upNeedleHeightRange.x * 0.60f,
                    maxH: upNeedleHeightRange.y * 0.60f,
                    count: countInner,
                    startDelay: delayInner,
                    activeDuration: activeDuration,
                    sizeCurve: sizeCurveInner,
                    colorGrad: colorGrad,
                    mat: mat,
                    hdrCol: hdrUp,
                    tiltAngle: needleTiltAngle * 0.65f
                );
            }
        }
    }

    private void ConfigureSingleUpNeedlePS(
        ParticleSystem ps,
        float radius,
        float radiusScatter,
        float minH,
        float maxH,
        int count,
        float startDelay,
        float activeDuration,
        AnimationCurve sizeCurve,
        Gradient colorGrad,
        Material mat,
        Color hdrCol,
        float tiltAngle)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        bool useMesh = (needleRenderStyle == NeedleRenderStyle.Mesh3D) || (Mathf.Abs(tiltAngle) > 0.001f);
        float absTilt = Mathf.Clamp(Mathf.Abs(tiltAngle), 0f, 85f);
        bool isInward = tiltAngle < -0.001f;

        var main = ps.main;
        main.duration = cycleDuration;
        main.loop = loop;
        main.startDelay = startDelay;
        main.startLifetime = activeDuration;
        // Non-zero startSpeed gives shape.alignToDirection a valid direction vector
        main.startSpeed = useMesh ? 0.001f : 0f;
        main.startSize3D = true;
        if (useMesh)
        {
            // For cross-quad spike mesh: X is width, Y is thickness, Z is height along spike
            main.startSizeX = upNeedleWidth;
            main.startSizeY = upNeedleWidth;
            main.startSizeZ = new ParticleSystem.MinMaxCurve(minH, maxH);
        }
        else
        {
            // Vertical billboard: X is width, Y is height, Z is 1
            main.startSizeX = upNeedleWidth;
            main.startSizeY = new ParticleSystem.MinMaxCurve(minH, maxH);
            main.startSizeZ = 1f;
        }

        // If inward tilt, rotate around local tangent X by -2 * absTilt so the spike leans inward
        if (useMesh && isInward)
        {
            main.startRotation3D = true;
            main.startRotationX = -2f * absTilt * Mathf.Deg2Rad;
            main.startRotationY = 0f;
            main.startRotationZ = 0f;
        }
        else
        {
            main.startRotation3D = false;
            main.startRotation = 0f;
        }

        main.startColor = hdrCol;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = false;
        main.maxParticles = Mathf.Max(count * 2, 8);

        // Limit velocity so particles remain 100% stationary on the circle perimeter
        var lvol = ps.limitVelocityOverLifetime;
        lvol.enabled = useMesh;
        if (useMesh)
        {
            lvol.limit = 0f;
            lvol.dampen = 1f;
        }

        // Synchronous burst at cycle start
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.enabled = true;
        if (useMesh)
        {
            // Cone shape angled at absTilt radiating UP (+Y)
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = absTilt;
            shape.radius = radius;
            shape.radiusThickness = radiusScatter; // Controlled radial jitter for organic sacred look
            shape.arc = 360f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread;
            shape.arcSpread = 0.02f; // Subtle organic angular variation
            shape.rotation = new Vector3(-90f, 0f, 0f); // Points UP (+Y)
            shape.alignToDirection = true; // Aligns mesh +Z (base->tip) along the cone direction!
            shape.length = 0.01f;
        }
        else
        {
            // Pure Vertical Billboard on ground circle
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = radiusScatter;
            shape.arc = 360f;
            shape.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread;
            shape.arcSpread = 0.02f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            shape.alignToDirection = false;
        }

        // Size over lifetime: Explosive emergence -> solid hold -> double blink -> plunge down
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Color over lifetime: Luminous flash on emergence, hold, blinding blink pulses, snap fade
        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(colorGrad);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            if (useMesh)
            {
                renderer.renderMode = ParticleSystemRenderMode.Mesh;
                renderer.mesh = GetOrCreateSpikeMesh();
                renderer.alignment = ParticleSystemRenderSpace.Local;
                renderer.pivot = Vector3.zero; // Base is at Z=0 in the cross-quad mesh
            }
            else
            {
                renderer.renderMode = ParticleSystemRenderMode.VerticalBillboard;
                renderer.pivot = new Vector3(0f, -0.5f, 0f); // Base at ground (Y=0), tip in +Y
            }

            renderer.sharedMaterial = mat;
            renderer.sharedMaterials = new Material[] { mat };
            if (Application.isPlaying)
            {
                renderer.material = mat;
            }
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
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
        float minSpark = Mathf.Max(0.005f, sparkSize.x * sparkSizeMultiplier);
        float maxSpark = Mathf.Max(minSpark, sparkSize.y * sparkSizeMultiplier);
        main.startSize = new ParticleSystem.MinMaxCurve(minSpark, maxSpark);

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
        main.maxParticles = Mathf.Max(40, Mathf.RoundToInt(sparkRate * 2.5f));

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = sparkRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = arenaRadius * 0.85f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = 0.4f;
        velocity.z = 0f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0f, 0f);
        sc.AddKey(0.2f, 1.1f);
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
        if (upNeedlesPS != null)
        {
            upNeedlesPS.transform.localRotation = Quaternion.Euler(0f, upNeedlesPS.transform.localEulerAngles.y, 180f);
            upNeedlesPS.Play(true);
        }
        if (upNeedlesMidPS != null)
        {
            upNeedlesMidPS.transform.localRotation = Quaternion.identity;
            upNeedlesMidPS.Play(true);
        }
        if (upNeedlesInnerPS != null)
        {
            upNeedlesInnerPS.transform.localRotation = Quaternion.identity;
            upNeedlesInnerPS.Play(true);
        }
        if (sparksPS != null) sparksPS.Play(true);
    }

    [ContextMenu("Stop Taunt")]
    public void Stop()
    {
        if (upNeedlesPS != null) upNeedlesPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (upNeedlesMidPS != null) upNeedlesMidPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (upNeedlesInnerPS != null) upNeedlesInnerPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (sparksPS != null) sparksPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
