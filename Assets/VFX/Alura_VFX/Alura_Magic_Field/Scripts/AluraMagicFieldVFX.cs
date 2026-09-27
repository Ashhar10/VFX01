using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AluraMagicFieldVFX - Master Controller & Unified 4-Phase Necromantic Magic Field VFX for Alura.
/// Matched to Storyboard Reference Image 2:
/// - Phase 1: START / TARGETING (2x2 glowing green floor grid, cyan ally ring with floating '+', red enemy ring, targeting scanner)
/// - Phase 2: CAST / FIELD ACTIVATION (4 rotating rune pads, surging energy columns: green for ally, purple for enemy)
/// - Phase 3: HEAL + DAMAGE EFFECT (Ally gets upward healing pillar, rising '+' crosses, sparkle diamonds; Enemy gets necrotic smoke miasma, rising skulls, downward dark energy strikes)
/// - Phase 4: RECOVERY / DISSIPATION (Grid & runes fade out smoothly, particles dissipate upward into air)
/// 
/// Real-time Features:
/// - Plays automatically on Enable (toggle On/Off to replay).
/// - Live updates in Scene View when editing values in Inspector.
/// - Dedicated Bloom / Emissive intensity controls.
/// - 100% Edit-Mode Safe: ZERO renderer.material leaks!
/// </summary>
[ExecuteAlways]
[SelectionBase]
[DisallowMultipleComponent]
public class AluraMagicFieldVFX : MonoBehaviour
{
    public enum PlaybackMode
    {
        RunFullSequence,
        RunSoloStage,
        ManualTriggerOnly
    }

    public enum SoloStage
    {
        Stage1_Targeting = 1,
        Stage2_Activation = 2,
        Stage3_HealAndDamage = 3,
        Stage4_Dissipation = 4
    }

    [Header("=== Playback Controls ===")]
    [Tooltip("Choose whether to run all stages in sequence or run a single stage solo.")]
    [SerializeField] private PlaybackMode playbackMode = PlaybackMode.RunFullSequence;

    [Tooltip("Which stage to run when Playback Mode is set to 'RunSoloStage'.")]
    [SerializeField] private SoloStage soloStage = SoloStage.Stage1_Targeting;

    [Tooltip("Continuously loop the full sequence (showcase/review mode).")]
    [SerializeField] private bool loopSequence = false;

    [Header("=== Bloom / Emissive Intensity Controls ===")]
    [Tooltip("Global Bloom & Emissive Glow intensity multiplier. Controls HDR bloom intensity across all elements.")]
    [SerializeField, Range(0.5f, 15f)] private float bloomIntensity = 4.2f;

    [Header("=== Global Field Settings ===")]
    [Tooltip("Total outer dimension of the 2x2 magic field in world units.")]
    [SerializeField, Range(2f, 10f)] private float fieldSize = 4.2f;

