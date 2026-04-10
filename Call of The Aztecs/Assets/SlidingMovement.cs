using UnityEngine;


public class SlidingMovement : MonoBehaviour
{
    [Header("Sliding Settings")]
    [Tooltip("Horizontal push applied to force the player away from steep surfaces.")]
    public float slideForce = 6f;

    [Tooltip("Minimum Y component of a contact normal considered as 'ground'.")]
    public float groundNormalYThreshold = 0.65f;

    [Tooltip("Maximum horizontal speed allowed after sliding push (prevents explosive velocities).")]
    public float maxHorizontalSpeed = 10f;

    [Tooltip("Layers to consider for sliding (useful to ignore triggers or non-world objects).")]
    public LayerMask slideLayerMask = ~0; // default: everything

    [Tooltip("Enable debug logs/gizmos.")]
    public bool debugMode = false;

    Rigidbody rb;

    // Accumulators used per physics step
    Vector3 accumulatedWallHorizontalNormal;
    bool hasGroundContact;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("SlidingMovement requires a Rigidbody on the same GameObject.", this);
            enabled = false;
            return;
        }

        // Prevent unexpected rotation induced by pushes
        rb.freezeRotation = true;
    }

    void OnCollisionStay(Collision collision)
    {
        // Only consider collisions with selected layers
        if (((1 << collision.gameObject.layer) & slideLayerMask) == 0)
            return;

        accumulatedWallHorizontalNormal = Vector3.zero;
        hasGroundContact = false;

        // Analyze contacts for this collision
        foreach (ContactPoint cp in collision.contacts)
        {
            Vector3 n = cp.normal;

            // If contact normal is mostly up, consider it ground
            if (n.y >= groundNormalYThreshold)
            {
                hasGroundContact = true;
            }
            else
            {
                // Treat this as a wall/steep contact: accumulate its horizontal normal
                Vector3 horizontal = new Vector3(n.x, 0f, n.z);
                if (horizontal.sqrMagnitude > 0.0001f)
                    accumulatedWallHorizontalNormal += horizontal.normalized;
            }
        }

        // If we are grounded and have one or more steep contacts, push away horizontally
        if (hasGroundContact && accumulatedWallHorizontalNormal.sqrMagnitude > 0.0001f)
        {
            Vector3 pushDir = accumulatedWallHorizontalNormal.normalized;

            // Apply push away from the wall (pushDir points away from surface)
            Vector3 vel = rb.linearVelocity;

            // Only affect horizontal components
            Vector3 horizVel = new Vector3(vel.x, 0f, vel.z);

            // Apply a small impulse-like change per physics step
            Vector3 added = pushDir * slideForce * Time.fixedDeltaTime;
            horizVel += added;

            // Clamp horizontal speed to avoid runaway velocity
            float speed = horizVel.magnitude;
            if (speed > maxHorizontalSpeed)
                horizVel = horizVel.normalized * maxHorizontalSpeed;

            // Recompose final velocity (preserve vertical velocity)
            rb.linearVelocity = new Vector3(horizVel.x, vel.y, horizVel.z);

            if (debugMode)
            {
                Debug.DrawRay(transform.position, pushDir * 1.2f, Color.cyan, 0.1f);
                Debug.Log($"SlidingMovement: applied push {added} -> horizSpeed {horizVel.magnitude:F2}", this);
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        // Reset accumulators so old contact normals don't persist between collisions
        accumulatedWallHorizontalNormal = Vector3.zero;
        hasGroundContact = false;
    }

    // Optional: show a small gizmo when debugging
    void OnDrawGizmosSelected()
    {
        if (!debugMode || !Application.isPlaying) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.25f);
    }
}
