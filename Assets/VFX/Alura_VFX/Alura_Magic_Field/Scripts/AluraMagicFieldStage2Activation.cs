using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AluraMagicFieldStage2Activation - Dedicated Stage 2: CAST / FIELD ACTIVATION VFX.
/// Matched to Storyboard Reference Image 2:
/// - 4 Spinning Rune Pads activate on the 4 tiles of the 2x2 field.
/// - Surging energy columns:
///   * Ally Tile: Radiant Emerald/Cyan energy column.
///   * Enemy Tile: Necromantic Violet energy column.
///   * Center: Arcane surge.
/// - Concentric shockwave circles & activation burst.
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
public class AluraMagicFieldStage2Activation : MonoBehaviour
{
    [Header("=== Global Dimension & Timings ===")]
    [Tooltip("Total outer dimension of the 2x2 field in world units.")]
    [SerializeField, Range(2f, 10f)] private float fieldSize = 4.2f;

    [Tooltip("Duration in seconds for the stage when played in sequence (0 = infinite / continuous).")]
    [SerializeField, Range(0f, 6f)] private float stageDuration = 2.2f;

    [Header("=== Bloom / Emissive Intensity Controls ===")]
    [Tooltip("Global Bloom & Emissive Glow intensity multiplier for Stage 2.")]
    [SerializeField, Range(0.5f, 15f)] private float bloomIntensity = 4.0f;
    public float BloomIntensity { get => bloomIntensity; set { bloomIntensity = value; RefreshLiveView(); } }

    [Header("=== Rune Pads Configuration ===")]
    [Tooltip("Keep rune pads continuously rotating in a loop as long as the effect is active.")]
    [SerializeField] private bool loopRotation = true;

    [Tooltip("Rune circle rotation speed (degrees/sec).")]
    [SerializeField, Range(10f, 180f)] private float runeRotationSpeed = 65f;

    [Tooltip("Rune pad tint color.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color runeColor = new Color(0.70f, 1.0f, 0.85f, 1.0f);

    [Header("=== Surging Energy Columns ===")]
    [Tooltip("Ally energy column color (Radiant Emerald/Cyan).")]
    [ColorUsage(true, true)]
    [SerializeField] private Color allyColumnColor = new Color(0.30f, 1.0f, 0.65f, 1.0f);

    [Tooltip("Enemy energy column color (Necromantic Violet).")]
    [ColorUsage(true, true)]
    [SerializeField] private Color enemyColumnColor = new Color(0.75f, 0.25f, 1.0f, 1.0f);

    [Header("=== Assigned URP Materials ===")]
    [SerializeField] private Material runeMaterial;
    [SerializeField] private Material gridMaterial;
    [SerializeField] private Material sparkMaterial;
    [SerializeField] private Material softGlowMaterial;

    private Transform stageRoot;
    private Coroutine activeRoutine;
    private readonly List<GameObject> rotatingPads = new List<GameObject>();
    private readonly List<LineRenderer> activeLines = new List<LineRenderer>();
    private readonly List<ParticleSystem> activeParticles = new List<ParticleSystem>();
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

    private void Update()
    {
        if (rotatingPads.Count == 0) return;

        if (loopRotation || activeRoutine != null)
        {
            float dt = Application.isPlaying ? Time.deltaTime : 0.016f;
            foreach (GameObject pad in rotatingPads)
            {
                if (pad != null)
                    pad.transform.Rotate(0f, 0f, runeRotationSpeed * dt, Space.Self);
            }
        }
    }

    #region Context Menus & Public Controls

    [ContextMenu("Play Stage 2 (Activation)")]
    public void PlayStage()
    {
        StopStage();
        activeRoutine = StartCoroutine(Stage2Routine());
    }

    [ContextMenu("Stop Stage 2")]
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
        CreateFreshRoot("STAGE_2_ACTIVATION_CONTENT");
        SpawnRunePads(1f);
        SpawnSurgingColumns();
        SpawnConcentricCircles();
        SpawnEnergyBurst(transform.position, runeColor, 85, 3.2f);
        UpdateLiveParameters();
    }

    #endregion

    #region Routine & Builders

    private IEnumerator Stage2Routine()
    {
        BuildStaticHierarchy();

        if (stageDuration > 0f)
        {
            yield return new WaitForSeconds(stageDuration);
            if (!loopRotation)
            {
                activeRoutine = null;
            }
        }
        else
        {
            while (true) yield return null;
        }
    }

    private void SpawnRunePads(float scale)
    {
        float h = fieldSize * 0.26f;
        Vector3[] padPositions = {
            new Vector3(-h, 0.035f, -h),
            new Vector3(h, 0.035f, -h),
            new Vector3(-h, 0.035f, h),
            new Vector3(h, 0.035f, h)
        };

        foreach (Vector3 p in padPositions)
        {
            GameObject pad = CreateQuadObject("RunePad", p, Quaternion.Euler(90f, 0f, 0f), Vector3.one * (scale * 1.55f), runeMaterial, runeColor);
            rotatingPads.Add(pad);
        }
    }

    private void SpawnSurgingColumns()
    {
        float h = fieldSize * 0.26f;

        SpawnSingleColumn(new Vector3(-h, 0f, 0f), allyColumnColor, true);
        SpawnSingleColumn(new Vector3(h, 0f, 0f), enemyColumnColor, false);
        SpawnSingleColumn(Vector3.zero, runeColor, true);
    }

    private void SpawnSingleColumn(Vector3 localPos, Color col, bool upward)
    {
        GameObject go = CreateChild("SurgingColumn");
        go.transform.localPosition = localPos;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, col);
        main.maxParticles = 65;

        var emission = ps.emission;
        emission.rateOverTime = 45;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.22f;

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
        vol.y = upward ? new ParticleSystem.MinMaxCurve(1.6f, 4.0f) : new ParticleSystem.MinMaxCurve(1.2f, 3.5f);
        vol.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 0.8f;

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = softGlowMaterial != null ? softGlowMaterial : gridMaterial;
        activeParticles.Add(ps);
        ps.Play();
    }