    [Header("=== Phase 1: Targeting Grid & Highlights ===")]
    [Tooltip("Grid line color for the 2x2 field.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color gridColor = new Color(0.25f, 1.0f, 0.55f, 1.0f);

    [Tooltip("Ally tile offset relative to field center (Default: Left front tile).")]
    [SerializeField] private Vector3 allyTileOffset = new Vector3(-1.05f, 0f, 0f);

    [Tooltip("Enemy tile offset relative to field center (Default: Right front tile).")]
    [SerializeField] private Vector3 enemyTileOffset = new Vector3(1.05f, 0f, 0f);

    [Tooltip("Ally highlight ring & icon tint (Cyan).")]
    [ColorUsage(true, true)]
    [SerializeField] private Color allyHighlightColor = new Color(0.18f, 0.90f, 1.0f, 1.0f);

    [Tooltip("Enemy highlight ring tint (Crimson Red).")]
    [ColorUsage(true, true)]
    [SerializeField] private Color enemyHighlightColor = new Color(1.0f, 0.22f, 0.25f, 1.0f);

    [Header("=== Phase 2: Field Activation & Columns ===")]
    [Tooltip("Rune circle rotation speed in degrees per second.")]
    [SerializeField, Range(10f, 180f)] private float runeRotationSpeed = 65f;

    [Tooltip("Rune pad tint color.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color runeColor = new Color(0.70f, 1.0f, 0.85f, 1.0f);

    [Tooltip("Ally energy column color (Radiant Emerald/Cyan).")]
    [ColorUsage(true, true)]
    [SerializeField] private Color allyColumnColor = new Color(0.30f, 1.0f, 0.65f, 1.0f);

    [Tooltip("Enemy energy column color (Necromantic Violet).")]
    [ColorUsage(true, true)]
    [SerializeField] private Color enemyColumnColor = new Color(0.75f, 0.25f, 1.0f, 1.0f);

    [Header("=== Phase 3: Heal & Damage FX ===")]
    [Tooltip("Healing radiant cross color.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color healCrossColor = new Color(0.85f, 1.0f, 0.92f, 1.0f);

    [Tooltip("Damage skull tint color.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color damageSkullColor = new Color(0.92f, 0.45f, 1.0f, 1.0f);

    [Tooltip("Necrotic ground smoke color.")]
    [SerializeField] private Color necroticSmokeColor = new Color(0.48f, 0.15f, 0.75f, 0.70f);

    [Tooltip("Downward necrotic energy beam color.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color necroticStrikeColor = new Color(0.60f, 0.10f, 0.95f, 1.0f);

    [Header("=== Phase Timings (Seconds) ===")]
    [SerializeField, Range(0.5f, 4f)] private float stage1Duration = 1.3f;
    [SerializeField, Range(0.5f, 4f)] private float stage2Duration = 1.4f;
    [SerializeField, Range(1f, 6f)] private float stage3Duration = 2.5f;
    [SerializeField, Range(0.5f, 4f)] private float stage4FadeDuration = 1.6f;

    [Header("=== Assigned URP Materials ===")]
    [SerializeField] private Material gridMaterial;
    [SerializeField] private Material runeMaterial;
    [SerializeField] private Material healPlusMaterial;
    [SerializeField] private Material damageSkullMaterial;
    [SerializeField] private Material smokeMaterial;
    [SerializeField] private Material sparkMaterial;
    [SerializeField] private Material allyRingMaterial;
    [SerializeField] private Material enemyRingMaterial;
    [SerializeField] private Material softGlowMaterial;

    private Transform fxRoot;
    private Coroutine activeSequence;
    private readonly List<LineRenderer> activeLines = new List<LineRenderer>();
    private readonly List<GameObject> activeObjects = new List<GameObject>();
    private readonly List<ParticleSystem> activeParticleSystems = new List<ParticleSystem>();
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
        if (playbackMode == PlaybackMode.RunFullSequence)
            PlayFullSequence();
        else if (playbackMode == PlaybackMode.RunSoloStage)
            PlayStage((int)soloStage);
    }

    private void OnDisable()
    {
        StopAndClear();
    }

    private void OnDestroy()
    {
        StopAndClear();
    }

    private void OnApplicationQuit()
    {
        CleanupHierarchy();
    }

    #region Context Menu & Public Controls

    [ContextMenu("Play Full Sequence (1 -> 4)")]
    public void PlayFullSequence()
    {
        StopAndClear();
        activeSequence = StartCoroutine(FullSequenceRoutine());
    }

    [ContextMenu("Play Stage 1 (Targeting)")]
    public void PlayStage1Solo() => PlayStage(1);

    [ContextMenu("Play Stage 2 (Activation)")]
    public void PlayStage2Solo() => PlayStage(2);

    [ContextMenu("Play Stage 3 (Heal & Damage)")]
    public void PlayStage3Solo() => PlayStage(3);

    [ContextMenu("Play Stage 4 (Dissipation)")]
    public void PlayStage4Solo() => PlayStage(4);

    public void PlayStage(int stageNumber)
    {
        StopAndClear();
        activeSequence = StartCoroutine(SoloStageRoutine(stageNumber));
    }

    [ContextMenu("Stop & Clear Field")]
    public void StopAndClear()
    {
        if (activeSequence != null)
        {
            StopCoroutine(activeSequence);
            activeSequence = null;
        }
        CleanupHierarchy();
    }

    [ContextMenu("Refresh Live View")]
    public void RefreshLiveView()
    {
        if (fxRoot == null)
        {
            BuildGridGeometry();
            SpawnHighlightRings();
            UpdateLiveParameters();
        }
        else
        {
            UpdateLiveParameters();
        }
    }

    #endregion

    #region Sequence Coroutines

    private IEnumerator FullSequenceRoutine()
    {
        do
        {
            CreateFreshRoot("ALURA_MAGIC_FIELD_ACTIVE");

            yield return Stage1Targeting();
            yield return Stage2Activation();
            yield return Stage3HealAndDamage();
            yield return Stage4Dissipation();

            yield return new WaitForSeconds(0.2f);
            CleanupHierarchy();

        } while (loopSequence && Application.isPlaying);
    }

    private IEnumerator SoloStageRoutine(int stage)
    {
        CreateFreshRoot("ALURA_MAGIC_FIELD_STAGE_" + stage);
        switch (stage)
        {
            case 1:
                yield return Stage1Targeting();
                break;
            case 2:
                BuildGridGeometry();
                yield return Stage2Activation();
                break;
            case 3:
                BuildGridGeometry();
                SpawnRunePads(1f);
                yield return Stage3HealAndDamage();
                break;
            case 4:
                BuildGridGeometry();
                SpawnRunePads(1f);
                yield return Stage4Dissipation();
                CleanupHierarchy();
                break;
        }
    }

    #endregion

    #region Phase 1: Start / Targeting

    private IEnumerator Stage1Targeting()
    {
        BuildGridGeometry();
        SpawnHighlightRings();
        SpawnFloatingCrossIcon(allyTileOffset + new Vector3(0.55f, 0.85f, 0f), 0.35f, allyHighlightColor, stage1Duration + 0.5f);
        SpawnScannerParticles();
        UpdateLiveParameters();

        yield return FadeLineAlpha(0f, 1f, 0.45f);
        yield return new WaitForSeconds(stage1Duration);
    }

    #endregion

    #region Phase 2: Cast / Field Activation

    private IEnumerator Stage2Activation()
    {
        SpawnRunePads(1f);
        SpawnSurgingColumns();
        SpawnExpandingCircles();
        SpawnEnergyBurst(transform.position, runeColor, 85, 3.2f);
        UpdateLiveParameters();

        yield return new WaitForSeconds(stage2Duration);
    }

    #endregion

    #region Phase 3: Heal + Damage Effect

    private IEnumerator Stage3HealAndDamage()
    {
        Vector3 allyPos = transform.position + allyTileOffset;
        SpawnHealingAura(allyPos);
        SpawnRisingHealCrossParticles(allyPos);
        SpawnSparkleDiamonds(allyPos, allyColumnColor, 35);

        Vector3 enemyPos = transform.position + enemyTileOffset;
        SpawnNecroticAura(enemyPos);
        SpawnNecroticMiasmaSmoke(enemyPos);
        SpawnRisingSpectralSkullParticles(enemyPos);
        SpawnDownwardNecroticStrikes(enemyPos);

        SpawnArcaneGroundFog();
        SpawnCellPulsePads();
        UpdateLiveParameters();

        yield return new WaitForSeconds(stage3Duration);
    }

    #endregion

    #region Phase 4: Recovery / Dissipation

    private IEnumerator Stage4Dissipation()
    {
        SpawnEnergyBurst(transform.position, runeColor, 120, 2.0f);
        SpawnDissipationMotes();
        UpdateLiveParameters();
        yield return FadeAllElements(1f, 0f, stage4FadeDuration);
    }

    #endregion

    #region Geometry & Particle Builders

    private void BuildGridGeometry()
    {
        float h = fieldSize * 0.5f;
        float y = 0.02f;

        CreateLine(new Vector3(-h, y, -h), new Vector3(h, y, -h), gridColor, 0.035f);
        CreateLine(new Vector3(h, y, -h), new Vector3(h, y, h), gridColor, 0.035f);
        CreateLine(new Vector3(h, y, h), new Vector3(-h, y, h), gridColor, 0.035f);
        CreateLine(new Vector3(-h, y, h), new Vector3(-h, y, -h), gridColor, 0.035f);

        CreateLine(new Vector3(0f, y, -h), new Vector3(0f, y, h), gridColor, 0.04f);
        CreateLine(new Vector3(-h, y, 0f), new Vector3(h, y, 0f), gridColor, 0.04f);

        float q = h * 0.65f;
        CreateLine(new Vector3(-q, y, -q), new Vector3(q, y, -q), gridColor, 0.015f);
        CreateLine(new Vector3(q, y, -q), new Vector3(q, y, q), gridColor, 0.015f);
        CreateLine(new Vector3(q, y, q), new Vector3(-q, y, q), gridColor, 0.015f);
        CreateLine(new Vector3(-q, y, q), new Vector3(-q, y, -q), gridColor, 0.015f);

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
        CreateCircle(allyTileOffset + Vector3.up * 0.03f, 0.55f, allyHighlightColor, 0.03f, 48, allyRingMaterial);
        CreateCircle(enemyTileOffset + Vector3.up * 0.03f, 0.55f, enemyHighlightColor, 0.03f, 48, enemyRingMaterial);
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
        activeParticleSystems.Add(ps);
        ps.Play();
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
            StartCoroutine(RotateObjectRoutine(pad, runeRotationSpeed));
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
        activeParticleSystems.Add(ps);
        ps.Play();
    }

    private void SpawnExpandingCircles()
    {
        CreateCircle(Vector3.up * 0.04f, fieldSize * 0.44f, runeColor, 0.03f, 72, gridMaterial);
        CreateCircle(Vector3.up * 0.045f, fieldSize * 0.32f, allyColumnColor, 0.02f, 64, gridMaterial);
    }

    private void SpawnHealingAura(Vector3 worldCenter)
    {
        Vector3 localCenter = transform.InverseTransformPoint(worldCenter);
        CreateCircle(localCenter + Vector3.up * 0.035f, 0.85f, allyColumnColor, 0.03f, 56, allyRingMaterial);
        CreateCircle(localCenter + Vector3.up * 0.04f, 0.55f, Color.white, 0.018f, 48, allyRingMaterial);

        GameObject go = CreateChild("HealingAuraLight");
        go.transform.localPosition = localCenter;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, allyColumnColor);
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
        activeParticleSystems.Add(ps);
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
        activeParticleSystems.Add(ps);
        ps.Play();
    }

    private void SpawnFloatingCrossIcon(Vector3 localPos, float scale, Color col, float duration)
    {
        GameObject icon = CreateQuadObject("TargetingAllyCross", localPos, Quaternion.identity, Vector3.one * scale, healPlusMaterial, col);
        StartCoroutine(HoverIconRoutine(icon, duration));
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
        activeParticleSystems.Add(ps);
        ps.Play();
    }

    private void SpawnNecroticAura(Vector3 worldCenter)
    {
        Vector3 localCenter = transform.InverseTransformPoint(worldCenter);
        CreateCircle(localCenter + Vector3.up * 0.035f, 0.85f, enemyColumnColor, 0.03f, 56, enemyRingMaterial);
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
        activeParticleSystems.Add(ps);
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
        activeParticleSystems.Add(ps);
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
        activeParticleSystems.Add(ps);
        ps.Play();
    }

    private void SpawnCellPulsePads()
    {
        float h = fieldSize * 0.26f;
        Vector3[] padPositions = {
            new Vector3(-h, 0.04f, -h),
            new Vector3(h, 0.04f, -h),
            new Vector3(-h, 0.04f, h),
            new Vector3(h, 0.04f, h)
        };

        for (int i = 0; i < padPositions.Length; i++)
        {
            Color col = (padPositions[i].x < 0) ? allyColumnColor : enemyColumnColor;
            CreateCircle(padPositions[i], 0.35f, col, 0.02f, 36, gridMaterial);
        }
    }

    private void SpawnEnergyBurst(Vector3 worldPos, Color col, int count, float speed)
    {
        GameObject go = CreateChild("EnergyBurst");
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
        activeParticleSystems.Add(ps);
        ps.Play();
    }

    private void SpawnDissipationMotes()
    {
        GameObject go = CreateChild("DissipationMotes");
        ParticleSystem ps = go.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(runeColor, Color.white);
        main.maxParticles = 90;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 80) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(fieldSize, 0.1f, fieldSize);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        vol.y = new ParticleSystem.MinMaxCurve(0.5f, 2.0f);
        vol.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);

        ParticleSystemRenderer rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = sparkMaterial != null ? sparkMaterial : gridMaterial;
        activeParticleSystems.Add(ps);
        ps.Play();
    }

    public void UpdateLiveParameters()
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        // Update line renderers
        foreach (LineRenderer lr in activeLines)
        {
            if (lr != null)
            {
                lr.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                lr.SetPropertyBlock(propertyBlock);
            }
        }

        // Update quads
        foreach (GameObject obj in activeObjects)
        {
            if (obj != null)
            {
                MeshRenderer mr = obj.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetFloat("_Intensity", bloomIntensity);
                    mr.SetPropertyBlock(propertyBlock);
                }
            }
        }

        // Update particle systems
        foreach (ParticleSystem ps in activeParticleSystems)
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

    #endregion

    #region Helpers & Animation Routines

    private IEnumerator RotateObjectRoutine(GameObject obj, float speed)
    {
        while (obj != null)
        {
            obj.transform.Rotate(0f, 0f, speed * Time.deltaTime, Space.Self);
            yield return null;
        }
    }

    private IEnumerator HoverIconRoutine(GameObject icon, float duration)
    {
        Vector3 basePos = icon.transform.localPosition;
        float elapsed = 0f;

        while (icon != null && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float bob = Mathf.Sin(elapsed * 4.5f) * 0.06f;
            icon.transform.localPosition = basePos + Vector3.up * bob;
            yield return null;
        }
    }

    private IEnumerator FadeLineAlpha(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, t / duration);
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
            yield return null;
        }
    }

    private IEnumerator FadeAllElements(float from, float to, float duration)
    {
        float t = 0f;
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, t / duration);

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

            foreach (GameObject obj in activeObjects)
            {
                if (obj != null)
                {
                    Renderer r = obj.GetComponent<Renderer>();
                    if (r != null)
                    {
                        r.GetPropertyBlock(propertyBlock);
                        Color c = propertyBlock.GetColor("_BaseColor");
                        if (c == Color.clear && r.sharedMaterial != null)
                            c = r.sharedMaterial.color;
                        c.a = alpha;
                        propertyBlock.SetColor("_BaseColor", c);
                        propertyBlock.SetFloat("_Intensity", bloomIntensity);
                        r.SetPropertyBlock(propertyBlock);
                    }
                }
            }

            yield return null;
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
        GameObject go = CreateChild("FieldCircle");
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

        activeObjects.Add(go);
        return go;
    }

    private GameObject CreateChild(string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(fxRoot, false);
        return child;
    }

    private void CreateFreshRoot(string rootName)
    {
        CleanupHierarchy();
        GameObject rootGo = new GameObject(rootName);
        rootGo.hideFlags = HideFlags.DontSave;
        rootGo.transform.SetParent(transform, false);
        fxRoot = rootGo.transform;
    }

    [ContextMenu("Clean Stale Hierarchy")]
    public void CleanupHierarchy()
    {
        if (fxRoot != null)
        {
            SafeDestroy(fxRoot.gameObject);
            fxRoot = null;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null && child.name.StartsWith("ALURA_MAGIC_FIELD"))
            {
                SafeDestroy(child.gameObject);
            }
        }

        activeLines.Clear();
        activeObjects.Clear();
        activeParticleSystems.Clear();
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
        if (runeMaterial == null)
            runeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Magic_Field/Materials/M_Alura_MagicField_Rune.mat");
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
