using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AluraMagicFieldStage3Heal - Standalone Stage 3 Ally Healing VFX.
/// Matched to Storyboard Reference Image 2:
/// - Allies receive radiant upward healing.
/// - Upward glowing light pillar.
/// - Rising Heal '+' crosses with gentle sway and alpha fade.
/// - Orbiting and floating diamond sparkle motes (◆).
/// - Pulsing cyan/emerald ground aura rings.
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
public class AluraMagicFieldStage3Heal : MonoBehaviour
{
    [Header("=== Global Timings & Placement ===")]
    [Tooltip("Ally center offset relative to this transform (Default: Left side).")]
    [SerializeField] private Vector3 allyOffset = new Vector3(-1.05f, 0f, 0f);

    [Tooltip("Duration in seconds for the heal effect (0 = infinite / continuous).")]
    [SerializeField, Range(0f, 8f)] private float duration = 3.0f;

    [Header("=== Bloom / Emissive Intensity Controls ===")]
    [Tooltip("Global Bloom & Emissive Glow intensity multiplier for the Healing VFX.")]
    [SerializeField, Range(0.5f, 15f)] private float bloomIntensity = 4.5f;
    public float BloomIntensity { get => bloomIntensity; set { bloomIntensity = value; RefreshLiveView(); } }

    [Header("=== Healing Color Palette ===")]
    [Tooltip("Radiant emerald/cyan upward healing pillar color.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color healingPillarColor = new Color(0.30f, 1.0f, 0.65f, 1.0f);

    [Tooltip("Rising healing '+' cross color.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color healCrossColor = new Color(0.85f, 1.0f, 0.92f, 1.0f);

    [Tooltip("Diamond sparkles color (◆).")]
    [ColorUsage(true, true)]
    [SerializeField] private Color sparkleColor = new Color(0.95f, 1.0f, 0.98f, 1.0f);

    [Tooltip("Ground aura ring color.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color groundAuraColor = new Color(0.20f, 0.90f, 0.70f, 1.0f);

    [Header("=== Particle Counts & Sizing ===")]
    [SerializeField, Range(5, 50)] private int healCrossRate = 12;
    [SerializeField, Range(10, 80)] private int sparkleRate = 35;
    [SerializeField, Range(0.1f, 1.0f)] private float crossSize = 0.32f;
    [SerializeField, Range(0.5f, 3.0f)] private float auraRadius = 0.85f;

    [Header("=== Assigned URP Materials ===")]
    [SerializeField] private Material healPlusMaterial;
    [SerializeField] private Material sparkMaterial;
    [SerializeField] private Material allyRingMaterial;
    [SerializeField] private Material gridMaterial;
    [SerializeField] private Material softGlowMaterial;

    private Transform stageRoot;
    private Coroutine activeRoutine;
    private readonly List<LineRenderer> activeLines = new List<LineRenderer>();
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

    [ContextMenu("Play Heal VFX")]
    public void PlayStage()
    {
        StopStage();
        activeRoutine = StartCoroutine(HealRoutine());
    }

    [ContextMenu("Stop Heal VFX")]
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
        CreateFreshRoot("STAGE_3_HEAL_CONTENT");

        Vector3 pos = transform.position + allyOffset;
        SpawnHealingAura(pos);
        SpawnRisingHealCrosses(pos);
        SpawnSparkleDiamonds(pos);
        UpdateLiveParameters();
    }

    #endregion

    #region Routine & Builders

    private IEnumerator HealRoutine()
    {
        BuildStaticHierarchy();

        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }
    }

    private void SpawnHealingAura(Vector3 worldCenter)
    {
        Vector3 localCenter = transform.InverseTransformPoint(worldCenter);
        CreateCircle(localCenter + Vector3.up * 0.035f, auraRadius, groundAuraColor, 0.03f, 56, allyRingMaterial);
        CreateCircle(localCenter + Vector3.up * 0.04f, auraRadius * 0.65f, Color.white, 0.018f, 48, allyRingMaterial);

        GameObject go = CreateChild("HealingPillar");
        go.transform.localPosition = localCenter;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, healingPillarColor);
        main.maxParticles = 90;

        var emission = ps.emission;
        emission.rateOverTime = 50;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = auraRadius * 0.75f;

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        vol.y = new ParticleSystem.MinMaxCurve(0.8f, 2.4f);
        vol.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 0.6f;

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = softGlowMaterial != null ? softGlowMaterial : gridMaterial;
        activeParticles.Add(ps);
        ps.Play();
    }

    private void SpawnRisingHealCrosses(Vector3 worldCenter)
    {
        GameObject go = CreateChild("RisingHealCrosses");
        go.transform.localPosition = transform.InverseTransformPoint(worldCenter);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(crossSize * 0.75f, crossSize * 1.25f);
        main.startColor = healCrossColor;
        main.maxParticles = healCrossRate * 3;

        var emission = ps.emission;
        emission.rateOverTime = healCrossRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = auraRadius * 0.65f;

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
        vol.y = new ParticleSystem.MinMaxCurve(0.7f, 1.6f);
        vol.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-25f * Mathf.Deg2Rad, 25f * Mathf.Deg2Rad);

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = healPlusMaterial != null ? healPlusMaterial : gridMaterial;
        activeParticles.Add(ps);
        ps.Play();
    }

    private void SpawnSparkleDiamonds(Vector3 worldCenter)
    {
        GameObject go = CreateChild("SparkleDiamonds");
        go.transform.localPosition = transform.InverseTransformPoint(worldCenter);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, sparkleColor);
        main.maxParticles = sparkleRate * 2;

        var emission = ps.emission;
        emission.rateOverTime = sparkleRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = auraRadius * 0.6f;

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        vol.y = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);
        vol.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = sparkMaterial != null ? sparkMaterial : gridMaterial;
        activeParticles.Add(ps);
        ps.Play();
    }

    public void UpdateLiveParameters()
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        // Update lines with bloom
        foreach (LineRenderer lr in activeLines)
        {
            if (lr != null)
            {
                lr.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                lr.SetPropertyBlock(propertyBlock);
            }
        }

        // Update particle systems
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
        GameObject go = CreateChild("HealCircle");
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

    private GameObject CreateChild(string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(stageRoot, false);
        return child;
    }

    private const string CONTENT_ROOT_NAME = "STAGE_3_HEAL_CONTENT";

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
            if (child != null && (child.name == CONTENT_ROOT_NAME || child.name.StartsWith("STAGE_3_HEAL")))
            {
                SafeDestroy(child.gameObject);
            }
        }

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

    private void ValidateMaterials()
    {
#if UNITY_EDITOR
        if (healPlusMaterial == null)
            healPlusMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_HealPlus.mat");
        if (sparkMaterial == null)
            sparkMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_Spark.mat");
        if (allyRingMaterial == null)
            allyRingMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_AllyRing.mat");
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
