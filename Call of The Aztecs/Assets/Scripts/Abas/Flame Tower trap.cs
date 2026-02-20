using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FlameTowertrap : MonoBehaviour
{
    [Header("Activation")]
    [Tooltip("Automatically start the warning -> active sequence on Start.")]
    public bool autoActivate = false;

    [Tooltip("If true the trap will continuously repeat cycles.")]
    public bool autoRepeat = false;

    [Tooltip("Time (seconds) the warning effect plays before flames start.")]
    public float warningDuration = 1.5f;

    [Tooltip("Time the trap waits with all flames active before beginning shutdown.")]
    public float flamesHoldTime = 2.0f;

    [Tooltip("Delay between full cycles when autoRepeat is enabled.")]
    public float repeatDelay = 5.0f;

    [Tooltip("When true the top warning stays on during the whole active period; when false the top warning stops when damaging flames start.")]
    public bool keepWarningDuringActive = true;

    [Header("Per-flame timing")]
    [Tooltip("Delay between starting each flame (first -> second -> third).")]
    public float flameStartInterval = 0.4f;

    [Tooltip("Delay between stopping each flame when shutting down.")]
    public float flameStopInterval = 0.4f;

    [Header("Damage")]
    [Tooltip("Damage applied per tick")]
    public int damageAmount = 10;

    [Tooltip("How often damage is applied per target (seconds).")]
    public float tickInterval = 0.5f;

    [Tooltip("Layer mask used to filter damaged targets.")]
    public LayerMask targetLayer = ~0;

    [Tooltip("Optional tag filter for targets (empty = ignore tag).")]
    public string targetTag = "Player";

    [Header("References")]
    [Tooltip("Particle system used as the 'warning' (e.g. small flame or smoke on top).")]
    public ParticleSystem warningEffect;

    [Tooltip("Particle systems used for the active flame/fire. Assign your three flame ParticleSystems here in order.")]
    public ParticleSystem[] flameEffects;

    [Header("Debug")]
    public bool debugMode = false;

    // runtime state
    bool isActive = false;           // true while flames are playing
    bool isWarningPlaying = false;   // true while warningEffect is playing
    Coroutine cycleCoroutine = null;

    // per-target cooldown/tick bookkeeping
    readonly Dictionary<Transform, float> lastDamageTime = new Dictionary<Transform, float>();

    void Awake()
    {
        // Stop assigned particle systems initially and ensure they will send collision messages.
        if (warningEffect != null)
            warningEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (flameEffects != null)
        {
            foreach (var p in flameEffects)
            {
                if (p == null) continue;
                p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                // Enable collision module and send collision messages so OnParticleCollision is fired.
                var cm = p.collision;
                cm.enabled = true;
                cm.sendCollisionMessages = true;
                // NOTE: configure other collision settings (Collides With, Type, Quality) in the inspector as needed.

                // Add proxy on the particle system GameObject to forward collisions to this trap.
                if (p.gameObject != gameObject)
                {
                    var proxy = p.gameObject.GetComponent<ParticleCollisionProxy>();
                    if (proxy == null) proxy = p.gameObject.AddComponent<ParticleCollisionProxy>();
                    proxy.owner = this;
                }
            }
        }
    }

    void Start()
    {
        if (autoActivate)
            StartCoroutine(ActivationSequence(warningDuration));
    }

    public void ActivateTrap(float delay)
    {
        StartCoroutine(ActivationSequence(delay));
    }

    public void ActivateImmediate()
    {
        StartCoroutine(ActivateImmediateRoutine());
    }

    IEnumerator ActivateImmediateRoutine()
    {
        yield return null;
        StartFlames();
        // Use flamesHoldTime (not a missing variable) to control how long flames remain
        yield return new WaitForSeconds(flamesHoldTime);
        StopFlames();
    }

    IEnumerator ActivationSequence(float delay)
    {
        if (isActive || isWarningPlaying) yield break;

        isWarningPlaying = true;
        if (warningEffect != null) warningEffect.Play();
        if (debugMode) Debug.Log($"[FlameTowertrap] Warning started for {delay} seconds.");

        yield return new WaitForSeconds(delay);

        if (!keepWarningDuringActive && warningEffect != null)
            warningEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        isWarningPlaying = false;
        StartFlames();

        // Use flamesHoldTime (not activeDuration) to control how long flames remain
        yield return new WaitForSeconds(flamesHoldTime);

        StopFlames();
    }

    void StartFlames()
    {
        if (isActive) return;
        isActive = true;

        if (flameEffects != null)
            foreach (var p in flameEffects) if (p != null) p.Play();

        lastDamageTime.Clear();

        if (debugMode) Debug.Log("[FlameTowertrap] Flames activated.");
    }

    void StopFlames()
    {
        if (!isActive) return;
        isActive = false;

        if (flameEffects != null)
            foreach (var p in flameEffects) if (p != null) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (keepWarningDuringActive && warningEffect != null)
            warningEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (debugMode) Debug.Log("[FlameTowertrap] Flames deactivated.");
    }

    // Called by ParticleCollisionProxy when a particle from any flame hits something.
    // Use per-target tick cooldown to avoid applying damage every particle/frame.
    public void OnParticleHit(GameObject other)
    {
        if (!isActive) return;
        if (other == null) return;

        // layer filter
        if (((1 << other.layer) & targetLayer) == 0) return;

        // tag filter
        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag)) return;

        TryDamage(other);
    }

    void TryDamage(GameObject obj)
    {
        if (obj == null) return;
        Transform t = obj.transform;
        float now = Time.time;

        if (lastDamageTime.TryGetValue(t, out float last) && now - last < tickInterval) return;

        var ph = obj.GetComponent<playerHealth>() ?? obj.GetComponentInParent<playerHealth>();
        if (ph != null && damageAmount > 0)
        {
            if (debugMode) Debug.Log($"[FlameTowertrap] Particle hit damaging '{obj.name}' for {damageAmount}");
            ph.TakeDamage(damageAmount);
        }
        else if (debugMode)
        {
            Debug.Log("[FlameTowertrap] No playerHealth found on target");
        }

        lastDamageTime[t] = now;
    }

    void OnDisable()
    {
        isActive = false;
        isWarningPlaying = false;

        if (warningEffect != null) warningEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (flameEffects != null)
            foreach (var p in flameEffects) if (p != null) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // Proxy component attached to each flame ParticleSystem GameObject to forward OnParticleCollision to the trap.
    public class ParticleCollisionProxy : MonoBehaviour
    {
        [HideInInspector] public FlameTowertrap owner;

        void OnParticleCollision(GameObject other)
        {
            owner?.OnParticleHit(other);
        }
    }
}
