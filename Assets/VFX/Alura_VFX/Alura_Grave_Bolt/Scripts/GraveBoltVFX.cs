using UnityEngine;
using System.Collections;

/// <summary>
/// GraveBoltVFX - Alura Graves' Grave Bolt Ability VFX.
/// 
/// Exact Visual Design:
/// 1. Start & End Blasts (SAME BLAST, SAME SIZE):
///    - Center has a big bright glowing ball (Glow_Ball_Core.png).
///    - Surrounding big sparkle shaped like an asterisk '*' sign with edges acting as crackling electricity (Electric_Asterisk_Blast.png).
///    - Both Alura's palm (start point) and the enemy target (end point) feature the EXACT SAME blast and same size!
/// 
/// 2. Straight Arrow Projectile:
///    - Straight electric beam running forward from palm to target with length adjustment slider.
/// 
/// 3. Rotating Helix Rope ("Robe Type Stuff Rotating Like A Spark"):
///    - Helical spark rope coiling and spinning continuously around the straight arrow shaft like a spark coil!
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class GraveBoltVFX : MonoBehaviour
{
    // ==========================================
    //  SCENE ANCHORS & TARGETING
    // ==========================================
    [Header("=== Scene Anchors & Targeting ===")]
    [Tooltip("Starting position: Alura's palm / cast hand bone.")]
    [SerializeField] private Transform castHandTransform;

    [Tooltip("Ending position: Enemy target transform.")]
    [SerializeField] private Transform targetTransform;

    [Tooltip("Alura's Animator component (to trigger 'Grave_Bolt' animation clip).")]
    [SerializeField] private Animator aluraAnimator;

    // ==========================================
    //  AUTO-LOOP PLAYBACK
    // ==========================================
    [Header("=== Auto-Loop Playback ===")]
    [Tooltip("Play the ability in a continuous loop when enabled / during play mode.")]
    [SerializeField] private bool loop = true;

    [Tooltip("Delay in seconds between sequence loops.")]
    [SerializeField, Range(0.1f, 3.0f)] private float loopInterval = 0.5f;

    // ==========================================
    //  SEQUENCE TIMING (SECONDS)
    // ==========================================
    [Header("=== Sequence Timing (Seconds) ===")]
    [Tooltip("Duration of Stage [1] Wind-up / Anticipation charge.")]
    [SerializeField, Range(0.1f, 1.0f)] private float windupDuration = 0.35f;

    [Tooltip("Duration of Stage [2] & [3] Arrow bolt strike.")]
    [SerializeField, Range(0.05f, 0.6f)] private float strikeDuration = 0.22f;

    [Tooltip("Duration of Stage [3] Impact burst.")]
    [SerializeField, Range(0.1f, 1.0f)] private float impactDuration = 0.35f;

    [Tooltip("Duration of Stage [4] Recovery & linger fade.")]
    [SerializeField, Range(0.2f, 1.5f)] private float recoveryDuration = 0.50f;

    // ==========================================
    //  START & END POINT BLAST CONTROLS (SAME SIZE)
    // ==========================================
    [Header("=== Start & End Blast Controls (Identical Blasts) ===")]
    [Tooltip("Uniform size for BOTH starting (palm) and ending (target) blasts.")]
    [SerializeField, Range(0.2f, 2.0f)] private float blastScale = 0.70f;

    [Tooltip("Brightness intensity of the center glowing ball.")]
    [SerializeField, Range(0.5f, 4.0f)] private float centerBallBrightness = 1.8f;

    [Tooltip("Relative size of the center glowing ball compared to the asterisk rays.")]
    [SerializeField, Range(0.3f, 1.5f)] private float centerBallSize = 0.65f;

    [Tooltip("Size multiplier for the asterisk '*' electric prongs.")]
    [SerializeField, Range(0.3f, 2.0f)] private float asteriskSparksScale = 1.0f;

    // ==========================================
    //  ARROW SHAFT & LENGTH ADJUSTMENT
    // ==========================================
    [Header("=== Arrow Shaft & Length Adjustment ===")]
    [Tooltip("Keep the central arrow shaft straight to the front.")]
    [SerializeField] private bool straightArrow = true;

    [Tooltip("Manual arrow length override in meters (when enabled).")]
    [SerializeField, Range(0.5f, 12.0f)] private float arrowLength = 3.5f;

    [Tooltip("If true, stretches arrow by manual length slider; if false, reaches exactly to target.")]
    [SerializeField] private bool useManualLength = false;

    [Tooltip("Width of the arrow shaft beam.")]
    [SerializeField, Range(0.02f, 0.35f)] private float arrowWidth = 0.08f;

    [Tooltip("Small electric jitter on the arrow shaft.")]
    [SerializeField, Range(0.0f, 0.25f)] private float arrowShaftJitter = 0.05f;

    // ==========================================
    // ==========================================
    //  INWARD GATHERING SPARKLES (START CHARGE)
    // ==========================================
    [Header("=== Inward Gathering Sparkles (Start Charge) ===")]
    [Tooltip("Enable inward gathering sparkles at the starting hand during wind-up charge.")]
    [SerializeField] private bool enableGatheringSparkles = true;

    [Tooltip("Spawn radius spread around the starting palm (meters).")]
    [SerializeField, Range(0.3f, 2.5f)] private float gatheringRadius = 0.85f;

    [Tooltip("Size scale for the gathering sparkles.")]
    [SerializeField, Range(0.2f, 2.5f)] private float gatheringSparklesScale = 1.0f;

    [Tooltip("Number / density of gathering sparkles.")]
    [SerializeField, Range(10, 80)] private int gatheringSparklesCount = 35;

    // ==========================================
    //  SKULL MOTES (IMPACT BLAST ONLY) & SPARKLES
    // ==========================================
    [Header("=== Skull Motes (Impact Only) & Sparkle Motes ===")]
    [Tooltip("Enable drifting spectral skull motes around impact blast (strictly on target, not at start).")]
    [SerializeField] private bool enableSkullMotes = true;

    [Tooltip("Size scale for the spectral skull motes.")]
    [SerializeField, Range(0.2f, 2.5f)] private float skullMotesScale = 1.0f;

    [Tooltip("Number of skull motes spawned per blast.")]
    [SerializeField, Range(2, 16)] private int skullMotesCount = 6;

    [Tooltip("Enable small '*' sparkle motes around blasts.")]
    [SerializeField] private bool enableSparkleMotes = true;

    [Tooltip("Size scale for the small '*' sparkle motes.")]
    [SerializeField, Range(0.2f, 2.5f)] private float sparkleMotesScale = 1.0f;

    [Tooltip("Number of small '*' sparkle motes spawned per blast.")]
    [SerializeField, Range(6, 40)] private int sparkleMotesCount = 20;

    // ==========================================
    //  STAGE 4 TARGET DEBUFF (CLOUDY LIGHT & SKULLS)
    // ==========================================
    [Header("=== Stage 4 Target Debuff (Cloudy Light & Skulls) ===")]
    [Tooltip("Optional reference to the standalone CloudySkullVFX debuff aura to trigger on enemy during Stage 4 recovery.")]
    [SerializeField] private CloudySkullVFX targetCloudyDebuff;

    // ==========================================
    //  SNAKE-LIKE WAVING ROPES ("Vave Like Snack")
    // ==========================================
    [Header("=== Multiple Snake Ropes ('Vave Like Snack') ===")]
    [Tooltip("Enable multiple snake-like waving ropes around the arrow shaft.")]
    [SerializeField] private bool enableSnakeRopes = true;

    [Tooltip("Amplitude / wave height of the snake undulation (meters).")]
    [SerializeField, Range(0.04f, 0.40f)] private float snakeAmplitude = 0.14f;

    [Tooltip("Number of sinusoidal wave cycles along the arrow shaft.")]
    [SerializeField, Range(1f, 8f)] private float snakeWaves = 3.0f;

    [Tooltip("Swimming / waving speed of the snakes traveling forward along the arrow.")]
    [SerializeField, Range(1f, 20f)] private float snakeSpeed = 7.0f;

    [Tooltip("Width of the snake rope lines.")]
    [SerializeField, Range(0.02f, 0.25f)] private float snakeWidth = 0.06f;

    [Tooltip("Electric crackle jitter on the snake ropes.")]
    [SerializeField, Range(0.0f, 0.15f)] private float snakeElectricJitter = 0.035f;

    // ==========================================
    //  HDR BLOOM & COLOR PALETTE
    // ==========================================
    [Header("=== HDR Bloom & Color Palette ===")]
    [Tooltip("HDR Bloom Multiplier (must be >= 1.5 to trigger URP Bloom filter).")]
    [SerializeField, Range(1.5f, 8.0f)] private float bloomMultiplier = 3.8f;

    [Tooltip("Pure white-hot core glow.")]
    [SerializeField] private Color neonCoreColor = new Color(0.95f, 1.0f, 1.0f, 1.0f);

    [Tooltip("Electric bolt & arc color (Neon Green / Cyan).")]
    [SerializeField] private Color electricGreen = new Color(0.25f, 1.0f, 0.45f, 1.0f);

    [Tooltip("Deep electric rim color.")]
    [SerializeField] private Color toxicJade = new Color(0.08f, 0.65f, 0.35f, 1.0f);

    // ==========================================
    //  MATERIALS
    // ==========================================
    [Header("=== Materials ===")]
    [SerializeField] private Material asteriskBlastMaterial;
    [SerializeField] private Material glowBallMaterial;
    [SerializeField] private Material helixRopeMaterial;
    [SerializeField] private Material lightningMaterial;
    [SerializeField] private Material moteMaterial;
    [SerializeField] private Material skullMaterial;

    // Child Components
    private JaggedLightningBolt lightningBolt;

    // Start Point (Palm)
    private ParticleSystem startAsteriskPS;
    private ParticleSystem startGlowBallPS;
    private ParticleSystem startProngSparksPS;
    private ParticleSystem startGatheringSparklesPS;
    private ParticleSystem startSparkleMotesPS;

    // End Point Blast (Target - includes Skulls!)
    private ParticleSystem endAsteriskPS;
    private ParticleSystem endGlowBallPS;
    private ParticleSystem endProngSparksPS;
    private ParticleSystem endSkullPS;
    private ParticleSystem endSparkleMotesPS;

    private Coroutine sequenceRoutine;
    private string currentStageName = "Idle";
    private bool isPlayingSequence = false;

    // Public Getters & Setters
    public float BlastScale
    {
        get => blastScale;
        set { blastScale = Mathf.Clamp(value, 0.2f, 2.5f); ConfigureAllParticleSystems(); }
    }

    public float CenterBallBrightness
    {
        get => centerBallBrightness;
        set { centerBallBrightness = Mathf.Clamp(value, 0.5f, 5.0f); ConfigureAllParticleSystems(); }
    }

    public float CenterBallSize
    {
        get => centerBallSize;
        set { centerBallSize = Mathf.Clamp(value, 0.2f, 2.0f); ConfigureAllParticleSystems(); }
    }

    public float AsteriskSparksScale
    {
        get => asteriskSparksScale;
        set { asteriskSparksScale = Mathf.Clamp(value, 0.2f, 3.0f); ConfigureAllParticleSystems(); }
    }

    public float ArrowLength
    {
        get => arrowLength;
        set
        {
            arrowLength = Mathf.Clamp(value, 0.5f, 15f);
            if (lightningBolt != null) lightningBolt.ManualLength = arrowLength;
            UpdateImpactAnchorPosition();
        }
    }

    public bool UseManualLength
    {
        get => useManualLength;
        set
        {
            useManualLength = value;
            if (lightningBolt != null) lightningBolt.UseManualLength = useManualLength;
            UpdateImpactAnchorPosition();
        }
    }

    public bool StraightArrow
    {
        get => straightArrow;
        set
        {
            straightArrow = value;
            if (lightningBolt != null) lightningBolt.StraightArrow = straightArrow;
        }
    }

    public bool EnableSnakeRopes
    {
        get => enableSnakeRopes;
        set
        {
            enableSnakeRopes = value;
            if (lightningBolt != null) lightningBolt.EnableSnakeRopes = enableSnakeRopes;
        }
    }

    public float SnakeAmplitude
    {
        get => snakeAmplitude;
        set
        {
            snakeAmplitude = Mathf.Clamp(value, 0.02f, 0.5f);
            if (lightningBolt != null) lightningBolt.SnakeAmplitude = snakeAmplitude;
        }
    }

    public float SnakeWaves
    {
        get => snakeWaves;
        set
        {
            snakeWaves = Mathf.Clamp(value, 1f, 10f);
            if (lightningBolt != null) lightningBolt.SnakeWaves = snakeWaves;
        }
    }

    public float SnakeSpeed
    {
        get => snakeSpeed;
        set
        {
            snakeSpeed = Mathf.Clamp(value, 0f, 30f);
            if (lightningBolt != null) lightningBolt.SnakeSpeed = snakeSpeed;
        }
    }

    public float SnakeWidth
    {
        get => snakeWidth;
        set
        {
            snakeWidth = Mathf.Clamp(value, 0.01f, 0.3f);
            if (lightningBolt != null) lightningBolt.SnakeWidth = snakeWidth;
        }
    }

    public float SnakeElectricJitter
    {
        get => snakeElectricJitter;
        set
        {
            snakeElectricJitter = Mathf.Clamp(value, 0f, 0.2f);
            if (lightningBolt != null) lightningBolt.SnakeElectricJitter = snakeElectricJitter;
        }
    }

    // Backwards compatibility aliases
    public bool EnableHelixRope { get => enableSnakeRopes; set => EnableSnakeRopes = value; }
    public float HelixRadius { get => snakeAmplitude; set => SnakeAmplitude = value; }
    public float HelixCoils { get => snakeWaves; set => SnakeWaves = value; }
    public float HelixRotationSpeed { get => snakeSpeed * 100f; set => SnakeSpeed = value / 100f; }
    public float HelixWidth { get => snakeWidth; set => SnakeWidth = value; }

    public float ArrowWidth
    {
        get => arrowWidth;
        set
        {
            arrowWidth = Mathf.Clamp(value, 0.01f, 0.5f);
            if (lightningBolt != null) lightningBolt.ShaftWidth = arrowWidth;
        }
    }

    public float ArrowShaftJitter
    {
        get => arrowShaftJitter;
        set
        {
            arrowShaftJitter = Mathf.Clamp(value, 0.0f, 0.5f);
            if (lightningBolt != null) lightningBolt.ShaftElectricJitter = arrowShaftJitter;
        }
    }

    public bool EnableGatheringSparkles
    {
        get => enableGatheringSparkles;
        set { enableGatheringSparkles = value; ConfigureAllParticleSystems(); }
    }

    public float GatheringRadius
    {
        get => gatheringRadius;
        set { gatheringRadius = Mathf.Clamp(value, 0.2f, 3.0f); ConfigureAllParticleSystems(); }
    }

    public float GatheringSparklesScale
    {
        get => gatheringSparklesScale;
        set { gatheringSparklesScale = Mathf.Clamp(value, 0.2f, 3.0f); ConfigureAllParticleSystems(); }
    }

    public int GatheringSparklesCount
    {
        get => gatheringSparklesCount;
        set { gatheringSparklesCount = Mathf.Clamp(value, 5, 120); ConfigureAllParticleSystems(); }
    }

    public bool EnableSkullMotes
    {
        get => enableSkullMotes;
        set { enableSkullMotes = value; ConfigureAllParticleSystems(); }
    }

    public float SkullMotesScale
    {
        get => skullMotesScale;
        set { skullMotesScale = Mathf.Clamp(value, 0.2f, 2.5f); ConfigureAllParticleSystems(); }
    }

    public int SkullMotesCount
    {
        get => skullMotesCount;
        set { skullMotesCount = Mathf.Clamp(value, 2, 16); ConfigureAllParticleSystems(); }
    }

    public bool EnableSparkleMotes
    {
        get => enableSparkleMotes;
        set { enableSparkleMotes = value; ConfigureAllParticleSystems(); }
    }

    public float SparkleMotesScale
    {
        get => sparkleMotesScale;
        set { sparkleMotesScale = Mathf.Clamp(value, 0.2f, 2.5f); ConfigureAllParticleSystems(); }
    }

    public int SparkleMotesCount
    {
        get => sparkleMotesCount;
        set { sparkleMotesCount = Mathf.Clamp(value, 6, 40); ConfigureAllParticleSystems(); }
    }

    public float BloomMultiplier
    {
        get => bloomMultiplier;
        set
        {
            bloomMultiplier = Mathf.Clamp(value, 1.5f, 10f);
            ConfigureAllParticleSystems();
            if (lightningBolt != null) lightningBolt.BloomIntensityMultiplier = bloomMultiplier;
        }
    }

    public Color NeonCoreColor
    {
        get => neonCoreColor;
        set
        {
            neonCoreColor = value;
            ConfigureAllParticleSystems();
            if (lightningBolt != null) lightningBolt.CoreColor = neonCoreColor;
        }
    }

    public Color ElectricGreen
    {
        get => electricGreen;
        set
        {
            electricGreen = value;
            ConfigureAllParticleSystems();
            if (lightningBolt != null) lightningBolt.GlowColor = electricGreen;
        }
    }

    public Material AsteriskBlastMaterial { get => asteriskBlastMaterial; set => asteriskBlastMaterial = value; }
    public Material GlowBallMaterial { get => glowBallMaterial; set => glowBallMaterial = value; }
    public Material HelixRopeMaterial { get => helixRopeMaterial; set => helixRopeMaterial = value; }
    public Material LightningMaterial { get => lightningMaterial; set => lightningMaterial = value; }
    public Material MoteMaterial { get => moteMaterial; set => moteMaterial = value; }
    public Material SkullMaterial { get => skullMaterial; set => skullMaterial = value; }

    public JaggedLightningBolt LightningBolt => lightningBolt;
    public string CurrentStageName => currentStageName;
    public bool IsPlayingSequence => isPlayingSequence;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Update()
    {
        UpdateImpactAnchorPosition();
        Transform handAnchor = transform.Find("1_Cast_Hand_Anchor");
        if (handAnchor != null)
        {
            handAnchor.position = GetHandPosition();
        }
    }

    /// <summary>
    /// Calculates the exact tip/end of the arrow.
    /// The impact blast is GUARANTEED to be born directly at this point.
    /// </summary>
    public Vector3 GetEffectiveEndPoint()
    {
        Vector3 handPos = GetHandPosition();
        Vector3 targetPos = GetTargetPosition();
        if (lightningBolt != null)
        {
            return lightningBolt.CalculateEffectiveEndPoint(handPos, targetPos);
        }
        if (useManualLength)
        {
            Vector3 dir = targetPos - handPos;
            if (dir.sqrMagnitude > 0.0001f)
                return handPos + dir.normalized * arrowLength;
        }
        return targetPos;
    }

    /// <summary>
    /// Locks 3_Target_Impact_Anchor position to the exact tip of the arrow.
    /// </summary>
    public void UpdateImpactAnchorPosition()
    {
        Transform impactAnchor = transform.Find("3_Target_Impact_Anchor");
        if (impactAnchor != null)
        {
            impactAnchor.position = GetEffectiveEndPoint();
        }
    }

    private void OnEnable()
    {
        EnsureInitialized();
        if (Application.isPlaying)
        {
            if (loop)
            {
                StartLoop();
            }
            else
            {
                CastGraveBolt();
            }
        }
    }

    private void OnDisable()
    {
        StopLoop();
        ResetAll();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)) return;

        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && !UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this) && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                CleanupLegacyAndOffendingObjects();
                ConfigureAllParticleSystems();
            }
        };
