using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class WindTrap : MonoBehaviour
{
    [Header("Activation")]
    public bool autoActivate = true;
    public bool autoRepeat = true;

    [Min(0f)]
    public float repeatDelay = 2f;

    [Header("Wind Settings")]
    [Min(0f)]
    public float pushForce = 25f;

    [Min(0f)]
    public float pushDuration = 5f;

    public bool useLocalDirection = true;
    public Vector3 pushDirection = Vector3.forward;

    [Header("Cooldown")]
    [Min(0f)]
    public float cooldown = 0f;

    public bool useCooldown = false;

    [Header("Detection")]
    public string playerTag = "Player";

    [Header("References")]
    public ParticleSystem windParticles;
    public Animator animator;

    [Header("Debug")]
    public bool debugMode = false;

    private bool isActive = false;
    private bool isCoolingDown = false;

    private Coroutine activationCoroutine;
    private Coroutine cooldownCoroutine;
    private Coroutine repeatCoroutine;

    private readonly HashSet<Collider> playersInside =
        new HashSet<Collider>();

    private void Awake()
    {
        // Ensure collider is trigger
        BoxCollider col = GetComponent<BoxCollider>();
        col.isTrigger = true;
    }

    private void Start()
    {
        if (windParticles != null)
        {
            windParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        if (autoActivate)
        {
            ActivateTrap(0f);
        }
    }

    public void ActivateTrap(float delay)
    {
        if (isCoolingDown || isActive)
            return;

        if (activationCoroutine != null)
            StopCoroutine(activationCoroutine);

        activationCoroutine =
            StartCoroutine(ActivationSequence(delay));
    }

    public void ActivateImmediate()
    {
        ActivateTrap(0f);
    }

    private IEnumerator ActivationSequence(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        isActive = true;

        if (windParticles != null)
            windParticles.Play();

        if (animator != null)
            animator.SetBool("IsActive", true);

        if (debugMode)
            Debug.Log("[WindTrap] Activated");

        yield return new WaitForSeconds(pushDuration);

        StopTrap();

        if (useCooldown)
        {
            cooldownCoroutine =
                StartCoroutine(CooldownRoutine());
        }

        if (autoRepeat)
        {
            repeatCoroutine =
                StartCoroutine(AutoRepeatRoutine());
        }

        activationCoroutine = null;
    }

    private void StopTrap()
    {
        isActive = false;

        if (windParticles != null)
        {
            windParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmitting
            );
        }

        if (animator != null)
            animator.SetBool("IsActive", false);

        if (debugMode)
            Debug.Log("[WindTrap] Stopped");
    }

    private IEnumerator CooldownRoutine()
    {
        isCoolingDown = true;

        yield return new WaitForSeconds(cooldown);

        isCoolingDown = false;
        cooldownCoroutine = null;
    }

    private IEnumerator AutoRepeatRoutine()
    {
        if (useCooldown)
            yield return new WaitForSeconds(cooldown);

        yield return new WaitForSeconds(repeatDelay);

        ActivateTrap(0f);

        repeatCoroutine = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playersInside.Add(other);

        if (debugMode)
            Debug.Log("[WindTrap] Player entered");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playersInside.Remove(other);

        if (debugMode)
            Debug.Log("[WindTrap] Player exited");
    }

    private void FixedUpdate()
    {
        if (!isActive || playersInside.Count == 0)
            return;

        Vector3 dir =
            useLocalDirection
            ? transform.forward
            : transform.TransformDirection(pushDirection.normalized);

        foreach (Collider col in playersInside.ToArray())
        {
            if (col == null)
                continue;

            Rigidbody rb =
                col.attachedRigidbody ??
                col.GetComponentInParent<Rigidbody>();

            if (rb == null)
                continue;

            rb.AddForce(
                dir.normalized * pushForce,
                ForceMode.Acceleration
            );

            if (debugMode)
            {
                Debug.Log(
                    $"[WindTrap] Pushing {col.name}"
                );
            }
        }
    }

    private void OnDisable()
    {
        if (activationCoroutine != null)
            StopCoroutine(activationCoroutine);

        if (cooldownCoroutine != null)
            StopCoroutine(cooldownCoroutine);

        if (repeatCoroutine != null)
            StopCoroutine(repeatCoroutine);

        playersInside.Clear();

        isActive = false;
        isCoolingDown = false;

        if (windParticles != null)
        {
            windParticles.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }

        if (animator != null)
            animator.SetBool("IsActive", false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 dir =
            useLocalDirection
            ? transform.forward
            : transform.TransformDirection(pushDirection.normalized);

        dir.Normalize();

        Vector3 start = transform.position;
        Vector3 end = start + dir * 3f;

        // Main line
        Gizmos.DrawLine(start, end);

        // Arrow head
        Vector3 right =
            Quaternion.LookRotation(dir) *
            Quaternion.Euler(0, 150, 0) *
            Vector3.forward;

        Vector3 left =
            Quaternion.LookRotation(dir) *
            Quaternion.Euler(0, -150, 0) *
            Vector3.forward;

        Gizmos.DrawLine(end, end + right * 0.5f);
        Gizmos.DrawLine(end, end + left * 0.5f);

        // Draw trigger area
        BoxCollider box = GetComponent<BoxCollider>();

        if (box != null)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);

            Matrix4x4 oldMatrix = Gizmos.matrix;

            Gizmos.matrix =
                transform.localToWorldMatrix;

            Gizmos.DrawWireCube(
                box.center,
                box.size
            );

            Gizmos.matrix = oldMatrix;
        }
    }
}