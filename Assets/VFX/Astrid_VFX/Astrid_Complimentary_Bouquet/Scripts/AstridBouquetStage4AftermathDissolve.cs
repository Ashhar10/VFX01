using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AstridBouquetStage4AftermathDissolve - Stage 4: Aftermath & Dissolve.
/// Storyboard Ref: Petals drift and fade across the area as Astrid returns to her stance.
/// VFX Scenario 4:
/// - When Stage 4 enables or plays, it dissolves and disappears the Stage 3 shrapnel explosion.
/// - Spawns gentle, settling aftermath petals drifting down across the area and softly dissolving.
/// - Residual ground ring and energy embers fade out as Astrid recovers her stance.
/// - Functions both as a coordinated controller (dissolving Stage 3) and as an independent aftermath VFX.
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
public class AstridBouquetStage4AftermathDissolve : MonoBehaviour
{
    private const string CONTENT_ROOT_NAME = "STAGE_4_AFTERMATH_DISSOLVE_CONTENT";

    [Header("=== Stage 3 Dissolve Coordination ===")]
    [Tooltip("Optional direct reference to Stage 3. If null, automatically looks for it in the parent/scene.")]
    [SerializeField] private AstridBouquetStage3ShrapnelExplosion targetStage3;

    [Tooltip("Duration in seconds over which Stage 3's shrapnel volume dissolves and disappears.")]
    [SerializeField, Range(0.4f, 3.0f)] private float stage3DissolveDuration = 1.2f;

    [Header("=== Aftermath Settling Petals ===")]
    [Tooltip("Number of soft aftermath petals drifting and settling across the area.")]
    [SerializeField, Range(10, 100)] private int settlingPetalCount = 40;

    [Tooltip("Size of settling petals.")]
    [SerializeField, Range(0.1f, 0.7f)] private float petalSize = 0.24f;

    [Tooltip("Lifetime of aftermath settling petals (seconds).")]
    [SerializeField, Range(1.0f, 6.0f)] private float aftermathDuration = 3.2f;

    [Tooltip("Downward gravity causing petals to gently flutter and settle onto the floor.")]
    [SerializeField, Range(0.02f, 0.25f)] private float gravityStrength = 0.07f;

    [Tooltip("Spread radius of the aftermath area.")]
    [SerializeField, Range(1.5f, 6.0f)] private float areaRadius = 3.5f;

    [Header("=== Bloom / Emissive Intensity Controls ===")]
    [Tooltip("Global Bloom & Emissive Glow intensity multiplier for the aftermath.")]
    [SerializeField, Range(0.5f, 15f)] private float bloomIntensity = 3.0f;
    public float BloomIntensity { get => bloomIntensity; set { bloomIntensity = value; RefreshLiveView(); } }

