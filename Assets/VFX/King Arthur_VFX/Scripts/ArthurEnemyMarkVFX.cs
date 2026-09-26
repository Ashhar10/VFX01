using UnityEngine;

/// <summary>
/// ArthurEnemyMarkVFX - Red glowing target skull surrounded by a reticle ring,
/// floating above an enemy's head as a status debuff / mark indicator.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class ArthurEnemyMarkVFX : MonoBehaviour
{
    [Header("=== Target Follow ===")]
    [Tooltip("Target enemy transform to follow. If null, uses this transform.")]
    [SerializeField] private Transform targetEnemy;

    [Tooltip("Offset above the target's origin (default reaches above head).")]
    [SerializeField] private Vector3 headOffset = new Vector3(0f, 1.85f, 0f);

    [Header("=== Skull & Reticle Ring ===")]
    [Tooltip("Material for the skull and surrounded ring (uses Enemy_Skull_Ring.png).")]
    [SerializeField] private Material skullRingMaterial;

    [Tooltip("Vibrant glowing crimson/red HDR color.")]
    [ColorUsage(true, true)]
    [SerializeField] private Color markColor = new Color(2.4f, 0.35f, 0.2f, 1.0f);

    [Tooltip("Diameter of the skull & ring icon (meters).")]
    [SerializeField] private float iconSize = 0.65f;

    [Header("=== Animation & Hover ===")]
    [Tooltip("Speed of the vertical floating bob.")]
    [SerializeField] private float hoverSpeed = 2.4f;

    [Tooltip("Vertical bobbing amplitude (meters).")]
    [SerializeField] private float hoverAmplitude = 0.045f;

    [Tooltip("Speed of the breathing pulse.")]
    [SerializeField] private float pulseSpeed = 3.0f;

    [Tooltip("Pulse scale intensity (0.0 = static).")]
    [SerializeField] private float pulseScale = 0.05f;

    [Header("=== Rising Embers ===")]
    [SerializeField] private bool enableEmberSparks = true;
    [SerializeField] private int emberRate = 5;

    // Components
    private ParticleSystem iconPS;
    private ParticleSystem embersPS;
    private bool isInitialized = false;

    private const string SKULL_MAT_PATH = "Assets/VFX/King Arthur_VFX/Materials/KingArthur_Enemy_SkullRing_Mat.mat";
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

    private void LateUpdate()
    {
        // Smoothly follow target position if assigned
        Vector3 basePos = targetEnemy != null ? targetEnemy.position : transform.position;
        float time = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;

        float hoverY = Mathf.Sin(time * hoverSpeed) * hoverAmplitude;
        Vector3 finalPos = basePos + headOffset + new Vector3(0f, hoverY, 0f);

        if (targetEnemy != null)
        {
            transform.position = finalPos;
        }

        // Apply breathing pulse scale
        float pulse = 1f + Mathf.Sin(time * pulseSpeed) * pulseScale;
        if (iconPS != null)
        {
            iconPS.transform.localScale = Vector3.one * pulse;
        }
    }

    private void InitializeComponents()
    {
        LoadDefaultMaterials();

        // 1. Skull and Reticle Icon
        iconPS = GetOrCreateChildParticleSystem("1_Skull_Reticle_Icon");
        // 2. Rising Ember Sparks
        embersPS = GetOrCreateChildParticleSystem("2_Rising_Ember_Sparks");

        ApplySettings();
        isInitialized = true;
    }

    private void LoadDefaultMaterials()
    {
#if UNITY_EDITOR
        if (skullRingMaterial == null)
            skullRingMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(SKULL_MAT_PATH);
#endif
    }

    public void ApplySettings()
    {
        ConfigureIcon(iconPS);
        ConfigureEmbers(embersPS);
    }

    private void ConfigureIcon(ParticleSystem ps)
    {
        if (ps == null) return;

        bool wasPlaying = ps.isPlaying;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 1.0f;
        main.loop = true;
        main.startLifetime = Mathf.Infinity;
        main.startSpeed = 0f;
        main.startSize = iconSize;
        main.startColor = markColor;
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
            if (skullRingMaterial != null)
                renderer.sharedMaterial = skullRingMaterial;
        }

        if (wasPlaying || !Application.isPlaying)
            ps.Play();
    }

    private void ConfigureEmbers(ParticleSystem ps)
    {
        if (ps == null) return;

        bool wasPlaying = ps.isPlaying;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (!enableEmberSparks) return;

        var main = ps.main;
        main.duration = 2.0f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.startColor = new ParticleSystem.MinMaxGradient(markColor, new Color(1.0f, 0.8f, 0.3f, 1.0f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.playOnAwake = false;
        main.maxParticles = 20;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = emberRate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.28f;
        shape.position = Vector3.zero;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = 0f;
        velocity.y = 0.5f;
        velocity.z = 0f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve ac = new AnimationCurve();
        ac.AddKey(0f, 0f);
        ac.AddKey(0.2f, 1f);
        ac.AddKey(0.8f, 0.8f);
        ac.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, ac);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(markColor, 0.5f),
                new GradientColorKey(new Color(0.8f, 0.1f, 0.05f), 1f)
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
            if (skullRingMaterial != null)
                renderer.sharedMaterial = skullRingMaterial;
        }

        if (wasPlaying || !Application.isPlaying)
            ps.Play();
    }

    [ContextMenu("Play Enemy Mark")]
    public void Play()
    {
        if (!isInitialized) InitializeComponents();
        if (iconPS != null) iconPS.Play();
        if (embersPS != null && enableEmberSparks) embersPS.Play();
    }

    [ContextMenu("Stop Enemy Mark")]
    public void Stop()
    {
        if (iconPS != null) iconPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (embersPS != null) embersPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void SetTarget(Transform target)
    {
        targetEnemy = target;
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
