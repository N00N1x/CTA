using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple WindTrap:
/// - Uses a trigger collider area to detect the Player (CompareTag("Player")).
/// - Plays a ParticleSystem only while active (visual only).
/// - Applies a smooth, continuous push to Player Rigidbodies inside the trigger using FixedUpdate + ForceMode.Acceleration.
/// - Cycle: idle -> active (pushDuration) -> cooldown -> optional auto-repeat.
/// - Exposes pushForce, pushDuration, cooldown, direction, autoRepeat, repeatDelay and a single Animator (toggled via bool "IsActive") in the Inspector.
/// - Includes OnValidate clamps and an editor gizmo showing wind direction.
/// </summary>
public class WindTrap : MonoBehaviour
{
    [Header("Activation")]
    [Tooltip("Automatically start the wind trap on Start.")]
    public bool autoActivate = false;

    [Tooltip("If true the trap will automatically repeat cycles.")]
    public bool autoRepeat = false;

    [Tooltip("Delay between automatic repeats when autoRepeat is enabled.")]
    [Min(0f)]
    public float repeatDelay = 2f;

    [Header("Wind Settings")]
    [Tooltip("Acceleration applied to players while inside the wind area (ForceMode.Acceleration).")]
    [Min(0f)]
    public float pushForce = 10f;

    [Tooltip("How long (seconds) the trap remains active (push + particles).")]
    [Min(0f)]
    public float pushDuration = 1.0f;

    [Tooltip("If true use this transform's forward as wind direction. Otherwise use pushDirection (local).")]
    public bool useLocalDirection = true;

    [Tooltip("Local-space direction used when useLocalDirection is false.")]
    public Vector3 pushDirection = Vector3.forward;

    [Header("Cooldown")]
    [Tooltip("Time (seconds) to wait after an activation before trap may activate again.")]
    [Min(0f)]
    public float cooldown = 3.0f;

    [Tooltip("If true the trap will perform a cooldown after each activation.")]
    public bool useCooldown = true;

    [Header("References")]
    [Tooltip("Particle system used to visualise the wind. PlayOnAwake should usually be false.")]
    public ParticleSystem windParticles;

    [Tooltip("Animator to show active/idle states. Script toggles bool parameter named 'IsActive' when starting/stopping.")]
    public Animator animator;

    [Header("Debug")]
    [Tooltip("Enable debug logging.")]
    public bool debugMode = false;

    // runtime
    bool isActive = false;
    bool isCoolingDown = false;

    Coroutine activationCoroutine;
    Coroutine cooldownCoroutine;
    Coroutine repeatCoroutine;

    // players inside trigger (only colliders tagged Player are added)
    readonly HashSet<Collider> playersInside = new HashSet<Collider>();

    void OnValidate()
    {
        // keep sensible values in Inspector
        if (pushForce < 0f) pushForce = 0f;
        if (pushDuration < 0f) pushDuration = 0f;
        if (cooldown < 0f) cooldown = 0f;
        if (repeatDelay < 0f) repeatDelay = 0f;
        if (pushDirection == Vector3.zero) pushDirection = Vector3.forward;
    }

    void Start()
    {
        // ensure visuals are off initially
        if (windParticles != null)
            windParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (autoActivate)
            ActivateTrap(0f);
    }

    /// <summary>
    /// Schedule activation after optional delay. Will not start if currently cooling down or already active.
    /// </summary>
    public void ActivateTrap(float delay)
    {
        if (isCoolingDown || isActive)
        {
            if (debugMode) Debug.Log("[WindTrap] Cannot activate: already active or cooling down.");
            return;
        }

        if (activationCoroutine != null) StopCoroutine(activationCoroutine);
        activationCoroutine = StartCoroutine(ActivationSequence(delay));
    }

    public void ActivateImmediate() => ActivateTrap(0f);

