using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ForcePush : MonoBehaviour
{
    [Header("Force Settings")]
    public float pushForce = 25f;

    [Tooltip("Use this object's forward direction")]
    public bool useLocalForward = true;

    public Vector3 pushDirection = Vector3.forward;

    [Header("Detection")]
    public string playerTag = "Player";

    [Header("References")]
    public ParticleSystem windParticles;

    [Header("Debug")]
    public bool debugMode = false;

    private void Start()
    {
        // Make sure trigger is enabled
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        // Play particles automatically
        if (windParticles != null)
            windParticles.Play();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        Rigidbody rb =
            other.attachedRigidbody ??
            other.GetComponentInParent<Rigidbody>();

        if (rb == null)
            return;

        Vector3 dir = useLocalForward
            ? transform.forward
            : transform.TransformDirection(pushDirection.normalized);

        rb.AddForce(dir.normalized * pushForce, ForceMode.Acceleration);

        if (debugMode)
        {
            Debug.Log($"[ForcePush] pushing {other.name}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 dir = useLocalForward
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
    }
}