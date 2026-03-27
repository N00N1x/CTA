using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class windTrap : MonoBehaviour
{
    [Header("Auto Cycle")]
    public bool autoActivate = true;
    public float repeatDelay = 2f;

    [Header("Wind Settings")]
    public float pushForce = 10f;
    public float pushDuration = 1.0f;

    public bool useLocalDirection = true;
    public Vector3 pushDirection = Vector3.forward;

    [Header("Area")]
    public Vector3 boxSize = new Vector3(2, 2, 2); // area of effect

    [Header("References")]
    public ParticleSystem windParticles;
    public Animator animator;

    bool isActive = false;

    void Start()
    {
        if (windParticles != null)
            windParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (autoActivate)
            StartCoroutine(AutoCycle());
    }

    IEnumerator AutoCycle()
    {
        while (true)
        {
            yield return StartCoroutine(ActivateRoutine());
            yield return new WaitForSeconds(repeatDelay);
        }
    }

    IEnumerator ActivateRoutine()
    {
        isActive = true;

        if (windParticles != null) windParticles.Play();
        if (animator != null) animator.SetBool("IsActive", true);

        yield return new WaitForSeconds(pushDuration);

        if (windParticles != null)
            windParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (animator != null)
            animator.SetBool("IsActive", false);

        isActive = false;
    }

    void FixedUpdate()
    {
        if (!isActive) return;

        Vector3 dir = useLocalDirection
            ? transform.forward
            : transform.TransformDirection(pushDirection.normalized);

        Collider[] hits = Physics.OverlapBox(transform.position, boxSize / 2);

        foreach (var col in hits)
        {
            Rigidbody rb = col.attachedRigidbody ?? col.GetComponentInParent<Rigidbody>();
            if (rb == null) continue;

            rb.AddForce(dir * pushForce, ForceMode.Acceleration);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        // draw area
        Gizmos.DrawWireCube(transform.position, boxSize);

        // draw direction
        Vector3 dir = useLocalDirection
            ? transform.forward
            : transform.TransformDirection(pushDirection.normalized);

        Gizmos.DrawLine(transform.position, transform.position + dir * 2f);
    }
}