using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AstridBouquetStage1Preparation - Stage 1: Bouquet Preparation.
/// Storyboard Ref: Astrid approaches target and prepares a large bouquet.
/// VFX Scenario 1:
/// - Petal particle system emitting petals slowly everywhere around character.
/// - Petals tumble and rotate on their own as if carried by a natural breeze.
/// - Born at a specific configurable size, drifting on wind effects.
/// - Subtle gravity pulls them gently downward as they drift and dissolve.
/// 
/// Real-time Features:
/// - Instant play/replay on Enable/Disable (toggle GameObject in Hierarchy).
/// - Live updates in Scene View on Inspector property changes (OnValidate + delayCall).
/// - Dedicated Bloom & Emissive Intensity slider.
/// - Zero material leak (uses sharedMaterial & MaterialPropertyBlock).
/// - HideFlags.DontSave prevents duplicate accumulation.
/// </summary>
[ExecuteAlways]
[SelectionBase]
[DisallowMultipleComponent]
public class AstridBouquetStage1Preparation : MonoBehaviour
{
    private const string CONTENT_ROOT_NAME = "STAGE_1_PREPARATION_CONTENT";

    [Header("=== Global Emission & Wind Settings ===")]
    [Tooltip("Rate of petals emitted per second.")]
    [SerializeField, Range(5, 80)] private int petalRate = 22;

    [Tooltip("Specific base size of born petals.")]
    [SerializeField, Range(0.05f, 0.8f)] private float petalSize = 0.22f;

    [Tooltip("Lifetime of drifting petals in seconds.")]
    [SerializeField, Range(1f, 6f)] private float petalLifetime = 3.2f;

    [Tooltip("Gentle downward gravity pulling petals as they drift.")]
    [SerializeField, Range(0f, 0.3f)] private float gravityStrength = 0.05f;

    [Tooltip("Wind drift / turbulence speed.")]
    [SerializeField, Range(0.2f, 3.0f)] private float windSpeed = 0.9f;

    [Tooltip("Wind turbulence noise intensity.")]
    [SerializeField, Range(0f, 1.5f)] private float windTurbulence = 0.45f;

    [Tooltip("Emission volume radius tightly hugging Astrid and bouquet.")]
    [SerializeField, Range(0.1f, 3.0f)] private float emissionRadius = 0.45f;

    [Header("=== Bloom / Emissive Intensity Controls ===")]
    [Tooltip("Global Bloom & Emissive Glow intensity multiplier for the petals.")]
    [SerializeField, Range(0.5f, 15f)] private float bloomIntensity = 3.5f;
    public float BloomIntensity { get => bloomIntensity; set { bloomIntensity = value; RefreshLiveView(); } }

