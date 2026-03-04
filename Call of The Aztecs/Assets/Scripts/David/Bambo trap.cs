using System.Collections;
using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public string triggerName = "Activate";

    [Header("Cycle")]
    public bool autoCycle = true;
    public float repeatInterval = 2f;     // time between activations
    public float activeDuration = 0.6f;   // how long spikes are dangerous
    public float damageDelay = 0.1f;      // delay before applying damage after animation start

    [Header("Damage")]
    public int damageAmount = 100;
    public LayerMask playerLayer = 1 << 6; // set to your player layer in Inspector

    [Header("Damage area (local)")]
    public Vector3 damageBoxCenter = new Vector3(0f, 0.5f, 0.5f);
    public Vector3 damageBoxSize = new Vector3(1f, 1f, 1f);

    // runtime
    private bool isBusy;

    private void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void Start()
    {
        if (autoCycle)
            StartCoroutine(AutoCycleRoutine());
    }

    // public API to trigger the trap once (e.g. from a pressure plate)
    public void TriggerOnce()
    {
        if (!isBusy)
            StartCoroutine(ActivateRoutine(null));
    }

    private IEnumerator AutoCycleRoutine()
    {
        while (true)
        {
            if (!isBusy)
                StartCoroutine(ActivateRoutine(null));

            yield return new WaitForSeconds(repeatInterval);
        }
    }

    // If the trigger is a plate, this will start the activation and also pass the player reference for immediate damage
    private void OnTriggerEnter(Collider other)
    {
        if (!other) return;
        if (!other.CompareTag("Player")) return;

        // If the trap is busy, ignore additional enters
        if (isBusy) return;

        // start activation and pass in the player that stepped on it
        StartCoroutine(ActivateRoutine(other.gameObject));
    }

    private IEnumerator ActivateRoutine(GameObject triggeringPlayer)
    {
        isBusy = true;

        // play animation if assigned
        if (animator != null && !string.IsNullOrEmpty(triggerName))
            animator.SetTrigger(triggerName);

        // wait a short delay so spikes have time to appear (tune damageDelay to match animation)
        if (damageDelay > 0f)
            yield return new WaitForSeconds(damageDelay);

        // apply damage once to any player inside the damage box
        ApplyDamageInBox();

        // if a specific triggering player was provided, also attempt to damage via direct reference (redundant but immediate)
        if (triggeringPlayer != null)
        {
            var ph = triggeringPlayer.GetComponent<playerHealth>();
            if (ph != null)
                ph.TakeDamage((float)damageAmount);
            else
                triggeringPlayer.SendMessage("TakeDamage", damageAmount, SendMessageOptions.DontRequireReceiver);
        }

        // remain active for the visual duration
        yield return new WaitForSeconds(activeDuration);

        // cooldown finished
        isBusy = false;
    }

    private void ApplyDamageInBox()
    {
        Vector3 worldCenter = transform.TransformPoint(damageBoxCenter);
        Vector3 halfExtents = damageBoxSize * 0.5f;
        Collider[] hits = Physics.OverlapBox(worldCenter, halfExtents, transform.rotation, playerLayer, QueryTriggerInteraction.Collide);

        foreach (var c in hits)
        {
            if (c == null) continue;
            var ph = c.GetComponent<playerHealth>();
            if (ph != null)
                ph.TakeDamage((float)damageAmount);
            else
                c.gameObject.SendMessage("TakeDamage", damageAmount, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 worldCenter = transform.TransformPoint(damageBoxCenter);
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(worldCenter, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, damageBoxSize);
        Gizmos.matrix = old;
    }
}