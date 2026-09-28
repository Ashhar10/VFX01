using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// Master controller that synchronizes VFX playback with animation timing.
/// 
/// Features:
/// 1. Sliders in SECONDS (large range 0 to 15 seconds) so you can type or slide any time.
/// 2. Chained Delay Mode: When you add delay on the 1st effect, subsequent effects 
///    automatically continue after that delay.
/// 3. Absolute Seconds Mode: Each VFX has its own exact delay in seconds from the start of the attack.
/// 4. Normalized Animation Time Mode: Synchronized to exact animation percentage (0.0 to 1.0).
/// 5. Seamless Animation Looping: Automatically detects loop wrap-around and cleanly resets.
/// </summary>
[DisallowMultipleComponent]
public class VFXAnimationSync : MonoBehaviour
{
    public enum TimingMode
    {
        ChainedSequence,        // VFX 2 and VFX 3 trigger at offsets AFTER VFX 1 starts (Cascading)
        AbsoluteSeconds,        // Each VFX triggers at its own exact second timestamp from cycle start
        AnimationNormalizedTime // Triggers at animation normalized percentage (0.0 to 1.0)
    }

    // ==========================================
    //  VFX REFERENCES
    // ==========================================
    [Header("=== VFX References ===")]
    [Tooltip("VFX 1: Sword Slash Arc (SwordTrailVFX attached to sword).")]
    [SerializeField] private SwordTrailVFX swordTrailVFX;

    [Tooltip("VFX 2: Holy Ground Ring (HolyGroundRingVFX on floor).")]
    [SerializeField] private HolyGroundRingVFX holyGroundRingVFX;

    [Tooltip("VFX 3: Ground Slash & Fissure (GroundSlashVFX traveling wave).")]
    [SerializeField] private GroundSlashVFX groundSlashVFX;

    // ==========================================
    //  ANIMATION & LOOPING
    // ==========================================
    [Header("=== Animation & Looping ===")]
    [Tooltip("Animator component. Auto-searches children if null.")]
    [SerializeField] private Animator animator;

    [Tooltip("Name of the attack animation state in Animator (e.g. '01').")]
    [SerializeField] private string attackStateName = "01";

    [Tooltip("Timing mode for triggering VFX.")]
    [SerializeField] private TimingMode timingMode = TimingMode.ChainedSequence;

    [Tooltip("Automatically re-trigger VFX on every loop cycle when the animation loops.")]
    [SerializeField] private bool loopWithAnimation = true;

    // ==========================================
    //  DELAYS IN SECONDS (LARGE SLIDERS UP TO 15 SECONDS)
    // ==========================================
    [Header("=== Delays in Seconds (Large Sliders 0 to 15s) ===")]
    [Tooltip("Master initial delay in seconds before the sequence begins.")]
    [SerializeField, Range(0f, 15f)] private float masterInitialDelay = 0f;

    [Header("--- VFX 1: Sword Slash Arc ---")]
    [Tooltip("Enable VFX 1 (Sword Slash).")]
    [SerializeField] private bool enableVFX1_SwordSlash = true;

    [Tooltip("Start Delay in SECONDS for VFX 1 (Sword Slash) after master start.")]
    [SerializeField, Range(0f, 15f)] private float vfx1_SlashStartDelay = 0.2f;

    [Tooltip("Duration in SECONDS the sword trail remains active.")]
    [SerializeField, Range(0.1f, 10f)] private float vfx1_SlashDuration = 0.5f;

    [Header("--- VFX 2: Holy Ground Ring ---")]
    [Tooltip("Enable VFX 2 (Holy Ground Ring).")]
    [SerializeField] private bool enableVFX2_HolyRing = true;

    [Tooltip("Delay in SECONDS for Holy Ground Ring. In Chained mode, this is AFTER VFX 1 starts!")]
    [SerializeField, Range(0f, 15f)] private float vfx2_RingDelay = 0.35f;

    [Header("--- VFX 3: Ground Slash & Fissure ---")]
    [Tooltip("Enable VFX 3 (Ground Slash).")]
    [SerializeField] private bool enableVFX3_GroundSlash = true;