    IEnumerator ActivationSequence(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        // start active
        isActive = true;

        if (windParticles != null) windParticles.Play();

        // set animator bool "IsActive" if animator is present
        if (animator != null)
            animator.SetBool("IsActive", true);

        if (debugMode) Debug.Log($"[WindTrap] Activated for {pushDuration}s. pushForce={pushForce}");

        // push happens in FixedUpdate
        yield return new WaitForSeconds(pushDuration);

        // stop active state
        StopTrap();

        // start cooldown if requested
        if (useCooldown)
        {
            if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = StartCoroutine(CooldownRoutine());
        }

        // schedule auto-repeat if enabled
        if (autoRepeat)
        {
            if (repeatCoroutine != null) StopCoroutine(repeatCoroutine);
            repeatCoroutine = StartCoroutine(AutoRepeatRoutine());
        }

        activationCoroutine = null;
    }

    void StopTrap()
    {
        if (windParticles != null) windParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (animator != null)
            animator.SetBool("IsActive", false);

        isActive = false;

        if (debugMode) Debug.Log("[WindTrap] Stopped.");
    }

    IEnumerator CooldownRoutine()
    {
        isCoolingDown = true;
        if (debugMode) Debug.Log($"[WindTrap] Cooling down for {cooldown}s.");
        yield return new WaitForSeconds(cooldown);
        isCoolingDown = false;
        cooldownCoroutine = null;
        if (debugMode) Debug.Log("[WindTrap] Cooldown complete.");
    }

    IEnumerator AutoRepeatRoutine()
    {
        yield return new WaitForSeconds(repeatDelay);
        repeatCoroutine = null;
        if (!isCoolingDown && !isActive)
            ActivateTrap(0f);
    }

    // Trigger area only cares about Player-tagged colliders per project rules.
    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (!other.CompareTag("Player")) return;
        playersInside.Add(other);
    }

    void OnTriggerExit(Collider other)
    {
        if (other == null) return;
        if (!other.CompareTag("Player")) return;
        playersInside.Remove(other);
    }

    // Apply smooth constant push in physics step
    void FixedUpdate()
    {
        if (!isActive || playersInside.Count == 0) return;

        Vector3 dir = useLocalDirection ? transform.forward : pushDirection.normalized;
        dir.Normalize();

        foreach (var col in playersInside)
        {
            if (col == null) continue;
            // find rigidbody: attached or in parent
            Rigidbody rb = col.attachedRigidbody ?? col.GetComponentInParent<Rigidbody>();
            if (rb == null) continue;

            // acceleration-based push to feel smooth and mass-independent
            rb.AddForce(dir * pushForce, ForceMode.Acceleration);
        }

        if (debugMode)
            Debug.Log($"[WindTrap] Applying wind to {playersInside.Count} player(s) — force={pushForce} dir={(useLocalDirection ? transform.forward : pushDirection.normalized)}");
    }

    void OnDisable()
    {
        // stop coroutines
        if (activationCoroutine != null) { StopCoroutine(activationCoroutine); activationCoroutine = null; }
        if (cooldownCoroutine != null) { StopCoroutine(cooldownCoroutine); cooldownCoroutine = null; }
        if (repeatCoroutine != null) { StopCoroutine(repeatCoroutine); repeatCoroutine = null; }

        // reset state
        isActive = false;
        isCoolingDown = false;

        if (windParticles != null)
            windParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        playersInside.Clear();

        if (animator != null)
            animator.SetBool("IsActive", false);
    }

    // Editor gizmo: show wind direction when selected
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position;
        Vector3 dir = useLocalDirection ? transform.forward : transform.TransformDirection(pushDirection.normalized);
        Gizmos.DrawLine(origin, origin + dir.normalized * 2.0f);
        // small arrow head
        Vector3 right = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(dir) * Quaternion.Euler(0, -150, 0) * Vector3.forward;
        Gizmos.DrawLine(origin + dir.normalized * 2.0f, origin + dir.normalized * 1.6f + right * 0.3f);
        Gizmos.DrawLine(origin + dir.normalized * 2.0f, origin + dir.normalized * 1.6f + left * 0.3f);
    }
}