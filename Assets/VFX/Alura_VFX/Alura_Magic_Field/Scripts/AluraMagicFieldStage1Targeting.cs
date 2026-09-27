using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AluraMagicFieldStage1Targeting - Dedicated Stage 1: START / TARGETING VFX.
/// Matched to Storyboard Reference Image 2:
/// - 2x2 Glowing Arcane Green Floor Grid with corner markers.
/// - Ally Highlight: Floor targeting ring (Cyan) + floating '+' indicator.
/// - Enemy Highlight: Floor targeting ring (Red).
/// - Floor scanner particle sweep.
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
public class AluraMagicFieldStage1Targeting : MonoBehaviour
{
    [Header("=== Global Dimension & Timings ===")]
    [Tooltip("Total outer dimension of the 2x2 field in world units.")]
    [SerializeField, Range(2f, 10f)] private float fieldSize = 4.2f;

    [Tooltip("Duration in seconds for the stage when played in sequence (0 = infinite / continuous).")]
    [SerializeField, Range(0f, 6f)] private float stageDuration = 2.0f;

    [Tooltip("Duration of the grid line fade-in.")]
    [SerializeField, Range(0.1f, 1.5f)] private float fadeInDuration = 0.45f;

    [Header("=== Bloom / Emissive Intensity Controls ===")]
    [Tooltip("Global Bloom & Emissive Glow intensity multiplier for Stage 1.")]
    [SerializeField, Range(0.5f, 15f)] private float bloomIntensity = 4.0f;
    public float BloomIntensity { get => bloomIntensity; set { bloomIntensity = value; RefreshLiveView(); } }

