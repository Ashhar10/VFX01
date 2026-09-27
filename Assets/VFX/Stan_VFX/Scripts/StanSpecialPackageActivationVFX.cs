using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// StanSpecialPackageActivationVFX - VFX 2: Special Package Activation.
/// Storyboard Ref: Stan activates the special package. Energy and particles build to a climax.
/// 
/// Key Visual Requirements:
/// - Starburst sticks 100% to the assigned pointer / transform every frame (no random positions!).
/// - Starburst explosion matches King_Arthur_Enemy_Contact_VFX: high-impact optical starburst flash + outward sparkles,
///   exploding and disappearing in a sharp, crisp burst!
/// - Center package glow at the cube.
/// - Popping cyber runes (boxed X, cyber crosses, digital diamonds) spreading into the air.
/// - STRICTLY NO GROUND RINGS!
/// - Velocity curves in identical MinMaxCurveMode (0 warnings, 0 errors).
/// </summary>
[ExecuteAlways]
[SelectionBase]
[DisallowMultipleComponent]
public class StanSpecialPackageActivationVFX : MonoBehaviour
{
    private const string CONTENT_ROOT_NAME = "STAN_SPECIAL_PACKAGE_ACTIVATION_CONTENT";

    [Header("=== Playback Controls ===")]
    [Tooltip("Keep playing the activation build-up in a continuous loop for Scene view preview.")]
    [SerializeField] private bool loopPreview = true;

    [Tooltip("Cycle duration in seconds for energy build-up when Loop Preview is ON.")]
    [SerializeField, Range(0.8f, 4.0f)] private float loopCycleDuration = 1.8f;

    [Header("=== Starburst Pointer & Sticking ===")]
    [Tooltip("Target pointer/bone/cube transform that the starburst MUST stick to. If assigned, starburst glues directly to it!")]
    [SerializeField] private Transform starburstPointer;

    [Tooltip("Local position offset for the starburst (relative to starburstPointer if assigned, or relative to Stan).")]
    [SerializeField] private Vector3 starburstOffset = new Vector3(0f, 0.75f, 0.65f);

    [Header("=== King Arthur Style Starburst Explosion ===")]
    [Tooltip("Diameter of the central starburst explosion flash.")]
    [SerializeField, Range(1.0f, 5.0f)] private float starburstSize = 2.6f;

    [Tooltip("Snappy lifetime of the starburst flash before disappearing (seconds).")]
    [SerializeField, Range(0.2f, 1.0f)] private float starburstLifetime = 0.35f;

    [Tooltip("Number of sharp sparkles fired outward upon starburst explosion.")]
    [SerializeField, Range(10, 60)] private int starburstSparkleCount = 35;

    [Header("=== Package Origin & Center Glow ===")]
    [Tooltip("Center origin offset where Stan holds the package cube.")]
    [SerializeField] private Vector3 packageOffset = new Vector3(0f, 0.75f, 0.65f);

    [Tooltip("Base size of the glowing package core.")]
    [SerializeField, Range(0.3f, 2.0f)] private float centerGlowSize = 1.0f;

    [Header("=== Popping Cyber Runes & Glyphs ===")]
    [Tooltip("Rate of digital runes/glyphs popping out from the package.")]
    [SerializeField, Range(10, 80)] private int glyphEmissionRate = 28;

    [Tooltip("Base size of the popping cyber runes.")]
    [SerializeField, Range(0.1f, 0.6f)] private float glyphSize = 0.25f;

    [Tooltip("Ejection speed when popping out from the cube center.")]
    [SerializeField, Range(1.0f, 8.0f)] private float popOutSpeed = 3.6f;

    [Tooltip("Upward drift speed of the cyber runes as they spread into the air.")]
    [SerializeField, Range(0.2f, 3.0f)] private float upwardDrift = 1.1f;

