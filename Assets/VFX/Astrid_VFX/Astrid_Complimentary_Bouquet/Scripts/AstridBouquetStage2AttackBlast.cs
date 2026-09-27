using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AstridBouquetStage2AttackBlast - Stage 2: Bouquet Attack Blast.
/// Storyboard Ref: Astrid forcefully shoves the entire bouquet into the target.
/// VFX Scenario 2:
/// - Violent forward/impact blast of petal particles.
/// - Fast explosive initial velocity, rapidly decelerating from air drag.
/// - Rapid tumbling and spin.
/// - Quick dissolve and fade out over a punchy duration (~0.6s - 1.0s).
/// - Core impact glow flash and shockwave burst.
/// 
/// Real-time Features:
/// - Instant play/replay on Enable/Disable (toggle GameObject in Hierarchy).
/// - Live updates in Scene View on Inspector property changes (OnValidate + delayCall).
/// - Dedicated Bloom & Emissive Intensity slider.
/// - Optional loop interval for continuous preview testing in editor.
/// - Zero material leak (uses sharedMaterial & MaterialPropertyBlock).
/// - HideFlags.DontSave prevents duplicate accumulation.
/// </summary>
[ExecuteAlways]
[SelectionBase]
[DisallowMultipleComponent]
public class AstridBouquetStage2AttackBlast : MonoBehaviour
{
    private const string CONTENT_ROOT_NAME = "STAGE_2_ATTACK_BLAST_CONTENT";

    [Header("=== Blast Physics & Dynamics ===")]
    [Tooltip("Number of petals emitted in the blast burst.")]
    [SerializeField, Range(15, 120)] private int blastPetalCount = 55;

    [Tooltip("Initial explosive blast speed.")]
    [SerializeField, Range(4f, 25f)] private float blastSpeed = 12f;

    [Tooltip("Base size of the blasted petals.")]
    [SerializeField, Range(0.1f, 0.8f)] private float petalSize = 0.28f;

    [Tooltip("Lifetime of blast petals before complete dissolve (seconds).")]
    [SerializeField, Range(0.4f, 2.0f)] private float blastLifetime = 0.85f;

    [Tooltip("Spread angle of the blast cone (degrees). 0 = direct forward beam, 90 = hemisphere.")]
    [SerializeField, Range(15f, 90f)] private float blastAngle = 40f;

    [Tooltip("Forward offset of impact point relative to this transform.")]
    [SerializeField] private Vector3 blastOffset = new Vector3(0f, 1.0f, 0.8f);

    [Header("=== Preview Loop Settings ===")]
    [Tooltip("Continuously repeat blast every X seconds (0 = single shot on play/enable).")]
    [SerializeField, Range(0f, 4f)] private float loopInterval = 1.6f;

    [Header("=== Bloom / Emissive Intensity Controls ===")]
    [Tooltip("Global Bloom & Emissive Glow intensity multiplier for the blast.")]
    [SerializeField, Range(0.5f, 15f)] private float bloomIntensity = 4.5f;
    public float BloomIntensity { get => bloomIntensity; set { bloomIntensity = value; RefreshLiveView(); } }

