using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BamboTrap : MonoBehaviour
{
    [Header("Cycle")]
    public bool autoCycle = true;
    public bool startOnAwake = true;
    public float idleDuration = 2f;      // time between activations
    public float activeDuration = 1f;    // time trap is "dangerous"

    [Header("Detection (optional)")]
    [Tooltip("If true, the trap will only start cycling while a player stands on the platform (requires a trigger collider on the platform).")]
    public bool onlyWhenPlayerOnPlatform = false;

    [Header("Animator / VFX")]
    public Animator animator;            // Animator with a bool parameter named animatorBoolParameter
    public string animatorBoolParameter = "IsActive";
    public ParticleSystem particles;

    [Header("Damage / knockback")]
    public LayerMask playerLayer = 1 << 8; // default to layer 8 (set in Inspector)
    public int damageAmount = 1;
    public float knockbackForce = 6f;
    public Vector3 damageBoxCenter = new Vector3(0f, 0.5f, 0.5f); // local offset
    public Vector3 damageBoxSize = new Vector3(1f, 1f, 1f);      // world-aligned box extents

    [Header("Gizmo")]
    public Color gizmoColor = new Color(1f, 0.5f, 0f, 0.25f);

    // runtime
    private bool isActive = false;
    private Coroutine cycleCoroutine;
    private readonly HashSet<Transform> damagedThisActivation = new HashSet<Transform>();
    private readonly HashSet<Collider> playersOnPlatform = new HashSet<Collider>();

    private void Awake()
    {
        // ensure particle system does not play on awake
        if (particles != null)
        {
            var main = particles.main;
            main.loop = false;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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
            // wait until platform has player if required
            if (onlyWhenPlayerOnPlatform)
            {
                while (playersOnPlatform.Count == 0)
                    yield return null;
            }

            yield return new WaitForSeconds(idleDuration);

            // same check before activation
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
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    private void FixedUpdate()
    {
        if (!isActive) return;

        // overlap box in world space
        Vector3 worldCenter = transform.TransformPoint(damageBoxCenter);
        Vector3 halfExtents = damageBoxSize * 0.5f;
        Collider[] cols = Physics.OverlapBox(worldCenter, halfExtents, transform.rotation, playerLayer, QueryTriggerInteraction.Collide);

        foreach (var c in cols)
        {
            if (c == null || c.transform == null) continue;

            if (damagedThisActivation.Contains(c.transform)) continue;

            // apply knockback
            Rigidbody rb = c.attachedRigidbody ?? c.GetComponent<Rigidbody>();
            if (rb != null && knockbackForce > 0f)
            {
                Vector3 dir = (c.transform.position - worldCenter).normalized;
                dir.y = Mathf.Max(dir.y, 0.2f); // give slight upward lift
                rb.AddForce(dir * knockbackForce, ForceMode.Impulse);
            }

            // apply damage if target has a PlayerHealth (optional)
         //   var ph = c.GetComponent<PlayerHealth>();
          //  if (ph != null && damageAmount > 0)
          //      ph.TakeDamage(damageAmount);

            damagedThisActivation.Add(c.transform);
        }
    }

    // Optional: track players standing on the platform if onlyWhenPlayerOnPlatform is enabled
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