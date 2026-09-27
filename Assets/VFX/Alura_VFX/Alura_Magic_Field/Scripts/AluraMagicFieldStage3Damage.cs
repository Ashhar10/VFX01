using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AluraMagicFieldStage3Damage - Standalone Stage 3 Enemy Necrotic Damage VFX.
/// Matched to Storyboard Reference Image 2:
/// - Enemies take necrotic damage.
/// - Swirling dark violet & toxic green smoke miasma cloud.
/// - Rising spectral skulls with eerie drift and alpha fade.
/// - Downward dark energy strikes slamming into the enemy from above.
/// - Necrotic ground aura rings & ground fog.
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
public class AluraMagicFieldStage3Damage : MonoBehaviour
{
    [Header("=== Global Timings & Placement ===")]
    [Tooltip("Enemy center offset relative to this transform (Default: Right side).")]
    [SerializeField] private Vector3 enemyOffset = new Vector3(1.05f, 0f, 0f);

    [Tooltip("Duration in seconds for the damage effect (0 = infinite / continuous).")]
    [SerializeField, Range(0f, 8f)] private float duration = 3.0f;

    [Header("=== Bloom / Emissive Intensity Controls ===")]
    [Tooltip("Global Bloom & Emissive Glow intensity multiplier for the Damage VFX.")]
    [SerializeField, Range(0.5f, 15f)] private float bloomIntensity = 4.2f;
    public float BloomIntensity { get => bloomIntensity; set { bloomIntensity = value; RefreshLiveView(); } }

    [Header("=== Necrotic Damage Color Palette ===")]
    [Tooltip("Necrotic aura and column color (Violet/Purple).")]
    [ColorUsage(true, true)]
    [SerializeField] private Color enemyDamageColor = new Color(0.75f, 0.25f, 1.0f, 1.0f);

    [Tooltip("Rising spectral damage skull tint color.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color damageSkullColor = new Color(0.92f, 0.45f, 1.0f, 1.0f);

    [Tooltip("Swirling necrotic smoke miasma color.")]
    [SerializeField] private Color necroticSmokeColor = new Color(0.48f, 0.15f, 0.75f, 0.70f);

    [Tooltip("Downward dark necrotic strike beam color.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color necroticStrikeColor = new Color(0.60f, 0.10f, 0.95f, 1.0f);

    [Header("=== Particle Counts & Sizing ===")]
    [SerializeField, Range(4, 30)] private int skullRate = 8;
    [SerializeField, Range(8, 40)] private int smokeRate = 16;
    [SerializeField, Range(0.1f, 1.0f)] private float skullSize = 0.38f;
    [SerializeField, Range(0.5f, 3.0f)] private float auraRadius = 0.85f;

    [Header("=== Assigned URP Materials ===")]
    [SerializeField] private Material damageSkullMaterial;
    [SerializeField] private Material smokeMaterial;
    [SerializeField] private Material enemyRingMaterial;
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

    [ContextMenu("Play Damage VFX")]
    public void PlayStage()
    {
        StopStage();
        activeRoutine = StartCoroutine(DamageRoutine());
    }

    [ContextMenu("Stop Damage VFX")]
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
        CreateFreshRoot("STAGE_3_DAMAGE_CONTENT");

        Vector3 pos = transform.position + enemyOffset;
        SpawnNecroticAura(pos);
        SpawnNecroticMiasmaSmoke(pos);
        SpawnRisingSpectralSkulls(pos);
        SpawnDownwardNecroticStrikes(pos);
        SpawnArcaneGroundFog(pos);
        UpdateLiveParameters();
    }

    #endregion

    #region Routine & Builders

    private IEnumerator DamageRoutine()
    {
        BuildStaticHierarchy();

        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }
    }

    private void SpawnNecroticAura(Vector3 worldCenter)
    {
        Vector3 localCenter = transform.InverseTransformPoint(worldCenter);
        CreateCircle(localCenter + Vector3.up * 0.035f, auraRadius, enemyDamageColor, 0.03f, 56, enemyRingMaterial);
        CreateCircle(localCenter + Vector3.up * 0.04f, auraRadius * 0.65f, new Color(0.3f, 0.9f, 0.4f, 0.8f), 0.016f, 48, enemyRingMaterial);
    }

    private void SpawnNecroticMiasmaSmoke(Vector3 worldCenter)
    {
        GameObject go = CreateChild("NecroticMiasmaSmoke");
        go.transform.localPosition = transform.InverseTransformPoint(worldCenter);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.6f);
        main.startColor = new ParticleSystem.MinMaxGradient(necroticSmokeColor, new Color(0.2f, 0.8f, 0.35f, 0.45f));
        main.maxParticles = smokeRate * 3;

        var emission = ps.emission;
        emission.rateOverTime = smokeRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = auraRadius * 0.8f;

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
        vol.y = new ParticleSystem.MinMaxCurve(0.1f, 0.55f);
        vol.z = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-45f * Mathf.Deg2Rad, 45f * Mathf.Deg2Rad);

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = smokeMaterial != null ? smokeMaterial : gridMaterial;
        activeParticles.Add(ps);
        ps.Play();
    }