    [Header("=== Grid Configuration ===")]
    [Tooltip("Grid line color for the 2x2 field.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color gridColor = new Color(0.25f, 1.0f, 0.55f, 1.0f);

    [Header("=== Targeting Highlights ===")]
    [Tooltip("Ally tile offset relative to field center (Default: Left tile).")]
    [SerializeField] private Vector3 allyTileOffset = new Vector3(-1.05f, 0f, 0f);

    [Tooltip("Enemy tile offset relative to field center (Default: Right tile).")]
    [SerializeField] private Vector3 enemyTileOffset = new Vector3(1.05f, 0f, 0f);

    [Tooltip("Ally highlight ring & icon tint (Cyan).")]
    [ColorUsage(true, true)]
    [SerializeField] private Color allyHighlightColor = new Color(0.18f, 0.90f, 1.0f, 1.0f);

    [Tooltip("Enemy highlight ring tint (Crimson Red).")]
    [ColorUsage(true, true)]
    [SerializeField] private Color enemyHighlightColor = new Color(1.0f, 0.22f, 0.25f, 1.0f);

    [Header("=== Assigned URP Materials ===")]
    [SerializeField] private Material gridMaterial;
    [SerializeField] private Material allyRingMaterial;
    [SerializeField] private Material enemyRingMaterial;
    [SerializeField] private Material healPlusMaterial;
    [SerializeField] private Material softGlowMaterial;

    private Transform stageRoot;
    private Coroutine activeRoutine;
    private readonly List<LineRenderer> activeLines = new List<LineRenderer>();
    private readonly List<ParticleSystem> activeParticles = new List<ParticleSystem>();
    private GameObject allyCrossIcon;
    private MaterialPropertyBlock propertyBlock;
    private static Mesh cachedQuadMesh;

    private void Awake()
    {
        EnsureQuadMesh();
        ValidateMaterials();
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        CleanupHierarchy();
    }

    private void OnEnable()
    {
        EnsureQuadMesh();
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

    [ContextMenu("Play Stage 1 (Targeting)")]
    public void PlayStage()
    {
        StopStage();
        activeRoutine = StartCoroutine(Stage1Routine());
    }

    [ContextMenu("Stop Stage 1")]
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
            SetLinesAlpha(1f);
        }
        else
        {
            UpdateLiveParameters();
        }
    }

    public void BuildStaticHierarchy()
    {
        CleanupHierarchy();
        CreateFreshRoot("STAGE_1_TARGETING_CONTENT");
        BuildGridGeometry();
        SpawnHighlightRings();
        SpawnFloatingCrossIcon();
        SpawnScannerParticles();
        UpdateLiveParameters();
    }

    #endregion

    #region Routine & Builders

    private IEnumerator Stage1Routine()
    {
        BuildStaticHierarchy();
        yield return FadeLineAlpha(0f, 1f, fadeInDuration);

        if (stageDuration > 0f)
        {
            yield return new WaitForSeconds(stageDuration);
        }
    }

    private void BuildGridGeometry()
    {
        float h = fieldSize * 0.5f;
        float y = 0.02f;

        // Outer 2x2 boundary
        CreateLine(new Vector3(-h, y, -h), new Vector3(h, y, -h), gridColor, 0.035f);
        CreateLine(new Vector3(h, y, -h), new Vector3(h, y, h), gridColor, 0.035f);
        CreateLine(new Vector3(h, y, h), new Vector3(-h, y, h), gridColor, 0.035f);
        CreateLine(new Vector3(-h, y, h), new Vector3(-h, y, -h), gridColor, 0.035f);

        // Center cross dividers
        CreateLine(new Vector3(0f, y, -h), new Vector3(0f, y, h), gridColor, 0.04f);
        CreateLine(new Vector3(-h, y, 0f), new Vector3(h, y, 0f), gridColor, 0.04f);

        // Inner decorative cell framing
        float q = h * 0.65f;
        CreateLine(new Vector3(-q, y, -q), new Vector3(q, y, -q), gridColor, 0.015f);
        CreateLine(new Vector3(q, y, -q), new Vector3(q, y, q), gridColor, 0.015f);
        CreateLine(new Vector3(q, y, q), new Vector3(-q, y, q), gridColor, 0.015f);
        CreateLine(new Vector3(-q, y, q), new Vector3(-q, y, -q), gridColor, 0.015f);

        // Corner tick marks
        float m = 0.22f;
        Vector3[] corners = {
            new Vector3(-h, y, -h),
            new Vector3(h, y, -h),
            new Vector3(-h, y, h),
            new Vector3(h, y, h)
        };
        foreach (Vector3 c in corners)
        {
            CreateLine(c + new Vector3(-m, 0, 0), c + new Vector3(m, 0, 0), gridColor, 0.025f);
            CreateLine(c + new Vector3(0, 0, -m), c + new Vector3(0, 0, m), gridColor, 0.025f);
        }
    }

    private void SpawnHighlightRings()
    {
        CreateCircle(allyTileOffset + Vector3.up * 0.03f, 0.55f, allyHighlightColor, 0.032f, 48, allyRingMaterial);
        CreateCircle(enemyTileOffset + Vector3.up * 0.03f, 0.55f, enemyHighlightColor, 0.032f, 48, enemyRingMaterial);
    }

    private void SpawnFloatingCrossIcon()
    {
        Vector3 spawnPos = allyTileOffset + new Vector3(0.55f, 0.85f, 0f);
        allyCrossIcon = CreateChild("AllyFloatingPlusIcon");
        allyCrossIcon.transform.localPosition = spawnPos;
        allyCrossIcon.transform.localScale = Vector3.one * 0.38f;

        MeshFilter mf = allyCrossIcon.AddComponent<MeshFilter>();
        mf.sharedMesh = cachedQuadMesh;

        MeshRenderer mr = allyCrossIcon.AddComponent<MeshRenderer>();
        mr.sharedMaterial = healPlusMaterial != null ? healPlusMaterial : gridMaterial;

        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        mr.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", allyHighlightColor);
        propertyBlock.SetFloat("_Intensity", bloomIntensity);
        mr.SetPropertyBlock(propertyBlock);
    }

    private void SpawnScannerParticles()
    {
        GameObject go = CreateChild("TargetingScanner");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = 0.6f;
        main.startSpeed = 0.1f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.07f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.3f, 1f, 0.6f, 0.6f), Color.white);
        main.maxParticles = 60;