    private void SpawnConcentricCircles()
    {
        CreateCircle(Vector3.up * 0.04f, fieldSize * 0.44f, runeColor, 0.03f, 72, gridMaterial);
        CreateCircle(Vector3.up * 0.045f, fieldSize * 0.32f, allyColumnColor, 0.02f, 64, gridMaterial);
    }

    private void SpawnEnergyBurst(Vector3 worldPos, Color col, int count, float speed)
    {
        GameObject go = CreateChild("ActivationBurst");
        go.transform.position = worldPos;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.3f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
        main.startColor = col;
        main.maxParticles = count;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

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

    public void UpdateLiveParameters()
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        // Update rune pads
        foreach (GameObject pad in rotatingPads)
        {
            if (pad != null)
            {
                MeshRenderer mr = pad.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetColor("_BaseColor", runeColor);
                    propertyBlock.SetFloat("_Intensity", bloomIntensity);
                    mr.SetPropertyBlock(propertyBlock);
                }
            }
        }

        // Update concentric circles
        foreach (LineRenderer lr in activeLines)
        {
            if (lr != null)
            {
                lr.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                lr.SetPropertyBlock(propertyBlock);
            }
        }

        // Update particle columns with bloom
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
        GameObject go = CreateChild("ConcentricCircle");
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

    private GameObject CreateQuadObject(string objName, Vector3 localPos, Quaternion localRot, Vector3 localScale, Material mat, Color col)
    {
        EnsureQuadMesh();
        GameObject go = CreateChild(objName);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale = localScale;

        MeshFilter mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = cachedQuadMesh;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat != null ? mat : gridMaterial;

        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        mr.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", col);
        propertyBlock.SetFloat("_Intensity", bloomIntensity);
        mr.SetPropertyBlock(propertyBlock);

        return go;
    }

    private GameObject CreateChild(string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(stageRoot, false);
        return child;
    }

    private const string CONTENT_ROOT_NAME = "STAGE_2_ACTIVATION_CONTENT";

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
            if (child != null && (child.name == CONTENT_ROOT_NAME || child.name.StartsWith("STAGE_2_ACTIVATION")))
            {
                SafeDestroy(child.gameObject);
            }
        }

        rotatingPads.Clear();
        activeLines.Clear();
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
        if (runeMaterial == null)
            runeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_Rune.mat");
        if (gridMaterial == null)
            gridMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_Grid.mat");
        if (sparkMaterial == null)
            sparkMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_Spark.mat");
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
