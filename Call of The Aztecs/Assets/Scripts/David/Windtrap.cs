using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Windtrap : MonoBehaviour
{
    [Header("Wind settings")]
    public float windForce = 10f;
    public Vector3 windDirection = Vector3.forward;
    public float windDuration = 3f;      // how long wind is ON
    public float windCooldown = 2f;      // how long wind is OFF (between blows)

    [Header("Auto cycle")]
    public bool autoCycle = true;        // if true the trap will toggle on/off automatically
    public float startDelay = 0f;        // delay before first cycle starts

    [Header("Visuals")]
    public Animator windtrapAnimator;    // Animator parameter 'IsActive' expected
    public ParticleSystem windParticles; // particle system for wind VFX

    [Header("Gizmo / area (oriented box)")]
    public float windAreaLength = 5f;    // forward direction length
    public float windAreaWidth = 2f;
    public float windAreaHeight = 2f;
    public Color gizmoColor = new Color(0f, 0.5f, 1f, 0.25f);

    // runtime
    private bool isActive = true;
    private bool isCoolingDown = false;
    private Coroutine cycleCoroutine;
    private readonly HashSet<Collider> trackedColliders = new HashSet<Collider>();

    private void Awake()
    {
        // ensure particle doesn't play by itself
        if (windParticles != null)
        {
            var main = windParticles.main;
            main.loop = false;
            windParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void Start()
    {
        if (autoCycle)
            cycleCoroutine = StartCoroutine(WindCycleRoutine());
    }

    private void OnDisable()
    {
        StopCycle();
        SetVisuals(false);
    }

    private void OnDestroy()
    {
        StopCycle();
    }

    // Optional: add a child trigger collider and tag player with "Player".
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            trackedColliders.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            trackedColliders.Remove(other);
    }

    // Apply wind force every physics step while active
    private void FixedUpdate()
    {
        if (!isActive) return;

        Vector3 dir = windDirection;
        if (dir.sqrMagnitude < 0.0001f)
            dir = transform.forward;
        dir = dir.normalized;

        // iterate over a snapshot to avoid collection-modification issues
        var snapshot = new Collider[trackedColliders.Count];
        trackedColliders.CopyTo(snapshot);
        foreach (var col in snapshot)
        {
            if (col == null) continue;

            // ensure the player is within the oriented wind area (matches gizmo)
            if (!IsInsideWindArea(col.transform.position))
                continue;

            Rigidbody rb = col.attachedRigidbody ?? col.GetComponent<Rigidbody>();
            if (rb == null) continue;

            rb.AddForce(dir * windForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
    }

    // Public control
    public void StartBlowingOnce()
    {
        if (cycleCoroutine != null)
        {
            StopCoroutine(cycleCoroutine);
            cycleCoroutine = null;
        }
        StartCoroutine(BlowOnceCoroutine());
    }

    public void StopCycle()
    {
        if (cycleCoroutine != null)
        {
            StopCoroutine(cycleCoroutine);
            cycleCoroutine = null;
        }
        isActive = false;
        isCoolingDown = false;
        SetVisuals(false);
    }

    private IEnumerator WindCycleRoutine()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        while (true)
        {
            Debug.Log($"Windtrap: cycle ON at {Time.time}");
            yield return BlowOnceCoroutine();
            Debug.Log($"Windtrap: cycle OFF (cooldown) at {Time.time}");

            // OFF phase (cooldown)
            isCoolingDown = true;
            float timer = 0f;
            while (timer < windCooldown)
            {
                timer += Time.deltaTime;
                yield return null;
            }
            isCoolingDown = false;
        }
    }

    private IEnumerator BlowOnceCoroutine()
    {
        isActive = true;
        SetVisuals(true);

        float timer = 0f;
        while (timer < windDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        isActive = false;
        SetVisuals(false);
    }

    private void SetVisuals(bool on)
    {
        if (windtrapAnimator != null)
            windtrapAnimator.SetBool("IsActive", on);

        if (windParticles != null)
        {
            if (on)
            {
                if (!windParticles.isPlaying) windParticles.Play();
            }
            else
            {
                windParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    private bool IsInsideWindArea(Vector3 pos)
    {
        Vector3 forward = windDirection.normalized;
        if (forward.sqrMagnitude < 0.0001f)
            forward = transform.forward;

        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, forward);
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.right;
        right = right.normalized;
        up = Vector3.Cross(forward, right).normalized;

        Vector3 center = transform.position + forward * (windAreaLength / 2f);
        Vector3 d = pos - center;

        float halfLength = windAreaLength / 2f;
        float halfWidth = windAreaWidth / 2f;
        float halfHeight = windAreaHeight / 2f;

        if (Mathf.Abs(Vector3.Dot(d, forward)) > halfLength) return false;
        if (Mathf.Abs(Vector3.Dot(d, right))   > halfWidth)  return false;
        if (Mathf.Abs(Vector3.Dot(d, up))      > halfHeight) return false;
        return true;
    }

    // Draw oriented gizmo
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        Vector3 forward = windDirection.normalized;
        if (forward.sqrMagnitude < 0.0001f)
            forward = transform.forward;

        Vector3 center = transform.position + forward * (windAreaLength / 2f);
        Vector3 size = new Vector3(windAreaWidth, windAreaHeight, windAreaLength);

        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, rot, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, size);
        Gizmos.matrix = old;
    }
}
