using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AstridBouquetStage3ShrapnelExplosion - Stage 3: Petal Shrapnel Explosion & 4-Way Spinning Vortex.
/// Storyboard Ref: Bouquet erupts! Petals explode and scatter in 4 horizontal sides with a spinning loop.
/// VFX Scenario 3:
/// - 4 directional horizontal arms (0°, 90°, 180°, 270°) continuously spinning in a loop around the Y-axis.
/// - Scatters petals strictly in the horizontal XZ plane, forming 4 expanding curved vortex arms outward.
/// - Particles stay on the wind: aerodynamic braking (limitVelocityOverLifetime) decelerates them into a gentle hover,
///   while Perlin noise turbulence creates realistic organic wind flutter.
/// - Automatic dissolve: alpha and scale gracefully fade out over particle lifetime into thin air.
/// - Supporting visuals: expanding horizontal ground floral shockwave ring and core flash.
/// - Coordinated dissolve: provides public DissolveAndDisappear(float duration) called by Stage 4.
/// 
/// Real-time Features:
/// - Instant play/replay on Enable/Disable (toggle GameObject in Hierarchy).
/// - Live updates in Scene View on Inspector property changes (OnValidate + delayCall).
/// - Dedicated Bloom & Emissive Intensity slider.
/// - Zero material leak (uses sharedMaterial & MaterialPropertyBlock).
/// - HideFlags.DontSave prevents duplicate accumulation.
/// </summary>
[ExecuteAlways]
[SelectionBase]
[DisallowMultipleComponent]
public class AstridBouquetStage3ShrapnelExplosion : MonoBehaviour
{
    private const string CONTENT_ROOT_NAME = "STAGE_3_SHRAPNEL_EXPLOSION_CONTENT";

    [Header("=== 4-Way Horizontal Spinning Vortex ===")]
    [Tooltip("Continuously spin and emit petals in a loop.")]
    [SerializeField] private bool loopSpinning = true;

    [Tooltip("Rotation speed of the 4 horizontal arms (degrees/second).")]
    [SerializeField, Range(30f, 540f)] private float spinSpeed = 160f;

    [Tooltip("Spin clockwise (true) or counter-clockwise (false).")]
    [SerializeField] private bool spinClockwise = true;

    [Tooltip("Petals emitted per arm nozzle per second.")]
    [SerializeField, Range(10, 100)] private int petalsPerArmRate = 35;

    [Tooltip("Initial outward horizontal ejection velocity.")]
    [SerializeField, Range(3f, 22f)] private float horizontalSpeed = 9.0f;

    [Tooltip("Base size of single petals.")]
    [SerializeField, Range(0.1f, 0.8f)] private float petalSize = 0.28f;

    [Tooltip("Lifetime of floating petals in seconds before dissolving.")]
    [SerializeField, Range(1.5f, 6.0f)] private float petalLifetime = 3.2f;

    [Tooltip("Gentle gravity (keep very low so petals hover/stay on the wind).")]
    [SerializeField, Range(0.0f, 0.05f)] private float gravityStrength = 0.012f;

    [Tooltip("Aerodynamic drag dampen (rapidly brakes initial fling into a hover).")]
    [SerializeField, Range(0.1f, 0.8f)] private float dragDampen = 0.38f;

    [Tooltip("Minimum floating / hover speed after drag dampening.")]
    [SerializeField, Range(0.2f, 2.5f)] private float hoverSpeed = 0.65f;

    [Tooltip("Wind turbulence / noise flutter intensity.")]
    [SerializeField, Range(0.0f, 1.5f)] private float windTurbulence = 0.45f;

    [Tooltip("Center origin offset (waist/chest height around Astrid).")]
    [SerializeField] private Vector3 centerOffset = new Vector3(0f, 0.7f, 0f);

    [Header("=== Petal Cluster Settings ===")]
    [Tooltip("Include larger floating petal clusters alongside single petals.")]
    [SerializeField] private bool includeClusters = true;

    [Tooltip("Petal clusters emitted per arm nozzle per second.")]
    [SerializeField, Range(2, 35)] private int clustersPerArmRate = 10;

    [Header("=== Shockwave & Core Flash ===")]
    [Tooltip("Trigger ground floral shockwave ring on play.")]
    [SerializeField] private bool includeFloorShockwave = true;

    [Tooltip("Radius of the expanding floral shockwave ring.")]
    [SerializeField, Range(1.5f, 6.0f)] private float shockwaveRadius = 3.8f;

