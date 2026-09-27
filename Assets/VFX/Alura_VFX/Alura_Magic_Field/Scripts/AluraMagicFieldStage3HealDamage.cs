using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AluraMagicFieldStage3HealDamage - Combined Stage 3: HEAL + DAMAGE EFFECT VFX.
/// Matched to Storyboard Reference Image 2:
/// - Allies receive healing (radiant upward light, rising '+' crosses, sparkle diamonds, ground aura).
/// - Enemies take necrotic damage (swirling dark necrotic miasma/smoke, rising spectral skulls, downward dark energy strikes).
/// - Field-wide ground fog & pulsing cell pads.
/// 
/// Real-time Features:
/// - Plays automatically on Enable (toggle On/Off to replay).
/// - Live updates in Scene View when editing values in Inspector.
/// - Dedicated Bloom / Emissive intensity controls.
/// - Independent toggles for Heal and Damage layers.
/// - 100% Edit-Mode safe: Zero renderer.material leaks!
/// </summary>
[ExecuteAlways]
[SelectionBase]
[DisallowMultipleComponent]
public class AluraMagicFieldStage3HealDamage : MonoBehaviour
{
    [Header("=== Global Dimension & Timings ===")]
    [Tooltip("Total outer dimension of the 2x2 field in world units.")]
    [SerializeField, Range(2f, 10f)] private float fieldSize = 4.2f;

    [Tooltip("Duration in seconds for the stage when played in sequence (0 = infinite / continuous).")]
    [SerializeField, Range(0f, 8f)] private float stageDuration = 3.0f;

    [Header("=== Bloom / Emissive Intensity Controls ===")]
    [Tooltip("Global Bloom & Emissive Glow intensity multiplier for Stage 3.")]
    [SerializeField, Range(0.5f, 15f)] private float bloomIntensity = 4.5f;
    public float BloomIntensity { get => bloomIntensity; set { bloomIntensity = value; RefreshLiveView(); } }

    [Header("=== Layer Toggles ===")]
    [Tooltip("Enable Ally Healing effect (upward light, crosses, sparkles).")]
    [SerializeField] private bool enableHeal = true;

    [Tooltip("Enable Enemy Necrotic Damage effect (smoke, skulls, downward strikes).")]
    [SerializeField] private bool enableDamage = true;

    [Header("=== Ally Side: Upward Healing ===")]
    [Tooltip("Ally tile offset relative to field center (Default: Left tile).")]
    [SerializeField] private Vector3 allyTileOffset = new Vector3(-1.05f, 0f, 0f);