    [Header("=== Color Palette ===")]
    [Tooltip("Vibrant Cherry Blossom Shock Pink.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color blastPink = new Color(1.0f, 0.42f, 0.66f, 1.0f);

    [Tooltip("Bright Core White / Sakura Flash.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color flashWhite = new Color(1.0f, 0.95f, 0.98f, 1.0f);

    [Tooltip("Deep Magenta Petal Shrapnel.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color deepMagenta = new Color(0.73f, 0.20f, 0.46f, 1.0f);

    [Header("=== Assigned URP Materials ===")]
    [SerializeField] private Material singlePetalAddMat;
    [SerializeField] private Material singlePetalAlphaMat;
    [SerializeField] private Material softGlowMat;
    [SerializeField] private Material shockwaveMat;

    private Transform stageRoot;
    private Coroutine loopRoutine;
    private ParticleSystem blastPS;
    private ParticleSystem flashPS;
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

    [ContextMenu("Trigger Petal Blast")]
    public void PlayStage()
    {
        StopStage();
        loopRoutine = StartCoroutine(BlastLoopRoutine());
    }

    [ContextMenu("Stop Stage 2")]
    public void StopStage()
    {
        if (loopRoutine != null)
        {
            StopCoroutine(loopRoutine);
            loopRoutine = null;
        }
        CleanupHierarchy();
    }

    [ContextMenu("Refresh Live View")]
    public void RefreshLiveView()
    {
        UpdateLiveParameters();
    }

    public void BuildStaticHierarchy()
    {
        CleanupHierarchy();
        CreateFreshRoot(CONTENT_ROOT_NAME);

        SpawnBlastParticleSystem();
        SpawnImpactFlash();
        UpdateLiveParameters();
    }

    #endregion

    #region Routine & Builders

    private IEnumerator BlastLoopRoutine()
    {
        do
        {
            BuildStaticHierarchy();
            TriggerInstantBurst();

            if (loopInterval > 0f)
            {
                yield return new WaitForSeconds(loopInterval);
            }
            else
            {
                yield return new WaitForSeconds(blastLifetime + 0.2f);
                break;
            }
        } while (loopInterval > 0f && enabled && gameObject.activeInHierarchy);
    }

    public void TriggerInstantBurst()
    {
        if (blastPS != null)
        {
            blastPS.Clear();
            blastPS.Play();
        }
        if (flashPS != null)
        {
            flashPS.Clear();
            flashPS.Play();
        }
    }

    private void SpawnBlastParticleSystem()
    {
        GameObject go = CreateChild("PetalBlastEmitter");
        go.transform.localPosition = blastOffset;
        go.transform.localRotation = Quaternion.identity; // Directed forward along Z
        blastPS = go.AddComponent<ParticleSystem>();

        var main = blastPS.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(blastLifetime * 0.7f, blastLifetime * 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(blastSpeed * 0.6f, blastSpeed * 1.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(petalSize * 0.8f, petalSize * 1.3f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier = 0.08f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        // Gradient: White flash -> Bright pink -> Magenta -> Dissolve
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(flashWhite, 0.0f),
                new GradientColorKey(blastPink, 0.25f),
                new GradientColorKey(deepMagenta, 0.75f)
            },
            new[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.5f),
                new GradientAlphaKey(0.0f, 1.0f) // Rapid dissolve
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        // Emission: Single sharp burst
        var emission = blastPS.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] {
            new ParticleSystem.Burst(0.0f, (short)blastPetalCount)
        });

        // Shape: Cone directed forward
        var shape = blastPS.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = blastAngle;
        shape.radius = 0.25f;
        shape.length = 0.5f;

        // Limit Velocity Over Lifetime (aerodynamic deceleration):
        // Petals blast forward explosive then quickly brake in the air!
        var limitVel = blastPS.limitVelocityOverLifetime;
        limitVel.enabled = true;
        limitVel.limit = new ParticleSystem.MinMaxCurve(0.8f);
        limitVel.dampen = 0.45f;

        // Rotation Over Lifetime (intense tumble on blast)
        var rot = blastPS.rotationOverLifetime;
        rot.enabled = true;
        rot.separateAxes = true;
        rot.x = new ParticleSystem.MinMaxCurve(-4.5f, 4.5f);
        rot.y = new ParticleSystem.MinMaxCurve(-5.0f, 5.0f);
        rot.z = new ParticleSystem.MinMaxCurve(-6.0f, 6.0f);

        // Size Over Lifetime: Snappy shrink before disappearing
        var sizeLife = blastPS.sizeOverLifetime;
        sizeLife.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.9f);
        curve.AddKey(0.15f, 1.15f);
        curve.AddKey(0.7f, 1.0f);
        curve.AddKey(1.0f, 0.1f);
        sizeLife.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        // Renderer
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = singlePetalAddMat;
        rend.sortMode = ParticleSystemSortMode.YoungestInFront;
    }

    private void SpawnImpactFlash()
    {
        GameObject go = CreateChild("ImpactCoreFlash");
        go.transform.localPosition = blastOffset;
        flashPS = go.AddComponent<ParticleSystem>();

        var main = flashPS.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.28f;
        main.startSpeed = 0f;
        main.startSize = petalSize * 4.5f;
        main.startColor = flashWhite;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = flashPS.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] {
            new ParticleSystem.Burst(0.0f, 1)
        });

        var sizeLife = flashPS.sizeOverLifetime;
        sizeLife.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.4f);
        curve.AddKey(0.2f, 1.2f);
        curve.AddKey(1.0f, 0.0f);
        sizeLife.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = softGlowMat;
    }

    #endregion

    #region Live Inspector Refresh & Cleanup

    public void UpdateLiveParameters()
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        if (blastPS != null)
        {
            var main = blastPS.main;
            main.startSpeed = new ParticleSystem.MinMaxCurve(blastSpeed * 0.6f, blastSpeed * 1.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(petalSize * 0.8f, petalSize * 1.3f);

            var emission = blastPS.emission;
            emission.SetBursts(new[] {
                new ParticleSystem.Burst(0.0f, (short)blastPetalCount)
            });

            var shape = blastPS.shape;
            shape.angle = blastAngle;

            var rend = blastPS.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                rend.SetPropertyBlock(propertyBlock);
            }
        }

        if (flashPS != null)
        {
            var rend = flashPS.GetComponent<ParticleSystemRenderer>();
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
            if (child != null && (child.name == CONTENT_ROOT_NAME || child.name.StartsWith("STAGE_2_ATTACK_BLAST")))
            {
                SafeDestroy(child.gameObject);
            }
        }

        blastPS = null;
        flashPS = null;
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
        if (singlePetalAddMat == null)
            singlePetalAddMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Astrid_VFX/Astrid_Complimentary_Bouquet/Materials/M_Astrid_Petal_Single_Add.mat");
        if (singlePetalAlphaMat == null)
            singlePetalAlphaMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Astrid_VFX/Astrid_Complimentary_Bouquet/Materials/M_Astrid_Petal_Single_Alpha.mat");
        if (softGlowMat == null)
            softGlowMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Astrid_VFX/Astrid_Complimentary_Bouquet/Materials/M_Astrid_SoftGlow.mat");
        if (shockwaveMat == null)
            shockwaveMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Astrid_VFX/Astrid_Complimentary_Bouquet/Materials/M_Astrid_Floral_Shockwave.mat");
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
