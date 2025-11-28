using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class BamboTrap : MonoBehaviour
{
    [Header("Cycle")]
    public bool autoCycle = true;
    public bool startOnAwake = true;
    public float idleDuration = 2f;      // time between activations
    public float activeDuration = 1f;    // how long the spikes are dangerous

    [Header("Detection (optional)")]
    [Tooltip("If true, the trap will only start cycling while a player stands on the platform (requires a trigger collider on the platform).")]
    public bool onlyWhenPlayerOnPlatform = false;

    [Header("Animator / VFX")]
    public Animator animator;
    public string animatorBoolParameter = "IsActive";
    public ParticleSystem particles;

    [Header("Damage")]
    public LayerMask playerLayer = 1 << 8; // set to your Player layer in the Inspector
    public int damageAmount = 1;
    public Vector3 damageBoxCenter = new Vector3(0f, 0.5f, 0.5f); // local offset
    public Vector3 damageBoxSize = new Vector3(1f, 1f, 1f);      // box extents

    [Header("Gizmo")]
    public Color gizmoColor = new Color(1f, 0.5f, 0f, 0.25f);

    // runtime
    private bool isActive = false;
    private Coroutine cycleCoroutine;
    private readonly HashSet<Transform> damagedThisActivation = new HashSet<Transform>();
    private readonly HashSet<Collider> playersOnPlatform = new HashSet<Collider>();

    private void Awake()
    {
        if (particles != null)
        {
            var main = particles.main;
            main.loop = false;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Clear();
        }
    }

    private void Start()
    {
        if (autoCycle && startOnAwake)
            StartCycle();
    }

    public void StartCycle()
    {
        if (cycleCoroutine == null)
            cycleCoroutine = StartCoroutine(CycleRoutine());
    }

    public void StopCycle()
    {
        if (cycleCoroutine != null)
        {
            StopCoroutine(cycleCoroutine);
            cycleCoroutine = null;
        }
        Deactivate();
    }

    private IEnumerator CycleRoutine()
    {
        while (true)
        {
            if (onlyWhenPlayerOnPlatform)
            {
                while (playersOnPlatform.Count == 0)
                    yield return null;
            }

            yield return new WaitForSeconds(idleDuration);

            if (onlyWhenPlayerOnPlatform && playersOnPlatform.Count == 0)
                continue;

            Activate();
            yield return new WaitForSeconds(activeDuration);
            Deactivate();
        }
    }

    private void Activate()
    {
        isActive = true;
        damagedThisActivation.Clear();

        if (animator != null)
            animator.SetBool(animatorBoolParameter, true);

        if (particles != null && !particles.isPlaying)
        {
            particles.Clear();
            particles.Play();
        }
    }

    private void Deactivate()
    {
        isActive = false;

        if (animator != null)
            animator.SetBool(animatorBoolParameter, false);

        if (particles != null)
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void FixedUpdate()
    {
        if (!isActive) return;

        Vector3 worldCenter = transform.TransformPoint(damageBoxCenter);
        Vector3 halfExtents = damageBoxSize * 0.5f;
        Collider[] cols = Physics.OverlapBox(worldCenter, halfExtents, transform.rotation, playerLayer, QueryTriggerInteraction.Collide);

        foreach (var c in cols)
        {
            if (c == null || c.transform == null) continue;
            if (damagedThisActivation.Contains(c.transform)) continue;

            // Traditional spike trap: only apply damage (no knockback)
            if (damageAmount > 0)
                TryInvokeTakeDamage(c.gameObject, damageAmount);

            damagedThisActivation.Add(c.transform);
        }
    }

    // Safe reflection-based damage caller: calls TakeDamage(int) on any component if present.
    private void TryInvokeTakeDamage(GameObject target, int amount)
    {
        // try a component named "PlayerHealth" first (avoids compile-time dependency)
        Component phComp = target.GetComponent("PlayerHealth") as Component;
        if (phComp != null)
        {
            MethodInfo m = phComp.GetType().GetMethod("TakeDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int) }, null);
            if (m != null)
            {
                m.Invoke(phComp, new object[] { amount });
                return;
            }
        }

        // fallback: search any component for a TakeDamage(int) method
        var comps = target.GetComponents<Component>();
        foreach (var comp in comps)
        {
            if (comp == null) continue;
            MethodInfo method = comp.GetType().GetMethod("TakeDamage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int) }, null);
            if (method != null)
            {
                method.Invoke(comp, new object[] { amount });
                return;
            }
        }
    }

    // Optional: track players standing on the platform if onlyWhenPlayerOnPlatform is enabled
    // Existing OnTriggerEnter/Exit will still work if detector is the same GameObject.
    // To support a separate detection GameObject, call the public methods below from the detector.
    private void OnTriggerEnter(Collider other)
    {
        if (!onlyWhenPlayerOnPlatform) return;
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;
        playersOnPlatform.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!onlyWhenPlayerOnPlatform) return;
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;
        playersOnPlatform.Remove(other);
    }

    // PUBLIC API for an external detection GameObject to forward trigger events to this trap.
    // Attach a small forwarding script to your detection GameObject and reference this BamboTrap.
    public void OnDetectorEnter(Collider other)
    {
        if (!onlyWhenPlayerOnPlatform) return;
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;
        playersOnPlatform.Add(other);
    }

    public void OnDetectorExit(Collider other)
    {
        if (!onlyWhenPlayerOnPlatform) return;
        if (((1 << other.gameObject.layer) & playerLayer) == 0) return;
        playersOnPlatform.Remove(other);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Vector3 worldCenter = transform.TransformPoint(damageBoxCenter);
        Vector3 size = damageBoxSize;
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(worldCenter, transform.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, size);
        Gizmos.matrix = old;
    }
}