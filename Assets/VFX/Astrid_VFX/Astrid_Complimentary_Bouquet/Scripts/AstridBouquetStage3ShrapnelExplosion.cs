using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// AstridBouquetStage3ShrapnelExplosion - Stage 3: Emissive Spinning Ball & Petal Circular Force Vortex.
/// Storyboard Ref: Bouquet erupts! A small emissive ball rotates rapidly in a circle, emitting petals
/// that fling and spread outward with its circular force, staying floating on the wind before dissolving.
/// 
/// VFX Scenario 3 Features:
/// - Small emissive glowing ball (sphere mesh + soft glow halo + speed motion trail) rotating in a circle.
/// - Configurable orbit radius, spin speed, and ball count (default 1 solo spinning ball, expandable to 4).
/// - Petals emit directly from the emissive ball and inherit its circular velocity (inheritVelocity).
/// - Tangential fling angle spreads petals outward in a dramatic expanding Archimedean vortex.
/// - Particles stay on the wind: aerodynamic braking (limitVelocityOverLifetime) decelerates them into a gentle hover,
///   while Perlin noise turbulence creates realistic organic wind flutter.
/// - Automatic dissolve: alpha and scale gracefully fade out over particle lifetime into thin air.
/// - Supporting visuals: expanding horizontal ground floral shockwave ring and core flash.
/// - Coordinated dissolve: provides public DissolveAndDisappear(float duration) called by Stage 4, smoothly shrinking the ball and fading petals.
/// 
/// Real-time Features:
/// - Instant play/replay on Enable/Disable (toggle GameObject in Hierarchy).
/// - Live updates in Scene View on Inspector property changes (OnValidate + delayCall).
/// - Dedicated Bloom & Emissive Intensity slider.
/// - Zero material leak (uses sharedMaterial & MaterialPropertyBlock).
/// - Safe editor selection tracking prevents GameObjectInspector MissingReferenceException.
/// - HideFlags.DontSave prevents duplicate accumulation.
/// </summary>
[ExecuteAlways]
[SelectionBase]
[DisallowMultipleComponent]
public class AstridBouquetStage3ShrapnelExplosion : MonoBehaviour
{
    private const string CONTENT_ROOT_NAME = "STAGE_3_SHRAPNEL_EXPLOSION_CONTENT";

    [Header("=== Emissive Spinning Ball & Circular Force ===")]
    [Tooltip("Continuously spin and emit petals in a loop.")]
    [SerializeField] private bool loopSpinning = true;

    [Tooltip("Number of orbiting emissive balls (1 for a solo spinning ball, up to 4 for symmetric vortex arms).")]
    [SerializeField, Range(1, 4)] private int ballCount = 1;

    [Tooltip("Radius of the circular orbit path (distance from center).")]
    [SerializeField, Range(0.2f, 2.5f)] private float orbitRadius = 0.75f;

    [Tooltip("Diameter of the small emissive ball.")]
    [SerializeField, Range(0.06f, 0.45f)] private float ballSize = 0.18f;

    [Tooltip("Rotation speed of the ball in the circle (degrees/second).")]
    [SerializeField, Range(60f, 1440f)] private float spinSpeed = 580f;

    [Tooltip("Spin clockwise (true) or counter-clockwise (false).")]
    [SerializeField] private bool spinClockwise = true;

    [Tooltip("Outward ejection angle bias (degrees) along the circular spin direction.")]
    [SerializeField, Range(0f, 60f)] private float circularFlingAngle = 35f;

    [Tooltip("Ratio of tangential circular velocity inherited by petals (spreads petals with the ball's circular force).")]
    [SerializeField, Range(0.0f, 2.0f)] private float inheritCircularForce = 1.0f;

    [Tooltip("Enable glowing speed trail behind the moving ball.")]
    [SerializeField] private bool enableBallTrail = true;

    [Tooltip("Duration of the glowing motion trail (seconds).")]
    [SerializeField, Range(0.05f, 0.4f)] private float ballTrailTime = 0.16f;

    [Tooltip("Enable soft optical bloom halo around the ball.")]
    [SerializeField] private bool enableBallAura = true;

    [Tooltip("Center origin offset (waist/chest height around Astrid).")]
    [SerializeField] private Vector3 centerOffset = new Vector3(0f, 0.7f, 0f);

