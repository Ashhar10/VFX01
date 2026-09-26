using UnityEngine;

/// <summary>
/// ArthurAllyShieldVFX - Blue glowing shield buff for allies.
/// Features:
/// 1. Top of Head: Glowing shield inside a crest ring floating above the ally's head.
/// 2. Below (Body): Translucent glowing energy shield barrier covering the front of the body.
/// 3. Ambient energy sparks floating upward around the ally.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class ArthurAllyShieldVFX : MonoBehaviour
{
    [Header("=== Target Follow ===")]
    [Tooltip("Target ally transform to follow. If null, uses this transform.")]
    [SerializeField] private Transform targetAlly;

    [Header("=== 1. Top of Head (Shield & Ring Crest) ===")]
    [SerializeField] private bool enableHeadCrest = true;

    [Tooltip("Offset above the target origin (above head).")]
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 1.85f, 0f);

    [Tooltip("Material for the head crest (uses Ally_Shield_Ring.png).")]
    [SerializeField] private Material headCrestMaterial;

    [Tooltip("Vibrant cyan/blue HDR color.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color headCrestColor = new Color(0.35f, 1.2f, 2.4f, 1.0f);

    [Tooltip("Diameter of the head crest icon (meters).")]
    [SerializeField] private float headCrestSize = 0.65f;

    [Header("=== 2. Below Body (Translucent Energy Shield) ===")]
    [SerializeField] private bool enableBodyShield = true;

    [Tooltip("Offset for the body shield relative to ally origin (centered on torso, slightly forward).")]
    [SerializeField] private Vector3 bodyShieldOffset = new Vector3(0f, 0.95f, 0.28f);

    [Tooltip("Material for the body shield (uses Ally_Body_Shield.png).")]
    [SerializeField] private Material bodyShieldMaterial;

    [Tooltip("Luminous cyan HDR color with neon rim.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color bodyShieldColor = new Color(0.4f, 1.3f, 2.5f, 1.0f);

    [Tooltip("Scale/size of the front body shield.")]
    [SerializeField] private Vector2 bodyShieldSize = new Vector2(1.35f, 1.45f);

    [Header("=== 3. Energy Sparkles ===")]
    [SerializeField] private bool enableEnergySparkles = true;
    [SerializeField] private int sparkleRate = 7;

    [Header("=== Animation & Pulse ===")]
    [SerializeField] private float hoverSpeed = 2.2f;
    [SerializeField] private float hoverAmplitude = 0.04f;
    [SerializeField] private float pulseSpeed = 2.8f;
    [SerializeField] private float pulseScale = 0.04f;

    // Component references
    private ParticleSystem headCrestPS;
    private ParticleSystem bodyShieldPS;
    private ParticleSystem energySparklesPS;
    private bool isInitialized = false;

    private const string CREST_MAT_PATH = "Assets/VFX/King Arthur_VFX/Materials/KingArthur_Ally_ShieldRing_Mat.mat";
    private const string BODY_MAT_PATH = "Assets/VFX/King Arthur_VFX/Materials/KingArthur_Ally_BodyShield_Mat.mat";
    private const string SPARKS_MAT_PATH = "Assets/VFX/King Arthur_VFX/Materials/KingArthur_Contact_Sparks_Mat.mat";

    private void Awake()
    {
        InitializeComponents();
    }

    private void OnEnable()
    {
        InitializeComponents();
        Play();
    }

    private void OnValidate()
    {
        if (!gameObject.scene.IsValid()) return;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            InitializeComponents();
            ApplySettings();
        };
#endif
    }

    [ContextMenu("Set Theme: Pink")]
    public void SetThemePink()
    {
        headCrestColor = new Color(2.5f, 0.45f, 1.85f, 1.0f);
        bodyShieldColor = new Color(2.4f, 0.40f, 1.75f, 1.0f);
        ApplySettings();
    }

    [ContextMenu("Set Theme: Blue")]
    public void SetThemeBlue()
    {
        headCrestColor = new Color(0.35f, 1.2f, 2.4f, 1.0f);
        bodyShieldColor = new Color(0.4f, 1.3f, 2.5f, 1.0f);
        ApplySettings();
    }

    private void LateUpdate()
    {
        Vector3 basePos = targetAlly != null ? targetAlly.position : transform.position;
        float time = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;

        float hoverY = Mathf.Sin(time * hoverSpeed) * hoverAmplitude;
        float pulse = 1f + Mathf.Sin(time * pulseSpeed) * pulseScale;

        // Position Head Crest
        if (headCrestPS != null)
        {
            headCrestPS.transform.position = basePos + headOffset + new Vector3(0f, hoverY, 0f);
            headCrestPS.transform.localScale = Vector3.one * pulse;
        }

        // Position Body Shield
        if (bodyShieldPS != null)
        {
            Quaternion rot = targetAlly != null ? targetAlly.rotation : transform.rotation;
            Vector3 worldBodyOffset = rot * bodyShieldOffset;
            bodyShieldPS.transform.position = basePos + worldBodyOffset + new Vector3(0f, hoverY * 0.4f, 0f);
            bodyShieldPS.transform.rotation = rot;
            bodyShieldPS.transform.localScale = new Vector3(pulse, pulse, 1f);
        }

        // Position Energy Sparkles
        if (energySparklesPS != null)
        {
            energySparklesPS.transform.position = basePos + new Vector3(0f, 0.7f, 0f);
        }

        if (targetAlly != null)
        {
            transform.position = basePos;
            transform.rotation = targetAlly.rotation;
        }
    }

    private void InitializeComponents()
    {
        LoadDefaultMaterials();

        // 1. Head Crest (Shield inside ring)
        headCrestPS = GetOrCreateChildParticleSystem("1_Head_Shield_Crest");
        // 2. Body Shield (Translucent barrier)
        bodyShieldPS = GetOrCreateChildParticleSystem("2_Body_Energy_Shield");
        // 3. Ambient Sparkles
        energySparklesPS = GetOrCreateChildParticleSystem("3_Shield_Ambient_Sparkles");

        ApplySettings();
        isInitialized = true;
    }

    private void LoadDefaultMaterials()
    {
#if UNITY_EDITOR
        if (headCrestMaterial == null)
            headCrestMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(CREST_MAT_PATH);
        if (bodyShieldMaterial == null)
            bodyShieldMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(BODY_MAT_PATH);
#endif
    }

    public void ApplySettings()
    {
        ConfigureHeadCrest(headCrestPS);
        ConfigureBodyShield(bodyShieldPS);
        ConfigureSparkles(energySparklesPS);
    }

    private void ConfigureHeadCrest(ParticleSystem ps)
    {
        if (ps == null) return;

        bool wasPlaying = ps.isPlaying;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (!enableHeadCrest) return;

        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = Mathf.Infinity;
        main.startSpeed = 0f;
        main.startSize = headCrestSize;
        main.startColor = headCrestColor;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = false;
        main.maxParticles = 1;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (headCrestMaterial != null)
                renderer.sharedMaterial = headCrestMaterial;
        }

        if (wasPlaying || !Application.isPlaying)
            ps.Play();
    }

    private void ConfigureBodyShield(ParticleSystem ps)
    {
        if (ps == null) return;

        bool wasPlaying = ps.isPlaying;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (!enableBodyShield) return;

        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = Mathf.Infinity;
        main.startSpeed = 0f;
        main.startSize3D = true;
        main.startSizeX = bodyShieldSize.x;
        main.startSizeY = bodyShieldSize.y;
        main.startSizeZ = 1f;
        main.startColor = bodyShieldColor;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = false;
        main.maxParticles = 1;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

        var shape = ps.shape;
        shape.enabled = false;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            // Vertical billboard / mesh orientation aligned with character facing
            renderer.renderMode = ParticleSystemRenderMode.VerticalBillboard;
            if (bodyShieldMaterial != null)
                renderer.sharedMaterial = bodyShieldMaterial;
        }

        if (wasPlaying || !Application.isPlaying)
            ps.Play();
    }

    private void ConfigureSparkles(ParticleSystem ps)
    {
        if (ps == null) return;

        bool wasPlaying = ps.isPlaying;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (!enableEnergySparkles) return;

        var main = ps.main;
        main.duration = 2.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(headCrestColor, new Color(0.8f, 1.0f, 1.5f, 1.0f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = false;
        main.maxParticles = 24;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = sparkleRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.45f;
        shape.position = Vector3.zero;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = 0.6f;
        velocity.z = 0f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve ac = new AnimationCurve();
        ac.AddKey(0f, 0f);
        ac.AddKey(0.2f, 1.2f);
        ac.AddKey(0.7f, 0.8f);
        ac.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, ac);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(headCrestColor, 0.5f),
                new GradientColorKey(new Color(0.1f, 0.5f, 1.0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(0.8f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = grad;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (headCrestMaterial != null)
                renderer.sharedMaterial = headCrestMaterial;
        }

        if (wasPlaying || !Application.isPlaying)
            ps.Play();
    }

    [ContextMenu("Play Ally Shield")]
    public void Play()
    {
        if (!isInitialized) InitializeComponents();
        if (headCrestPS != null && enableHeadCrest) headCrestPS.Play();
        if (bodyShieldPS != null && enableBodyShield) bodyShieldPS.Play();
        if (energySparklesPS != null && enableEnergySparkles) energySparklesPS.Play();
    }

    [ContextMenu("Stop Ally Shield")]
    public void Stop()
    {
        if (headCrestPS != null) headCrestPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (bodyShieldPS != null) bodyShieldPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (energySparklesPS != null) energySparklesPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void SetTarget(Transform target)
    {
        targetAlly = target;
    }

    private ParticleSystem GetOrCreateChildParticleSystem(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }
        ParticleSystem ps = child.GetComponent<ParticleSystem>();
        if (ps == null) ps = child.gameObject.AddComponent<ParticleSystem>();
        return ps;
    }
}
