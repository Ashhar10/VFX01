using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AluraMagicFieldStage4Dissipation - Dedicated Stage 4: RECOVERY / DISSIPATION VFX.
/// Matched to Storyboard Reference Image 2:
/// - The field fades. Particles dissipate upward.
/// - Outward energy burst.
/// - Rising arcane motes.
/// - Smooth opacity fade from full down to 0 with zero material leaks and zero popping.
/// 
/// Real-time Features:
/// - Plays automatically on Enable (toggle On/Off to replay).
/// - Live updates in Scene View when editing values in Inspector.
/// - Dedicated Bloom / Emissive intensity controls.
/// - 100% Edit-Mode safe: Zero renderer.material leaks!
/// </summary>
[ExecuteAlways]
[SelectionBase]
[DisallowMultipleComponent]
public class AluraMagicFieldStage4Dissipation : MonoBehaviour
{
    [Header("=== Global Dimension & Timings ===")]
    [Tooltip("Total outer dimension of the 2x2 field in world units.")]
    [SerializeField, Range(2f, 10f)] private float fieldSize = 4.2f;

    [Tooltip("Total duration for dissipation and fade-out.")]
    [SerializeField, Range(0.5f, 5f)] private float dissipationDuration = 1.8f;

    [Header("=== Bloom / Emissive Intensity Controls ===")]
    [Tooltip("Global Bloom & Emissive Glow intensity multiplier for Stage 4.")]
    [SerializeField, Range(0.5f, 15f)] private float bloomIntensity = 4.0f;
    public float BloomIntensity { get => bloomIntensity; set { bloomIntensity = value; RefreshLiveView(); } }

    [Header("=== Dissipation Colors ===")]
    [ColorUsage(true, true)]
    [SerializeField] private Color burstColor = new Color(0.70f, 1.0f, 0.85f, 1.0f);

    [ColorUsage(true, true)]
    [SerializeField] private Color motesColor = new Color(0.90f, 1.0f, 0.95f, 1.0f);

    [Header("=== Assigned URP Materials ===")]
    [SerializeField] private Material sparkMaterial;
    [SerializeField] private Material gridMaterial;
    [SerializeField] private Material softGlowMaterial;

    private Transform stageRoot;
    private Coroutine activeRoutine;
    private readonly List<LineRenderer> fadingLines = new List<LineRenderer>();
    private readonly List<ParticleSystem> activeParticles = new List<ParticleSystem>();
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

    #region Context Menus & Public Controls

    [ContextMenu("Play Stage 4 (Dissipation)")]
    public void PlayStage()
    {
        StopStage();
        activeRoutine = StartCoroutine(Stage4Routine());
    }

    [ContextMenu("Stop Stage 4")]
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
        CreateFreshRoot("STAGE_4_DISSIPATION_CONTENT");
        SpawnDissipationBurst();
        SpawnDissipationMotes();
        SpawnFadingFieldRings();
        UpdateLiveParameters();
    }

    #endregion

    #region Routine & Builders

    private IEnumerator Stage4Routine()
    {
        BuildStaticHierarchy();

        float elapsed = 0f;
        while (elapsed < dissipationDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / dissipationDuration);
            SetLinesAlpha(alpha);
            yield return null;
        }

        SetLinesAlpha(0f);
        yield return new WaitForSeconds(0.2f);
        CleanupHierarchy();
    }

    private void SpawnDissipationBurst()
    {
        GameObject go = CreateChild("DissipationBurst");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = burstColor;
        main.maxParticles = 120;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 110) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        vol.y = new ParticleSystem.MinMaxCurve(0.2f, 1.2f);
        vol.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = sparkMaterial != null ? sparkMaterial : gridMaterial;
        activeParticles.Add(ps);
        ps.Play();
    }

    private void SpawnDissipationMotes()
    {
        GameObject go = CreateChild("RisingDissipationMotes");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
        main.startColor = new ParticleSystem.MinMaxGradient(burstColor, motesColor);
        main.maxParticles = 100;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 85) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(fieldSize, 0.1f, fieldSize);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        vol.y = new ParticleSystem.MinMaxCurve(0.6f, 2.2f);
        vol.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = sparkMaterial != null ? sparkMaterial : gridMaterial;
        activeParticles.Add(ps);
        ps.Play();
    }

    private void SpawnFadingFieldRings()
    {
        CreateCircle(Vector3.up * 0.045f, fieldSize * 0.50f, burstColor, 0.026f, 72, gridMaterial);
        CreateCircle(Vector3.up * 0.05f, fieldSize * 0.35f, motesColor, 0.018f, 60, gridMaterial);
    }

    public void UpdateLiveParameters()
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        foreach (LineRenderer lr in fadingLines)
        {
            if (lr != null)
            {
                lr.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                lr.SetPropertyBlock(propertyBlock);
            }
        }

        foreach (ParticleSystem ps in activeParticles)
        {
            if (ps != null)
            {
                ParticleSystemRenderer rend = ps.GetComponent<ParticleSystemRenderer>();
                if (rend != null)
                {
                    rend.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetFloat("_Intensity", bloomIntensity);
                    rend.SetPropertyBlock(propertyBlock);
                }
            }
        }
    }

    private void CreateCircle(Vector3 center, float radius, Color col, float width, int segments, Material mat)
    {
        GameObject go = CreateChild("DissipatingCircle");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = segments;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.sharedMaterial = mat != null ? mat : gridMaterial;
        lr.startColor = col;
        lr.endColor = col;

        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        lr.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat("_Intensity", bloomIntensity);
        lr.SetPropertyBlock(propertyBlock);

        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            Vector3 pos = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            lr.SetPosition(i, pos);
        }
        fadingLines.Add(lr);
    }

    private void SetLinesAlpha(float alpha)
    {
        foreach (LineRenderer lr in fadingLines)
        {
            if (lr != null)
            {
                Color s = lr.startColor;
                s.a = alpha;
                lr.startColor = s;
                lr.endColor = s;
            }
        }
    }

    private GameObject CreateChild(string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(stageRoot, false);
        return child;
    }

    private const string CONTENT_ROOT_NAME = "STAGE_4_DISSIPATION_CONTENT";

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
            if (child != null && (child.name == CONTENT_ROOT_NAME || child.name.StartsWith("STAGE_4_DISSIPATION")))
            {
                SafeDestroy(child.gameObject);
            }
        }

        fadingLines.Clear();
        activeParticles.Clear();
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
        if (sparkMaterial == null)
            sparkMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_Spark.mat");
        if (gridMaterial == null)
            gridMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_Grid.mat");
        if (softGlowMaterial == null)
            softGlowMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_SoftGlow.mat");
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
