using UnityEngine;
using System.Collections;

/// <summary>
/// JaggedLightningBolt - Straight electric arrow projectile with twin undulating snake ropes ("vave like snack").
/// Features:
/// - Straight central arrow shaft with length adjustment.
/// - Multiple serpentine electric ropes that wave and undulate like swimming snakes around the arrow shaft.
/// - Procedural LineRenderers with zero clutter and complete cleanup of legacy meshes.
/// - Always positions the impact blast directly on the tip of the arrow.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
[ExecuteAlways]
public class JaggedLightningBolt : MonoBehaviour
{
    // ==========================================
    //  ARROW GEOMETRY & LENGTH
    // ==========================================
    [Header("=== Arrow Shaft & Length Adjustment ===")]
    [Tooltip("Keep the central arrow shaft straight to the front.")]
    [SerializeField] private bool straightArrow = true;

    [Tooltip("Manual arrow length override in meters (when enabled).")]
    [SerializeField, Range(0.5f, 12.0f)] private float manualLength = 3.5f;

    [Tooltip("If true, stretches arrow by manual length slider; if false, reaches exactly to target.")]
    [SerializeField] private bool useManualLength = false;

    [Tooltip("Number of segments along the arrow shaft.")]
    [SerializeField, Range(8, 48)] private int segments = 28;

    [Tooltip("Small electric jagged jitter on the arrow shaft.")]
    [SerializeField, Range(0.0f, 0.25f)] private float shaftElectricJitter = 0.04f;

    [Tooltip("Width of the arrow shaft beam.")]
    [SerializeField, Range(0.02f, 0.35f)] private float shaftWidth = 0.09f;

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

    [Tooltip("Material for the undulating snake ropes.")]
    [SerializeField] private Material snakeRopeMaterial;

    // ==========================================
    //  HDR BLOOM & COLORS
    // ==========================================
    [Header("=== Colors & HDR Bloom ===")]
    [Tooltip("HDR Bloom Intensity multiplier (must be >= 1.5 to bloom in URP).")]
    [SerializeField, Range(1.5f, 8.0f)] private float bloomIntensityMultiplier = 3.8f;

    [Tooltip("Pure white-hot core color.")]
    [SerializeField] private Color coreColor = new Color(0.95f, 1.0f, 1.0f, 1.0f);

    [Tooltip("Electric neon green/cyan glow color.")]
    [SerializeField] private Color glowColor = new Color(0.25f, 1.0f, 0.45f, 1.0f);

    [Header("=== Live Editor Preview ===")]
    [Tooltip("Keep crackling arrow and waving snake ropes visible in Scene view.")]
    [SerializeField] private bool liveScenePreview = false;

    // Child LineRenderers: Shaft + 3 Snake Ropes ("Multiple Robe On Arrow")
    private LineRenderer arrowLineRenderer;
    private LineRenderer snakeRope1;
    private LineRenderer snakeRope2;
    private LineRenderer snakeRope3;

    private Vector3[] arrowPoints;
    private Vector3[] snake1Points;
    private Vector3[] snake2Points;
    private Vector3[] snake3Points;
    private const int SnakeSegments = 48;

    private bool isStriking = false;
    private Coroutine strikeRoutine;
    private Vector3 lastStartPoint;
    private Vector3 lastEndPoint;

    // Public Properties
    public bool IsStriking => isStriking;
    public LineRenderer Line => arrowLineRenderer;
    public LineRenderer Snake1 => snakeRope1;
    public LineRenderer Snake2 => snakeRope2;
    public LineRenderer Snake3 => snakeRope3;

    public bool LiveScenePreview
    {
        get => liveScenePreview;
        set
        {
            liveScenePreview = value;
            if (!liveScenePreview)
            {
                StopStrike();
            }
            else
            {
                ApplyLineProperties();
            }
        }
    }

    public float BloomIntensityMultiplier
    {
        get => bloomIntensityMultiplier;
        set { bloomIntensityMultiplier = Mathf.Clamp(value, 1.5f, 10f); ApplyLineProperties(); }
    }

    public Color CoreColor
    {
        get => coreColor;
        set { coreColor = value; ApplyLineProperties(); }
    }