    [Header("=== Petal Emission & Wind Dynamics ===")]
    [FormerlySerializedAs("petalsPerArmRate")]
    [Tooltip("Petals emitted per ball per second.")]
    [SerializeField, Range(10, 500)] private int petalsPerBallRate = 140;

    [Tooltip("Initial outward ejection speed from the ball.")]
    [SerializeField, Range(3f, 30f)] private float horizontalSpeed = 12.0f;

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

    [Header("=== Petal Cluster Settings ===")]
    [Tooltip("Include larger floating petal clusters alongside single petals.")]
    [SerializeField] private bool includeClusters = true;

    [FormerlySerializedAs("clustersPerArmRate")]
    [Tooltip("Petal clusters emitted per ball per second.")]
    [SerializeField, Range(2, 120)] private int clustersPerBallRate = 32;

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
    [Tooltip("Blazing Emissive Ball Core Color.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color ballCoreColor = new Color(3.5f, 1.2f, 2.8f, 1.0f);

    [Tooltip("Intense Cherry Blossom Blast Pink.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color explosionPink = new Color(1.0f, 0.35f, 0.65f, 1.0f);

    [Tooltip("Hot Core Magenta Shrapnel.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color coreMagenta = new Color(0.85f, 0.15f, 0.50f, 1.0f);

    [Tooltip("Bright White/Sakura Flash Spark.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color flashWhite = new Color(1.0f, 0.95f, 0.98f, 1.0f);

    [Header("=== Assigned URP Materials & Meshes ===")]
    [Tooltip("Default Unity primitive Sphere mesh.")]
    [SerializeField] private Mesh sphereMesh;
    [SerializeField] private Material singlePetalAddMat;
    [SerializeField] private Material clusterPetalAddMat;
    [SerializeField] private Material floralShockwaveMat;
    [SerializeField] private Material softGlowMat;

    private Transform stageRoot;
    private Transform spinningRoot;
    private Coroutine activeRoutine;
    private Coroutine dissolveRoutine;
    private bool isBurstSpinning = false;

    private readonly List<Transform> emissiveBallNodes = new List<Transform>();
    private readonly List<MeshRenderer> ballRenderers = new List<MeshRenderer>();
    private readonly List<TrailRenderer> ballTrails = new List<TrailRenderer>();
    private readonly List<ParticleSystem> ballAuraPSList = new List<ParticleSystem>();
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
            if (dt <= 0f) dt = 0.016f;
            float dir = spinClockwise ? 1f : -1f;
            spinningRoot.Rotate(0f, dir * spinSpeed * dt, 0f, Space.Self);
        }
    }

    #region Public Playback Controls

    [ContextMenu("Play Emissive Spinning Ball Explosion")]
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
        if (stageRoot == null || emissiveBallNodes.Count != ballCount)
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

        emissiveBallNodes.Clear();
        ballRenderers.Clear();
        ballTrails.Clear();
        ballAuraPSList.Clear();
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

        // 2. Smoothly fade out bloom, shrink emissive balls, and fade existing particles
        float elapsed = 0f;
        float startBloom = bloomIntensity;
        Vector3 startScale = Vector3.one * ballSize;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float fadeFactor = 1.0f - t;

            // Shrink balls smoothly to 0
            foreach (var b in emissiveBallNodes)
            {
                if (b != null) b.localScale = startScale * fadeFactor;
            }

            if (propertyBlock != null)
            {
                propertyBlock.SetFloat("_Intensity", startBloom * fadeFactor);
                foreach (var mr in ballRenderers)
                {
                    if (mr != null) mr.SetPropertyBlock(propertyBlock);
                }
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

    private Mesh GetSphereMesh()
    {
        if (sphereMesh != null) return sphereMesh;

#if UNITY_EDITOR
        if (sphereMesh == null)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            SafeDestroy(temp);
        }
#endif
        return sphereMesh;
    }

    private void SpawnSpinningVortexSystem()
    {
        GameObject vortexGo = CreateChild("SpinningArmVortex");
        vortexGo.transform.localPosition = centerOffset;
        spinningRoot = vortexGo.transform;

        float angleStep = 360f / Mathf.Max(1, ballCount);

        for (int i = 0; i < ballCount; i++)
        {
            float baseAngle = angleStep * i;
            GameObject armPivot = new GameObject($"ArmPivot_{i}");
            armPivot.transform.SetParent(spinningRoot, false);
            armPivot.transform.localRotation = Quaternion.Euler(0f, baseAngle, 0f);

            // Small Emissive Ball GameObject orbiting at orbitRadius
            GameObject ballGo = new GameObject($"EmissiveBall_{i}");
            ballGo.transform.SetParent(armPivot.transform, false);
            ballGo.transform.localPosition = new Vector3(0f, 0f, orbitRadius);

            // Outward ejection direction angled with circular spin motion (tangential centrifugal release)
            float tangentAngle = (spinClockwise ? 1f : -1f) * circularFlingAngle;
            ballGo.transform.localRotation = Quaternion.Euler(0f, tangentAngle, 0f);
            ballGo.transform.localScale = Vector3.one * ballSize;

            emissiveBallNodes.Add(ballGo.transform);

            // 1. Physical 3D Sphere Mesh with Emissive Material (0 GameObjects created during mesh fetch)
            MeshFilter mf = ballGo.AddComponent<MeshFilter>();
            mf.sharedMesh = GetSphereMesh();

            MeshRenderer mr = ballGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = softGlowMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            ballRenderers.Add(mr);

            // 2. Speed Motion Trail (ribbon showing fast circular rotation)
            if (enableBallTrail)
            {
                TrailRenderer tr = ballGo.AddComponent<TrailRenderer>();
                tr.time = ballTrailTime;
                tr.startWidth = ballSize * 0.9f;
                tr.endWidth = 0.01f;
                tr.minVertexDistance = 0.02f;
                tr.sharedMaterial = softGlowMat;
                tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tr.receiveShadows = false;

                Gradient trGrad = new Gradient();
                trGrad.SetKeys(
                    new[] {
                        new GradientColorKey(flashWhite, 0.0f),
                        new GradientColorKey(explosionPink, 0.4f),
                        new GradientColorKey(coreMagenta, 1.0f)
                    },
                    new[] {
                        new GradientAlphaKey(0.85f, 0.0f),
                        new GradientAlphaKey(0.50f, 0.5f),
                        new GradientAlphaKey(0.0f, 1.0f)
                    }
                );
                tr.colorGradient = trGrad;
                ballTrails.Add(tr);
            }

            // 3. Soft Optical Glow Corona / Aura
            if (enableBallAura)
            {
                GameObject auraGo = new GameObject("BallAura");
                auraGo.transform.SetParent(ballGo.transform, false);
                ParticleSystem auraPS = auraGo.AddComponent<ParticleSystem>();
                ballAuraPSList.Add(auraPS);
                allParticleSystems.Add(auraPS);

                var aMain = auraPS.main;
                aMain.loop = loopSpinning;
                aMain.playOnAwake = true;
                aMain.startLifetime = 0.45f;
                aMain.startSpeed = 0f;
                aMain.startSize = ballSize * 2.8f;
                aMain.startColor = ballCoreColor;
                aMain.simulationSpace = ParticleSystemSimulationSpace.Local;
                aMain.scalingMode = ParticleSystemScalingMode.Hierarchy;

                var aEm = auraPS.emission;
                aEm.enabled = true;
                aEm.rateOverTime = 8f;

                var aCol = auraPS.colorOverLifetime;
                aCol.enabled = true;
                Gradient aGrad = new Gradient();
                aGrad.SetKeys(
                    new[] { new GradientColorKey(flashWhite, 0f), new GradientColorKey(explosionPink, 1f) },
                    new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.6f, 0.5f), new GradientAlphaKey(0f, 1f) }
                );
                aCol.color = new ParticleSystem.MinMaxGradient(aGrad);

                var aRend = auraGo.GetComponent<ParticleSystemRenderer>();
                aRend.renderMode = ParticleSystemRenderMode.Billboard;
                aRend.sharedMaterial = softGlowMat;

                auraPS.Play();
            }

            // 4. Petals emitted directly from the ball, spreading with circular force
            SpawnBallSinglePetals(ballGo, i);

            if (includeClusters)
            {
                SpawnBallClusterPetals(ballGo, i);
            }
        }
    }

    private void SpawnBallSinglePetals(GameObject ballGo, int ballIndex)
    {
        GameObject child = new GameObject($"Ball_{ballIndex}_SinglePetals");
        child.transform.SetParent(ballGo.transform, false);

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
        main.simulationSpace = ParticleSystemSimulationSpace.World; // World space: leaves the ball and flies free
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

        // Emission rate from the ball
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = petalsPerBallRate;
        if (!loopSpinning)
        {
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.0f, (short)(petalsPerBallRate * 2)) });
        }

        // Shape: Petals spawn directly from the ball surface
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 14.0f; // clean horizontal scatter cone
        shape.radius = ballSize * 0.45f;
        shape.radiusThickness = 0.5f;

        // INHERIT CIRCULAR VELOCITY: The ball flings the petals with its circular speed!
        var inherit = ps.inheritVelocity;
        inherit.enabled = true;
        inherit.mode = ParticleSystemInheritVelocityMode.Initial;
        inherit.curve = new ParticleSystem.MinMaxCurve(inheritCircularForce);

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

    private void SpawnBallClusterPetals(GameObject ballGo, int ballIndex)
    {
        GameObject child = new GameObject($"Ball_{ballIndex}_ClusterPetals");
        child.transform.SetParent(ballGo.transform, false);

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
        emission.rateOverTime = clustersPerBallRate;
        if (!loopSpinning)
        {
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.0f, (short)(clustersPerBallRate * 2)) });
        }

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 16.0f;
        shape.radius = ballSize * 0.5f;