    [Tooltip("Delay in SECONDS for Ground Slash. In Chained mode, this is AFTER VFX 1 starts!")]
    [SerializeField, Range(0f, 15f)] private float vfx3_GroundSlashDelay = 0.55f;

    // ==========================================
    //  NORMALIZED ANIMATION TIME (ALTERNATIVE MODE)
    // ==========================================
    [Header("=== Normalized Animation Time Mode (0.0 to 1.0) ===")]
    [Tooltip("Normalized time when sword slash starts (0.0 to 1.0).")]
    [SerializeField, Range(0f, 1f)] private float trailStartNormalized = 0.15f;

    [Tooltip("Normalized time when sword slash stops (0.0 to 1.0).")]
    [SerializeField, Range(0f, 1f)] private float trailStopNormalized = 0.55f;

    [Tooltip("Normalized time when holy floor ring triggers (0.0 to 1.0).")]
    [SerializeField, Range(0f, 1f)] private float floorRingNormalized = 0.35f;

    [Tooltip("Normalized time when ground slash triggers (0.0 to 1.0).")]
    [SerializeField, Range(0f, 1f)] private float groundSlashNormalized = 0.42f;

    // ==========================================
    //  INPUT COOLDOWN
    // ==========================================
    [Header("=== Input Cooldown ===")]
    [Tooltip("Cooldown between manual attack inputs (seconds).")]
    [SerializeField, Range(0f, 5f)] private float attackCooldown = 0.4f;

    // ==========================================
    //  ANTICIPATION WIND-UP (Pre-Strike Energy Gather)
    // ==========================================
    [Header("=== Anticipation Wind-Up (Pre-Strike) ===")]
    [Tooltip("Enable anticipation wind-up: brief energy gather / light dim before the swing.")]
    [SerializeField] private bool enableAnticipation = true;

    [Tooltip("Duration of the anticipation wind-up (seconds).")]
    [SerializeField, Range(0.05f, 1.5f)] private float anticipationDuration = 0.25f;

    [Tooltip("Enable inward suction particles gathering toward the blade during wind-up.")]
    [SerializeField] private bool enableSuctionParticles = true;

    [Tooltip("Number of suction motes spawned during wind-up.")]
    [SerializeField, Range(3, 30)] private int suctionParticleCount = 12;

    [Tooltip("Enable a brief light dimming during the anticipation wind-up.")]
    [SerializeField] private bool enableAnticipationDim = true;

    [Tooltip("How much the ambient light dims (0 = no dim, 1 = pitch black).")]
    [SerializeField, Range(0f, 0.5f)] private float anticipationDimAmount = 0.15f;

    // ==========================================
    //  IMPACT FRAME & HITSTOP (Combat Weight)
    // ==========================================
    [Header("=== Impact Frame & Hitstop (Combat Weight) ===")]
    [Tooltip("Enable hitstop micro-pause on impact for visceral physical weight.")]
    [SerializeField] private bool enableHitstop = true;

    [Tooltip("Hitstop freeze duration in REAL seconds (Time.unscaledDeltaTime based).")]
    [SerializeField, Range(0.01f, 0.15f)] private float hitstopDuration = 0.045f;

    [Tooltip("Time scale during hitstop (0.0 = full freeze, 0.1 = near-freeze with slight drift).")]
    [SerializeField, Range(0f, 0.2f)] private float hitstopTimeScale = 0.05f;

    [Tooltip("Enable HDR flash spike on the impact frame.")]
    [SerializeField] private bool enableImpactFlash = true;

    [Tooltip("HDR flash intensity (multiplied on a temporary point light).")]
    [SerializeField, Range(1f, 30f)] private float impactFlashIntensity = 12f;

    [Tooltip("Flash duration (seconds).")]
    [SerializeField, Range(0.02f, 0.3f)] private float impactFlashDuration = 0.08f;

    [Tooltip("Flash light color.")]
    [SerializeField] private Color impactFlashColor = new Color(1f, 0.9f, 0.5f, 1f);

    [Tooltip("Enable camera shake on impact.")]
    [SerializeField] private bool enableImpactCameraShake = true;