    public Color GlowColor
    {
        get => glowColor;
        set { glowColor = value; ApplyLineProperties(); }
    }

    public float StartWidth
    {
        get => shaftWidth;
        set { shaftWidth = value; ApplyLineProperties(); }
    }

    public float EndWidth
    {
        get => shaftWidth;
        set { shaftWidth = value; ApplyLineProperties(); }
    }

    public int Segments
    {
        get => segments;
        set => segments = Mathf.Clamp(value, 8, 48);
    }

    public float JaggedAmplitude
    {
        get => shaftElectricJitter;
        set => shaftElectricJitter = Mathf.Clamp(value, 0.0f, 0.35f);
    }

    public float MicroJaggedness
    {
        get => shaftElectricJitter * 0.5f;
        set { }
    }

    public bool StraightArrow
    {
        get => straightArrow;
        set => straightArrow = value;
    }

    public float ManualLength
    {
        get => manualLength;
        set => manualLength = Mathf.Clamp(value, 0.5f, 15f);
    }

    public bool UseManualLength
    {
        get => useManualLength;
        set => useManualLength = value;
    }

    public bool EnableSnakeRopes
    {
        get => enableSnakeRopes;
        set { enableSnakeRopes = value; ApplyLineProperties(); }
    }

    public float SnakeAmplitude
    {
        get => snakeAmplitude;
        set => snakeAmplitude = Mathf.Clamp(value, 0.02f, 0.5f);
    }

    public float SnakeWaves
    {
        get => snakeWaves;
        set => snakeWaves = Mathf.Clamp(value, 1f, 10f);
    }

    public float SnakeSpeed
    {
        get => snakeSpeed;
        set => snakeSpeed = Mathf.Clamp(value, 0f, 30f);
    }

    public float SnakeWidth
    {
        get => snakeWidth;
        set { snakeWidth = Mathf.Clamp(value, 0.01f, 0.3f); ApplyLineProperties(); }
    }

    public float SnakeElectricJitter
    {
        get => snakeElectricJitter;
        set => snakeElectricJitter = Mathf.Clamp(value, 0f, 0.2f);
    }

    public Material SnakeRopeMaterial
    {
        get => snakeRopeMaterial;
        set { snakeRopeMaterial = value; ApplyLineProperties(); }
    }

    // Backwards compatibility aliases
    public bool EnableHelixRope { get => enableSnakeRopes; set => enableSnakeRopes = value; }
    public float HelixRadius { get => snakeAmplitude; set => snakeAmplitude = value; }
    public float HelixCoils { get => snakeWaves; set => snakeWaves = value; }
    public float HelixRotationSpeed { get => snakeSpeed * 100f; set => snakeSpeed = value / 100f; }
    public float HelixWidth { get => snakeWidth; set => snakeWidth = value; }
    public Material HelixMaterial { get => snakeRopeMaterial; set => snakeRopeMaterial = value; }
    public float ShaftWidth { get => shaftWidth; set => shaftWidth = value; }
    public float ShaftElectricJitter { get => shaftElectricJitter; set => shaftElectricJitter = value; }

    private void Awake()
    {
        EnsureComponents();
    }

    private void OnEnable()
    {
        EnsureComponents();
        if (!liveScenePreview && !isStriking)
        {
            SetVisible(false);
        }
    }

    private void OnDisable()
    {
        StopStrike();
    }

    private void OnValidate()
    {
        EnsureComponents();
        ApplyLineProperties();
    }

    private void Update()
    {
        if (enableSnakeRopes && (isStriking || liveScenePreview))
        {
            if (lastStartPoint != Vector3.zero && lastEndPoint != Vector3.zero)
            {
                UpdateSnakeRopes(lastStartPoint, lastEndPoint);
            }
        }

        if (liveScenePreview && !Application.isPlaying)
        {
            Vector3 start = lastStartPoint != Vector3.zero ? lastStartPoint : transform.position + new Vector3(-0.35f, 1.25f, 0.45f);
            Vector3 end = lastEndPoint != Vector3.zero ? lastEndPoint : transform.position + new Vector3(-1.66f, 1.1f, 2.83f);
            GenerateJaggedBolt(start, end);
#if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
#endif
        }
    }