        // INHERIT CIRCULAR VELOCITY
        var inherit = ps.inheritVelocity;
        inherit.enabled = true;
        inherit.mode = ParticleSystemInheritVelocityMode.Initial;
        inherit.curve = new ParticleSystem.MinMaxCurve(inheritCircularForce);

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

        // 1. Update Emissive Ball nodes and renderers
        for (int i = 0; i < emissiveBallNodes.Count; i++)
        {
            var b = emissiveBallNodes[i];
            if (b != null)
            {
                b.localPosition = new Vector3(0f, 0f, orbitRadius);
                b.localScale = Vector3.one * ballSize;
                float tangentAngle = (spinClockwise ? 1f : -1f) * circularFlingAngle;
                b.localRotation = Quaternion.Euler(0f, tangentAngle, 0f);
            }
        }

        foreach (var mr in ballRenderers)
        {
            if (mr != null)
            {
                mr.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat("_Intensity", bloomIntensity);
                propertyBlock.SetColor("_BaseColor", ballCoreColor);
                mr.SetPropertyBlock(propertyBlock);
            }
        }

        foreach (var tr in ballTrails)
        {
            if (tr != null)
            {
                tr.enabled = enableBallTrail;
                tr.time = ballTrailTime;
                tr.startWidth = ballSize * 0.9f;
            }
        }

