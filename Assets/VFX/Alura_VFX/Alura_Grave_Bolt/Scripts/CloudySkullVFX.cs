using UnityEngine;
using System.Collections;

/// <summary>
/// CloudySkullVFX - Standalone Necromantic Cloudy Light & Spectral Skull Aura VFX.
/// 
/// Recreates the Stage [4] Recovery / Debuff effect (Image 4):
/// - Soft rolling cloudy light & miasma mist wrapping around the character/enemy.
/// - Dynamic real-time Point Light casting atmospheric eerie green illumination onto the character and ground.
/// - Volumetric ambient glow core providing soft atmospheric depth.
/// - Floating ethereal spectral skulls drifting upwards and orbiting around the target.
/// - Twinkling 4-point star and cross motes shimmering in the haze.
/// - Ground creeping mist anchoring the effect to the floor.
/// 
/// Completely standalone: can be attached to any enemy/target or spawned dynamically.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class CloudySkullVFX : MonoBehaviour
{
    // ==========================================
    //  TARGET & POSITIONING
    // ==========================================
    [Header("=== Target & Follow ===")]
    [Tooltip("Optional transform to follow (e.g. enemy torso/chest). If null, stays at current transform position.")]
    [SerializeField] private Transform target;

    [Tooltip("Position offset relative to target (default y=0.9m centers on torso).")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.9f, 0f);

    [Tooltip("Smoothly follow target position every frame.")]
    [SerializeField] private bool followTarget = true;

    // ==========================================
    //  PLAYBACK & LIFECYCLE
    // ==========================================
    [Header("=== Playback & Lifecycle ===")]
    [Tooltip("Continuous looping aura (true) or one-shot timed effect (false).")]
    [SerializeField] private bool loop = true;

    [Tooltip("Duration in seconds when loop is disabled.")]
    [SerializeField, Range(0.5f, 15f)] private float duration = 3.5f;

    [Tooltip("Duration in seconds for smooth fade-out.")]
    [SerializeField, Range(0.1f, 3.0f)] private float fadeOutTime = 0.8f;

    [Tooltip("Automatically play when enabled.")]
    [SerializeField] private bool playOnAwake = true;

    // ==========================================
    //  LAYER 1: POINT LIGHT (CLOUDY ILLUMINATION)
    // ==========================================
    [Header("=== Layer 1: Point Light (Cloudy Illumination) ===")]
    [Tooltip("Enable real-time Point Light casting green light on the character and ground.")]
    [SerializeField] private bool enableLight = true;

    [Tooltip("Color of the atmospheric cloudy light.")]
    [ColorUsage(false, true)]
    [SerializeField] private Color lightColor = new Color(0.25f, 1.0f, 0.55f, 1f);

    [Tooltip("Intensity of the light source.")]
    [SerializeField, Range(0f, 8f)] private float lightIntensity = 2.8f;

    [Tooltip("Range of the light illumination in meters.")]
    [SerializeField, Range(0.5f, 8f)] private float lightRange = 3.5f;

    [Tooltip("Speed of organic breathing / pulsation.")]
    [SerializeField, Range(0.1f, 8f)] private float lightPulseSpeed = 2.2f;

    [Tooltip("Depth of pulse variation (0 = steady light, 0.4 = subtle organic breathing).")]
    [SerializeField, Range(0f, 0.8f)] private float lightPulseDepth = 0.25f;

    // ==========================================
    //  ORBIT ROTATION
    // ==========================================
    [Header("=== Orbit Rotation ===")]
    [Tooltip("Rotation speed of the entire VFX around the target (deg/s). Skulls and clouds orbit.")]
    [SerializeField, Range(-180f, 180f)] private float orbitRotationSpeed = 40f;

    // ==========================================
    //  LAYER 2: BILLOWING CLOUD MIST
    // ==========================================
    [Header("=== Layer 3: Billowing Cloud Mist ===")]
    [Tooltip("Enable soft rolling smoke / mist clouds around the character.")]
    [SerializeField] private bool enableCloudMist = true;

    [Tooltip("Radius of the mist cloud around the target.")]
    [SerializeField, Range(0.2f, 2.5f)] private float cloudRadius = 0.65f;

    [Tooltip("Height of the mist cloud cylinder.")]
    [SerializeField, Range(0.4f, 3.0f)] private float cloudHeight = 1.6f;

    [Tooltip("Density / emission rate multiplier of the smoke clouds.")]
    [SerializeField, Range(0.1f, 3.0f)] private float mistDensity = 1.0f;

    [Tooltip("Scale multiplier for the smoke billow puffs.")]
    [SerializeField, Range(0.3f, 2.5f)] private float mistParticleScale = 1.15f;

    [Tooltip("Upward drift velocity of the smoke billows.")]
    [SerializeField, Range(0.05f, 1.5f)] private float mistUpwardSpeed = 0.35f;

    [Tooltip("Swirl rotation speed around the target (deg/s).")]
    [SerializeField, Range(-180f, 180f)] private float mistSwirlSpeed = 35f;

    [Tooltip("Smoky cloud color at peak density.")]
    [SerializeField] private Color mistColor = new Color(0.22f, 0.85f, 0.48f, 0.55f);

    // ==========================================
    //  LAYER 4: FLOATING SPECTRAL SKULLS
    // ==========================================
    [Header("=== Layer 4: Floating Spectral Skulls ===")]
    [Tooltip("Enable floating ethereal spectral skulls.")]
    [SerializeField] private bool enableSkulls = true;

    [Tooltip("Average number of spectral skulls spawned per second.")]
    [SerializeField, Range(0.2f, 5.0f)] private float skullSpawnRate = 1.3f;

    [Tooltip("Scale of the floating spectral skulls.")]
    [SerializeField, Range(0.15f, 1.2f)] private float skullSize = 0.45f;

    [Tooltip("HDR Color and emission of the floating skulls.")]
    [ColorUsage(false, true)]
    [SerializeField] private Color skullColor = new Color(0.45f, 2.5f, 1.1f, 0.95f);

    [Tooltip("Upward floating speed of the spectral skulls.")]
    [SerializeField, Range(0.1f, 1.8f)] private float skullFloatSpeed = 0.38f;

    [Tooltip("Slow orbital swirl of floating skulls around target.")]
    [SerializeField, Range(-90f, 90f)] private float skullOrbitSpeed = 22f;

    [Tooltip("Side-to-side wavy wobble / ethereal jitter.")]
    [SerializeField, Range(0f, 1f)] private float skullWobble = 0.28f;

    [Tooltip("Lifetime of each floating skull in seconds.")]
    [SerializeField, Range(1.0f, 5.0f)] private float skullLifetime = 2.6f;

    // ==========================================
    //  LAYER 5: TWINKLING SPARKLE MOTES
    // ==========================================
    [Header("=== Layer 5: Twinkling Sparkle Motes ===")]
    [Tooltip("Enable twinkling 4-point star and cross motes in the cloud.")]
    [SerializeField] private bool enableSparkles = true;

    [Tooltip("Quantity of twinkling sparkles.")]
    [SerializeField, Range(0f, 25f)] private float sparkleQuantity = 9.0f;

    [Tooltip("Size of each twinkling star mote.")]
    [SerializeField, Range(0.01f, 0.1f)] private float sparkleSize = 0.032f;

    [Tooltip("HDR Color of the twinkling motes.")]
    [ColorUsage(false, true)]
    [SerializeField] private Color sparkleColor = new Color(0.7f, 2.8f, 1.3f, 1f);

    // ==========================================
    //  LAYER 6: GROUND CREEPING MIST
    // ==========================================
    [Header("=== Layer 6: Ground Creeping Mist ===")]
    [Tooltip("Enable ground-hugging mist ring.")]
    [SerializeField] private bool enableGroundMist = true;

    [Tooltip("Radius of the ground mist ring.")]
    [SerializeField, Range(0.4f, 3.0f)] private float groundMistRadius = 1.05f;

    [Tooltip("Ground mist color tint.")]
    [SerializeField] private Color groundMistColor = new Color(0.12f, 0.55f, 0.28f, 0.40f);

    // ==========================================
    //  MATERIALS
    // ==========================================
    [Header("=== Materials ===")]
    [SerializeField] private Material smokeMaterial;
    [SerializeField] private Material skullMaterial;
    [SerializeField] private Material sparkleMaterial;
    [SerializeField] private Material groundMistMaterial;

    // ==========================================
    //  CACHED INTERNAL COMPONENTS
    // ==========================================
    private Light pointLightComponent;
    private ParticleSystem cloudMistPS;
    private ParticleSystem skullsPS;
    private ParticleSystem sparklesPS;
    private ParticleSystem groundMistPS;

    private Coroutine activePlayRoutine;
    private bool isPlaying = false;
    private float currentMasterAlpha = 1.0f;

    // Properties for scripting access
    public Transform Target { get => target; set => target = value; }
    public bool Loop { get => loop; set => loop = value; }
    public float Duration { get => duration; set => duration = Mathf.Max(0.1f, value); }
    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        InitializeComponents();
    }

    private void OnEnable()
    {
        InitializeComponents();
        if (playOnAwake)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    private void Update()
    {
        // Follow target if assigned
        if (target != null && followTarget)
        {
            transform.position = target.position + offset;
        }

        // Orbit rotation — rotate the entire VFX so skulls & clouds orbit around target
        if (isPlaying && Mathf.Abs(orbitRotationSpeed) > 0.01f)
        {
            float dt = Application.isPlaying ? Time.deltaTime : 0.016f; // ~60fps fallback in editor
            transform.Rotate(Vector3.up, orbitRotationSpeed * dt, Space.World);
        }

        // Animate light breathing pulse
        if (pointLightComponent != null && enableLight && isPlaying)
        {
            float time = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;
            float pulse = 1.0f + Mathf.Sin(time * lightPulseSpeed * Mathf.PI) * lightPulseDepth;
            pointLightComponent.intensity = lightIntensity * pulse * currentMasterAlpha;
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!gameObject.scene.IsValid()) return;
        UnityEditor.EditorApplication.delayCall -= OnEditorValidate;
        UnityEditor.EditorApplication.delayCall += OnEditorValidate;
#endif
    }

#if UNITY_EDITOR
    private void OnEditorValidate()
    {
        if (this == null || !gameObject.scene.IsValid()) return;
        InitializeComponents();
        ApplySettingsToAllSystems();
    }
#endif

    public void LoadDefaultMaterials()
    {
#if UNITY_EDITOR
        if (smokeMaterial == null)
            smokeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Miasma/Materials/NecromanticSmokeMat.mat");
        if (skullMaterial == null)
            skullMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Grave_Bolt/Materials/GraveBolt_Skull_Mat.mat");
        if (sparkleMaterial == null)
            sparkleMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Grave_Bolt/Materials/GraveBolt_Mote_Mat.mat");
        if (groundMistMaterial == null)
            groundMistMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Miasma/Materials/NecromanticWispMat.mat");
#endif
    }

    public void InitializeComponents()
    {
        LoadDefaultMaterials();

        // Layer 1: Point Light
        pointLightComponent = GetOrCreateChildComponent<Light>("1_Cloudy_Light");
        pointLightComponent.type = LightType.Point;
        pointLightComponent.color = lightColor;
        pointLightComponent.intensity = enableLight ? lightIntensity : 0f;
        pointLightComponent.range = lightRange;
        pointLightComponent.shadows = LightShadows.None;
        pointLightComponent.transform.localPosition = Vector3.zero;

        // Layer 2: Billowing Cloud Mist
        cloudMistPS = GetOrCreateChildComponent<ParticleSystem>("3_Billowing_Cloud_Mist");
        ConfigureCloudMist(cloudMistPS);

        // Layer 3: Floating Spectral Skulls
        skullsPS = GetOrCreateChildComponent<ParticleSystem>("4_Floating_Spectral_Skulls");
        ConfigureSkulls(skullsPS);

        // Layer 4: Twinkling Sparkle Motes
        sparklesPS = GetOrCreateChildComponent<ParticleSystem>("5_Twinkling_Sparkle_Motes");
        ConfigureSparkles(sparklesPS);

        // Layer 5: Ground Creeping Mist
        groundMistPS = GetOrCreateChildComponent<ParticleSystem>("6_Ground_Creeping_Mist");
        ConfigureGroundMist(groundMistPS);
    }

    public void ApplySettingsToAllSystems()
    {
        if (pointLightComponent != null)
        {
            pointLightComponent.enabled = enableLight;
            pointLightComponent.color = lightColor;
            pointLightComponent.range = lightRange;
            pointLightComponent.intensity = enableLight ? lightIntensity * currentMasterAlpha : 0f;
        }

        if (cloudMistPS != null)
        {
            cloudMistPS.gameObject.SetActive(enableCloudMist);
            if (enableCloudMist) ConfigureCloudMist(cloudMistPS);
        }

        if (skullsPS != null)
        {
            skullsPS.gameObject.SetActive(enableSkulls);
            if (enableSkulls) ConfigureSkulls(skullsPS);
        }

        if (sparklesPS != null)
        {
            bool active = enableSparkles && sparkleQuantity > 0.01f;
            sparklesPS.gameObject.SetActive(active);
            if (active) ConfigureSparkles(sparklesPS);
        }

        if (groundMistPS != null)
        {
            groundMistPS.gameObject.SetActive(enableGroundMist);
            if (enableGroundMist) ConfigureGroundMist(groundMistPS);
        }
    }

    private T GetOrCreateChildComponent<T>(string childName) where T : Component
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName);
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create " + childName);
#endif
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            child = go.transform;
        }

        T comp = child.GetComponent<T>();
        if (comp == null)
        {
            comp = child.gameObject.AddComponent<T>();
        }
        return comp;
    }

    // ==========================================
    //  PARTICLE CONFIGURATIONS
    // ==========================================


    private void ConfigureCloudMist(ParticleSystem ps)
    {
        if (ps == null) return;
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.2f, 3.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.9f * mistParticleScale, 1.5f * mistParticleScale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.maxParticles = 40;

        var emission = ps.emission;
        emission.rateOverTime = 14f * mistDensity;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f; // Cylinder
        shape.radius = cloudRadius;
        shape.radiusThickness = 0.75f;
        shape.length = cloudHeight;
        shape.position = new Vector3(0f, -cloudHeight * 0.4f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.y = mistUpwardSpeed;
        vol.orbitalY = mistSwirlSpeed * Mathf.Deg2Rad;
        vol.radial = 0.02f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.55f);
        sc.AddKey(0.4f, 1.05f);
        sc.AddKey(1.0f, 1.35f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.90f, 0.98f, 0.92f), 0.0f),
                new GradientColorKey(mistColor, 0.35f),
                new GradientColorKey(new Color(0.10f, 0.35f, 0.20f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(mistColor.a, 0.22f),
                new GradientAlphaKey(mistColor.a * 0.85f, 0.65f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.20f;
        noise.frequency = 0.35f;
        noise.scrollSpeed = 0.20f;
        noise.damping = true;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = smokeMaterial;
        renderer.sortingOrder = 3;
    }

    private void ConfigureSkulls(ParticleSystem ps)
    {
        if (ps == null) return;
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = skullLifetime;
        main.startSize = new ParticleSystem.MinMaxCurve(skullSize * 0.85f, skullSize * 1.15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(skullFloatSpeed * 0.8f, skullFloatSpeed * 1.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-15f * Mathf.Deg2Rad, 15f * Mathf.Deg2Rad);
        main.startColor = skullColor;
        main.maxParticles = 6;

        var emission = ps.emission;
        emission.rateOverTime = skullSpawnRate;

        // Torso/Chest region emitter
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = cloudRadius * 0.65f;
        shape.position = new Vector3(0f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.y = skullFloatSpeed;
        vol.orbitalY = skullOrbitSpeed * Mathf.Deg2Rad;
        vol.radial = 0.03f;

        // Gentle scale breathing
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.4f);
        sc.AddKey(0.2f, 1.0f);
        sc.AddKey(0.7f, 1.05f);
        sc.AddKey(1.0f, 0.75f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        // Alpha fade-in, sustain luminous skull, dissolve fade-out
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(skullColor, 0.0f),
                new GradientColorKey(skullColor, 0.7f),
                new GradientColorKey(new Color(0.2f, 1.0f, 0.5f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(skullColor.a, 0.20f),
                new GradientAlphaKey(skullColor.a * 0.90f, 0.75f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        col.color = g;

        // Wobble noise for ethereal floating
        var noise = ps.noise;
        noise.enabled = skullWobble > 0.01f;
        noise.strength = skullWobble;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.3f;
        noise.damping = true;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = skullMaterial != null ? skullMaterial : smokeMaterial;
        renderer.sortingOrder = 6;
    }

    private void ConfigureSparkles(ParticleSystem ps)
    {
        if (ps == null) return;
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(sparkleSize * 0.75f, sparkleSize * 1.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = sparkleColor;
        main.maxParticles = 30;

        var emission = ps.emission;
        emission.rateOverTime = sparkleQuantity;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 0f;
        shape.radius = cloudRadius * 1.15f;
        shape.length = cloudHeight * 1.1f;
        shape.position = new Vector3(0f, -cloudHeight * 0.4f, 0f);
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.y = mistUpwardSpeed * 0.8f;
        vol.orbitalY = mistSwirlSpeed * 1.4f * Mathf.Deg2Rad;

        // Twinkle shimmer curve
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.0f);
        sc.AddKey(0.15f, 1.0f);
        sc.AddKey(0.45f, 0.35f);
        sc.AddKey(0.70f, 1.25f);
        sc.AddKey(1.0f, 0.0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-120f * Mathf.Deg2Rad, 120f * Mathf.Deg2Rad);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(sparkleColor, 0f), new GradientColorKey(Color.white, 0.5f), new GradientColorKey(sparkleColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.85f, 0.8f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = sparkleMaterial != null ? sparkleMaterial : smokeMaterial;
        renderer.sortingOrder = 5;
    }

    private void ConfigureGroundMist(ParticleSystem ps)
    {
        if (ps == null) return;
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = groundMistColor;
        main.maxParticles = 20;

        var emission = ps.emission;
        emission.rateOverTime = 8f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = groundMistRadius;
        shape.donutRadius = 0.25f;
        shape.position = new Vector3(0f, -offset.y + 0.05f, 0f); // Placed at ground feet level
        shape.rotation = new Vector3(90f, 0f, 0f);

        var vol = ps.velocityOverLifetime;
        vol.enabled = true;
        vol.space = ParticleSystemSimulationSpace.Local;
        vol.orbitalY = mistSwirlSpeed * 0.8f * Mathf.Deg2Rad;
        vol.radial = 0.03f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0.0f, 0.4f);
        sc.AddKey(0.5f, 1.0f);
        sc.AddKey(1.0f, 1.25f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(groundMistColor, 0f), new GradientColorKey(groundMistColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(groundMistColor.a, 0.25f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = groundMistMaterial != null ? groundMistMaterial : smokeMaterial;
        renderer.sortingOrder = 1;
    }

    // ==========================================
    //  PUBLIC PLAYBACK API
    // ==========================================

    public void Play()
    {
        isPlaying = true;
        currentMasterAlpha = 1.0f;
        ApplySettingsToAllSystems();

        if (cloudMistPS != null && enableCloudMist) cloudMistPS.Play();
        if (skullsPS != null && enableSkulls) skullsPS.Play();
        if (sparklesPS != null && enableSparkles) sparklesPS.Play();
        if (groundMistPS != null && enableGroundMist) groundMistPS.Play();

        if (activePlayRoutine != null)
        {
            StopCoroutine(activePlayRoutine);
            activePlayRoutine = null;
        }

        if (!loop && Application.isPlaying)
        {
            activePlayRoutine = StartCoroutine(DurationTimerRoutine());
        }
    }

    public void Stop()
    {
        isPlaying = false;
        if (activePlayRoutine != null)
        {
            StopCoroutine(activePlayRoutine);
            activePlayRoutine = null;
        }

        if (cloudMistPS != null) cloudMistPS.Stop();
        if (skullsPS != null) skullsPS.Stop();
        if (sparklesPS != null) sparklesPS.Stop();
        if (groundMistPS != null) groundMistPS.Stop();
        if (pointLightComponent != null) pointLightComponent.intensity = 0f;
    }

    public void Restart()
    {
        Stop();
        Clear();
        Play();
    }

    public void Clear()
    {
        if (cloudMistPS != null) cloudMistPS.Clear();
        if (skullsPS != null) skullsPS.Clear();
        if (sparklesPS != null) sparklesPS.Clear();
        if (groundMistPS != null) groundMistPS.Clear();
    }

    public void FadeOut(float fadeTime = -1f)
    {
        if (fadeTime <= 0f) fadeTime = fadeOutTime;
        if (activePlayRoutine != null)
        {
            StopCoroutine(activePlayRoutine);
        }
        if (gameObject.activeInHierarchy)
        {
            activePlayRoutine = StartCoroutine(FadeOutRoutine(fadeTime));
        }
    }

    private IEnumerator DurationTimerRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, duration - fadeOutTime));
        yield return FadeOutRoutine(fadeOutTime);
    }

    private IEnumerator FadeOutRoutine(float fadeTime)
    {
        // Stop emitting new particles so existing ones can naturally decay
        if (cloudMistPS != null) cloudMistPS.Stop();
        if (skullsPS != null) skullsPS.Stop();
        if (sparklesPS != null) sparklesPS.Stop();
        if (groundMistPS != null) groundMistPS.Stop();

        float elapsed = 0f;
        float startAlpha = currentMasterAlpha;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            currentMasterAlpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeTime);
            if (pointLightComponent != null && enableLight)
            {
                pointLightComponent.intensity = lightIntensity * currentMasterAlpha;
            }
            yield return null;
        }

        currentMasterAlpha = 0f;
        if (pointLightComponent != null) pointLightComponent.intensity = 0f;
        isPlaying = false;
        activePlayRoutine = null;
    }
}
