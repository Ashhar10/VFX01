using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// StanResolutionDeRezVFX - VFX 3: Resolution + De-Rez.
/// Storyboard Ref: The package effect resolves. Stan's body digitizes and de-rez's completely. NO CORPSE REMAINS.
/// 
/// Key Visual Requirements:
/// - STRICTLY NO GLOW PART! (No central glow sphere, no flash quad).
/// - STRICTLY NO GROUND RINGS! (No decals, no ground circles).
/// - PURE MASSIVE VOLUME OF PARTICLE OVERFLOW!
/// - Dense volumetric fountain of rising digital voxels, pixel shards, and cyber rune glyphs erupting from Stan's body.
/// - Particles scatter, jitter organically in the air, and dissipate completely into data dust.
/// - Supports one-shot action playback and continuous loop preview in Scene view.
/// - Zero material leaks (sharedMaterial & MaterialPropertyBlock).
/// </summary>
[ExecuteAlways]
[SelectionBase]
[DisallowMultipleComponent]
public class StanResolutionDeRezVFX : MonoBehaviour
{
    private const string CONTENT_ROOT_NAME = "STAN_RESOLUTION_DEREZ_CONTENT";

    [Header("=== Playback Controls ===")]
    [Tooltip("Keep playing the de-rez particle eruption in a continuous loop for Scene view preview.")]
    [SerializeField] private bool loopPreview = true;

    [Tooltip("Interval in seconds between successive eruptions when Loop Preview is ON.")]
    [SerializeField, Range(1.0f, 5.0f)] private float loopInterval = 2.6f;

    [Header("=== Volumetric Body Digitization ===")]
    [Tooltip("Center origin of the disintegrating body volume.")]
    [SerializeField] private Vector3 bodyCenter = new Vector3(0f, 0.85f, 0f);

    [Tooltip("Radius of the character body volume from which particles overflow.")]
    [SerializeField, Range(0.2f, 1.2f)] private float bodyRadius = 0.55f;

    [Tooltip("Height of the character body volume from which particles overflow.")]
    [SerializeField, Range(0.5f, 2.5f)] private float bodyHeight = 1.70f;

    [Header("=== Massive Particle Volume Overflow ===")]
    [Tooltip("Burst volume count of digital voxels overflowing from the body.")]
    [SerializeField, Range(100, 600)] private int voxelOverflowCount = 280;

    [Tooltip("Burst volume count of cyber rune glyphs (boxed X, cross, diamond) rising into the air.")]
    [SerializeField, Range(40, 300)] private int glyphOverflowCount = 120;

    [Tooltip("Burst count of micro data-dust particles expanding outward.")]
    [SerializeField, Range(80, 500)] private int dataDustCount = 220;

    [Header("=== Eruption & Dispersal Dynamics ===")]
    [Tooltip("Upward eruption velocity of the de-rez volume overflow.")]
    [SerializeField, Range(2.0f, 12.0f)] private float upwardEruptionSpeed = 5.5f;

    [Tooltip("Radial outward expansion spread of the erupting particles.")]
    [SerializeField, Range(1.0f, 8.0f)] private float outwardSpreadSpeed = 3.5f;

    [Tooltip("Lifetime of de-rezzing particles before dissolving completely (seconds).")]
    [SerializeField, Range(1.0f, 5.0f)] private float particleLifetime = 2.2f;

    [Tooltip("Digital turbulence / jitter strength as particles rise into the air.")]
    [SerializeField, Range(0.1f, 1.5f)] private float digitalJitter = 0.65f;

    [Header("=== Bloom / Emissive Intensity Controls ===")]
    [Tooltip("Global Bloom & Emissive Glow intensity multiplier for the digital particles.")]
    [SerializeField, Range(0.5f, 15f)] private float bloomIntensity = 4.2f;
    public float BloomIntensity { get => bloomIntensity; set { bloomIntensity = value; RefreshLiveView(); } }