    [Tooltip("Trigger core energy flash on play.")]
    [SerializeField] private bool includeCoreFlash = true;

    [Header("=== Bloom / Emissive Intensity Controls ===")]
    [Tooltip("Global Bloom & Emissive Glow intensity multiplier for the explosion.")]
    [SerializeField, Range(0.5f, 15f)] private float bloomIntensity = 4.5f;
    public float BloomIntensity { get => bloomIntensity; set { bloomIntensity = value; RefreshLiveView(); } }

    [Header("=== Color Palette ===")]
    [Tooltip("Intense Cherry Blossom Blast Pink.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color explosionPink = new Color(1.0f, 0.35f, 0.65f, 1.0f);

    [Tooltip("Hot Core Magenta Shrapnel.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color coreMagenta = new Color(0.85f, 0.15f, 0.50f, 1.0f);

    [Tooltip("Bright White/Sakura Flash Spark.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color flashWhite = new Color(1.0f, 0.95f, 0.98f, 1.0f);

    [Header("=== Assigned URP Materials ===")]
    [SerializeField] private Material singlePetalAddMat;
    [SerializeField] private Material clusterPetalAddMat;
    [SerializeField] private Material floralShockwaveMat;
    [SerializeField] private Material softGlowMat;

    private Transform stageRoot;
    private Transform spinningRoot;
    private Coroutine activeRoutine;
    private Coroutine dissolveRoutine;
    private bool isBurstSpinning = false;

    private readonly List<ParticleSystem> armSinglePetalPSList = new List<ParticleSystem>();
    private readonly List<ParticleSystem> armClusterPetalPSList = new List<ParticleSystem>();
    private readonly List<ParticleSystem> allParticleSystems = new List<ParticleSystem>();
    private ParticleSystem shockwavePS;
    private ParticleSystem flashPS;
    private MaterialPropertyBlock propertyBlock;

    public bool IsDissolving { get; private set; }

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
        if (spinningRoot != null && (loopSpinning || isBurstSpinning))
        {
            float dt = Application.isPlaying ? Time.deltaTime : 0.016f;
            float dir = spinClockwise ? 1f : -1f;
            spinningRoot.Rotate(0f, dir * spinSpeed * dt, 0f, Space.Self);
        }
    }

    #region Public Playback Controls

    [ContextMenu("Play 4-Way Spinning Shrapnel Explosion")]
    public void PlayStage()
    {
        StopStage();
        IsDissolving = false;
        activeRoutine = StartCoroutine(ExplosionRoutine());
    }

    [ContextMenu("Stop Stage 3")]
    public void StopStage()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
        if (dissolveRoutine != null)
        {
            StopCoroutine(dissolveRoutine);
            dissolveRoutine = null;
        }
        IsDissolving = false;
        isBurstSpinning = false;
        CleanupHierarchy();
    }

    /// <summary>
    /// Smoothly dissolves and disappears the Stage 3 shrapnel explosion.
    /// Called directly by Stage 4 (Aftermath & Dissolve).
    /// </summary>
    /// <param name="fadeDuration">Seconds over which active petals and energy fade out.</param>
    public void DissolveAndDisappear(float fadeDuration = 1.2f)
    {
        if (!enabled || !gameObject.activeInHierarchy || stageRoot == null) return;
        if (dissolveRoutine != null) StopCoroutine(dissolveRoutine);
        dissolveRoutine = StartCoroutine(DissolveRoutine(fadeDuration));
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

        armSinglePetalPSList.Clear();
        armClusterPetalPSList.Clear();
        allParticleSystems.Clear();

        SpawnSpinningVortexSystem();
        if (includeFloorShockwave) SpawnFloralShockwave();
        if (includeCoreFlash) SpawnCoreFlash();

        UpdateLiveParameters();
    }

    #endregion

    #region Routines & Builders

    private IEnumerator ExplosionRoutine()
    {
        BuildStaticHierarchy();
        TriggerInstantBurst();

        if (loopSpinning)
        {
            // Continuous spinning loop & emission
            yield break;
        }
        else
        {
            isBurstSpinning = true;
            yield return new WaitForSeconds(petalLifetime + 0.5f);
            isBurstSpinning = false;
        }
    }

    public void TriggerInstantBurst()
    {
        foreach (var ps in allParticleSystems)
        {
            if (ps != null)
            {
                ps.Clear();
                ps.Play();
            }
        }
    }

    private IEnumerator DissolveRoutine(float duration)
    {
        IsDissolving = true;
        loopSpinning = false;
        isBurstSpinning = false;

        // 1. Immediately cut new emission on all particle systems
        foreach (var ps in allParticleSystems)
        {
            if (ps != null)
            {
                var em = ps.emission;
                em.enabled = false;
            }
        }

        // 2. Smoothly fade out bloom and existing particles over duration
        float elapsed = 0f;
        float startBloom = bloomIntensity;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float fadeFactor = 1.0f - t;

            if (propertyBlock != null)
            {
                propertyBlock.SetFloat("_Intensity", startBloom * fadeFactor);
                foreach (var ps in allParticleSystems)
                {
                    if (ps != null) ps.GetComponent<ParticleSystemRenderer>()?.SetPropertyBlock(propertyBlock);
                }
            }

            yield return null;
        }

        // 3. Fully clear
        CleanupHierarchy();
        IsDissolving = false;
        dissolveRoutine = null;
    }

    private void SpawnSpinningVortexSystem()
    {
        GameObject vortexGo = CreateChild("SpinningArmVortex");
        vortexGo.transform.localPosition = centerOffset;
        spinningRoot = vortexGo.transform;

        // 4 Horizontal Directions (0°, 90°, 180°, 270°)
        float[] angles = new float[] { 0f, 90f, 180f, 270f };
        string[] armNames = new string[] { "Arm_0_North", "Arm_1_East", "Arm_2_South", "Arm_3_West" };

        for (int i = 0; i < 4; i++)
        {
            GameObject armGo = new GameObject(armNames[i]);
            armGo.transform.SetParent(spinningRoot, false);
            armGo.transform.localRotation = Quaternion.Euler(0f, angles[i], 0f);
            armGo.transform.localPosition = armGo.transform.forward * 0.25f;

            // Single Petal horizontal beam
            SpawnArmSinglePetals(armGo, i);

            // Cluster Petal horizontal beam
            if (includeClusters)
            {
                SpawnArmClusterPetals(armGo, i);
            }
        }
    }

    private void SpawnArmSinglePetals(GameObject armGo, int armIndex)
    {
        GameObject child = new GameObject($"Arm_{armIndex}_SinglePetals");
        child.transform.SetParent(armGo.transform, false);

        ParticleSystem ps = child.AddComponent<ParticleSystem>();
        armSinglePetalPSList.Add(ps);
        allParticleSystems.Add(ps);

        var main = ps.main;
        main.loop = loopSpinning;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(petalLifetime * 0.8f, petalLifetime * 1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(horizontalSpeed * 0.75f, horizontalSpeed * 1.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(petalSize * 0.8f, petalSize * 1.25f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier = gravityStrength;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        // Dynamic Dissolve Gradient (Sakura Pink to White Flash to Deep Magenta, alpha dissolves to 0)
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(flashWhite, 0.0f),
                new GradientColorKey(explosionPink, 0.25f),
                new GradientColorKey(coreMagenta, 0.85f)
            },
            new[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.10f),
                new GradientAlphaKey(0.95f, 0.65f), // stays visible while floating on the wind
                new GradientAlphaKey(0.0f, 1.0f)    // automatically dissolves away
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        // Emission
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = petalsPerArmRate;
        if (!loopSpinning)
        {
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.0f, (short)(petalsPerArmRate * 2)) });
        }

        // Shape: Narrow cone pointing strictly forward in horizontal plane
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 6.0f; // strictly narrow horizontal scatter
        shape.radius = 0.12f;
        shape.radiusThickness = 0.4f;

        // Limit Velocity Over Lifetime: Aerodynamic drag deceleration so petals stay floating on wind
        var limitVel = ps.limitVelocityOverLifetime;
        limitVel.enabled = true;
        limitVel.limit = new ParticleSystem.MinMaxCurve(hoverSpeed);
        limitVel.dampen = dragDampen;

        // Noise module: Wind turbulence & natural breeze flutter
        if (windTurbulence > 0.01f)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = windTurbulence;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.6f;
            noise.damping = true;
            noise.octaveCount = 1;
        }

        // Rotation Over Lifetime: 3D tumbling rotation
        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.separateAxes = true;
        rot.x = new ParticleSystem.MinMaxCurve(-3.5f, 3.5f);
        rot.y = new ParticleSystem.MinMaxCurve(-4.0f, 4.0f);
        rot.z = new ParticleSystem.MinMaxCurve(-5.0f, 5.0f);

        // Size Over Lifetime: Born, slight flutter, graceful shrink before dissolve
        var sizeLife = ps.sizeOverLifetime;
        sizeLife.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.5f);
        curve.AddKey(0.12f, 1.15f);
        curve.AddKey(0.68f, 0.95f);
        curve.AddKey(1.0f, 0.05f); // dissolves automatically
        sizeLife.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        // Renderer
        var rend = child.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = singlePetalAddMat;
        rend.sortMode = ParticleSystemSortMode.Distance;

        ps.Play();
    }