    [Tooltip("Healing light column & aura color.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color allyHealColor = new Color(0.30f, 1.0f, 0.65f, 1.0f);

    [Tooltip("Rising healing cross tint color.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color healCrossColor = new Color(0.85f, 1.0f, 0.92f, 1.0f);

    [Header("=== Enemy Side: Downward Necrotic Damage ===")]
    [Tooltip("Enemy tile offset relative to field center (Default: Right tile).")]
    [SerializeField] private Vector3 enemyTileOffset = new Vector3(1.05f, 0f, 0f);

    [Tooltip("Necrotic aura and column color.")]
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

    [Header("=== Assigned URP Materials ===")]
    [SerializeField] private Material healPlusMaterial;
    [SerializeField] private Material damageSkullMaterial;
    [SerializeField] private Material smokeMaterial;
    [SerializeField] private Material sparkMaterial;
    [SerializeField] private Material allyRingMaterial;
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

    [ContextMenu("Play Stage 3 (Heal & Damage)")]
    public void PlayStage()
    {
        StopStage();
        activeRoutine = StartCoroutine(Stage3Routine());
    }

    [ContextMenu("Stop Stage 3")]
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
        CreateFreshRoot("STAGE_3_HEAL_DAMAGE_CONTENT");

        // Ally Side
        if (enableHeal)
        {
            Vector3 allyPos = transform.position + allyTileOffset;
            SpawnHealingAura(allyPos);
            SpawnRisingHealCrossParticles(allyPos);
            SpawnSparkleDiamonds(allyPos, allyHealColor, 35);
        }

        // Enemy Side
        if (enableDamage)
        {
            Vector3 enemyPos = transform.position + enemyTileOffset;
            SpawnNecroticAura(enemyPos);
            SpawnNecroticMiasmaSmoke(enemyPos);
            SpawnRisingSpectralSkullParticles(enemyPos);
            SpawnDownwardNecroticStrikes(enemyPos);
        }

        // Field Ground Fog & Pulse Pads
        SpawnArcaneGroundFog();
        SpawnCellPulsePads();
        UpdateLiveParameters();
    }

    #endregion

    #region Routine & Builders

    private IEnumerator Stage3Routine()
    {
        BuildStaticHierarchy();

        if (stageDuration > 0f)
        {
            yield return new WaitForSeconds(stageDuration);
        }
    }

    private void SpawnHealingAura(Vector3 worldCenter)
    {
        Vector3 localCenter = transform.InverseTransformPoint(worldCenter);
        CreateCircle(localCenter + Vector3.up * 0.035f, 0.85f, allyHealColor, 0.03f, 56, allyRingMaterial);
        CreateCircle(localCenter + Vector3.up * 0.04f, 0.55f, Color.white, 0.018f, 48, allyRingMaterial);

        GameObject go = CreateChild("HealingAuraPillar");
        go.transform.localPosition = localCenter;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, allyHealColor);
        main.maxParticles = 90;

        var emission = ps.emission;
        emission.rateOverTime = 50;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.65f;

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

    private void SpawnRisingHealCrossParticles(Vector3 worldCenter)
    {
        GameObject go = CreateChild("RisingHealCrosses");
        go.transform.localPosition = transform.InverseTransformPoint(worldCenter);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.24f, 0.38f);
        main.startColor = healCrossColor;
        main.maxParticles = 25;

        var emission = ps.emission;
        emission.rateOverTime = 8;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.55f;

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

    private void SpawnSparkleDiamonds(Vector3 worldCenter, Color tint, int count)
    {
        GameObject go = CreateChild("ArcaneSparkles");
        go.transform.localPosition = transform.InverseTransformPoint(worldCenter);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, tint);
        main.maxParticles = count;

        var emission = ps.emission;
        emission.rateOverTime = 30;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

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

    private void SpawnNecroticAura(Vector3 worldCenter)
    {
        Vector3 localCenter = transform.InverseTransformPoint(worldCenter);
        CreateCircle(localCenter + Vector3.up * 0.035f, 0.85f, enemyDamageColor, 0.03f, 56, enemyRingMaterial);
        CreateCircle(localCenter + Vector3.up * 0.04f, 0.55f, new Color(0.3f, 0.9f, 0.4f, 0.8f), 0.016f, 48, enemyRingMaterial);
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
        main.maxParticles = 40;

        var emission = ps.emission;
        emission.rateOverTime = 16;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.7f;

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

    private void SpawnRisingSpectralSkullParticles(Vector3 worldCenter)
    {
        GameObject go = CreateChild("RisingSpectralSkulls");
        go.transform.localPosition = transform.InverseTransformPoint(worldCenter);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.46f);
        main.startColor = damageSkullColor;
        main.maxParticles = 20;

        var emission = ps.emission;
        emission.rateOverTime = 6;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.6f;

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

    private void SpawnArcaneGroundFog()
    {
        GameObject go = CreateChild("ArcaneGroundFog");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
        main.startSpeed = 0.06f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.2f, 0.9f, 0.5f, 0.12f), new Color(0.6f, 0.2f, 0.9f, 0.10f));
        main.maxParticles = 30;

        var emission = ps.emission;
        emission.rateOverTime = 10;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(fieldSize, 0.08f, fieldSize);

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

    private void SpawnCellPulsePads()
    {
        float h = fieldSize * 0.26f;
        Vector3[] padPositions = {
            new Vector3(-h, 0.04f, -h),
            new Vector3(h, 0.04f, -h),
            new Vector3(-h, 0.04f, -h),
            new Vector3(h, 0.04f, h)
        };

        for (int i = 0; i < padPositions.Length; i++)
        {
            Color col = (padPositions[i].x < 0) ? allyHealColor : enemyDamageColor;
            CreateCircle(padPositions[i], 0.35f, col, 0.02f, 36, gridMaterial);
        }
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
        GameObject go = CreateChild("AuraCircle");
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

    private const string CONTENT_ROOT_NAME = "STAGE_3_HEAL_DAMAGE_CONTENT";

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
            if (child != null && (child.name == CONTENT_ROOT_NAME || child.name.StartsWith("STAGE_3_HEAL_DAMAGE")))
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
        if (damageSkullMaterial == null)
            damageSkullMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_DamageSkull.mat");
        if (smokeMaterial == null)
            smokeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_Smoke.mat");
        if (sparkMaterial == null)
            sparkMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_Spark.mat");
        if (allyRingMaterial == null)
            allyRingMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_AllyRing.mat");
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