    [Header("=== Color Palette ===")]
    [Tooltip("Bright Pixel Core White.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color pixelWhite = new Color(1.0f, 0.92f, 0.92f, 1.0f);

    [Tooltip("Primary Cyber Red (De-rez Digitization).")]
    [ColorUsage(true, true)]
    [SerializeField] private Color cyberRed = new Color(1.0f, 0.18f, 0.22f, 1.0f);

    [Tooltip("Deep Crimson Data Edge.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color deepCrimson = new Color(0.80f, 0.08f, 0.16f, 1.0f);

    [Header("=== Assigned URP Materials ===")]
    [SerializeField] private Material voxelShardMat;
    [SerializeField] private Material cyberGlyphsMat;

    private Transform stageRoot;
    private Coroutine activeRoutine;
    private ParticleSystem voxelPS;
    private ParticleSystem glyphsPS;
    private ParticleSystem dataDustPS;
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

    #region Public Controls

    [ContextMenu("Trigger De-Rez Overflow")]
    public void PlayStage()
    {
        StopStage();
        activeRoutine = StartCoroutine(DeRezLoopRoutine());
    }

    [ContextMenu("Stop De-Rez Overflow")]
    public void StopStage()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
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

        SpawnVoxelOverflowSystem();
        SpawnGlyphOverflowSystem();
        SpawnDataDustSystem();

        UpdateLiveParameters();
    }

    #endregion

    #region Routines & Builders

    private IEnumerator DeRezLoopRoutine()
    {
        do
        {
            BuildStaticHierarchy();
            TriggerInstantBurst();

            if (loopPreview)
            {
                yield return new WaitForSeconds(loopInterval);
            }
            else
            {
                yield return new WaitForSeconds(particleLifetime + 0.4f);
                break;
            }
        } while (loopPreview && enabled && gameObject.activeInHierarchy);
    }

    public void TriggerInstantBurst()
    {
        if (voxelPS != null) { voxelPS.Clear(); voxelPS.Play(); }
        if (glyphsPS != null) { glyphsPS.Clear(); glyphsPS.Play(); }
        if (dataDustPS != null) { dataDustPS.Clear(); dataDustPS.Play(); }
    }

    private void SpawnVoxelOverflowSystem()
    {
        GameObject go = CreateChild("MassiveDeRezVoxelOverflow");
        go.transform.localPosition = bodyCenter;
        voxelPS = go.AddComponent<ParticleSystem>();

        var main = voxelPS.main;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetime * 0.7f, particleLifetime * 1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(upwardEruptionSpeed * 0.5f, upwardEruptionSpeed * 1.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(pixelWhite, 0.0f),
                new GradientColorKey(cyberRed, 0.25f),
                new GradientColorKey(deepCrimson, 0.85f)
            },
            new[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.65f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        var emission = voxelPS.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] {
            new ParticleSystem.Burst(0.0f, (short)voxelOverflowCount)
        });

        // Shape: Vertical Box matching Stan's body volume
        var shape = voxelPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(bodyRadius * 2f, bodyHeight, bodyRadius * 2f);

        // Velocity Over Lifetime: Initial explosive eruption then upward float
        var velLife = voxelPS.velocityOverLifetime;
        velLife.enabled = true;
        velLife.space = ParticleSystemSimulationSpace.World;
        velLife.x = new ParticleSystem.MinMaxCurve(-outwardSpreadSpeed * 0.6f, outwardSpreadSpeed * 0.6f);
        velLife.y = new ParticleSystem.MinMaxCurve(upwardEruptionSpeed * 0.4f, upwardEruptionSpeed * 1.1f);
        velLife.z = new ParticleSystem.MinMaxCurve(-outwardSpreadSpeed * 0.6f, outwardSpreadSpeed * 0.6f);

        // Aerodynamic drag
        var limitVel = voxelPS.limitVelocityOverLifetime;
        limitVel.enabled = true;
        limitVel.limit = new ParticleSystem.MinMaxCurve(0.8f);
        limitVel.dampen = 0.35f;

        // Digital Jitter / Noise
        var noise = voxelPS.noise;
        noise.enabled = true;
        noise.strength = digitalJitter;
        noise.frequency = 0.45f;
        noise.scrollSpeed = 0.8f;
        noise.damping = true;

        // Rotation over lifetime
        var rotLife = voxelPS.rotationOverLifetime;
        rotLife.enabled = true;
        rotLife.separateAxes = true;
        rotLife.x = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);
        rotLife.y = new ParticleSystem.MinMaxCurve(-2.5f, 2.5f);
        rotLife.z = new ParticleSystem.MinMaxCurve(-3.5f, 3.5f);

        // Size over lifetime: stays crisp, dissolves near end
        var sizeLife = voxelPS.sizeOverLifetime;
        sizeLife.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.7f);
        curve.AddKey(0.15f, 1.15f);
        curve.AddKey(0.70f, 0.95f);
        curve.AddKey(1.0f, 0.0f);
        sizeLife.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = voxelShardMat;
        rend.sortMode = ParticleSystemSortMode.Distance;

        voxelPS.Play();
    }

    private void SpawnGlyphOverflowSystem()
    {
        GameObject go = CreateChild("FloatingCyberRunesOverflow");
        go.transform.localPosition = bodyCenter;
        glyphsPS = go.AddComponent<ParticleSystem>();

        var main = glyphsPS.main;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetime * 0.8f, particleLifetime * 1.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(upwardEruptionSpeed * 0.4f, upwardEruptionSpeed * 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(pixelWhite, 0.0f),
                new GradientColorKey(cyberRed, 0.2f),
                new GradientColorKey(deepCrimson, 0.8f)
            },
            new[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.70f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        var emission = glyphsPS.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] {
            new ParticleSystem.Burst(0.02f, (short)glyphOverflowCount)
        });

        var shape = glyphsPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(bodyRadius * 2f, bodyHeight, bodyRadius * 2f);