    [Header("=== Bloom / Emissive Intensity Controls ===")]
    [Tooltip("Global Bloom & Emissive Glow intensity multiplier for the package activation.")]
    [SerializeField, Range(0.5f, 15f)] private float bloomIntensity = 5.5f;
    public float BloomIntensity { get => bloomIntensity; set { bloomIntensity = value; RefreshLiveView(); } }

    [Header("=== Color Palette ===")]
    [Tooltip("Blazing White Core Flash.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color coreWhite = new Color(1.0f, 0.96f, 0.96f, 1.0f);

    [Tooltip("Intense Cyber Crimson Red.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color cyberRed = new Color(1.0f, 0.18f, 0.24f, 1.0f);

    [Tooltip("Deep Digital Magenta.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color deepCrimson = new Color(0.85f, 0.08f, 0.18f, 1.0f);

    [Header("=== Assigned URP Materials ===")]
    [SerializeField] private Material centerGlowMat;
    [SerializeField] private Material starburstMat;
    [SerializeField] private Material cyberGlyphsMat;
    [SerializeField] private Material sparkMoteMat;

    private Transform stageRoot;
    private Transform starburstAnchorNode;
    private Coroutine activeRoutine;
    private ParticleSystem centerGlowPS;
    private ParticleSystem starburstPS;
    private ParticleSystem starburstSparklesPS;
    private ParticleSystem glyphsPS;
    private ParticleSystem motesPS;
    private MaterialPropertyBlock propertyBlock;

    public Transform StarburstAnchor => starburstAnchorNode;

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

    private void Update()
    {
        // GLUE THE STARBURST TO THE SPECIFIC POINTER TRANSFORM EVERY FRAME
        if (starburstAnchorNode != null)
        {
            if (starburstPointer != null)
            {
                starburstAnchorNode.position = starburstPointer.TransformPoint(starburstOffset);
                starburstAnchorNode.rotation = starburstPointer.rotation;
            }
            else
            {
                starburstAnchorNode.localPosition = starburstOffset;
                starburstAnchorNode.localRotation = Quaternion.identity;
            }
        }
    }

    #region Public Controls

    [ContextMenu("Trigger Package Activation")]
    public void PlayStage()
    {
        StopStage();
        activeRoutine = StartCoroutine(ActivationLoopRoutine());
    }

    [ContextMenu("Stop Package Activation")]
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

        SpawnStarburstAnchorAndSystem();
        SpawnCenterPackageGlow();
        SpawnPoppingCyberGlyphs();
        SpawnAscendingCyberMotes();

        UpdateLiveParameters();
    }

    #endregion

    #region Routines & Builders

    private IEnumerator ActivationLoopRoutine()
    {
        do
        {
            BuildStaticHierarchy();
            TriggerInstantBurst();

            if (loopPreview)
            {
                yield return new WaitForSeconds(loopCycleDuration);
            }
            else
            {
                yield return new WaitForSeconds(loopCycleDuration);
                break;
            }
        } while (loopPreview && enabled && gameObject.activeInHierarchy);
    }

    public void TriggerInstantBurst()
    {
        if (starburstPS != null) { starburstPS.Clear(); starburstPS.Play(); }
        if (starburstSparklesPS != null) { starburstSparklesPS.Clear(); starburstSparklesPS.Play(); }
        if (centerGlowPS != null) { centerGlowPS.Clear(); centerGlowPS.Play(); }
        if (glyphsPS != null) { glyphsPS.Clear(); glyphsPS.Play(); }
        if (motesPS != null) { motesPS.Clear(); motesPS.Play(); }
    }

    private void SpawnStarburstAnchorAndSystem()
    {
        // 1. Dedicated Anchor Node that follows starburstPointer
        GameObject anchorGo = CreateChild("StarburstAnchor");
        if (starburstPointer != null)
        {
            anchorGo.transform.position = starburstPointer.TransformPoint(starburstOffset);
            anchorGo.transform.rotation = starburstPointer.rotation;
        }
        else
        {
            anchorGo.transform.localPosition = starburstOffset;
        }
        starburstAnchorNode = anchorGo.transform;

        // 2. Starburst Core (King Arthur Enemy Contact Style)
        GameObject sbGo = new GameObject("KingArthur_Style_Starburst_Flash");
        sbGo.transform.SetParent(starburstAnchorNode, false);
        starburstPS = sbGo.AddComponent<ParticleSystem>();

        var main = starburstPS.main;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = starburstLifetime; // snappy 0.35s
        main.startSpeed = 0f;
        main.startSize = starburstSize;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = new ParticleSystem.MinMaxGradient(coreWhite, cyberRed);
        main.simulationSpace = ParticleSystemSimulationSpace.Local; // STRICTLY LOCAL: GLUED TO POINTER!
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var emission = starburstPS.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] {
            new ParticleSystem.Burst(0.0f, 1)
        });

        // King Arthur size curve: fast pop to max, subtle swell, then snap disappear!
        var sizeLife = starburstPS.sizeOverLifetime;
        sizeLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(new Keyframe(0f, 0.35f, 0f, 6f));
        sizeCurve.AddKey(new Keyframe(0.12f, 1.0f, 0.5f, 0.5f));
        sizeCurve.AddKey(new Keyframe(0.6f, 0.95f, -0.3f, -0.3f));
        sizeCurve.AddKey(new Keyframe(1.0f, 0.0f, -3.0f, 0f));
        sizeLife.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

        // Color over lifetime: bright core flash, then fades out cleanly
        var colLife = starburstPS.colorOverLifetime;
        colLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(coreWhite, 0.0f),
                new GradientColorKey(coreWhite, 0.35f),
                new GradientColorKey(cyberRed, 1.0f)
            },
            new[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.95f, 0.50f),
                new GradientAlphaKey(0.0f, 1.0f) // disappears completely!
            }
        );
        colLife.color = new ParticleSystem.MinMaxGradient(grad);

        var rend = sbGo.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = starburstMat;

        starburstPS.Play();

        // 3. Outward Sparkles (radial burst like King Arthur contact VFX)
        GameObject spGo = new GameObject("Outward_Explosion_Sparkles");
        spGo.transform.SetParent(starburstAnchorNode, false);
        starburstSparklesPS = spGo.AddComponent<ParticleSystem>();

        var spMain = starburstSparklesPS.main;
        spMain.loop = false;
        spMain.playOnAwake = true;
        spMain.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.55f);
        spMain.startSpeed = new ParticleSystem.MinMaxCurve(4.5f, 9.0f);
        spMain.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        spMain.startColor = new ParticleSystem.MinMaxGradient(coreWhite, cyberRed);
        spMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        spMain.scalingMode = ParticleSystemScalingMode.Hierarchy;

