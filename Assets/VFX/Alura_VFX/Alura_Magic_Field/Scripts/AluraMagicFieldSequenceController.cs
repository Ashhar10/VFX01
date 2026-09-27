using System.Collections;
using UnityEngine;

/// <summary>
/// AluraMagicFieldSequenceController - Master Sequencer & Modular Stage Runner.
/// 
/// User Requirements:
/// - Every stage is a different prefab and can be run separately.
/// - Master option to run in sequence (Stage 1 -> 2 -> 3 -> 4) OR run any stage separately on demand.
/// - Master Bloom / Emissive intensity slider.
/// - Live updates when editing values in Inspector.
/// - Plays automatically on Enable (toggle On/Off to replay).
/// - 100% Edit-Mode and Play-Mode safe (zero material leaks, zero curve errors).
/// </summary>
[ExecuteAlways]
[SelectionBase]
[DisallowMultipleComponent]
public class AluraMagicFieldSequenceController : MonoBehaviour
{
    public enum PlaybackMode
    {
        RunFullSequence,
        RunSoloStage,
        ManualTriggerOnly
    }

    public enum SoloStageSelection
    {
        Stage1_Targeting = 1,
        Stage2_Activation = 2,
        Stage3_HealAndDamage = 3,
        Stage3_HealOnly = 31,
        Stage3_DamageOnly = 32,
        Stage4_Dissipation = 4
    }

    [Header("=== Playback Configuration ===")]
    [Tooltip("Choose whether to run all stages in sequence or run a single stage solo.")]
    [SerializeField] private PlaybackMode playbackMode = PlaybackMode.RunFullSequence;

    [Tooltip("Which stage to run when Playback Mode is set to 'RunSoloStage'.")]
    [SerializeField] private SoloStageSelection soloStage = SoloStageSelection.Stage1_Targeting;

    [Tooltip("Continuously loop the full sequence (useful for showcase/review).")]
    [SerializeField] private bool loopSequence = false;

    [Header("=== Bloom / Emissive Intensity Controls ===")]
    [Tooltip("Master Bloom & Emissive Glow intensity multiplier applied across all stages.")]
    [SerializeField, Range(0.5f, 15f)] private float masterBloomIntensity = 4.2f;

    [Header("=== Stage Transition Timings (Seconds) ===")]
    [SerializeField, Range(0.5f, 4f)] private float stage1Time = 1.3f;
    [SerializeField, Range(0.5f, 4f)] private float stage2Time = 1.4f;
    [SerializeField, Range(1f, 6f)] private float stage3Time = 2.5f;
    [SerializeField, Range(0.5f, 4f)] private float stage4Time = 1.6f;

    [Header("=== Modular Stage Sub-Components / Prefabs ===")]
    [SerializeField] private AluraMagicFieldStage1Targeting stage1Targeting;
    [SerializeField] private AluraMagicFieldStage2Activation stage2Activation;
    [SerializeField] private AluraMagicFieldStage3HealDamage stage3HealDamage;
    [SerializeField] private AluraMagicFieldStage3Heal stage3HealOnly;
    [SerializeField] private AluraMagicFieldStage3Damage stage3DamageOnly;
    [SerializeField] private AluraMagicFieldStage4Dissipation stage4Dissipation;

    private Coroutine sequenceRoutine;

    private void Awake()
    {
        FindOrCreateStageComponents();
    }

    private void OnEnable()
    {
        FindOrCreateStageComponents();
        if (playbackMode == PlaybackMode.RunFullSequence)
            PlayFullSequence();
        else if (playbackMode == PlaybackMode.RunSoloStage)
            PlayStage((int)soloStage);
    }

    private void OnDisable()
    {
        StopAll();
    }

    private void OnDestroy()
    {
        StopAll();
    }

    private void OnApplicationQuit()
    {
        StopAll();
    }

    #region Context Menus & Public Execution Controls