    [Tooltip("Camera shake intensity on impact.")]
    [SerializeField, Range(0f, 0.4f)] private float impactShakeIntensity = 0.12f;

    [Tooltip("Camera shake duration on impact (seconds).")]
    [SerializeField, Range(0.03f, 0.4f)] private float impactShakeDuration = 0.15f;

    // ==========================================
    //  EVENTS
    // ==========================================
    public event Action OnSequenceStart;
    public event Action OnTrailStart;
    public event Action OnTrailStop;
    public event Action OnFloorRingTrigger;
    public event Action OnGroundSlashTrigger;

    // ==========================================
    //  INTERNAL STATE
    // ==========================================
    private bool isAttacking;
    private float lastAttackTime = -999f;
    private float lastNormalizedTime = 0f;

    private bool cycleStarted;
    private bool trailTriggeredThisCycle;
    private bool ringTriggeredThisCycle;
    private bool slashTriggeredThisCycle;

    private Coroutine chainedSequenceCoroutine;

    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (swordTrailVFX == null)
            swordTrailVFX = GetComponentInChildren<SwordTrailVFX>();

        if (holyGroundRingVFX == null)
            holyGroundRingVFX = GetComponentInChildren<HolyGroundRingVFX>();

        if (groundSlashVFX == null)
            groundSlashVFX = GetComponentInChildren<GroundSlashVFX>();
    }

    private void Update()
    {
        MonitorAnimation();
    }

    private void MonitorAnimation()
    {
        if (animator == null) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        bool inAttackState = string.IsNullOrEmpty(attackStateName) || 
                             stateInfo.IsName(attackStateName) || 
                             stateInfo.IsName("Base Layer." + attackStateName);

        if (inAttackState)
        {
            float normalizedTime = stateInfo.normalizedTime % 1f;

            // Detect loop wrap-around (e.g. from 0.99 back to 0.01)
            if (normalizedTime < lastNormalizedTime)
            {
                if (loopWithAnimation)
                {
                    ResetLoopCycle();
                }
            }
            lastNormalizedTime = normalizedTime;

            if (timingMode == TimingMode.ChainedSequence || timingMode == TimingMode.AbsoluteSeconds)
            {
                // Trigger sequence once per loop cycle
                if (!cycleStarted)
                {
                    cycleStarted = true;
                    isAttacking = true;
                    StartChainedSequence();
                }
            }
            else // Normalized Animation Time mode (0.0 to 1.0)
            {
                // 1. Sword Slash Start
                if (enableVFX1_SwordSlash && !trailTriggeredThisCycle && normalizedTime >= trailStartNormalized && normalizedTime < trailStopNormalized)
                {
                    trailTriggeredThisCycle = true;
                    if (swordTrailVFX != null) swordTrailVFX.Play();
                    OnTrailStart?.Invoke();
                }

                // 1. Sword Slash Stop
                if (enableVFX1_SwordSlash && trailTriggeredThisCycle && normalizedTime >= trailStopNormalized)
                {
                    if (swordTrailVFX != null && swordTrailVFX.IsPlaying)
                        swordTrailVFX.Stop();
                    OnTrailStop?.Invoke();
                }

                // 2. Holy Ring
                if (enableVFX2_HolyRing && !ringTriggeredThisCycle && normalizedTime >= floorRingNormalized)
                {
                    ringTriggeredThisCycle = true;
                    if (holyGroundRingVFX != null) holyGroundRingVFX.TriggerRing();
                    OnFloorRingTrigger?.Invoke();
                }

                // 3. Ground Slash
                if (enableVFX3_GroundSlash && !slashTriggeredThisCycle && normalizedTime >= groundSlashNormalized)
                {
                    slashTriggeredThisCycle = true;
                    if (groundSlashVFX != null) groundSlashVFX.TriggerSlash();
                    OnGroundSlashTrigger?.Invoke();
                }

                isAttacking = true;
            }
        }
        else
        {
            if (isAttacking)
            {
                StopAll();
            }
        }
    }

    private void ResetLoopCycle()
    {
        cycleStarted = false;
        trailTriggeredThisCycle = false;
        ringTriggeredThisCycle = false;
        slashTriggeredThisCycle = false;

        if (chainedSequenceCoroutine != null)
        {
            StopCoroutine(chainedSequenceCoroutine);
            chainedSequenceCoroutine = null;
        }
    }

    /// <summary>
    /// Play attack manually (e.g. from player input or UI).
    /// </summary>
    public void PlayAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown) return;
        lastAttackTime = Time.time;

        ResetLoopCycle();
        isAttacking = true;
        StartChainedSequence();
    }

    private void StartChainedSequence()
    {
        if (chainedSequenceCoroutine != null)
            StopCoroutine(chainedSequenceCoroutine);

        chainedSequenceCoroutine = StartCoroutine(ExecuteChainedSequenceRoutine());
    }

    /// <summary>
    /// Master timing coroutine in SECONDS:
    /// In ChainedSequence mode: VFX 2 and VFX 3 wait relative to VFX 1's starting moment!
    /// In AbsoluteSeconds mode: Each VFX waits from cycle start!
    /// </summary>
    private IEnumerator ExecuteChainedSequenceRoutine()
    {
        OnSequenceStart?.Invoke();

        // 1. Master Initial Delay (Seconds)
        if (masterInitialDelay > 0f)
            yield return new WaitForSeconds(masterInitialDelay);

        if (timingMode == TimingMode.ChainedSequence)
        {
            // --- CHAINED MODE (VFX 1 Starts, then VFX 2 and VFX 3 cascade after it) ---

            // ANTICIPATION WIND-UP: gather energy before the swing
            if (enableAnticipation)
                yield return StartCoroutine(PlayAnticipationRoutine());

            // Wait for VFX 1 start delay
            if (vfx1_SlashStartDelay > 0f)
                yield return new WaitForSeconds(vfx1_SlashStartDelay);

            // Trigger VFX 1: Sword Slash
            if (enableVFX1_SwordSlash && swordTrailVFX != null)
            {
                swordTrailVFX.Play();
                OnTrailStart?.Invoke();
            }

            // IMPACT FRAME: HDR Flash + Camera Shake + Hitstop on strike
            if (enableImpactFlash)
                StartCoroutine(PlayImpactFlashRoutine());
            if (enableImpactCameraShake)
                StartCoroutine(PlayImpactCameraShakeRoutine());
            if (enableHitstop)
                StartCoroutine(PlayHitstopRoutine());

            // VFX 2 (Holy Ring) continues AFTER VFX 1 has started
            if (enableVFX2_HolyRing && holyGroundRingVFX != null)
                StartCoroutine(TriggerVFX2WithRelativeOffset(vfx2_RingDelay));

            // VFX 3 (Ground Slash) continues AFTER VFX 1 has started
            if (enableVFX3_GroundSlash && groundSlashVFX != null)
                StartCoroutine(TriggerVFX3WithRelativeOffset(vfx3_GroundSlashDelay));

            // VFX 1 Slash Duration
            yield return new WaitForSeconds(vfx1_SlashDuration);
            if (swordTrailVFX != null && swordTrailVFX.IsPlaying)
            {
                swordTrailVFX.Stop();
                OnTrailStop?.Invoke();
            }
        }
        else // AbsoluteSeconds mode
        {
            // Anticipation for absolute mode too
            if (enableAnticipation)
                yield return StartCoroutine(PlayAnticipationRoutine());

            // Each VFX triggers at its own absolute delay in seconds
            if (enableVFX1_SwordSlash && swordTrailVFX != null)
                StartCoroutine(ExecuteAbsoluteVFX1Routine());

            if (enableVFX2_HolyRing && holyGroundRingVFX != null)
                StartCoroutine(TriggerVFX2WithRelativeOffset(vfx2_RingDelay));

            if (enableVFX3_GroundSlash && groundSlashVFX != null)
                StartCoroutine(TriggerVFX3WithRelativeOffset(vfx3_GroundSlashDelay));
        }

        chainedSequenceCoroutine = null;
    }

    private IEnumerator ExecuteAbsoluteVFX1Routine()
    {
        if (vfx1_SlashStartDelay > 0f)
            yield return new WaitForSeconds(vfx1_SlashStartDelay);

        if (swordTrailVFX != null)
        {
            swordTrailVFX.Play();
            OnTrailStart?.Invoke();
        }

        yield return new WaitForSeconds(vfx1_SlashDuration);
        if (swordTrailVFX != null && swordTrailVFX.IsPlaying)
        {
            swordTrailVFX.Stop();
            OnTrailStop?.Invoke();
        }
    }

    private IEnumerator TriggerVFX2WithRelativeOffset(float offsetInSeconds)
    {
        if (offsetInSeconds > 0f)
            yield return new WaitForSeconds(offsetInSeconds);

        if (holyGroundRingVFX != null)
        {
            holyGroundRingVFX.TriggerRing();
            OnFloorRingTrigger?.Invoke();
        }
    }

    private IEnumerator TriggerVFX3WithRelativeOffset(float offsetInSeconds)
    {
        if (offsetInSeconds > 0f)
            yield return new WaitForSeconds(offsetInSeconds);

        if (groundSlashVFX != null)
        {
            groundSlashVFX.TriggerSlash();
            OnGroundSlashTrigger?.Invoke();
        }
    }

    // ==========================================
    //  MANUAL TRIGGER METHODS
    // ==========================================

    /// <summary>
    /// Trigger Sword Slash Arc manually.
    /// </summary>
    public void TriggerTrailVFX()
    {
        if (swordTrailVFX != null)
        {
            swordTrailVFX.Play();
            OnTrailStart?.Invoke();
        }
    }

    /// <summary>
    /// Trigger Holy Ground Ring manually.
    /// </summary>
    public void TriggerFloorRingVFX()
    {
        if (holyGroundRingVFX != null)
        {
            holyGroundRingVFX.TriggerRing();
            OnFloorRingTrigger?.Invoke();
        }
    }

    /// <summary>
    /// Trigger Ground Slash manually.
    /// </summary>
    public void TriggerGroundSlashVFX()
    {
        if (groundSlashVFX != null)
        {
            groundSlashVFX.TriggerSlash();
            OnGroundSlashTrigger?.Invoke();
        }
    }

    /// <summary>
    /// Stop all active VFX immediately.
    /// </summary>
    public void StopAll()
    {
        if (chainedSequenceCoroutine != null)
        {
            StopCoroutine(chainedSequenceCoroutine);
            chainedSequenceCoroutine = null;
        }

        StopAllCoroutines();

        if (swordTrailVFX != null) swordTrailVFX.Stop();
        if (holyGroundRingVFX != null) holyGroundRingVFX.StopRing();
        if (groundSlashVFX != null) groundSlashVFX.CleanupAll();

        isAttacking = false;
        cycleStarted = false;
        trailTriggeredThisCycle = false;
        ringTriggeredThisCycle = false;
        slashTriggeredThisCycle = false;
        lastNormalizedTime = 0f;
    }

    // ==========================================
    //  ANIMATION EVENT HOOKS
    // ==========================================
    public void OnAnimEvent_TriggerVFX(int vfxIndex)
    {
        switch (vfxIndex)
        {
            case 0:
                TriggerTrailVFX();
                break;
            case 1:
                TriggerGroundSlashVFX();
                break;
            case 2:
                TriggerFloorRingVFX();
                break;
        }
    }

    public void OnAnimEvent_StopVFX(int vfxIndex)
    {
        switch (vfxIndex)
        {
            case 0:
                if (swordTrailVFX != null) swordTrailVFX.Stop();
                break;
            case 1:
                if (groundSlashVFX != null) groundSlashVFX.CleanupAll();
                break;
            case 2:
                if (holyGroundRingVFX != null) holyGroundRingVFX.StopRing();
                break;
        }
    }

    public void OnAnimEvent_AttackEnd()
    {
        if (!loopWithAnimation)
            StopAll();
    }

    // ==========================================
    //  ANTICIPATION WIND-UP COROUTINE
    // ==========================================

    /// <summary>
    /// Briefly dims ambient light and spawns inward-sucking golden motes
    /// converging on the sword to convey energy charging before the strike.
    /// </summary>
    private IEnumerator PlayAnticipationRoutine()
    {
        // 1. Dim ambient light
        Color originalAmbient = RenderSettings.ambientLight;
        Color dimmedAmbient = originalAmbient;

        if (enableAnticipationDim)
        {
            dimmedAmbient = Color.Lerp(originalAmbient, Color.black, anticipationDimAmount);
            RenderSettings.ambientLight = dimmedAmbient;
        }

        // 2. Spawn suction particles converging inward
        ParticleSystem suctionPS = null;
        if (enableSuctionParticles)
        {
            suctionPS = CreateSuctionParticleSystem();
            if (suctionPS != null)
                suctionPS.Emit(suctionParticleCount);
        }

        // 3. Wait for anticipation duration
        yield return new WaitForSeconds(anticipationDuration);

        // 4. Restore ambient light
        if (enableAnticipationDim)
            RenderSettings.ambientLight = originalAmbient;

        // 5. Cleanup suction particles
        if (suctionPS != null)
            Destroy(suctionPS.gameObject, 1.0f);
    }

    private ParticleSystem CreateSuctionParticleSystem()
    {
        Transform center = swordTrailVFX != null ? swordTrailVFX.transform : transform;

        GameObject suctionGO = new GameObject("AnticipationSuction");
        suctionGO.transform.position = center.position;

        ParticleSystem ps = suctionGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(anticipationDuration * 0.6f, anticipationDuration);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(-3f, -1.5f); // Negative = inward suction
        main.startColor = new Color(1f, 0.8f, 0.3f, 0.8f);
        main.maxParticles = 40;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        main.loop = false;

        var emission = ps.emission;
        emission.rateOverTime = 0;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.8f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0f, 1f);
        sc.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var renderer = suctionGO.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        Shader sparkShader = Shader.Find("VFX/AdditiveParticle");
        if (sparkShader == null) sparkShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Material mat = new Material(sparkShader);
        mat.SetColor("_Color", new Color(12f, 8f, 2f, 1f));
        mat.SetFloat("_Brightness", 2.5f);
        renderer.material = mat;

        return ps;
    }

    // ==========================================
    //  HITSTOP MICRO-PAUSE COROUTINE
    // ==========================================

    /// <summary>
    /// Freezes time briefly on the impact frame for visceral physical weight.
    /// Uses Time.unscaledDeltaTime to measure real elapsed time.
    /// </summary>
    private IEnumerator PlayHitstopRoutine()
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = hitstopTimeScale;

        float realElapsed = 0f;
        while (realElapsed < hitstopDuration)
        {
            realElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = originalTimeScale;
    }

    // ==========================================
    //  IMPACT HDR FLASH COROUTINE
    // ==========================================

    /// <summary>
    /// Spawns a brief intense point light at the sword's position on the strike frame.
    /// </summary>
    private IEnumerator PlayImpactFlashRoutine()
    {
        Transform flashPos = swordTrailVFX != null ? swordTrailVFX.transform : transform;

        GameObject flashGO = new GameObject("ImpactFlash_HDR");
        flashGO.transform.position = flashPos.position;

        Light flashLight = flashGO.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = impactFlashColor;
        flashLight.intensity = impactFlashIntensity;
        flashLight.range = 8f;
        flashLight.shadows = LightShadows.None;

        float elapsed = 0f;
        while (elapsed < impactFlashDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / impactFlashDuration);
            flashLight.intensity = Mathf.Lerp(impactFlashIntensity, 0f, t * t);
            yield return null;
        }

        Destroy(flashGO);
    }

    // ==========================================
    //  IMPACT CAMERA SHAKE COROUTINE
    // ==========================================

    /// <summary>
    /// Punchy camera shake on the impact frame.
    /// </summary>
    private IEnumerator PlayImpactCameraShakeRoutine()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 orig = cam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < impactShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = 1f - Mathf.Clamp01(elapsed / impactShakeDuration);
            float ox = UnityEngine.Random.Range(-impactShakeIntensity, impactShakeIntensity) * t;
            float oy = UnityEngine.Random.Range(-impactShakeIntensity, impactShakeIntensity) * t;
            cam.transform.localPosition = orig + new Vector3(ox, oy, 0);
            yield return null;
        }

        cam.transform.localPosition = orig;
    }
}