    private void SpawnArmClusterPetals(GameObject armGo, int armIndex)
    {
        GameObject child = new GameObject($"Arm_{armIndex}_ClusterPetals");
        child.transform.SetParent(armGo.transform, false);

        ParticleSystem ps = child.AddComponent<ParticleSystem>();
        armClusterPetalPSList.Add(ps);
        allParticleSystems.Add(ps);

        var main = ps.main;
        main.loop = loopSpinning;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(petalLifetime * 0.75f, petalLifetime * 1.15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(horizontalSpeed * 0.65f, horizontalSpeed * 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(petalSize * 1.4f, petalSize * 2.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier = gravityStrength * 1.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] {
                new GradientColorKey(explosionPink, 0.0f),
                new GradientColorKey(coreMagenta, 0.75f)
            },
            new[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.12f),
                new GradientAlphaKey(0.95f, 0.60f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        main.startColor = new ParticleSystem.MinMaxGradient(grad);

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = clustersPerArmRate;
        if (!loopSpinning)
        {
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.0f, (short)(clustersPerArmRate * 2)) });
        }

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 8.0f;
        shape.radius = 0.15f;

        var limitVel = ps.limitVelocityOverLifetime;
        limitVel.enabled = true;
        limitVel.limit = new ParticleSystem.MinMaxCurve(hoverSpeed * 0.9f);
        limitVel.dampen = dragDampen * 1.05f;

        if (windTurbulence > 0.01f)
        {
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = windTurbulence * 0.85f;
            noise.frequency = 0.35f;
            noise.scrollSpeed = 0.5f;
            noise.damping = true;
        }

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.separateAxes = true;
        rot.x = new ParticleSystem.MinMaxCurve(-3.0f, 3.0f);
        rot.y = new ParticleSystem.MinMaxCurve(-3.5f, 3.5f);
        rot.z = new ParticleSystem.MinMaxCurve(-4.0f, 4.0f);

        var sizeLife = ps.sizeOverLifetime;
        sizeLife.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.6f);
        curve.AddKey(0.15f, 1.2f);
        curve.AddKey(0.70f, 0.9f);
        curve.AddKey(1.0f, 0.0f);
        sizeLife.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        var rend = child.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = clusterPetalAddMat;
        rend.sortMode = ParticleSystemSortMode.Distance;