        var velLife = glyphsPS.velocityOverLifetime;
        velLife.enabled = true;
        velLife.space = ParticleSystemSimulationSpace.World;
        velLife.x = new ParticleSystem.MinMaxCurve(-outwardSpreadSpeed * 0.8f, outwardSpreadSpeed * 0.8f);
        velLife.y = new ParticleSystem.MinMaxCurve(upwardEruptionSpeed * 0.5f, upwardEruptionSpeed * 1.3f);
        velLife.z = new ParticleSystem.MinMaxCurve(-outwardSpreadSpeed * 0.8f, outwardSpreadSpeed * 0.8f);

        var limitVel = glyphsPS.limitVelocityOverLifetime;
        limitVel.enabled = true;
        limitVel.limit = new ParticleSystem.MinMaxCurve(0.7f);
        limitVel.dampen = 0.32f;

        var noise = glyphsPS.noise;
        noise.enabled = true;
        noise.strength = digitalJitter * 0.8f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.6f;
        noise.damping = true;

        // 2x2 Texture Sheet Animation for boxed X, cross, and diamond runes
        var texSheet = glyphsPS.textureSheetAnimation;
        texSheet.enabled = true;
        texSheet.numTilesX = 2;
        texSheet.numTilesY = 2;
        texSheet.animation = ParticleSystemAnimationType.SingleRow;
        texSheet.rowMode = ParticleSystemAnimationRowMode.Random;

        var rotLife = glyphsPS.rotationOverLifetime;
        rotLife.enabled = true;
        rotLife.z = new ParticleSystem.MinMaxCurve(-1.8f, 1.8f);

        var sizeLife = glyphsPS.sizeOverLifetime;
        sizeLife.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.4f);
        curve.AddKey(0.12f, 1.25f);
        curve.AddKey(0.70f, 0.95f);
        curve.AddKey(1.0f, 0.0f);
        sizeLife.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = cyberGlyphsMat;
        rend.sortMode = ParticleSystemSortMode.Distance;

        glyphsPS.Play();
    }

    private void SpawnDataDustSystem()
    {
        GameObject go = CreateChild("DispersingDataFountain");
        go.transform.localPosition = bodyCenter;
        dataDustPS = go.AddComponent<ParticleSystem>();

        var main = dataDustPS.main;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetime * 0.6f, particleLifetime * 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(upwardEruptionSpeed * 0.6f, upwardEruptionSpeed * 1.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(pixelWhite, cyberRed);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = dataDustPS.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] {
            new ParticleSystem.Burst(0.01f, (short)dataDustCount)
        });

        var shape = dataDustPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(bodyRadius * 2.2f, bodyHeight, bodyRadius * 2.2f);

        var velLife = dataDustPS.velocityOverLifetime;
        velLife.enabled = true;
        velLife.space = ParticleSystemSimulationSpace.World;
        velLife.x = new ParticleSystem.MinMaxCurve(-outwardSpreadSpeed, outwardSpreadSpeed);
        velLife.y = new ParticleSystem.MinMaxCurve(upwardEruptionSpeed * 0.6f, upwardEruptionSpeed * 1.5f);
        velLife.z = new ParticleSystem.MinMaxCurve(-outwardSpreadSpeed, outwardSpreadSpeed);

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = voxelShardMat;

        dataDustPS.Play();
    }

    #endregion

    #region Live Inspector Refresh & Cleanup

    public void UpdateLiveParameters()
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        if (voxelPS != null)
        {
            voxelPS.transform.localPosition = bodyCenter;
            var shape = voxelPS.shape;
            shape.scale = new Vector3(bodyRadius * 2f, bodyHeight, bodyRadius * 2f);

            var rend = voxelPS.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                rend.SetPropertyBlock(propertyBlock);
            }
        }

        if (glyphsPS != null)
        {
            glyphsPS.transform.localPosition = bodyCenter;
            var shape = glyphsPS.shape;
            shape.scale = new Vector3(bodyRadius * 2f, bodyHeight, bodyRadius * 2f);

            var rend = glyphsPS.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                rend.SetPropertyBlock(propertyBlock);
            }
        }

        if (dataDustPS != null)
        {
            dataDustPS.transform.localPosition = bodyCenter;
            var rend = dataDustPS.GetComponent<ParticleSystemRenderer>();
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
            if (child != null && (child.name == CONTENT_ROOT_NAME || child.name.StartsWith("STAN_RESOLUTION")))
            {
                SafeDestroy(child.gameObject);
            }
        }

        voxelPS = null;
        glyphsPS = null;
        dataDustPS = null;
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
        if (voxelShardMat == null)
            voxelShardMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Stan_VFX/Materials/M_Stan_Voxel_Shard.mat");
        if (cyberGlyphsMat == null)
            cyberGlyphsMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Stan_VFX/Materials/M_Stan_Cyber_Glyphs.mat");
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