    private void SpawnRisingSpectralSkulls(Vector3 worldCenter)
    {
        GameObject go = CreateChild("RisingSpectralSkulls");
        go.transform.localPosition = transform.InverseTransformPoint(worldCenter);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(skullSize * 0.75f, skullSize * 1.25f);
        main.startColor = damageSkullColor;
        main.maxParticles = skullRate * 3;

        var emission = ps.emission;
        emission.rateOverTime = skullRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = auraRadius * 0.7f;

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        vol.y = new ParticleSystem.MinMaxCurve(0.6f, 1.5f);
        vol.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.65f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-35f * Mathf.Deg2Rad, 35f * Mathf.Deg2Rad);

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = damageSkullMaterial != null ? damageSkullMaterial : gridMaterial;
        activeParticles.Add(ps);
        ps.Play();
    }

    private void SpawnDownwardNecroticStrikes(Vector3 worldCenter)
    {
        Vector3 localCenter = transform.InverseTransformPoint(worldCenter);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * (Mathf.PI * 2f / 4f) + Random.Range(-0.2f, 0.2f);
            float r = Random.Range(0.15f, 0.45f);
            Vector3 bottom = localCenter + new Vector3(Mathf.Cos(angle) * r, 0.05f, Mathf.Sin(angle) * r);
            Vector3 top = bottom + new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(2.2f, 3.2f), Random.Range(-0.15f, 0.15f));

            CreateLine(top, bottom, necroticStrikeColor, 0.045f);
        }
    }

    private void SpawnArcaneGroundFog(Vector3 worldCenter)
    {
        GameObject go = CreateChild("NecroticGroundFog");
        go.transform.localPosition = transform.InverseTransformPoint(worldCenter);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
        main.startSpeed = 0.06f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 0.2f, 0.9f, 0.12f), new Color(0.2f, 0.8f, 0.4f, 0.10f));
        main.maxParticles = 25;

        var emission = ps.emission;
        emission.rateOverTime = 8;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = auraRadius * 0.9f;

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.x = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);
        vol.y = new ParticleSystem.MinMaxCurve(0.02f, 0.10f);
        vol.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = smokeMaterial != null ? smokeMaterial : gridMaterial;
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

        // Update particles with bloom
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
        GameObject go = CreateChild("DamageCircle");
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

    private void CreateLine(Vector3 start, Vector3 end, Color col, float width)
    {
        GameObject go = CreateChild("DamageStrikeLine");
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

    private GameObject CreateChild(string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(stageRoot, false);
        return child;
    }

    private const string CONTENT_ROOT_NAME = "STAGE_3_DAMAGE_CONTENT";

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
            if (child != null && (child.name == CONTENT_ROOT_NAME || child.name.StartsWith("STAGE_3_DAMAGE")))
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
        if (damageSkullMaterial == null)
            damageSkullMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_DamageSkull.mat");
        if (smokeMaterial == null)
            smokeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_Smoke.mat");
        if (enemyRingMaterial == null)
            enemyRingMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_EnemyRing.mat");
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