#endif
    }

    private bool isInitialized = false;

    public void EnsureInitialized(bool force = false)
    {
#if UNITY_EDITOR
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)) return;
#endif
        if (isInitialized && !force) return;

        CleanupLegacyAndOffendingObjects();
        AutoFindReferences(false);
        InitializeComponents();
        isInitialized = true;
    }

    /// <summary>
    /// Safely cleans up all legacy cluttered objects (e.g. massive blue trees, skulls, mist boxes).
    /// </summary>
    public void CleanupLegacyAndOffendingObjects()
    {
#if UNITY_EDITOR
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)) return;
#endif
        string[] badNames = new string[] {
            "Start_Skull_Motes",
            "Impact_Jagged_Arcs",
            "Impact_Ground_Crawlers",
            "Impact_Stylized_Burst",
            "Leading_Edge_Head",
            "Palm_Core_Glow",
            "Impact_Mist",
            "Escort_Skull_Motes",
            "Electric_Branch_Arc",
            "Electric_Spearhead",
            "Muzzle_Flash",
            "Impact_Flash",
            "Escort_Skulls",
            "Impact_Skulls",
            "Traveling_Flame",
            "Flame_Power_Trail",
            "Escort_Sparkles",
            "Charge_Inward_Suction",
            "Charge_Sparkles",
            "Hand_Flame",
            "Impact_Flame",
            "Impact_Sparks"
        };

        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = all.Length - 1; i >= 0; i--)
        {
            Transform t = all[i];
            if (t == null || t == transform) continue;
            for (int b = 0; b < badNames.Length; b++)
            {
                if (t.name.Equals(badNames[b], System.StringComparison.OrdinalIgnoreCase))
                {
                    t.gameObject.SetActive(false);
                    if (Application.isPlaying)
                    {
                        Destroy(t.gameObject);
                    }
                    else
                    {
#if UNITY_EDITOR
                        if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(t.gameObject))
                        {
                            GameObject go = t.gameObject;
                            UnityEditor.EditorApplication.delayCall += () =>
                            {
                                if (go != null && !UnityEditor.PrefabUtility.IsPartOfPrefabAsset(go))
                                    DestroyImmediate(go);
                            };
                        }
#else
                        Destroy(t.gameObject);
#endif
                    }
                    break;
                }
            }
        }
    }

    public void AutoFindReferences(bool force = false)
    {
        if (aluraAnimator == null || force)
        {
            GameObject aluraGO = GameObject.Find("Alura_Grave_Bolt");
            if (aluraGO != null) aluraAnimator = aluraGO.GetComponent<Animator>();
        }

        if (castHandTransform == null || force)
        {
            castHandTransform = FindAluraHandBone();
        }

        if (targetTransform == null || force)
        {
            targetTransform = FindEnemyTarget();
        }

        if (targetCloudyDebuff == null || force)
        {
            targetCloudyDebuff = FindFirstObjectByType<CloudySkullVFX>();
        }

#if UNITY_EDITOR
        if (asteriskBlastMaterial == null)
            asteriskBlastMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Grave_Bolt/Materials/GraveBolt_AsteriskBlast_Mat.mat");
        if (glowBallMaterial == null)
            glowBallMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Grave_Bolt/Materials/GraveBolt_GlowBall_Mat.mat");
        if (helixRopeMaterial == null)
            helixRopeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Grave_Bolt/Materials/GraveBolt_HelixRope_Mat.mat");
        if (lightningMaterial == null)
            lightningMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Grave_Bolt/Materials/GraveBolt_Lightning_Mat.mat");
        if (moteMaterial == null)
            moteMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Grave_Bolt/Materials/GraveBolt_Mote_Mat.mat");
        if (skullMaterial == null)
            skullMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Grave_Bolt/Materials/GraveBolt_Skull_Mat.mat");
#endif
    }

    private Transform FindAluraHandBone()
    {
        string[] handNames = new string[] {
            "hand.l", "hand_l", "left_hand", "lefthand", "wrist_l", "wrist.l",
            "mixamorig:LeftHand", "b_LeftHand", "LeftHand"
        };

        GameObject aluraGO = GameObject.Find("Alura_Grave_Bolt");
        if (aluraGO != null)
        {
            for (int i = 0; i < handNames.Length; i++)
            {
                Transform found = FindRecursive(aluraGO.transform, handNames[i]);
                if (found != null) return found;
            }
        }
        return null;
    }

    private Transform FindEnemyTarget()
    {
        string[] enemyNames = new string[] {
            "Enemy", "Dummy", "Target", "Affected_Enemy", "Miasma_Affected_Enemy", "Chest", "Spine"
        };

        for (int i = 0; i < enemyNames.Length; i++)
        {
            GameObject go = GameObject.Find(enemyNames[i]);
            if (go != null && go != gameObject)
            {
                Transform chest = FindRecursive(go.transform, "chest");
                if (chest != null) return chest;
                Transform spine = FindRecursive(go.transform, "spine");
                if (spine != null) return spine;
                return go.transform;
            }
        }
        return null;
    }

    private Transform FindRecursive(Transform root, string targetName)
    {
        if (root == null) return null;
        if (root.name.IndexOf(targetName, System.StringComparison.OrdinalIgnoreCase) >= 0) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindRecursive(root.GetChild(i), targetName);
            if (found != null) return found;
        }
        return null;
    }

    public void InitializeComponents()
    {
        CleanupLegacyAndOffendingObjects();

        // 1. Cast Hand Anchor (Start Point)
        Transform handAnchor = transform.Find("1_Cast_Hand_Anchor");
        if (handAnchor == null)
        {
            GameObject go = new GameObject("1_Cast_Hand_Anchor");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(-0.25f, 0.92f, 0.35f);
            handAnchor = go.transform;
        }

        startAsteriskPS = GetOrCreateChildPS(handAnchor, "Start_Asterisk_Blast");
        startGlowBallPS = GetOrCreateChildPS(handAnchor, "Start_Glow_Ball");
        startProngSparksPS = GetOrCreateChildPS(handAnchor, "Start_Prong_Sparks");
        startGatheringSparklesPS = GetOrCreateChildPS(handAnchor, "Start_Gathering_Sparkles");
        startSparkleMotesPS = GetOrCreateChildPS(handAnchor, "Start_Sparkle_Motes");

        // 2. Arrow Beam & Multiple Snake Waving Ropes ("Vave Like Snack")
        Transform boltTransform = transform.Find("2_Jagged_Bolt_Beam");
        if (boltTransform == null)
        {
            GameObject boltGO = new GameObject("2_Jagged_Bolt_Beam");
            boltGO.transform.SetParent(transform, false);
            boltTransform = boltGO.transform;
        }
        lightningBolt = boltTransform.GetComponent<JaggedLightningBolt>();
        if (lightningBolt == null)
            lightningBolt = boltTransform.gameObject.AddComponent<JaggedLightningBolt>();

        lightningBolt.CoreColor = neonCoreColor;
        lightningBolt.GlowColor = electricGreen;
        lightningBolt.BloomIntensityMultiplier = bloomMultiplier;
        lightningBolt.StraightArrow = straightArrow;
        lightningBolt.ManualLength = arrowLength;
        lightningBolt.UseManualLength = useManualLength;
        lightningBolt.ShaftWidth = arrowWidth;
        lightningBolt.ShaftElectricJitter = arrowShaftJitter;
        lightningBolt.EnableSnakeRopes = enableSnakeRopes;
        lightningBolt.SnakeAmplitude = snakeAmplitude;
        lightningBolt.SnakeWaves = snakeWaves;
        lightningBolt.SnakeSpeed = snakeSpeed;
        lightningBolt.SnakeWidth = snakeWidth;
        lightningBolt.SnakeElectricJitter = snakeElectricJitter;
        if (helixRopeMaterial != null) lightningBolt.SnakeRopeMaterial = helixRopeMaterial;
        lightningBolt.EnsureComponents();

        LineRenderer lr = boltTransform.GetComponent<LineRenderer>();
        if (lr != null && lightningMaterial != null) lr.material = lightningMaterial;

        // 3. Target Impact Anchor (End Point - BORN DIRECTLY AT END OF ARROW)
        Transform impactAnchor = transform.Find("3_Target_Impact_Anchor");
        if (impactAnchor == null)
        {
            GameObject go = new GameObject("3_Target_Impact_Anchor");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(-1.66f, 1.1f, 2.83f);
            impactAnchor = go.transform;
        }

        endAsteriskPS = GetOrCreateChildPS(impactAnchor, "End_Asterisk_Blast");
        endGlowBallPS = GetOrCreateChildPS(impactAnchor, "End_Glow_Ball");
        endProngSparksPS = GetOrCreateChildPS(impactAnchor, "End_Prong_Sparks");
        endSkullPS = GetOrCreateChildPS(impactAnchor, "End_Skull_Motes");
        endSparkleMotesPS = GetOrCreateChildPS(impactAnchor, "End_Sparkle_Motes");

        UpdateImpactAnchorPosition();
        ConfigureAllParticleSystems();
    }

    private ParticleSystem GetOrCreateChildPS(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            child = go.transform;
        }
        ParticleSystem ps = child.GetComponent<ParticleSystem>();
        if (ps == null) ps = child.gameObject.AddComponent<ParticleSystem>();
        return ps;
    }

    /// <summary>
    /// Configures the identical Start and End blasts:
    /// - Asterisk '*' electric prongs burst (Electric_Asterisk_Blast.png) with animated crackle
    /// - Center glowing ball (Glow_Ball_Core.png)
    /// - Radial needle prongs sparks (plentiful & animated)
    /// - Floating spectral skull motes (Miasma_Spectral_Skull.png)
    /// - Twinkling small '*' sparkle motes (Sparkle_4Point.png)
    /// - Start blast (palm): Big glowing ball + '*' electric asterisk + radial needle sparks + sparkle motes. NO SKULLS!
    /// - Start charge: Inward gathering sparkles spread randomly in radius and size, collecting inward to the palm.
    /// - End blast (target): Big glowing ball + '*' electric asterisk + needle sparks + sparkle motes + floating spectral skulls!
    /// </summary>
    public void ConfigureAllParticleSystems()
    {
        float s = blastScale;
        Color hdrBloomCore = neonCoreColor * (bloomMultiplier * 1.6f);
        Color hdrBloomGreen = electricGreen * (bloomMultiplier * 1.3f);
        Color ballColor = Color.white * (bloomMultiplier * centerBallBrightness);

        // Configure START Blast (Palm - NO SKULLS!)
        ConfigureSingleBlast(startAsteriskPS, startGlowBallPS, startProngSparksPS, null, startSparkleMotesPS,
                             s, hdrBloomCore, hdrBloomGreen, ballColor);

        // Configure Inward Gathering Sparkles at Palm (Wind-up charge)
        ConfigureGatheringSparkles(startGatheringSparklesPS, s, hdrBloomCore, hdrBloomGreen);

        // Configure END Blast (Target - includes floating spectral skulls!)
        ConfigureSingleBlast(endAsteriskPS, endGlowBallPS, endProngSparksPS, endSkullPS, endSparkleMotesPS,
                             s, hdrBloomCore, hdrBloomGreen, ballColor);
    }

    private void ConfigureSingleBlast(ParticleSystem asteriskPS, ParticleSystem glowBallPS, ParticleSystem sparksPS,
                                       ParticleSystem skullPS, ParticleSystem sparkleMotesPS,
                                       float scale, Color coreCol, Color greenCol, Color ballCol)
    {
        // 1. Asterisk '*' Electric Prongs Blast (Randomly Animated Rapid Electric Crackle)
        if (asteriskPS != null)
        {
            var main = asteriskPS.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.32f;
            float astSize = 0.85f * scale * asteriskSparksScale;
            main.startSize = astSize;
            main.startSpeed = 0f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = greenCol;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = asteriskPS.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            // 2 overlapping quads with rapid rotation for energetic electric crackle
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 2) });

            var shape = asteriskPS.shape;
            shape.enabled = false;

            var rot = asteriskPS.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-360f * Mathf.Deg2Rad, 360f * Mathf.Deg2Rad);

            var sol = asteriskPS.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve sc = new AnimationCurve(
                new Keyframe(0f, 0.4f),
                new Keyframe(0.2f, 1.25f),
                new Keyframe(0.7f, 1.0f),
                new Keyframe(1f, 0.12f)
            );
            sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

            var col = asteriskPS.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(coreCol, 0f), new GradientColorKey(greenCol, 0.45f), new GradientColorKey(toxicJade, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.95f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            var renderer = asteriskPS.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (asteriskBlastMaterial != null) renderer.material = asteriskBlastMaterial;
        }

        // 2. Center Big Glowing Ball
        if (glowBallPS != null)
        {
            var main = glowBallPS.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.35f;
            float ballSize = 0.60f * scale * centerBallSize;
            main.startSize = ballSize;
            main.startSpeed = 0f;
            main.startColor = ballCol;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = glowBallPS.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

            var shape = glowBallPS.shape;
            shape.enabled = false;

            var sol = glowBallPS.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve bc = new AnimationCurve(
                new Keyframe(0f, 0.6f),
                new Keyframe(0.25f, 1.25f),
                new Keyframe(0.75f, 0.95f),
                new Keyframe(1f, 0.15f)
            );
            sol.size = new ParticleSystem.MinMaxCurve(1f, bc);

            var col = glowBallPS.colorOverLifetime;
            col.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white * (bloomMultiplier * centerBallBrightness), 0f), new GradientColorKey(greenCol, 0.7f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.6f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = grad;

            var renderer = glowBallPS.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (glowBallMaterial != null) renderer.material = glowBallMaterial;
        }

        // 3. Radial Needle Sparks (Act as Electricity off the Prongs - Plentiful & Animated)
        if (sparksPS != null)
        {
            var main = sparksPS.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.14f, 0.28f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f * scale, 0.13f * scale);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.0f * scale, 6.5f * scale);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = greenCol;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = sparksPS.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 24, 34) });

            var shape = sparksPS.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f * scale;

            var limit = sparksPS.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = 3.8f;

            var rot = sparksPS.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-720f * Mathf.Deg2Rad, 720f * Mathf.Deg2Rad);

            var sol = sparksPS.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1.25f, 1f, 0.08f));

            var renderer = sparksPS.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (moteMaterial != null) renderer.material = moteMaterial;
        }

        // 4. Floating Spectral Skull Motes Around Blast
        if (skullPS != null)
        {
            var main = skullPS.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 0.95f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.20f * scale * skullMotesScale, 0.32f * scale * skullMotesScale);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * scale, 1.3f * scale);
            main.startRotation = new ParticleSystem.MinMaxCurve(-35f * Mathf.Deg2Rad, 35f * Mathf.Deg2Rad);
            main.startColor = greenCol;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = skullPS.emission;
            emission.enabled = enableSkullMotes;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)skullMotesCount) });

            var shape = skullPS.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.28f * scale;

            var vel = skullPS.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.15f * scale, 0.15f * scale);
            vel.y = new ParticleSystem.MinMaxCurve(0.5f * scale, 1.1f * scale);
            vel.z = new ParticleSystem.MinMaxCurve(-0.15f * scale, 0.15f * scale);

            var sol = skullPS.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve skullCurve = new AnimationCurve(
                new Keyframe(0f, 0.25f),
                new Keyframe(0.25f, 1.15f),
                new Keyframe(0.7f, 1.0f),
                new Keyframe(1f, 0.08f)
            );
            sol.size = new ParticleSystem.MinMaxCurve(1f, skullCurve);

            var col = skullPS.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(coreCol, 0f), new GradientColorKey(greenCol, 0.5f), new GradientColorKey(toxicJade, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.85f, 0.7f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = g;

            var renderer = skullPS.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (skullMaterial != null) renderer.material = skullMaterial;
        }

        // 5. Small '*' Twinkling Sparkle Motes Around Blast
        if (sparkleMotesPS != null)
        {
            var main = sparkleMotesPS.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f * scale * sparkleMotesScale, 0.14f * scale * sparkleMotesScale);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f * scale, 3.0f * scale);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = sparkleMotesPS.emission;
            emission.enabled = enableSparkleMotes;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)sparkleMotesCount) });

            var shape = sparkleMotesPS.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f * scale;

            var limit = sparkleMotesPS.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.drag = 2.8f;

            var rot = sparkleMotesPS.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-540f * Mathf.Deg2Rad, 540f * Mathf.Deg2Rad);

            var sol = sparkleMotesPS.sizeOverLifetime;
            sol.enabled = true;
            AnimationCurve sparkleCurve = new AnimationCurve(
                new Keyframe(0f, 0.2f),
                new Keyframe(0.25f, 1.25f),
                new Keyframe(0.7f, 0.9f),
                new Keyframe(1f, 0.05f)
            );
            sol.size = new ParticleSystem.MinMaxCurve(1f, sparkleCurve);

            var col = sparkleMotesPS.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white * (bloomMultiplier * 1.5f), 0f), new GradientColorKey(greenCol, 0.6f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.65f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = g;

            var renderer = sparkleMotesPS.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (moteMaterial != null) renderer.material = moteMaterial;
        }
    }

    /// <summary>
    /// Configures the inward gathering sparkles at the starting hand:
    /// - Particles spawn spread randomly across a sphere radius around the palm.
    /// - Variable random sizes (small to prominent motes).
    /// - Inward negative startSpeed pulls them all directly to the palm center (0, 0, 0).
    /// - Right as they gather to the center point, the wind-up finishes and detonates the blast!
    /// </summary>
    private void ConfigureGatheringSparkles(ParticleSystem ps, float scale, Color coreCol, Color greenCol)
    {
        if (ps == null) return;
        if (ps.isPlaying) return;

        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        float dur = Mathf.Max(0.2f, windupDuration);
        main.duration = dur;
        // Sparkle lifetime matches wind-up travel time so they converge right at detonation
        main.startLifetime = new ParticleSystem.MinMaxCurve(dur * 0.45f, dur * 0.90f);

        // Randomly spread sizes: tiny sharp sparkles and larger glowing motes
        float baseSize = 0.08f * scale * gatheringSparklesScale;
        main.startSize = new ParticleSystem.MinMaxCurve(baseSize * 0.35f, baseSize * 1.55f);

        // Negative startSpeed: radial normal points outwards, so negative velocity draws them inwards to (0,0,0)
        // Ensure min <= max for valid AABB bounds!
        float targetSpeed = (gatheringRadius / Mathf.Max(0.15f, dur * 0.70f));
        main.startSpeed = new ParticleSystem.MinMaxCurve(-targetSpeed * 1.35f, -targetSpeed * 0.85f);

        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.Local; // Locks center directly to palm bone

        var emission = ps.emission;
        emission.enabled = enableGatheringSparkles;
        emission.rateOverTime = gatheringSparklesCount / dur * 0.7f;
        // Two staggered bursts so sparkles spawn spread in waves and collect inward
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, (short)Mathf.Max(4, gatheringSparklesCount / 3)),
            new ParticleSystem.Burst(dur * 0.25f, (short)Mathf.Max(6, gatheringSparklesCount / 2))
        });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = gatheringRadius;
        shape.radiusThickness = 0.85f; // Randomly spread throughout 15% to 100% of radius volume

        var limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.drag = 1.8f;

        var rot = ps.rotationOverLifetime;
        rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-720f * Mathf.Deg2Rad, 720f * Mathf.Deg2Rad);

        // Size over lifetime: starts small, swells bright while gathering inward, pinches to pinpoint at center
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve gatherSizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(0.35f, 1.3f),
            new Keyframe(0.75f, 1.0f),
            new Keyframe(1f, 0.05f)
        );
        sol.size = new ParticleSystem.MinMaxCurve(1f, gatherSizeCurve);

        // Color over lifetime: neon core glow, fading in and out smoothly
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white * (bloomMultiplier * 1.6f), 0f),
                new GradientColorKey(greenCol, 0.5f),
                new GradientColorKey(toxicJade, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.15f),
                new GradientAlphaKey(1f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        col.color = g;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (moteMaterial != null) renderer.material = moteMaterial;
    }

    public Vector3 GetHandPosition()
    {
        if (castHandTransform != null && castHandTransform.name != "1_Cast_Hand_Anchor" &&
            (castHandTransform.name.IndexOf("hand", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             castHandTransform.name.IndexOf("wrist", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             castHandTransform.name.IndexOf("palm", System.StringComparison.OrdinalIgnoreCase) >= 0))
        {
            return castHandTransform.position;
        }

        Transform handBone = FindAluraHandBone();
        if (handBone != null)
        {
            castHandTransform = handBone;
            return handBone.position;
        }

        if (aluraAnimator != null)
        {
            Transform at = aluraAnimator.transform;
            return at.position + at.forward * 0.55f + Vector3.up * 0.95f - at.right * 0.22f;
        }

        return transform.position + transform.forward * 0.55f + Vector3.up * 0.95f - transform.right * 0.22f;
    }

    public Vector3 GetTargetPosition()
    {
        if (targetTransform != null && targetTransform.name != "3_Target_Impact_Anchor")
        {
            if (targetTransform.name.IndexOf("chest", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                targetTransform.name.IndexOf("spine", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return targetTransform.position;
            }
            return targetTransform.position + Vector3.up * 1.1f;
        }

        Transform enemyTarget = FindEnemyTarget();
        if (enemyTarget != null)
        {
            targetTransform = enemyTarget;
            return enemyTarget.position + Vector3.up * 1.1f;
        }

        return transform.position + new Vector3(-1.66f, 1.1f, 2.83f);
    }

    public void CastGraveBolt()
    {
        EnsureInitialized();

        if (isPlayingSequence)
        {
            ResetAll();
        }

        if (Application.isPlaying)
        {
            if (loop)
            {
                StartLoop();
            }
            else
            {
                sequenceRoutine = StartCoroutine(AnimateFullSequence());
            }
        }
        else
        {
#if UNITY_EDITOR
            TriggerEditorSequence();
#else
            PlayStage2_CastRelease();
            PlayStage3_Impact();
#endif
        }
    }

    public void StartLoop()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }
        sequenceRoutine = StartCoroutine(LoopSequenceRoutine());
    }

    public void StopLoop()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }
        ResetAll();
    }

    private IEnumerator LoopSequenceRoutine()
    {
        while (loop && isActiveAndEnabled)
        {
            yield return AnimateFullSequence();
            yield return new WaitForSeconds(loopInterval);
        }
    }

    private void PlayAluraAnimation()
    {
        if (aluraAnimator == null) return;
        if (aluraAnimator.HasState(0, Animator.StringToHash("01")))
        {
            aluraAnimator.Play("01", 0, 0f);
        }
        else if (aluraAnimator.HasState(0, Animator.StringToHash("Grave_Bolt")))
        {
            aluraAnimator.Play("Grave_Bolt", 0, 0f);
        }
        else if (aluraAnimator.HasState(0, Animator.StringToHash("Attack")))
        {
            aluraAnimator.Play("Attack", 0, 0f);
        }
        else
        {
            var stateInfo = aluraAnimator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash != 0)
                aluraAnimator.Play(stateInfo.shortNameHash, 0, 0f);
        }
    }

    public void PlayStage1_Windup()
    {
        EnsureInitialized();
        currentStageName = "Stage [1]: Wind-Up (Gathering Sparkles Charge)";
        isPlayingSequence = true;

        PlayAluraAnimation();

        Vector3 handPos = GetHandPosition();
        Transform handAnchor = transform.Find("1_Cast_Hand_Anchor");
        if (handAnchor != null) handAnchor.position = handPos;

        // Trigger Inward Gathering Sparkles: spread randomly in radius and size, collecting into palm to form the blast
        if (enableGatheringSparkles && startGatheringSparklesPS != null)
        {
            startGatheringSparklesPS.transform.localPosition = Vector3.zero;
            startGatheringSparklesPS.Play();
        }
    }

    public void PlayStage2_CastRelease()
    {
        EnsureInitialized();
        currentStageName = "Stage [2]: Cast Straight Arrow & Rotating Rope";
        isPlayingSequence = true;

        Vector3 handPos = GetHandPosition();
        Vector3 targetPos = GetTargetPosition();

        Transform handAnchor = transform.Find("1_Cast_Hand_Anchor");
        if (handAnchor != null) handAnchor.position = handPos;

        // 1. Trigger START POINT BLAST at Alura's palm
        // (Big glowing ball + '*' asterisk electric prongs + radial sparks + motes - NO SKULLS!)
        if (startAsteriskPS != null)
        {
            startAsteriskPS.transform.position = handPos;
            startAsteriskPS.Play();
        }
        if (startGlowBallPS != null)
        {
            startGlowBallPS.transform.position = handPos;
            startGlowBallPS.Play();
        }
        if (startProngSparksPS != null)
        {
            startProngSparksPS.transform.position = handPos;
            startProngSparksPS.Play();
        }
        if (enableSparkleMotes && startSparkleMotesPS != null)
        {
            startSparkleMotesPS.transform.position = handPos;
            startSparkleMotesPS.Play();
        }

        // 2. Fire Straight Arrow and Multiple Snake Waving Ropes ("Vave Like Snack")
        float flightTime = Mathf.Max(0.08f, strikeDuration * 0.85f);
        if (lightningBolt != null)
        {
            lightningBolt.CoreColor = neonCoreColor;
            lightningBolt.GlowColor = electricGreen;
            lightningBolt.BloomIntensityMultiplier = bloomMultiplier;
            lightningBolt.StraightArrow = straightArrow;
            lightningBolt.ManualLength = arrowLength;
            lightningBolt.UseManualLength = useManualLength;
            lightningBolt.ShaftWidth = arrowWidth;
            lightningBolt.ShaftElectricJitter = arrowShaftJitter;
            lightningBolt.EnableSnakeRopes = enableSnakeRopes;
            lightningBolt.SnakeAmplitude = snakeAmplitude;
            lightningBolt.SnakeWaves = snakeWaves;
            lightningBolt.SnakeSpeed = snakeSpeed;
            lightningBolt.SnakeWidth = snakeWidth;
            lightningBolt.SnakeElectricJitter = snakeElectricJitter;

            lightningBolt.FireProjectile(
                handPos,
                targetPos,
                flightTime,
                (headPos) => { },
                () =>
                {
                    // 3. Arrow reaches destination -> Trigger END POINT BLAST
                    // (EXACT SAME BLAST, EXACT SAME SIZE, BORN DIRECTLY AT END OF ARROW)
                    PlayStage3_Impact();
                }
            );
        }
    }

    public void PlayStage3_Impact()
    {
        EnsureInitialized();
        currentStageName = "Stage [3]: Impact Blast (Born At End Of Arrow)";
        isPlayingSequence = true;

        Vector3 impactPos = GetEffectiveEndPoint();
        Transform impactAnchor = transform.Find("3_Target_Impact_Anchor");
        if (impactAnchor != null) impactAnchor.position = impactPos;

        // Trigger END POINT BLAST directly at arrow tip!
        // (EXACT SAME BLAST, EXACT SAME SIZE AS START POINT BLAST)
        if (endAsteriskPS != null)
        {
            endAsteriskPS.transform.position = impactPos;
            endAsteriskPS.Play();
        }
        if (endGlowBallPS != null)
        {
            endGlowBallPS.transform.position = impactPos;
            endGlowBallPS.Play();
        }
        if (endProngSparksPS != null)
        {
            endProngSparksPS.transform.position = impactPos;
            endProngSparksPS.Play();
        }
        if (enableSkullMotes && endSkullPS != null)
        {
            endSkullPS.transform.position = impactPos;
            endSkullPS.Play();
        }
        if (enableSparkleMotes && endSparkleMotesPS != null)
        {
            endSparkleMotesPS.transform.position = impactPos;
            endSparkleMotesPS.Play();
        }
    }

    public void PlayStage4_Recovery()
    {
        currentStageName = "Stage [4]: Recovery / Fade";
        if (lightningBolt != null) lightningBolt.StopStrike();
        if (targetCloudyDebuff != null) targetCloudyDebuff.Play();
    }

    public void ResetAll()
    {
        currentStageName = "Idle";
        isPlayingSequence = false;

        if (targetCloudyDebuff != null && !targetCloudyDebuff.Loop)
        {
            targetCloudyDebuff.Stop();
        }

        if (sequenceRoutine != null && Application.isPlaying)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= EditorSequenceUpdate;
        editorStageStep = 0;
#endif

        if (lightningBolt != null)
        {
            lightningBolt.LiveScenePreview = false;
            lightningBolt.StopStrike();
        }

        ParticleSystem[] allPS = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in allPS)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        CleanupLegacyAndOffendingObjects();
    }

    private IEnumerator AnimateFullSequence()
    {
        isPlayingSequence = true;

        // Stage 1: Wind-up / Anticipation
        PlayStage1_Windup();
        yield return new WaitForSeconds(windupDuration);

        // Stage 2: Cast / Release (Fires arrow with rotating rope; calls PlayStage3_Impact on arrival)
        PlayStage2_CastRelease();

        yield return new WaitForSeconds(strikeDuration + impactDuration);

        // Stage 4: Recovery / Fade
        PlayStage4_Recovery();
        yield return new WaitForSeconds(recoveryDuration);

        currentStageName = "Idle";
        isPlayingSequence = false;
        sequenceRoutine = null;
    }

