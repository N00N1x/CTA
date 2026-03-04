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

    [Header("Reset / Cooldown")]
    [Tooltip("If true the trap will perform a reset (cooldown) after the flames stop.")]
    public bool resetAfterActive = true;

    [Tooltip("Time (seconds) to wait before the trap is ready again after flames stop (cooldown).")]
    public float resetDelay = 5.0f;

    [Tooltip("If true the trap will automatically start again after the cooldown completes.")]
    public bool reactivateAfterCooldown = false;

    [Tooltip("Extra delay (seconds) after cooldown before automatic reactivation (if enabled).")]
    public float reactivateDelay = 0.0f;

    [Header("Cooldown warning (pre-shutdown)")]
    [Tooltip("If true the warning particle will play before flames stop to signal cooldown starting.")]
    public bool useCooldownWarning = true;

    [Tooltip("Particle system used to signal the trap is going into cooldown (can be same as warningEffect).")]
    public ParticleSystem cooldownWarningEffect;

    [Tooltip("How long the cooldown warning plays before flames are stopped.")]
    public float cooldownWarningDuration = 1.0f;

    [Tooltip("Extra time to wait after the cooldown-warning stops before damaging flames stop.")]
    public float postCooldownDelay = 0.5f;

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

    [Header("Audio")]
    [Tooltip("Optional AudioSource on the trap. If present it will be used for flames that live on the same GameObject.")]
    public AudioSource flameAudioSource;

    [Tooltip("Three (or more) flame audio clips — for each flame the script will pick a random clip from this array and play it while that flame is active.")]
    public AudioClip[] flameClips;

    [Tooltip("Optional audio clip played while the top warningEffect is playing.")]
    public AudioClip warningClip;

    [Header("Debug")]
    public bool debugMode = false;

    // runtime state
    bool isActive = false;           // true while flames visual are playing
    bool isWarningPlaying = false;   // true while warningEffect is playing
    Coroutine cycleCoroutine = null;
    Coroutine resetCoroutine = null;
    Coroutine reactivateCoroutine = null;
    // cooldown state: when true trap will refuse to start a new activation
    bool isCoolingDown = false;

    // per-target cooldown/tick bookkeeping
    readonly Dictionary<Transform, float> lastDamageTime = new Dictionary<Transform, float>();

    // per-flame AudioSources (one per flame ParticleSystem GameObject or fallback to trap AudioSource)
    AudioSource[] flameLocalAudioSources;

    // audio source for the warningEffect (on the warning GameObject or reusing trap AudioSource)
    AudioSource warningLocalAudioSource;

    void Awake()
    {
        // Stop assigned particle systems initially and ensure they will send collision messages.
        if (warningEffect != null)
            warningEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (cooldownWarningEffect != null && cooldownWarningEffect != warningEffect)
            cooldownWarningEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (flameEffects != null)
        {
            flameLocalAudioSources = new AudioSource[flameEffects.Length];

            for (int i = 0; i < flameEffects.Length; i++)
            {
                var p = flameEffects[i];
                if (p == null) continue;
                p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                // Enable collision module and send collision messages so OnParticleCollision is fired.
                var cm = p.collision;
                cm.enabled = true;
                cm.sendCollisionMessages = true;

                // Add proxy on the particle system GameObject to forward collisions to this trap.
                if (p.gameObject != gameObject)
                {
                    var proxy = p.gameObject.GetComponent<ParticleCollisionProxy>();
                    if (proxy == null) proxy = p.gameObject.AddComponent<ParticleCollisionProxy>();
                    proxy.owner = this;

                    // Ensure an AudioSource exists on the flame GameObject for per-flame audio
                    var a = p.gameObject.GetComponent<AudioSource>();
                    if (a == null) a = p.gameObject.AddComponent<AudioSource>();
                    a.playOnAwake = false;
                    a.loop = true;
                    // default to spatial (3D) so sounds feel local to flame position
                    a.spatialBlend = 1f;
                    flameLocalAudioSources[i] = a;
                }
                else
                {
                    // Flame lives on same GameObject as trap — fallback to trap AudioSource (create if necessary)
                    if (flameAudioSource == null)
                    {
                        flameAudioSource = GetComponent<AudioSource>();
                        if (flameAudioSource == null)
                        {
                            flameAudioSource = gameObject.AddComponent<AudioSource>();
                            flameAudioSource.playOnAwake = false;
                        }
                    }

                    // reuse trap AudioSource for this flame (note: multiple flames sharing same AudioSource will not overlap)
                    flameLocalAudioSources[i] = flameAudioSource;
                }
            }
        }
        else
        {
            flameLocalAudioSources = new AudioSource[0];
        }

        // Setup warning audio source (prefer audio on the warningEffect GameObject)
        if (warningEffect != null)
        {
            if (warningEffect.gameObject != gameObject)
            {
                var wa = warningEffect.gameObject.GetComponent<AudioSource>();
                if (wa == null) wa = warningEffect.gameObject.AddComponent<AudioSource>();
                wa.playOnAwake = false;
                wa.loop = true;
                wa.spatialBlend = 1f;
                warningLocalAudioSource = wa;
            }
            else
            {
                // warning lives on same GameObject as trap -> reuse/create flameAudioSource
                if (flameAudioSource == null)
                {
                    flameAudioSource = GetComponent<AudioSource>();
                    if (flameAudioSource == null)
                    {
                        flameAudioSource = gameObject.AddComponent<AudioSource>();
                        flameAudioSource.playOnAwake = false;
                    }
                }
                warningLocalAudioSource = flameAudioSource;
            }
        }

        // if flameClips array is empty, warn in debug
        if ((flameClips == null || flameClips.Length == 0) && debugMode)
            Debug.Log("[FlameTowertrap] No flameClips assigned - flames will not play per-flame audio.");

        if (warningEffect != null && warningClip == null && debugMode)
            Debug.Log("[FlameTowertrap] No warningClip assigned - top warning will not play audio.");
    }

    void Start()
    {
        if (autoActivate)
            StartCoroutine(ActivationSequence(warningDuration));
    }

    // Public API: do nothing while cooling down
    public void ActivateTrap(float delay)
    {
        if (isCoolingDown)
        {
            if (debugMode) Debug.Log("[FlameTowertrap] Cannot activate: trap cooling down.");
            return;
        }

        StartCoroutine(ActivationSequence(delay));
    }

    public void ActivateImmediate()
    {
        if (isCoolingDown)
        {
            if (debugMode) Debug.Log("[FlameTowertrap] Cannot activate immediate: trap cooling down.");
            return;
        }

        StartCoroutine(ActivateImmediateRoutine());
    }

    IEnumerator ActivateImmediateRoutine()
    {
        yield return null;
        StartFlames();
        // Use flamesHoldTime to control how long flames remain
        yield return new WaitForSeconds(flamesHoldTime);
        // when immediate sequence ends, perform shutdown sequence (with optional cooldown warning)
        yield return StartCoroutine(ShutdownSequence());
    }

    IEnumerator ActivationSequence(float delay)
    {
        // prevent starting while active, warning, or cooling down
        if (isActive || isWarningPlaying || isCoolingDown) yield break;

        isWarningPlaying = true;
        if (warningEffect != null)
        {
            warningEffect.Play();

            // start warning audio
            if (warningLocalAudioSource != null && warningClip != null)
            {
                warningLocalAudioSource.clip = warningClip;
                warningLocalAudioSource.loop = true;
                warningLocalAudioSource.Play();
            }
        }
        if (debugMode) Debug.Log($"[FlameTowertrap] Warning started for {delay} seconds.");

        yield return new WaitForSeconds(delay);

        if (!keepWarningDuringActive && warningEffect != null)
        {
            warningEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // stop warning audio when warning stops
            if (warningLocalAudioSource != null && warningLocalAudioSource.isPlaying)
            {
                warningLocalAudioSource.Stop();
                warningLocalAudioSource.clip = null;
                warningLocalAudioSource.loop = false;
            }
        }

        isWarningPlaying = false;

        // Start flames (sequential)
        StartFlames();

        // Wait while flames are active
        yield return new WaitForSeconds(flamesHoldTime);

        // run shutdown sequence (play cooldown warning then stop flames)
        yield return StartCoroutine(ShutdownSequence());
    }

    // Start flames sequentially (warning already played by ActivationSequence).
    void StartFlames()
    {
        if (isActive) return;

        // cancel pending reset/reactivate if manually reactivated
        if (resetCoroutine != null)
        {
            StopCoroutine(resetCoroutine);
            resetCoroutine = null;
            isCoolingDown = false;
            if (debugMode) Debug.Log("[FlameTowertrap] Pending reset cancelled because flames restarted.");
        }

        if (reactivateCoroutine != null)
        {
            StopCoroutine(reactivateCoroutine);
            reactivateCoroutine = null;
            if (debugMode) Debug.Log("[FlameTowertrap] Pending reactivation cancelled because flames restarted.");
        }

        isActive = true;
        lastDamageTime.Clear();
        StartCoroutine(StartFlamesSequential());

        // Damage application now follows flame visuals: while isActive == true OnParticleHit applies damage.
        if (debugMode) Debug.Log("[FlameTowertrap] Flames activated; damage will apply while active.");
    }

    // coroutine to start each flame particle system one-by-one
    IEnumerator StartFlamesSequential()
    {
        if (flameEffects == null || flameEffects.Length == 0)
            yield break;

        for (int i = 0; i < flameEffects.Length; i++)
        {
            var p = flameEffects[i];
            if (p != null)
            {
                p.Play();

                // assign a random clip to this flame's AudioSource and play it (only while flame active)
                if (flameClips != null && flameClips.Length > 0 && flameLocalAudioSources != null && i < flameLocalAudioSources.Length)
                {
                    var a = flameLocalAudioSources[i];
                    if (a != null)
                    {
                        var clip = flameClips[Random.Range(0, flameClips.Length)];
                        if (clip != null)
                        {
                            a.clip = clip;
                            a.loop = true;
                            a.Play();
                        }
                    }
                }

                if (debugMode) Debug.Log($"[FlameTowertrap] Flame {i + 1} started.");
            }
            yield return new WaitForSeconds(flameStartInterval);
        }
    }

    IEnumerator ShutdownSequence()
    {
        if (useCooldownWarning)
        {
            var cw = cooldownWarningEffect ?? warningEffect;
            if (cw != null)
            {
                cw.Play();
                if (debugMode) Debug.Log($"[FlameTowertrap] Cooldown warning started for {cooldownWarningDuration}s.");
                yield return new WaitForSeconds(cooldownWarningDuration);
                // stop only the cooldown warning, not the main warning if it should remain
                cw.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            else
            {
                if (debugMode) Debug.Log("[FlameTowertrap] Cooldown warning requested but no effect assigned.");
            }
        }

        // optional extra wait between cooldown warning and flame shutdown
        if (postCooldownDelay > 0f)
            yield return new WaitForSeconds(postCooldownDelay);

        // stop flames sequentially then schedule reset/auto-repeat
        yield return StartCoroutine(StopFlamesSequential());
    }

    // Stop flames one-by-one then schedule reset / auto-repeat
    IEnumerator StopFlamesSequential()
    {
        if (flameEffects != null)
        {
            for (int i = 0; i < flameEffects.Length; i++)
            {
                var p = flameEffects[i];
                if (p != null)
                {
                    p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    if (debugMode) Debug.Log($"[FlameTowertrap] Flame {i + 1} stopped.");
                }

                // stop per-flame audio
                if (flameLocalAudioSources != null && i < flameLocalAudioSources.Length)
                {
                    var a = flameLocalAudioSources[i];
                    if (a != null && a.isPlaying)
                    {
                        a.Stop();
                        a.clip = null;
                        a.loop = false;
                    }
                }

                yield return new WaitForSeconds(flameStopInterval);
            }
        }

        // stop main warning if it was kept during active
        if (keepWarningDuringActive && warningEffect != null)
        {
            warningEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // also stop warning audio if it was playing
            if (warningLocalAudioSource != null && warningLocalAudioSource.isPlaying)
            {
                warningLocalAudioSource.Stop();
                warningLocalAudioSource.clip = null;
                warningLocalAudioSource.loop = false;
            }
        }

        isActive = false;

        // schedule reset (cooldown) if requested
        if (resetAfterActive)
        {
            // enter cooldown immediately
            isCoolingDown = true;

            if (resetCoroutine != null) StopCoroutine(resetCoroutine);
            resetCoroutine = StartCoroutine(ResetRoutine());
        }

        // schedule auto-repeat if requested
        if (autoRepeat)
        {
            if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);
            cycleCoroutine = StartCoroutine(AutoRepeatRoutine());
        }

        yield break;
    }

    IEnumerator ResetRoutine()
    {
        if (debugMode) Debug.Log($"[FlameTowertrap] Cooling down for {resetDelay} seconds.");
        yield return new WaitForSeconds(resetDelay);

        // Reset state: clear damage history and make trap ready for next activation.
        lastDamageTime.Clear();
        isWarningPlaying = false;
        isActive = false;
        isCoolingDown = false;
        resetCoroutine = null;

        if (debugMode) Debug.Log("[FlameTowertrap] Cooldown complete - trap ready.");

        // optional automatic reactivation after cooldown
        if (reactivateAfterCooldown)
        {
            if (reactivateDelay > 0f)
            {
                if (debugMode) Debug.Log($"[FlameTowertrap] Reactivating in {reactivateDelay} seconds.");
                if (reactivateCoroutine != null) StopCoroutine(reactivateCoroutine);
                reactivateCoroutine = StartCoroutine(DelayedReactivateRoutine(reactivateDelay));
            }
            else
            {
                if (debugMode) Debug.Log("[FlameTowertrap] Reactivating now after cooldown.");
                StartCoroutine(ActivationSequence(warningDuration));
            }
        }
    }

    IEnumerator DelayedReactivateRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        reactivateCoroutine = null;
        if (!isCoolingDown && !isActive)
            StartCoroutine(ActivationSequence(warningDuration));
    }

    IEnumerator AutoRepeatRoutine()
    {
        yield return new WaitForSeconds(repeatDelay);
        cycleCoroutine = null;
        // start a fresh activation (warning -> flames) only if not cooling down
        if (!isCoolingDown)
            StartCoroutine(ActivationSequence(warningDuration));
    }

    // Called by ParticleCollisionProxy when a particle from any flame hits something.
    // Damage now applies whenever flame visuals are active (isActive).
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
        if (resetCoroutine != null) { StopCoroutine(resetCoroutine); resetCoroutine = null; }
        if (cycleCoroutine != null) { StopCoroutine(cycleCoroutine); cycleCoroutine = null; }
        if (reactivateCoroutine != null) { StopCoroutine(reactivateCoroutine); reactivateCoroutine = null; }

        isActive = false;
        isWarningPlaying = false;
        isCoolingDown = false;

        if (warningEffect != null) warningEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (cooldownWarningEffect != null && cooldownWarningEffect != warningEffect)
            cooldownWarningEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (flameEffects != null)
            foreach (var p in flameEffects) if (p != null) p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // stop all flame audio on disable
        if (flameLocalAudioSources != null)
        {
            for (int i = 0; i < flameLocalAudioSources.Length; i++)
            {
                var a = flameLocalAudioSources[i];
                if (a != null && a.isPlaying)
                {
                    a.Stop();
                    a.clip = null;
                    a.loop = false;
                }
            }
        }

        // stop warning audio on disable
        if (warningLocalAudioSource != null && warningLocalAudioSource.isPlaying)
        {
            warningLocalAudioSource.Stop();
            warningLocalAudioSource.clip = null;
            warningLocalAudioSource.loop = false;
        }
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