        var spEmission = starburstSparklesPS.emission;
        spEmission.enabled = true;
        spEmission.rateOverTime = 0;
        spEmission.SetBursts(new[] {
            new ParticleSystem.Burst(0.0f, (short)starburstSparkleCount)
        });

        var spShape = starburstSparklesPS.shape;
        spShape.enabled = true;
        spShape.shapeType = ParticleSystemShapeType.Sphere;
        spShape.radius = 0.12f;

        var spLimit = starburstSparklesPS.limitVelocityOverLifetime;
        spLimit.enabled = true;
        spLimit.limit = new ParticleSystem.MinMaxCurve(1.2f);
        spLimit.dampen = 0.35f;

        var spSizeLife = starburstSparklesPS.sizeOverLifetime;
        spSizeLife.enabled = true;
        AnimationCurve spCurve = new AnimationCurve();
        spCurve.AddKey(0f, 1.0f);
        spCurve.AddKey(0.7f, 0.8f);
        spCurve.AddKey(1f, 0.0f);
        spSizeLife.size = new ParticleSystem.MinMaxCurve(1.0f, spCurve);

        var spRend = spGo.GetComponent<ParticleSystemRenderer>();
        spRend.renderMode = ParticleSystemRenderMode.Billboard;
        spRend.sharedMaterial = sparkMoteMat;