        var emission = ps.emission;
        emission.rateOverTime = 30;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(fieldSize, 0.02f, fieldSize);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.x = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
        vol.y = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        vol.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = softGlowMaterial != null ? softGlowMaterial : gridMaterial;
        activeParticles.Add(ps);
        ps.Play();
    }

    public void UpdateLiveParameters()
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        // Update all line renderers with bloom intensity and colors
        foreach (LineRenderer lr in activeLines)
        {
            if (lr != null)
            {
                lr.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                lr.SetPropertyBlock(propertyBlock);
            }
        }

        // Update ally icon
        if (allyCrossIcon != null)
        {
            allyCrossIcon.transform.localPosition = allyTileOffset + new Vector3(0.55f, 0.85f, 0f);
            MeshRenderer mr = allyCrossIcon.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", allyHighlightColor);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                mr.SetPropertyBlock(propertyBlock);
            }
        }

        // Update particle scanners
        foreach (ParticleSystem ps in activeParticles)
        {
            if (ps != null)
            {
                var shape = ps.shape;
                shape.scale = new Vector3(fieldSize, 0.02f, fieldSize);
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

    private void CreateLine(Vector3 start, Vector3 end, Color col, float width)
    {
        GameObject go = CreateChild("FieldLine");
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.startWidth = width;
        lr.endWidth = width;
        lr.sharedMaterial = gridMaterial;
        lr.startColor = col;
        lr.endColor = col;

        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        lr.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat("_Intensity", bloomIntensity);
        lr.SetPropertyBlock(propertyBlock);

        activeLines.Add(lr);
    }

    private void CreateCircle(Vector3 center, float radius, Color col, float width, int segments, Material mat)
    {
        GameObject go = CreateChild("HighlightCircle");
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
        activeLines.Add(lr);
    }

    private IEnumerator FadeLineAlpha(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, t / duration);
            SetLinesAlpha(alpha);
            yield return null;
        }
        SetLinesAlpha(to);
    }

    public void SetLinesAlpha(float alpha)
    {
        foreach (LineRenderer lr in activeLines)
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

    private const string CONTENT_ROOT_NAME = "STAGE_1_TARGETING_CONTENT";

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
            if (child != null && (child.name == CONTENT_ROOT_NAME || child.name.StartsWith("STAGE_1_TARGETING")))
            {
                SafeDestroy(child.gameObject);
            }
        }

        activeLines.Clear();
        activeParticles.Clear();
        allyCrossIcon = null;
    }

    private static void SafeDestroy(GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying)
            Destroy(go);
        else
            DestroyImmediate(go);
    }

    private void EnsureQuadMesh()
    {
        if (cachedQuadMesh != null) return;
        cachedQuadMesh = new Mesh
        {
            name = "AluraQuadMesh",
            vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            },
            uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            },
            triangles = new[] { 0, 2, 1, 2, 3, 1 }
        };
        cachedQuadMesh.RecalculateNormals();
        cachedQuadMesh.RecalculateBounds();
    }

    private void ValidateMaterials()
    {
#if UNITY_EDITOR
        if (gridMaterial == null)
            gridMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_Grid.mat");
        if (allyRingMaterial == null)
            allyRingMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_AllyRing.mat");
        if (enemyRingMaterial == null)
            enemyRingMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_EnemyRing.mat");
        if (healPlusMaterial == null)
            healPlusMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_HealPlus.mat");
        if (softGlowMaterial == null)
            softGlowMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_SoftGlow.mat");
#endif
    }

    private void OnValidate()
    {
        EnsureQuadMesh();
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
