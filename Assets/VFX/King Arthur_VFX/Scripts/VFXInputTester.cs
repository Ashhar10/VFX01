using UnityEngine;

/// <summary>
/// Simple input tester for VFX. Press configured keys to trigger attack animation and VFX.
/// Attach to the same GameObject as VFXAnimationSync.
/// </summary>
[RequireComponent(typeof(VFXAnimationSync))]
public class VFXInputTester : MonoBehaviour
{
    [Header("=== Input Keys ===")]
    [Tooltip("Key to trigger the full attack (animation + both VFX with configured delays).")]
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;

    [Tooltip("Key to manually trigger only VFX 1 (sword slash) without animation.")]
    [SerializeField] private KeyCode trailOnlyKey = KeyCode.Alpha1;

    [Tooltip("Key to manually trigger only VFX 2 (Holy Ground Ring on the floor) without animation.")]
    [SerializeField] private KeyCode floorRingOnlyKey = KeyCode.Alpha2;

    [Tooltip("Key to manually trigger only VFX 3 (ground slash debris) without animation.")]
    [SerializeField] private KeyCode groundSlashOnlyKey = KeyCode.Alpha3;

    [Tooltip("Key to stop all VFX immediately.")]
    [SerializeField] private KeyCode stopAllKey = KeyCode.R;

    [Header("=== Debug ===")]
    [Tooltip("Show debug log messages in console.")]
    [SerializeField] private bool debugLog = true;

    private VFXAnimationSync syncController;

    private void Awake()
    {
        syncController = GetComponent<VFXAnimationSync>();
    }

    private void Update()
    {
        if (syncController == null) return;

        // Full attack
        if (Input.GetKeyDown(attackKey))
        {
            if (debugLog) Debug.Log("[VFXInputTester] Attack triggered!");
            syncController.PlayAttack();
        }

        // Sword Slash Trail only
        if (Input.GetKeyDown(trailOnlyKey))
        {
            if (debugLog) Debug.Log("[VFXInputTester] Sword Slash Trail triggered.");
            syncController.TriggerTrailVFX();
        }

        // Holy Ground Ring only
        if (Input.GetKeyDown(floorRingOnlyKey))
        {
            if (debugLog) Debug.Log("[VFXInputTester] Holy Ground Ring triggered.");
            syncController.TriggerFloorRingVFX();
        }

        // Ground slash debris only
        if (Input.GetKeyDown(groundSlashOnlyKey))
        {
            if (debugLog) Debug.Log("[VFXInputTester] Ground Slash VFX triggered.");
            syncController.TriggerGroundSlashVFX();
        }

        // Stop all
        if (Input.GetKeyDown(stopAllKey))
        {
            if (debugLog) Debug.Log("[VFXInputTester] All VFX stopped.");
            syncController.StopAll();
        }
    }
}