    [Header("=== Color Palette ===")]
    [Tooltip("Soft Sakura Dissolve Pink.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color aftermathPink = new Color(1.0f, 0.55f, 0.75f, 0.9f);

    [Tooltip("Fading White Petal Edge.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color softWhite = new Color(1.0f, 0.95f, 0.98f, 0.85f);

    [Header("=== Assigned URP Materials ===")]
    [SerializeField] private Material singlePetalAlphaMat;
    [SerializeField] private Material softGlowMat;

    private Transform stageRoot;
    private ParticleSystem settlingPetalsPS;
    private ParticleSystem groundEmbersPS;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        ValidateMaterials();
        FindTargetStage3();
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        CleanupHierarchy();
    }

    private void OnEnable()
    {
        ValidateMaterials();
        FindTargetStage3();
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

    [ContextMenu("Play Stage 4 (Dissolve & Aftermath)")]
    public void PlayStage()
    {
        CleanupHierarchy();

        // 1. Dissolve and disappear Stage 3 if present
        TriggerStage3Dissolve();

        // 2. Spawn and play Stage 4 Aftermath
        BuildStaticHierarchy();
    }

    [ContextMenu("Stop Stage 4")]
    public void StopStage()
    {
        CleanupHierarchy();
    }

    [ContextMenu("Trigger Stage 3 Dissolve Only")]
    public void TriggerStage3Dissolve()
    {
        FindTargetStage3();
        if (targetStage3 != null)
        {
            targetStage3.DissolveAndDisappear(stage3DissolveDuration);
        }
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

        SpawnSettlingPetals();
        SpawnGroundEmbers();
        UpdateLiveParameters();
    }

    #endregion

    #region Builders

    private void SpawnSettlingPetals()
    {
        GameObject go = CreateChild("SettlingAftermathPetals");
        settlingPetalsPS = go.AddComponent<ParticleSystem>();

        var main = settlingPetalsPS.main;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(aftermathDuration * 0.7f, aftermathDuration * 1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(petalSize * 0.75f, petalSize * 1.25f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier = gravityStrength;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(softWhite, 0.0f),
                new GradientColorKey(aftermathPink, 0.4f)
            },
            new[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(0.9f, 0.15f),
                new GradientAlphaKey(0.7f, 0.6f),
                new GradientAlphaKey(0.0f, 1.0f) // Smooth dissolve into floor
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        var emission = settlingPetalsPS.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] {
            new ParticleSystem.Burst(0.0f, (short)settlingPetalCount)
        });

        var shape = settlingPetalsPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(areaRadius * 1.5f, 1.2f, areaRadius * 1.5f);
        shape.position = new Vector3(0f, 1.2f, 0f);

        // Gentle rotational flutter
        var rot = settlingPetalsPS.rotationOverLifetime;
        rot.enabled = true;
        rot.separateAxes = true;
        rot.x = new ParticleSystem.MinMaxCurve(-1.0f, 1.0f);
        rot.y = new ParticleSystem.MinMaxCurve(-1.2f, 1.2f);
        rot.z = new ParticleSystem.MinMaxCurve(-1.8f, 1.8f);

        // Size Over Lifetime: Gentle fade-out
        var sizeLife = settlingPetalsPS.sizeOverLifetime;
        sizeLife.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.8f);
        curve.AddKey(0.3f, 1.0f);
        curve.AddKey(0.85f, 0.85f);
        curve.AddKey(1.0f, 0.3f);
        sizeLife.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = singlePetalAlphaMat;
        rend.sortMode = ParticleSystemSortMode.OldestInFront;

        settlingPetalsPS.Play();
    }

    private void SpawnGroundEmbers()
    {
        GameObject go = CreateChild("GroundDissipatingEmbers");
        groundEmbersPS = go.AddComponent<ParticleSystem>();

        var main = groundEmbersPS.main;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(aftermathDuration * 0.5f, aftermathDuration * 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(softWhite, aftermathPink);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = groundEmbersPS.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] {
            new ParticleSystem.Burst(0.0f, (short)(settlingPetalCount * 0.7f))
        });

        var shape = groundEmbersPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = areaRadius * 0.9f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = softGlowMat;

        groundEmbersPS.Play();
    }

    #endregion

    #region Helper & Cleanup

    private void FindTargetStage3()
    {
        if (targetStage3 != null) return;

        // 1. Search sibling components
        if (transform.parent != null)
        {
            targetStage3 = transform.parent.GetComponentInChildren<AstridBouquetStage3ShrapnelExplosion>(true);
        }

        // 2. Search scene
        if (targetStage3 == null)
        {
            targetStage3 = FindFirstObjectByType<AstridBouquetStage3ShrapnelExplosion>();
        }
    }

    public void UpdateLiveParameters()
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        if (settlingPetalsPS != null)
        {
            var main = settlingPetalsPS.main;
            main.startSize = new ParticleSystem.MinMaxCurve(petalSize * 0.75f, petalSize * 1.25f);
            main.gravityModifier = gravityStrength;

            var rend = settlingPetalsPS.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                rend.SetPropertyBlock(propertyBlock);
            }
        }

        if (groundEmbersPS != null)
        {
            var rend = groundEmbersPS.GetComponent<ParticleSystemRenderer>();
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
            if (child != null && (child.name == CONTENT_ROOT_NAME || child.name.StartsWith("STAGE_4_AFTERMATH")))
            {
                SafeDestroy(child.gameObject);
            }
        }

        settlingPetalsPS = null;
        groundEmbersPS = null;
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
