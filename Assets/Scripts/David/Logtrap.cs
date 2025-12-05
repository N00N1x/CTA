using UnityEngine;
using System.Collections;

public class Logtrap : MonoBehaviour
{
    public enum ReleaseMode { EnablePhysics, BreakHinge }

    [Header("General")]
    public string playerTag = "Player";
    public ReleaseMode releaseMode = ReleaseMode.EnablePhysics;
    public float initialImpulse = 8f;       // optional push when released
    public float rollDuration = 2f;         // time considered "rolling"
    public float cooldown = 4f;             // time before trap can be triggered again

    [Header("Reset (only for EnablePhysics/BreakHinge)")]
    public bool resetAfterRoll = true;
    public float resetDelay = 2f;

    [Header("Damage / Knockback")]
    public int damageAmount = 0;            // set >0 if you have PlayerHealth
    public float knockbackForce = 6f;

    private Rigidbody rb;
    private HingeJoint hinge;               // optional hinge if you used one to constrain the log
    private Vector3 startPos;
    private Quaternion startRot;
    private bool isActive = false;
    private bool isCoolingDown = false;
    private bool hadInitialHinge = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        hinge = GetComponent<HingeJoint>();
        startPos = transform.position;
        startRot = transform.rotation;

        hadInitialHinge = hinge != null;

        if (rb == null)
            Debug.LogWarning("Logtrap needs a Rigidbody.");
        else
        {
            // keep the log static until triggered
            rb.isKinematic = true;
            rb.useGravity = true;
        }
    }

    // Use a trigger collider (child recommended) to call this when player enters range
    private void OnTriggerEnter(Collider other)
    {
        if (isActive || isCoolingDown) return;
        if (!other.CompareTag(playerTag)) return;

        StartCoroutine(ActivateTrap(other));
    }

    private IEnumerator ActivateTrap(Collider player)
    {
        isActive = true;

        // determine direction from log to player on XZ plane (useful for initial impulse)
        Vector3 dir = (player.transform.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
        dir = dir.normalized;

        if (releaseMode == ReleaseMode.EnablePhysics)
        {
            if (rb != null)
            {
                rb.isKinematic = false; // let gravity and slope do the rest
                if (initialImpulse > 0f)
                    rb.AddForce(dir * initialImpulse, ForceMode.Impulse);
            }
        }
        else if (releaseMode == ReleaseMode.BreakHinge)
        {
            if (hinge != null)
            {
                // disable hinge instead of destroying so we can re-enable on reset
                hinge.enabled = false;
                if (rb != null)
                {
                    rb.isKinematic = false; // ensure physics active
                    if (initialImpulse > 0f)
                        rb.AddForce(dir * initialImpulse, ForceMode.Impulse);
                }
            }
            else
            {
                // fallback to enabling physics if no hinge present
                if (rb != null)
                {
                    rb.isKinematic = false;
                    if (initialImpulse > 0f)
                        rb.AddForce(dir * initialImpulse, ForceMode.Impulse);
                }
            }
        }

        // rolling window
        float timer = 0f;
        while (timer < rollDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        // stop active state and start cooldown
        isActive = false;
        isCoolingDown = true;

        // Reset position if requested
        if (resetAfterRoll)
        {
            yield return new WaitForSeconds(resetDelay);

            if (rb != null)
            {
                // stop motion first, then make kinematic so it doesn't "fly" on reset
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // re-enable hinge if we disabled it earlier
            if (hadInitialHinge && hinge != null)
            {
                hinge.enabled = true;
            }

            transform.position = startPos;
            transform.rotation = startRot;
        }

        yield return new WaitForSeconds(cooldown);
        isCoolingDown = false;
    }

    // Optional: damage / knockback when rolling into the player
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.CompareTag(playerTag))
        {
            // only apply damage/knockback while the log is considered active/rolling
            if (!isActive) return;

            // knockback
            Rigidbody playerRb = collision.collider.GetComponent<Rigidbody>();
            if (playerRb != null && knockbackForce > 0f)
            {
                Vector3 pushDir = (collision.transform.position - transform.position).normalized;
                pushDir.y = 0.5f; // small upward lift
                playerRb.AddForce(pushDir * knockbackForce, ForceMode.Impulse);
            }

            // damage (if you have PlayerHealth script)
            if (damageAmount > 0)
            {
                // var ph = collision.collider.GetComponent<PlayerHealth>();
                // if (ph != null) ph.TakeDamage(damageAmount);
            }
        }
    }

    // Editor visualization
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward);
    }
}