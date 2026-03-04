Assets/Scripts/Abas/WindTrap.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindTrap : MonoBehaviour
{
    [Header("Activation")]
    [Tooltip("Automatically start the wind trap on Start.")]
    public bool autoActivate = false;

    [Tooltip("If true the trap will continuously repeat cycles.")]
    public bool autoRepeat = true;

    [Tooltip("Delay between automatic repeats when autoRepeat is enabled.")]
    public float repeatDelay = 2f;

    [Header("Wind Settings")]
    [Tooltip("Force of the wind applied to the player (higher = stronger push).")]
    public float pushForce = 10f;

    [Tooltip("Direction of the wind in local space (use transform.forward by default).")]
    public Vector3 pushDirection = Vector3.forward;

    [Tooltip("If true, the trap uses its transform.forward as the push direction and ignores pushDirection field.")]
    public bool useLocalDirection = true;

    [Tooltip("How long (seconds) the wind animation / push remains active per activation.")]
    public float pushDuration = 1.0f;

    [Header("Cooldown")]
    [Tooltip("Time (seconds) the trap waits after an activation before it can be used again.")]
    public float cooldown = 3.0f;

    [Tooltip("If true the trap will perform cooldown after each activation.")]
    public bool useCooldown = true;

    [Header("References")]
    [Tooltip("Particle system used to visualise the wind. Particles only play while the trap is active.")]
    public ParticleSystem windParticles;

    [Header("Trigger / Target")]
    [Tooltip("Only colliders with this tag will be affected. Per project rule player must be checked with CompareTag(\"Player\").")]
    public string targetTag = "Player";

    [Header("Animation")]
    [Tooltip("Animator that drives the trap animation (optional).")]
    public Animator animator;

    [Tooltip("Trigger parameter name to invoke when the wind starts (optional).")]
    public string startTrigger = "Activate";

    [Tooltip("Trigger parameter name to invoke when the wind stops (optional).")]
    public string stopTrigger = "Deactivate";

    [Tooltip("If true the script will use a bool parameter instead of triggers. The bool will be set true on start and false on stop.")]
    public bool useBoolParameter = false;

    [Tooltip("Name of the bool parameter to toggle when useBoolParameter is enabled.")]
    public string boolParameter = "IsActive";

    [Header("Animator Duration")]
    [Tooltip("If true use the Animator's clip length (by name) as the active duration instead of 'pushDuration'.")]
    public bool useAnimatorDuration = false;

    [Tooltip("Name of the animation clip (or state) whose length will be used when useAnimatorDuration is enabled. If empty the first clip is used.")]
    public string animatorClipName = "";

    [Header("Debug")]
    [Tooltip("Enable debug logging.")]
    public bool debugMode = false;

    // runtime
    bool isActive = false;
    bool isCoolingDown = false;

    Coroutine activationCoroutine;
    Coroutine cooldownCoroutine;
    Coroutine repeatCoroutine;

    // keep track of colliders inside trigger area (only players per project rule)
    readonly HashSet<Collider> playersInside = new HashSet<Collider>();

    void Start()
    {
        // ensure particles are not playing initially
        if (windParticles != null)
            windParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (autoActivate)
            ActivateTrap(0f);
    }

    // Public API: schedule activation after optional delay (no-op while cooling)
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

    public void ActivateImmediate()
    {
        ActivateTrap(0f);
    }

    IEnumerator ActivationSequence(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // determine duration (pushDuration or animator clip length)
        float duration = pushDuration;
        if (useAnimatorDuration && animator != null)
        {
            var ac = animator.runtimeAnimatorController;
            if (ac != null && ac.animationClips != null && ac.animationClips.Length > 0)
            {
                AnimationClip clip = null;
                if (!string.IsNullOrEmpty(animatorClipName))
                {
                    foreach (var c in ac.animationClips)
                        if (c.name == animatorClipName) { clip = c; break; }
                }
                if (clip == null) clip = ac.animationClips[0];
                if (clip != null) duration = clip.length;
            }
        }

        // start active state
        isActive = true;

        if (windParticles != null)
            windParticles.Play();

        if (animator != null)
        {
            if (useBoolParameter)
                animator.SetBool(boolParameter, true);
            else if (!string.IsNullOrEmpty(startTrigger))
                animator.SetTrigger(startTrigger);
        }

        if (debugMode) Debug.Log($"[WindTrap] Activated for {duration} seconds.");

        // actively push in FixedUpdate; wait while active
        yield return new WaitForSeconds(duration);

        // stop active state
        StopWind();

        // start cooldown if requested
        if (useCooldown)
        {
            if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = StartCoroutine(CooldownRoutine());
        }

        // schedule auto repeat if enabled
        if (autoRepeat)
        {
            if (repeatCoroutine != null) StopCoroutine(repeatCoroutine);
            repeatCoroutine = StartCoroutine(AutoRepeatRoutine());
        }

        activationCoroutine = null;
    }

    void StopWind()
    {
        if (windParticles != null)
            windParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (animator != null)
        {
            if (useBoolParameter)
                animator.SetBool(boolParameter, false);
            else if (!string.IsNullOrEmpty(stopTrigger))
                animator.SetTrigger(stopTrigger);
        }

        isActive = false;

        if (debugMode) Debug.Log("[WindTrap] Stopped.");
    }

    IEnumerator CooldownRoutine()
    {
        isCoolingDown = true;
        if (debugMode) Debug.Log($"[WindTrap] Cooling down for {cooldown} seconds.");
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

    // Trigger area management - per project rule only affect colliders tagged "Player"
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

    // Apply smooth continuous push in FixedUpdate for stable physics
    void FixedUpdate()
    {
        if (!isActive || playersInside.Count == 0) return;

        Vector3 dir = useLocalDirection ? transform.forward : pushDirection.normalized;
        dir.Normalize();

        foreach (var col in playersInside)
        {
            if (col == null) continue;
            Rigidbody rb = col.attachedRigidbody ?? col.GetComponentInParent<Rigidbody>();
            if (rb == null) continue;

            // Apply acceleration for a smooth constant push (ignores mass)
            rb.AddForce(dir * pushForce, ForceMode.Acceleration);

            if (debugMode)
            {
                Debug.Log($"[WindTrap] Applying wind to '{col.name}' (force={pushForce}) dir={dir}.");
            }
        }
    }

    void OnDisable()
    {
        if (activationCoroutine != null) { StopCoroutine(activationCoroutine); activationCoroutine = null; }
        if (cooldownCoroutine != null) { StopCoroutine(cooldownCoroutine); cooldownCoroutine = null; }
        if (repeatCoroutine != null) { StopCoroutine(repeatCoroutine); repeatCoroutine = null; }

        isActive = false;
        isCoolingDown = false;

        if (windParticles != null)
            windParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        playersInside.Clear();

        if (animator != null && useBoolParameter)
            animator.SetBool(boolParameter, false);
    }
}