    [Header("=== Color Palette ===")]
    [Tooltip("Bright Cherry Blossom Petal Pink.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color primaryPink = new Color(1.0f, 0.42f, 0.66f, 1.0f);

    [Tooltip("Soft White / Sakura Pale Pink.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color softWhitePink = new Color(1.0f, 0.92f, 0.96f, 0.95f);

    [Tooltip("Deep Magenta Accent.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color deepMagenta = new Color(0.73f, 0.20f, 0.46f, 1.0f);

    [Header("=== Assigned URP Materials ===")]
    [SerializeField] private Material singlePetalAlphaMat;
    [SerializeField] private Material singlePetalAddMat;
    [SerializeField] private Material softGlowMat;

    private Transform stageRoot;
    private ParticleSystem mainPetalPS;
    private ParticleSystem ambientMotesPS;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        ValidateMaterials();
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        CleanupHierarchy();
    }

    private void OnEnable()
    {
        ValidateMaterials();
        CleanupHierarchy();
        PlayStage();
    }

    private void OnDisable()
    {
        StopStage();
    }

    private void OnDestroy()
    {
        StopStage();
    }

    private void OnApplicationQuit()
    {
        CleanupHierarchy();
    }

    #region Public Playback Controls

    [ContextMenu("Play Stage 1 (Preparation)")]
    public void PlayStage()
    {
        CleanupHierarchy();
        BuildStaticHierarchy();
    }

    [ContextMenu("Stop Stage 1")]
    public void StopStage()
    {
        CleanupHierarchy();
    }

    [ContextMenu("Refresh Live View")]
    public void RefreshLiveView()
    {
        if (stageRoot == null)
        {
            BuildStaticHierarchy();
        }
        else
        {
            UpdateLiveParameters();
        }
    }

    public void BuildStaticHierarchy()
    {
        CleanupHierarchy();
        CreateFreshRoot(CONTENT_ROOT_NAME);

        SpawnWindPetalSystem();
        SpawnAmbientGlowMotes();
        UpdateLiveParameters();
    }

    #endregion

    #region Builders

    private void SpawnWindPetalSystem()
    {
        GameObject go = CreateChild("WindDriftingPetals");
        mainPetalPS = go.AddComponent<ParticleSystem>();

        var main = mainPetalPS.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(petalLifetime * 0.75f, petalLifetime * 1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(windSpeed * 0.4f, windSpeed * 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(petalSize * 0.8f, petalSize * 1.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier = gravityStrength;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        // Dual-color gradient (Bright Pink to Soft White)
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(softWhitePink, 0.0f),
                new GradientColorKey(primaryPink, 0.4f),
                new GradientColorKey(deepMagenta, 1.0f)
            },
            new[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.15f),
                new GradientAlphaKey(0.9f, 0.7f),
                new GradientAlphaKey(0.0f, 1.0f) // Soft dissolve / fade out at end of life
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        // Emission
        var emission = mainPetalPS.emission;
        emission.enabled = true;
        emission.rateOverTime = petalRate;

        // Shape: Sphere/Cylinder around Astrid
        var shape = mainPetalPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = emissionRadius;
        shape.radiusThickness = 0.7f;

        // Wind / Velocity Over Lifetime: Gentle lateral drift
        var velocity = mainPetalPS.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-windSpeed * 0.5f, windSpeed * 0.5f);
        velocity.y = new ParticleSystem.MinMaxCurve(-gravityStrength * 0.5f, 0.2f);
        velocity.z = new ParticleSystem.MinMaxCurve(-windSpeed * 0.3f, windSpeed * 0.7f); // gentle forward-right breeze

        // Rotation Over Lifetime: Tumble and rotate like a real leaf/petal in the wind
        var rot = mainPetalPS.rotationOverLifetime;
        rot.enabled = true;
        rot.separateAxes = true;
        rot.x = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
        rot.y = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
        rot.z = new ParticleSystem.MinMaxCurve(-2.2f, 2.2f);

        // Size Over Lifetime: Born at size, slight flutter, gentle shrink before dissolve
        var sizeLife = mainPetalPS.sizeOverLifetime;
        sizeLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 0.5f);
        sizeCurve.AddKey(0.2f, 1.0f);
        sizeCurve.AddKey(0.8f, 0.95f);
        sizeCurve.AddKey(1.0f, 0.4f);
        sizeLife.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

        // Noise module for organic flutter
        if (windTurbulence > 0.01f)
        {
            var noise = mainPetalPS.noise;
            noise.enabled = true;
            noise.strength = windTurbulence;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.5f;
            noise.damping = true;
            noise.octaveCount = 1;
        }

        // Renderer
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = singlePetalAlphaMat;
        rend.sortMode = ParticleSystemSortMode.OldestInFront;

        mainPetalPS.Play();
    }

    private void SpawnAmbientGlowMotes()
    {
        GameObject go = CreateChild("AmbientPollenMotes");
        ambientMotesPS = go.AddComponent<ParticleSystem>();

        var main = ambientMotesPS.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.10f);
        main.startColor = new ParticleSystem.MinMaxGradient(softWhitePink, primaryPink);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ambientMotesPS.emission;
        emission.enabled = true;
        emission.rateOverTime = petalRate * 0.6f;

        var shape = ambientMotesPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = emissionRadius * 0.9f;

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = softGlowMat;

        ambientMotesPS.Play();
    }

    #endregion

    #region Live Inspector Refresh & Cleanup

    public void UpdateLiveParameters()
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        if (mainPetalPS != null)
        {
            var main = mainPetalPS.main;
            main.startSize = new ParticleSystem.MinMaxCurve(petalSize * 0.8f, petalSize * 1.2f);
            main.gravityModifier = gravityStrength;

            var emission = mainPetalPS.emission;
            emission.rateOverTime = petalRate;

            var shape = mainPetalPS.shape;
            shape.radius = emissionRadius;

            var rend = mainPetalPS.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                rend.SetPropertyBlock(propertyBlock);
            }
        }

        if (ambientMotesPS != null)
        {
            var emission = ambientMotesPS.emission;
            emission.rateOverTime = petalRate * 0.6f;

            var shape = ambientMotesPS.shape;
            shape.radius = emissionRadius * 0.9f;

            var rend = ambientMotesPS.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                rend.SetPropertyBlock(propertyBlock);
            }
        }
    }

    private GameObject CreateChild(string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(stageRoot, false);
        return child;
    }

    private void CreateFreshRoot(string rootName)
    {
        CleanupHierarchy();
        GameObject rootGo = new GameObject(rootName);
        rootGo.hideFlags = HideFlags.DontSave;
        rootGo.transform.SetParent(transform, false);
        stageRoot = rootGo.transform;
    }

    [ContextMenu("Clean Stale Hierarchy")]
    public void CleanupHierarchy()
    {
        if (stageRoot != null)
        {
            SafeDestroy(stageRoot.gameObject);
            stageRoot = null;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null && (child.name == CONTENT_ROOT_NAME || child.name.StartsWith("STAGE_1_PREPARATION")))
            {
                SafeDestroy(child.gameObject);
            }
        }

        mainPetalPS = null;
        ambientMotesPS = null;
    }

    private static void SafeDestroy(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying)
            Destroy(go);
        else
            DestroyImmediate(go);
    }

    private void ValidateMaterials()
    {
#if UNITY_EDITOR
        if (singlePetalAlphaMat == null)
            singlePetalAlphaMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Astrid_VFX/Astrid_Complimentary_Bouquet/Materials/M_Astrid_Petal_Single_Alpha.mat");
        if (singlePetalAddMat == null)
            singlePetalAddMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Astrid_VFX/Astrid_Complimentary_Bouquet/Materials/M_Astrid_Petal_Single_Add.mat");
        if (softGlowMat == null)
            softGlowMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Astrid_VFX/Astrid_Complimentary_Bouquet/Materials/M_Astrid_SoftGlow.mat");
#endif
    }

    private void OnValidate()
    {
        ValidateMaterials();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && gameObject.activeInHierarchy && enabled)
            {
                RefreshLiveView();
            }
        };
#endif
    }

    #endregion
}