        foreach (var aPS in ballAuraPSList)
        {
            if (aPS != null)
            {
                var main = aPS.main;
                main.startSize = ballSize * 2.8f;
                main.startColor = ballCoreColor;
                var rend = aPS.GetComponent<ParticleSystemRenderer>();
                if (rend != null)
                {
                    rend.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetFloat("_Intensity", bloomIntensity);
                    rend.SetPropertyBlock(propertyBlock);
                }
            }
        }

        // 2. Update Single Petals
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
                em.rateOverTime = petalsPerBallRate;

                var inherit = ps.inheritVelocity;
                inherit.curve = new ParticleSystem.MinMaxCurve(inheritCircularForce);

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

        // 3. Update Cluster Petals
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
                em.rateOverTime = clustersPerBallRate;

                var inherit = ps.inheritVelocity;
                inherit.curve = new ParticleSystem.MinMaxCurve(inheritCircularForce);

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

        // 4. Update Shockwave & Core Flash
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
#if UNITY_EDITOR
        // If a generated child was selected in the Inspector, redirect selection to the root component
        // so Unity's GameObjectInspector doesn't lose its target and throw MissingReferenceException
        if (UnityEditor.Selection.activeGameObject != null &&
            UnityEditor.Selection.activeGameObject != gameObject &&
            UnityEditor.Selection.activeGameObject.transform.IsChildOf(transform))
        {
            UnityEditor.Selection.activeGameObject = gameObject;
        }
#endif

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

        emissiveBallNodes.Clear();
        ballRenderers.Clear();
        ballTrails.Clear();
        ballAuraPSList.Clear();
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
        if (sphereMesh == null)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphereMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            SafeDestroy(temp);
        }
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
            if (this != null && gameObject != null && gameObject.activeInHierarchy && enabled)
            {
                RefreshLiveView();
            }
        };
#endif
    }

    #endregion
}