    public void ApplyMasterBloom(float intensity)
    {
        masterBloomIntensity = intensity;
        if (stage1Targeting != null) stage1Targeting.BloomIntensity = intensity;
        if (stage2Activation != null) stage2Activation.BloomIntensity = intensity;
        if (stage3HealDamage != null) stage3HealDamage.BloomIntensity = intensity;
        if (stage3HealOnly != null) stage3HealOnly.BloomIntensity = intensity;
        if (stage3DamageOnly != null) stage3DamageOnly.BloomIntensity = intensity;
        if (stage4Dissipation != null) stage4Dissipation.BloomIntensity = intensity;
    }

    [ContextMenu("Play Full Sequence (1 -> 4)")]
    public void PlayFullSequence()
    {
        StopAll();
        ApplyMasterBloom(masterBloomIntensity);
        sequenceRoutine = StartCoroutine(FullSequenceRoutine());
    }

    [ContextMenu("Play Stage 1 Solo (Targeting)")]
    public void PlayStage1Solo() => PlayStage(1);

    [ContextMenu("Play Stage 2 Solo (Activation)")]
    public void PlayStage2Solo() => PlayStage(2);

    [ContextMenu("Play Stage 3 Solo (Heal & Damage)")]
    public void PlayStage3Solo() => PlayStage(3);

    [ContextMenu("Play Stage 3 (Heal Only)")]
    public void PlayStage3HealOnly() => PlayStage(31);

    [ContextMenu("Play Stage 3 (Damage Only)")]
    public void PlayStage3DamageOnly() => PlayStage(32);

    [ContextMenu("Play Stage 4 Solo (Dissipation)")]
    public void PlayStage4Solo() => PlayStage(4);