    public void EnsureComponents()
    {
        // 1. Central Arrow Shaft LineRenderer
        if (arrowLineRenderer == null)
            arrowLineRenderer = GetComponent<LineRenderer>();

        if (arrowLineRenderer != null && arrowLineRenderer.positionCount == 0)
        {
            arrowLineRenderer.positionCount = 2;
            arrowLineRenderer.SetPositions(new Vector3[] { transform.position, transform.position });
        }

        CleanupStrayChildren();

        // 2. Snake Rope 1
        snakeRope1 = GetOrCreateLineChild("Snake_Rope_1");

        // 3. Snake Rope 2 (Twin Counter-Weaving Snake)
        snakeRope2 = GetOrCreateLineChild("Snake_Rope_2");

        // 4. Snake Rope 3 (Tri-braided Serpentine Snake)
        snakeRope3 = GetOrCreateLineChild("Snake_Rope_3");

#if UNITY_EDITOR
        if (snakeRopeMaterial == null)
        {
            snakeRopeMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/VFX/Alura_VFX/Alura_Grave_Bolt/Materials/GraveBolt_HelixRope_Mat.mat");
        }
#endif

        ApplyLineProperties();
    }

    private LineRenderer GetOrCreateLineChild(string childName)
    {
        LineRenderer firstLr = null;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child == null) continue;
            if (child.name == childName)
            {
                if (firstLr == null)
                {
                    firstLr = child.GetComponent<LineRenderer>();
                    if (firstLr == null) firstLr = child.gameObject.AddComponent<LineRenderer>();
                }
                else
                {
                    // Eradicate duplicate child!
                    child.gameObject.SetActive(false);
                    if (Application.isPlaying) Destroy(child.gameObject);
#if UNITY_EDITOR
                    else if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(child.gameObject))
                        DestroyImmediate(child.gameObject);
#else
                    else Destroy(child.gameObject);
#endif
                }
            }
        }

        if (firstLr == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            firstLr = go.AddComponent<LineRenderer>();
        }

        if (firstLr.positionCount == 0)
        {
            firstLr.positionCount = 2;
            firstLr.SetPositions(new Vector3[] { transform.position, transform.position });
        }

        return firstLr;
    }

    public void CleanupStrayChildren()
    {
        bool found1 = false;
        bool found2 = false;
        bool found3 = false;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child == null) continue;

            if (child.name == "Snake_Rope_1")
            {
                if (!found1) { found1 = true; continue; }
            }
            else if (child.name == "Snake_Rope_2")
            {
                if (!found2) { found2 = true; continue; }
            }
            else if (child.name == "Snake_Rope_3")
            {
                if (!found3) { found3 = true; continue; }
            }

            // Unknown object or duplicate snake rope -> remove it!
            child.gameObject.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
#if UNITY_EDITOR
                if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(child.gameObject))
                {
                    DestroyImmediate(child.gameObject);
                }
#else
                Destroy(child.gameObject);
