using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindTrap : MonoBehaviour
{
    [Header("Activation")]
    public bool autoActivate = false;
    public bool autoRepeat = false;
    [Min(0f)] public float repeatDelay = 2f;

    [Header("Wind Settings")]
    [Min(0f)] public float pushForce = 10f;
    [Min(0f)] public float pushDuration = 10.0f;

    public bool useLocalDirection = true;
    public Vector3 pushDirection = Vector3.forward;

    [Header("Cooldown")]
    [Min(0f)] public float cooldown = 3.0f;
    public bool useCooldown = true;

    [Header("References")]
    public ParticleSystem windParticles;
    public Animator animator;

    [Header("Debug")]
    public bool debugMode = false;

    bool isActive = false;
    bool isCoolingDown = false;

    Coroutine activationCoroutine;
    Coroutine cooldownCoroutine;
    Coroutine repeatCoroutine;

    readonly HashSet<Collider> playersInside = new HashSet<Collider>();

    void OnValidate()
    {
        pushForce = Mathf.Max(0f, pushForce);
        pushDuration = Mathf.Max(0f, pushDuration);
        cooldown = Mathf.Max(0f, cooldown);
        repeatDelay = Mathf.Max(0f, repeatDelay);

        if (pushDirection == Vector3.zero)
            pushDirection = Vector3.forward;
    }

    void Start()
    {
        if (windParticles != null)
            windParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (autoActivate)
            ActivateTrap(0f);
    }

    public void ActivateTrap(float delay)
    {
        if (isCoolingDown || isActive)
            return;

        if (activationCoroutine != null)
            StopCoroutine(activationCoroutine);

        activationCoroutine = StartCoroutine(ActivationSequence(delay));
    }

    public void ActivateImmediate() => ActivateTrap(0f);

    IEnumerator ActivationSequence(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        isActive = true;

        if (windParticles != null) windParticles.Play();
        if (animator != null) animator.SetBool("IsActive", true);

        yield return new WaitForSeconds(pushDuration);

        StopTrap();

        if (useCooldown)
            cooldownCoroutine = StartCoroutine(CooldownRoutine());

        if (autoRepeat)
            repeatCoroutine = StartCoroutine(AutoRepeatRoutine());

        activationCoroutine = null;
    }

    void StopTrap()
    {
        if (windParticles != null)
            windParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (animator != null)
            animator.SetBool("IsActive", false);

        isActive = false;
    }

    IEnumerator CooldownRoutine()
    {
        isCoolingDown = true;
        yield return new WaitForSeconds(cooldown);
        isCoolingDown = false;
        cooldownCoroutine = null;
    }

    IEnumerator AutoRepeatRoutine()
    {
        yield return new WaitForSeconds(repeatDelay);

        if (!isCoolingDown && !isActive)
            ActivateTrap(0f);

        repeatCoroutine = null;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playersInside.Add(other);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playersInside.Remove(other);
    }

    void FixedUpdate()
    {
        if (!isActive || playersInside.Count == 0)
            return;

        Vector3 dir = useLocalDirection
            ? transform.forward
            : transform.TransformDirection(pushDirection.normalized);

        foreach (var col in playersInside)
        {
            if (!col) continue;

            Rigidbody rb = col.attachedRigidbody ?? col.GetComponentInParent<Rigidbody>();
            if (!rb) continue;

            rb.AddForce(dir * pushForce, ForceMode.Acceleration);
        }
    }

    void OnDisable()
    {
        if (activationCoroutine != null) StopCoroutine(activationCoroutine);
        if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
        if (repeatCoroutine != null) StopCoroutine(repeatCoroutine);

        isActive = false;
        isCoolingDown = false;

        if (windParticles != null)
            windParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        playersInside.Clear();

        if (animator != null)
            animator.SetBool("IsActive", false);
    }
}