#if UNITY_EDITOR
    private double editorStartTime;
    private int editorStageStep = 0;
    private bool editorLooping = false;

    public bool EditorLooping => editorLooping;

    public void ToggleEditorLoop()
    {
        editorLooping = !editorLooping;
        if (editorLooping)
        {
            TriggerEditorSequence();
        }
        else
        {
            ResetAll();
        }
    }

    private void TriggerEditorSequence()
    {
        EnsureInitialized();
        editorStartTime = UnityEditor.EditorApplication.timeSinceStartup;
        editorStageStep = 1;
        PlayStage1_Windup();

        UnityEditor.EditorApplication.update -= EditorSequenceUpdate;
        UnityEditor.EditorApplication.update += EditorSequenceUpdate;
    }

    private void EditorSequenceUpdate()
    {
        if (this == null)
        {
            UnityEditor.EditorApplication.update -= EditorSequenceUpdate;
            return;
        }

        float elapsed = (float)(UnityEditor.EditorApplication.timeSinceStartup - editorStartTime);
        float flightTime = Mathf.Max(0.08f, strikeDuration * 0.85f);
        Vector3 handPos = GetHandPosition();
        Vector3 targetPos = GetTargetPosition();

        if (editorStageStep == 1 && elapsed >= windupDuration)
        {
            editorStageStep = 2;
            PlayStage2_CastRelease();
        }
        else if (editorStageStep == 2)
        {
            float flightElapsed = elapsed - windupDuration;
            if (flightElapsed < flightTime)
            {
                float t = Mathf.Clamp01(flightElapsed / flightTime);
                float smoothT = t * t * (3f - 2f * t);
                Vector3 finalEnd = lightningBolt != null ? lightningBolt.CalculateEffectiveEndPoint(handPos, targetPos) : targetPos;
                Vector3 currentHead = Vector3.Lerp(handPos, finalEnd, smoothT);
                if (lightningBolt != null)
                {
                    lightningBolt.GenerateJaggedBolt(handPos, currentHead);
                }
                UnityEditor.SceneView.RepaintAll();
            }
            else
            {
                editorStageStep = 3;
                PlayStage3_Impact();
                UnityEditor.SceneView.RepaintAll();
            }
        }
        else if (editorStageStep == 3 && elapsed >= windupDuration + flightTime + impactDuration)
        {
            editorStageStep = 4;
            PlayStage4_Recovery();
        }
        else if (editorStageStep == 4 && elapsed >= windupDuration + flightTime + impactDuration + recoveryDuration)
        {
            if (editorLooping)
            {
                if (elapsed >= windupDuration + flightTime + impactDuration + recoveryDuration + loopInterval)
                {
                    editorStartTime = UnityEditor.EditorApplication.timeSinceStartup;
                    editorStageStep = 1;
                    PlayStage1_Windup();
                }
            }
            else
            {
                UnityEditor.EditorApplication.update -= EditorSequenceUpdate;
                editorStageStep = 0;
                currentStageName = "Idle";
                isPlayingSequence = false;
            }
        }

        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