#endif
            }
        }
    }

    public void ApplyLineProperties()
    {
        // Arrow Shaft Line Properties
        if (arrowLineRenderer != null)
        {
            arrowLineRenderer.useWorldSpace = true;
            arrowLineRenderer.startWidth = shaftWidth;
            arrowLineRenderer.endWidth = shaftWidth * 0.75f;
            arrowLineRenderer.numCapVertices = 4;
            arrowLineRenderer.numCornerVertices = 4;
            arrowLineRenderer.alignment = LineAlignment.View;
            arrowLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            arrowLineRenderer.receiveShadows = false;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(coreColor * (bloomIntensityMultiplier * 1.5f), 0.0f),
                    new GradientColorKey(glowColor * (bloomIntensityMultiplier * 1.3f), 0.65f),
                    new GradientColorKey(coreColor * bloomIntensityMultiplier, 1.0f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.95f, 0.85f),
                    new GradientAlphaKey(0.80f, 1.0f)
                }
            );
            arrowLineRenderer.colorGradient = gradient;
        }

        // Configure all 3 Snake Ropes
        ConfigureSnakeLineRenderer(snakeRope1);
        ConfigureSnakeLineRenderer(snakeRope2);
        ConfigureSnakeLineRenderer(snakeRope3);

        if (!isStriking && !liveScenePreview)
        {
            SetVisible(false);
        }
    }

    private void ConfigureSnakeLineRenderer(LineRenderer lr)
    {
        if (lr == null) return;
        lr.useWorldSpace = true;
        lr.startWidth = snakeWidth;
        lr.endWidth = snakeWidth * 0.85f;
        lr.numCapVertices = 3;
        lr.numCornerVertices = 3;
        lr.alignment = LineAlignment.View;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        if (snakeRopeMaterial != null) lr.material = snakeRopeMaterial;

        Gradient hGrad = new Gradient();
        hGrad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(glowColor * (bloomIntensityMultiplier * 1.4f), 0.0f),
                new GradientColorKey(coreColor * (bloomIntensityMultiplier * 1.6f), 0.5f),
                new GradientColorKey(glowColor * (bloomIntensityMultiplier * 1.2f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.0f, 0.0f),
                new GradientAlphaKey(1.0f, 0.12f),
                new GradientAlphaKey(1.0f, 0.88f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        lr.colorGradient = hGrad;
    }

    private void SetVisible(bool visible)
    {
        if (arrowLineRenderer != null) arrowLineRenderer.enabled = visible;
        if (snakeRope1 != null) snakeRope1.enabled = visible && enableSnakeRopes;
        if (snakeRope2 != null) snakeRope2.enabled = visible && enableSnakeRopes;
        if (snakeRope3 != null) snakeRope3.enabled = visible && enableSnakeRopes;
    }

    /// <summary>
    /// Calculates the effective end point. The impact blast MUST always be born directly here!
    /// </summary>
    public Vector3 CalculateEffectiveEndPoint(Vector3 startPoint, Vector3 targetPoint)
    {
        Vector3 dir = (targetPoint - startPoint);
        float dist = dir.magnitude;
        if (dist < 0.001f) return startPoint + Vector3.forward * manualLength;

        dir.Normalize();
        if (useManualLength)
        {
            return startPoint + dir * manualLength;
        }
        return targetPoint;
    }

    /// <summary>
    /// Generates the straight electric arrow shaft and twin snake ropes ("vave like snack").
    /// </summary>
    public void GenerateJaggedBolt(Vector3 startPoint, Vector3 endPoint)
    {
        EnsureComponents();
        Vector3 finalEnd = CalculateEffectiveEndPoint(startPoint, endPoint);

        lastStartPoint = startPoint;
        lastEndPoint = finalEnd;

        float totalDist = Vector3.Distance(startPoint, finalEnd);
        if (totalDist < 0.01f)
        {
            SetVisible(false);
            return;
        }

        // 1. Generate Central Arrow Shaft
        if (arrowPoints == null || arrowPoints.Length != segments)
            arrowPoints = new Vector3[segments];

        arrowLineRenderer.positionCount = segments;
        Vector3 direction = (finalEnd - startPoint).normalized;

        Vector3 upAxis = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.9f ? Vector3.right : Vector3.up;
        Vector3 perp1 = Vector3.Cross(direction, upAxis).normalized;
        Vector3 perp2 = Vector3.Cross(direction, perp1).normalized;

        arrowPoints[0] = startPoint;
        arrowPoints[segments - 1] = finalEnd;

        for (int i = 1; i < segments - 1; i++)
        {
            float t = (float)i / (segments - 1);
            Vector3 linearPos = Vector3.Lerp(startPoint, finalEnd, t);

            if (!straightArrow && shaftElectricJitter > 0.001f)
            {
                float envelope = Mathf.Sin(t * Mathf.PI);
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float disp = Random.Range(-shaftElectricJitter, shaftElectricJitter) * envelope;
                Vector3 offset = (perp1 * Mathf.Cos(angle) + perp2 * Mathf.Sin(angle)) * disp;
                arrowPoints[i] = linearPos + offset;
            }
            else
            {
                arrowPoints[i] = linearPos;
            }
        }

        arrowLineRenderer.SetPositions(arrowPoints);
        arrowLineRenderer.enabled = true;

        // 2. Generate Twin Snake Waving Ropes ("Vave Like Snack")
        if (enableSnakeRopes)
        {
            UpdateSnakeRopes(startPoint, finalEnd);
        }
        else
        {
            if (snakeRope1 != null) snakeRope1.enabled = false;
            if (snakeRope2 != null) snakeRope2.enabled = false;
            if (snakeRope3 != null) snakeRope3.enabled = false;
        }
    }

    /// <summary>
    /// Updates the multiple snake-like traveling waves weaving forward along the arrow shaft ("vave like snack").
    /// </summary>
    private void UpdateSnakeRopes(Vector3 startPoint, Vector3 endPoint)
    {
        float totalDist = Vector3.Distance(startPoint, endPoint);
        if (totalDist < 0.05f)
        {
            if (snakeRope1 != null) snakeRope1.enabled = false;
            if (snakeRope2 != null) snakeRope2.enabled = false;
            if (snakeRope3 != null) snakeRope3.enabled = false;
            return;
        }

        if (snake1Points == null || snake1Points.Length != SnakeSegments) snake1Points = new Vector3[SnakeSegments];
        if (snake2Points == null || snake2Points.Length != SnakeSegments) snake2Points = new Vector3[SnakeSegments];
        if (snake3Points == null || snake3Points.Length != SnakeSegments) snake3Points = new Vector3[SnakeSegments];

        if (snakeRope1 != null) snakeRope1.positionCount = SnakeSegments;
        if (snakeRope2 != null) snakeRope2.positionCount = SnakeSegments;
        if (snakeRope3 != null) snakeRope3.positionCount = SnakeSegments;

        Vector3 direction = (endPoint - startPoint).normalized;
        Vector3 upAxis = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.9f ? Vector3.right : Vector3.up;
        Vector3 perp1 = Vector3.Cross(direction, upAxis).normalized;
        Vector3 perp2 = Vector3.Cross(direction, perp1).normalized;

        float time = Application.isPlaying ? Time.time : (float)UnityEditor.EditorApplication.timeSinceStartup;
        float waveK = snakeWaves * Mathf.PI * 2f;
        float waveW = snakeSpeed * Mathf.PI * 2f;

        for (int i = 0; i < SnakeSegments; i++)
        {
            if (i == 0)
            {
                snake1Points[0] = startPoint;
                snake2Points[0] = startPoint;
                snake3Points[0] = startPoint;
                continue;
            }
            if (i == SnakeSegments - 1)
            {
                snake1Points[SnakeSegments - 1] = endPoint;
                snake2Points[SnakeSegments - 1] = endPoint;
                snake3Points[SnakeSegments - 1] = endPoint;
                continue;
            }

            float t = (float)i / (SnakeSegments - 1);
            Vector3 centerPos = Vector3.Lerp(startPoint, endPoint, t);

            // Taper envelope so the snakes pinch neatly at palm and arrow tip
            // Guarantee sinVal >= 0 to prevent Mathf.Pow from producing NaN!
            float sinVal = Mathf.Max(0f, Mathf.Sin(t * Mathf.PI));
            float envelope = Mathf.Pow(sinVal, 0.45f) * snakeAmplitude;

            // Traveling sinusoidal waves spaced 120 degrees apart like a 3-strand electric serpent braid
            float theta1 = t * waveK - waveW * time;
            float theta2 = theta1 + (Mathf.PI * 2f / 3f);
            float theta3 = theta1 + (Mathf.PI * 4f / 3f);

            // Lateral snake S-curve motion (mostly in perp1 with organic 3D roll in perp2)
            float wave1X = Mathf.Sin(theta1) * envelope;
            float wave1Y = Mathf.Cos(theta1 * 1.3f) * (envelope * 0.45f);

            float wave2X = Mathf.Sin(theta2) * envelope;
            float wave2Y = Mathf.Cos(theta2 * 1.3f) * (envelope * 0.45f);

            float wave3X = Mathf.Sin(theta3) * envelope;
            float wave3Y = Mathf.Cos(theta3 * 1.3f) * (envelope * 0.45f);

            // Small high-voltage electric crackle jitter
            float j1 = Random.Range(-snakeElectricJitter, snakeElectricJitter);
            float j2 = Random.Range(-snakeElectricJitter, snakeElectricJitter);
            float j3 = Random.Range(-snakeElectricJitter, snakeElectricJitter);

            snake1Points[i] = centerPos + perp1 * (wave1X + j1) + perp2 * (wave1Y + j2);
            snake2Points[i] = centerPos + perp1 * (wave2X - j1) + perp2 * (wave2Y + j3);
            snake3Points[i] = centerPos + perp1 * (wave3X + j2) + perp2 * (wave3Y - j3);
        }

        if (snakeRope1 != null)
        {
            snakeRope1.SetPositions(snake1Points);
            snakeRope1.enabled = true;
        }
        if (snakeRope2 != null)
        {
            snakeRope2.SetPositions(snake2Points);
            snakeRope2.enabled = true;
        }
        if (snakeRope3 != null)
        {
            snakeRope3.SetPositions(snake3Points);
            snakeRope3.enabled = true;
        }
    }

    /// <summary>
    /// Executes a continuous strike of the arrow and waving snakes for a given duration.
    /// </summary>
    public void Strike(Vector3 startPoint, Vector3 endPoint, float duration = 0.25f)
    {
        EnsureComponents();
        ApplyLineProperties();

        if (strikeRoutine != null && Application.isPlaying)
        {
            StopCoroutine(strikeRoutine);
            strikeRoutine = null;
        }

        if (Application.isPlaying)
        {
            strikeRoutine = StartCoroutine(AnimateStrike(startPoint, endPoint, duration));
        }
        else
        {
            GenerateJaggedBolt(startPoint, endPoint);
        }
    }

    /// <summary>
    /// Animates a high-speed traveling projectile arrow from start to end,
    /// updating onUpdateLeadingEdge as it travels, and onImpact when it reaches the destination!
    /// </summary>
    public void FireProjectile(Vector3 startPoint, Vector3 endPoint, float flightDuration, System.Action<Vector3> onUpdateLeadingEdge, System.Action onImpact)
    {
        EnsureComponents();
        ApplyLineProperties();

        if (strikeRoutine != null && Application.isPlaying)
        {
            StopCoroutine(strikeRoutine);
            strikeRoutine = null;
        }

        if (Application.isPlaying)
        {
            strikeRoutine = StartCoroutine(AnimateProjectileFlight(startPoint, endPoint, flightDuration, onUpdateLeadingEdge, onImpact));
        }
        else
        {
            Vector3 finalEnd = CalculateEffectiveEndPoint(startPoint, endPoint);
            GenerateJaggedBolt(startPoint, finalEnd);
            onUpdateLeadingEdge?.Invoke(finalEnd);
            onImpact?.Invoke();
        }
    }

    public void StopStrike()
    {
        isStriking = false;
        if (strikeRoutine != null && Application.isPlaying)
        {
            StopCoroutine(strikeRoutine);
            strikeRoutine = null;
        }
        SetVisible(false);
    }

    private IEnumerator AnimateStrike(Vector3 startPoint, Vector3 endPoint, float duration)
    {
        isStriking = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            GenerateJaggedBolt(startPoint, endPoint);
            yield return null;
        }
        StopStrike();
    }

    private IEnumerator AnimateProjectileFlight(Vector3 startPoint, Vector3 endPoint, float flightDuration, System.Action<Vector3> onUpdateLeadingEdge, System.Action onImpact)
    {
        isStriking = true;
        float elapsed = 0f;
        Vector3 finalEnd = CalculateEffectiveEndPoint(startPoint, endPoint);

        while (elapsed < flightDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / flightDuration);
            float smoothT = t * t * (3f - 2f * t);
            Vector3 currentHead = Vector3.Lerp(startPoint, finalEnd, smoothT);

            GenerateJaggedBolt(startPoint, currentHead);
            onUpdateLeadingEdge?.Invoke(currentHead);
            yield return null;
        }

        // Arrived at destination: invoke leading edge at target and trigger impact blast
        onUpdateLeadingEdge?.Invoke(finalEnd);
        onImpact?.Invoke();

        // Brief flash discharge at target
        float linger = 0.08f;
        while (linger > 0f)
        {
            linger -= Time.deltaTime;
            GenerateJaggedBolt(startPoint, finalEnd);
            yield return null;
        }

        StopStrike();
    }
}