        starburstSparklesPS.Play();
    }

    private void SpawnCenterPackageGlow()
    {
        GameObject go = CreateChild("CenterPackageGlow");
        go.transform.localPosition = packageOffset;
        centerGlowPS = go.AddComponent<ParticleSystem>();

        var main = centerGlowPS.main;
        main.loop = loopPreview;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
        main.startSpeed = 0f;
        main.startSize = centerGlowSize;
        main.startColor = new ParticleSystem.MinMaxGradient(coreWhite, cyberRed);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = centerGlowPS.emission;
        emission.enabled = true;
        emission.rateOverTime = 4f;

        var sizeLife = centerGlowPS.sizeOverLifetime;
        sizeLife.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.4f);
        curve.AddKey(0.35f, 1.25f);
        curve.AddKey(0.70f, 0.95f);
        curve.AddKey(1.0f, 1.4f);
        sizeLife.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        var colLife = centerGlowPS.colorOverLifetime;
        colLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(coreWhite, 0.0f), new GradientColorKey(cyberRed, 0.5f) },
            new[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.2f), new GradientAlphaKey(0.85f, 0.7f), new GradientAlphaKey(0.0f, 1.0f) }
        );
        colLife.color = new ParticleSystem.MinMaxGradient(grad);

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = centerGlowMat;

        centerGlowPS.Play();
    }

    private void SpawnPoppingCyberGlyphs()
    {
        GameObject go = CreateChild("PoppingCyberGlyphs");
        go.transform.localPosition = packageOffset;
        glyphsPS = go.AddComponent<ParticleSystem>();

        var main = glyphsPS.main;
        main.loop = loopPreview;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(popOutSpeed * 0.7f, popOutSpeed * 1.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(glyphSize * 0.8f, glyphSize * 1.3f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(coreWhite, 0.0f),
                new GradientColorKey(cyberRed, 0.3f),
                new GradientColorKey(deepCrimson, 0.85f)
            },
            new[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.12f),
                new GradientAlphaKey(0.95f, 0.65f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        var emission = glyphsPS.emission;
        emission.enabled = true;
        emission.rateOverTime = glyphEmissionRate;

        var shape = glyphsPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        var limitVel = glyphsPS.limitVelocityOverLifetime;
        limitVel.enabled = true;
        limitVel.limit = new ParticleSystem.MinMaxCurve(0.6f);
        limitVel.dampen = 0.35f;

        // ALL 3 AXES IN TwoConstants MODE
        var velLife = glyphsPS.velocityOverLifetime;
        velLife.enabled = true;
        velLife.space = ParticleSystemSimulationSpace.World;
        velLife.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velLife.y = new ParticleSystem.MinMaxCurve(upwardDrift * 0.5f, upwardDrift * 1.2f);
        velLife.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var noise = glyphsPS.noise;
        noise.enabled = true;
        noise.strength = 0.35f;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.5f;
        noise.damping = true;

        var texSheet = glyphsPS.textureSheetAnimation;
        texSheet.enabled = true;
        texSheet.numTilesX = 2;
        texSheet.numTilesY = 2;
        texSheet.animation = ParticleSystemAnimationType.SingleRow;
        texSheet.rowMode = ParticleSystemAnimationRowMode.Random;

        var rotLife = glyphsPS.rotationOverLifetime;
        rotLife.enabled = true;
        rotLife.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

        var sizeLife = glyphsPS.sizeOverLifetime;
        sizeLife.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.2f);
        curve.AddKey(0.15f, 1.25f);
        curve.AddKey(0.70f, 0.95f);
        curve.AddKey(1.0f, 0.05f);
        sizeLife.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = cyberGlyphsMat;
        rend.sortMode = ParticleSystemSortMode.Distance;

        glyphsPS.Play();
    }

    private void SpawnAscendingCyberMotes()
    {
        GameObject go = CreateChild("AscendingCyberMotes");
        go.transform.localPosition = packageOffset;
        motesPS = go.AddComponent<ParticleSystem>();

        var main = motesPS.main;
        main.loop = loopPreview;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(coreWhite, cyberRed);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = motesPS.emission;
        emission.enabled = true;
        emission.rateOverTime = glyphEmissionRate * 0.75f;

        var shape = motesPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.35f;

        var velLife = motesPS.velocityOverLifetime;
        velLife.enabled = true;
        velLife.space = ParticleSystemSimulationSpace.World;
        velLife.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velLife.y = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
        velLife.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = centerGlowMat;

        motesPS.Play();
    }

    #endregion

    #region Live Inspector Refresh & Cleanup

    public void UpdateLiveParameters()
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        if (starburstAnchorNode != null)
        {
            if (starburstPointer != null)
            {
                starburstAnchorNode.position = starburstPointer.TransformPoint(starburstOffset);
                starburstAnchorNode.rotation = starburstPointer.rotation;
            }
            else
            {
                starburstAnchorNode.localPosition = starburstOffset;
            }
        }

        if (starburstPS != null)
        {
            var main = starburstPS.main;
            main.startSize = starburstSize;
            main.startLifetime = starburstLifetime;

            var rend = starburstPS.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                rend.SetPropertyBlock(propertyBlock);
            }
        }

        if (starburstSparklesPS != null)
        {
            var rend = starburstSparklesPS.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                rend.SetPropertyBlock(propertyBlock);
            }
        }

        if (centerGlowPS != null)
        {
            centerGlowPS.transform.localPosition = packageOffset;
            var main = centerGlowPS.main;
            main.startSize = centerGlowSize;

            var rend = centerGlowPS.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                rend.SetPropertyBlock(propertyBlock);
            }
        }

        if (glyphsPS != null)
        {
            glyphsPS.transform.localPosition = packageOffset;
            var main = glyphsPS.main;
            main.startSize = new ParticleSystem.MinMaxCurve(glyphSize * 0.8f, glyphSize * 1.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(popOutSpeed * 0.7f, popOutSpeed * 1.3f);

            var em = glyphsPS.emission;
            em.rateOverTime = glyphEmissionRate;

            var velLife = glyphsPS.velocityOverLifetime;
            velLife.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            velLife.y = new ParticleSystem.MinMaxCurve(upwardDrift * 0.5f, upwardDrift * 1.2f);
            velLife.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var rend = glyphsPS.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                rend.SetPropertyBlock(propertyBlock);
            }
        }

        if (motesPS != null)
        {
            motesPS.transform.localPosition = packageOffset;
            var rend = motesPS.GetComponent<ParticleSystemRenderer>();
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
            if (child != null && (child.name == CONTENT_ROOT_NAME || child.name.StartsWith("STAN_SPECIAL_PACKAGE")))
            {
                SafeDestroy(child.gameObject);
            }
        }

        centerGlowPS = null;
        starburstPS = null;
        starburstSparklesPS = null;
        glyphsPS = null;
        motesPS = null;
        starburstAnchorNode = null;
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
        if (centerGlowMat == null)
            centerGlowMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Stan_VFX/Materials/M_Stan_Center_Glow.mat");
        if (starburstMat == null)
            starburstMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Stan_VFX/Materials/M_Stan_Starburst_Rays.mat");
        if (cyberGlyphsMat == null)
            cyberGlyphsMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Stan_VFX/Materials/M_Stan_Cyber_Glyphs.mat");
        if (sparkMoteMat == null)
            sparkMoteMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Stan_VFX/Materials/M_Stan_Swing_Spark.mat");
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