    [ContextMenu("Stop All Stages")]
    public void StopAll()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        if (stage1Targeting != null) stage1Targeting.StopStage();
        if (stage2Activation != null) stage2Activation.StopStage();
        if (stage3HealDamage != null) stage3HealDamage.StopStage();
        if (stage3HealOnly != null) stage3HealOnly.StopStage();
        if (stage3DamageOnly != null) stage3DamageOnly.StopStage();
        if (stage4Dissipation != null) stage4Dissipation.StopStage();
    }

    public void PlayStage(int stageNumber)
    {
        StopAll();
        FindOrCreateStageComponents();
        ApplyMasterBloom(masterBloomIntensity);

        switch (stageNumber)
        {
            case 1:
                if (stage1Targeting != null) stage1Targeting.PlayStage();
                break;
            case 2:
                if (stage1Targeting != null) stage1Targeting.BuildStaticHierarchy();
                if (stage2Activation != null) stage2Activation.PlayStage();
                break;
            case 3:
                if (stage1Targeting != null) stage1Targeting.BuildStaticHierarchy();
                if (stage2Activation != null) stage2Activation.BuildStaticHierarchy();
                if (stage3HealDamage != null) stage3HealDamage.PlayStage();
                break;
            case 31: // Heal Only
                if (stage1Targeting != null) stage1Targeting.BuildStaticHierarchy();
                if (stage3HealOnly != null) stage3HealOnly.PlayStage();
                break;
            case 32: // Damage Only
                if (stage1Targeting != null) stage1Targeting.BuildStaticHierarchy();
                if (stage3DamageOnly != null) stage3DamageOnly.PlayStage();
                break;
            case 4:
                if (stage1Targeting != null) stage1Targeting.BuildStaticHierarchy();
                if (stage2Activation != null) stage2Activation.BuildStaticHierarchy();
                if (stage4Dissipation != null) stage4Dissipation.PlayStage();
                StartCoroutine(DelayedFadeOutStage1And2(stage4Time));
                break;
        }
    }

    #endregion

    #region Sequence Coroutine

    private IEnumerator FullSequenceRoutine()
    {
        do
        {
            FindOrCreateStageComponents();

            // Phase 1: Start / Targeting
            if (stage1Targeting != null) stage1Targeting.PlayStage();
            yield return new WaitForSeconds(stage1Time);

            // Phase 2: Cast / Field Activation
            if (stage2Activation != null) stage2Activation.PlayStage();
            yield return new WaitForSeconds(stage2Time);

            // Phase 3: Heal + Damage Effect
            if (stage3HealDamage != null) stage3HealDamage.PlayStage();
            yield return new WaitForSeconds(stage3Time);

            // Phase 4: Recovery / Dissipation
            if (stage3HealDamage != null) stage3HealDamage.StopStage();
            if (stage4Dissipation != null) stage4Dissipation.PlayStage();

            yield return DelayedFadeOutStage1And2(stage4Time);

            StopAll();
            yield return new WaitForSeconds(0.3f);

        } while (loopSequence && Application.isPlaying);
    }

    private IEnumerator DelayedFadeOutStage1And2(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            if (stage1Targeting != null) stage1Targeting.SetLinesAlpha(alpha);
            yield return null;
        }
        if (stage1Targeting != null) stage1Targeting.SetLinesAlpha(0f);
    }

    #endregion

    #region Auto-Detection / Linking

    private void FindOrCreateStageComponents()
    {
        if (stage1Targeting == null) stage1Targeting = GetComponentInChildren<AluraMagicFieldStage1Targeting>(true);
        if (stage2Activation == null) stage2Activation = GetComponentInChildren<AluraMagicFieldStage2Activation>(true);
        if (stage3HealDamage == null) stage3HealDamage = GetComponentInChildren<AluraMagicFieldStage3HealDamage>(true);
        if (stage3HealOnly == null) stage3HealOnly = GetComponentInChildren<AluraMagicFieldStage3Heal>(true);
        if (stage3DamageOnly == null) stage3DamageOnly = GetComponentInChildren<AluraMagicFieldStage3Damage>(true);
        if (stage4Dissipation == null) stage4Dissipation = GetComponentInChildren<AluraMagicFieldStage4Dissipation>(true);

        if (stage1Targeting == null)
        {
            Transform t = transform.Find("Stage1_Targeting");
            if (t == null) { GameObject go = new GameObject("Stage1_Targeting"); go.transform.SetParent(transform, false); t = go.transform; }
            stage1Targeting = t.gameObject.AddComponent<AluraMagicFieldStage1Targeting>();
        }
        if (stage2Activation == null)
        {
            Transform t = transform.Find("Stage2_Activation");
            if (t == null) { GameObject go = new GameObject("Stage2_Activation"); go.transform.SetParent(transform, false); t = go.transform; }
            stage2Activation = t.gameObject.AddComponent<AluraMagicFieldStage2Activation>();
        }
        if (stage3HealDamage == null)
        {
            Transform t = transform.Find("Stage3_HealDamage");
            if (t == null) { GameObject go = new GameObject("Stage3_HealDamage"); go.transform.SetParent(transform, false); t = go.transform; }
            stage3HealDamage = t.gameObject.AddComponent<AluraMagicFieldStage3HealDamage>();
        }
        if (stage4Dissipation == null)
        {
            Transform t = transform.Find("Stage4_Dissipation");
            if (t == null) { GameObject go = new GameObject("Stage4_Dissipation"); go.transform.SetParent(transform, false); t = go.transform; }
            stage4Dissipation = t.gameObject.AddComponent<AluraMagicFieldStage4Dissipation>();
        }
    }

    private void OnValidate()
    {
        FindOrCreateStageComponents();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && gameObject.activeInHierarchy && enabled)
            {
                ApplyMasterBloom(masterBloomIntensity);
                if (stage1Targeting != null) stage1Targeting.RefreshLiveView();
                if (stage2Activation != null) stage2Activation.RefreshLiveView();
                if (stage3HealDamage != null) stage3HealDamage.RefreshLiveView();
                if (stage3HealOnly != null) stage3HealOnly.RefreshLiveView();
                if (stage3DamageOnly != null) stage3DamageOnly.RefreshLiveView();
                if (stage4Dissipation != null) stage4Dissipation.RefreshLiveView();
            }
        };
#endif
    }

    #endregion
}