        ps.Play();
    }

    private void SpawnFloralShockwave()
    {
        GameObject go = CreateChild("FloralShockwaveRing");
        go.transform.localPosition = new Vector3(centerOffset.x, 0.04f, centerOffset.z);
        shockwavePS = go.AddComponent<ParticleSystem>();
        allParticleSystems.Add(shockwavePS);

        var main = shockwavePS.main;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = 0.85f;
        main.startSpeed = 0f;
        main.startSize = shockwaveRadius;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = explosionPink;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = shockwavePS.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.0f, 1) });

        var sizeLife = shockwavePS.sizeOverLifetime;
        sizeLife.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.15f);
        curve.AddKey(0.5f, 0.85f);
        curve.AddKey(1.0f, 1.25f);
        sizeLife.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        var colorLife = shockwavePS.colorOverLifetime;
        colorLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(flashWhite, 0f), new GradientColorKey(explosionPink, 0.5f) },
            new[] { new GradientAlphaKey(1.0f, 0f), new GradientAlphaKey(0.85f, 0.4f), new GradientAlphaKey(0f, 1.0f) }
        );
        colorLife.color = new ParticleSystem.MinMaxGradient(grad);

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
        rend.sharedMaterial = floralShockwaveMat;

        shockwavePS.Play();
    }

    private void SpawnCoreFlash()
    {
        GameObject go = CreateChild("CoreExplosionFlash");
        go.transform.localPosition = centerOffset;
        flashPS = go.AddComponent<ParticleSystem>();
        allParticleSystems.Add(flashPS);

        var main = flashPS.main;
        main.loop = false;
        main.playOnAwake = true;
        main.startLifetime = 0.4f;
        main.startSpeed = 0f;
        main.startSize = petalSize * 7.5f;
        main.startColor = flashWhite;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = flashPS.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0.0f, 1) });

        var sizeLife = flashPS.sizeOverLifetime;
        sizeLife.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 0.3f);
        curve.AddKey(0.2f, 1.3f);
        curve.AddKey(1.0f, 0.0f);
        sizeLife.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.sharedMaterial = softGlowMat;

        flashPS.Play();
    }

    #endregion

    #region Live Inspector Refresh & Cleanup

    public void UpdateLiveParameters()
    {
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

        foreach (var ps in armSinglePetalPSList)
        {
            if (ps != null)
            {
                var main = ps.main;
                main.startSpeed = new ParticleSystem.MinMaxCurve(horizontalSpeed * 0.75f, horizontalSpeed * 1.25f);
                main.startSize = new ParticleSystem.MinMaxCurve(petalSize * 0.8f, petalSize * 1.25f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(petalLifetime * 0.8f, petalLifetime * 1.25f);
                main.gravityModifier = gravityStrength;

                var em = ps.emission;
                em.rateOverTime = petalsPerArmRate;

                var lim = ps.limitVelocityOverLifetime;
                lim.limit = new ParticleSystem.MinMaxCurve(hoverSpeed);
                lim.dampen = dragDampen;

                var noise = ps.noise;
                noise.strength = windTurbulence;

                var rend = ps.GetComponent<ParticleSystemRenderer>();
                if (rend != null)
                {
                    rend.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetFloat("_Intensity", bloomIntensity);
                    rend.SetPropertyBlock(propertyBlock);
                }
            }
        }

        foreach (var ps in armClusterPetalPSList)
        {
            if (ps != null)
            {
                var main = ps.main;
                main.startSpeed = new ParticleSystem.MinMaxCurve(horizontalSpeed * 0.65f, horizontalSpeed * 1.1f);
                main.startSize = new ParticleSystem.MinMaxCurve(petalSize * 1.4f, petalSize * 2.2f);
                main.startLifetime = new ParticleSystem.MinMaxCurve(petalLifetime * 0.75f, petalLifetime * 1.15f);
                main.gravityModifier = gravityStrength * 1.2f;

                var em = ps.emission;
                em.rateOverTime = clustersPerArmRate;

                var lim = ps.limitVelocityOverLifetime;
                lim.limit = new ParticleSystem.MinMaxCurve(hoverSpeed * 0.9f);
                lim.dampen = dragDampen * 1.05f;

                var noise = ps.noise;
                noise.strength = windTurbulence * 0.85f;

                var rend = ps.GetComponent<ParticleSystemRenderer>();
                if (rend != null)
                {
                    rend.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetFloat("_Intensity", bloomIntensity);
                    rend.SetPropertyBlock(propertyBlock);
                }
            }
        }

        if (shockwavePS != null)
        {
            var main = shockwavePS.main;
            main.startSize = shockwaveRadius;

            var rend = shockwavePS.GetComponent<ParticleSystemRenderer>();
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
            if (child != null && (child.name == CONTENT_ROOT_NAME || child.name.StartsWith("STAGE_3_SHRAPNEL_EXPLOSION")))
            {
                SafeDestroy(child.gameObject);
            }
        }

        armSinglePetalPSList.Clear();
        armClusterPetalPSList.Clear();
        allParticleSystems.Clear();
        shockwavePS = null;
        flashPS = null;
        spinningRoot = null;
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
        if (clusterPetalAddMat == null)
            clusterPetalAddMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Astrid_VFX/Astrid_Complimentary_Bouquet/Materials/M_Astrid_Petal_Cluster.mat");
        if (floralShockwaveMat == null)
            floralShockwaveMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Astrid_VFX/Astrid_Complimentary_Bouquet/Materials/M_Astrid_Floral_Shockwave.mat");
        if (softGlowMat == null)
            softGlowMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Astrid_VFX/Astrid_Complimentary_Bouquet/Materials/M_Astrid_SoftGlow.mat");
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
