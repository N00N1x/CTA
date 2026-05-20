using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ForcePush : MonoBehaviour
{
    [Header("Force Settings")]
    public float pushForce = 25f;

    [Tooltip("How long the wind stays ON")]
    public float activeTime = 3f;

    [Tooltip("How long the wind stays OFF")]
    public float inactiveTime = 5f;

    [Tooltip("Use this object's forward direction")]
    public bool useLocalForward = true;

    public Vector3 pushDirection = Vector3.forward;

    [Header("Detection")]
    public string playerTag = "Player";

    [Header("References")]
    public ParticleSystem windParticles;

    [Header("Debug")]
    public bool debugMode = false;

    private bool isActive = true;
    private Collider triggerCol;

    private void Start()
    {
        triggerCol = GetComponent<Collider>();
        triggerCol.isTrigger = true;

        StartCoroutine(WindCycle());
    }

    IEnumerator WindCycle()
    {
        while (true)
        {
            // ===== WIND ON =====
            isActive = true;

            if (windParticles != null)
                windParticles.Play();

            if (debugMode)
                Debug.Log("[ForcePush] WIND ON");

            yield return new WaitForSeconds(activeTime);

            // ===== WIND OFF =====
            isActive = false;

            if (windParticles != null)
                windParticles.Stop();

            if (debugMode)
                Debug.Log("[ForcePush] WIND OFF");

            // During OFF state:
            // player can walk through freely
            yield return new WaitForSeconds(inactiveTime);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Do nothing if wind is OFF
        if (!isActive)
            return;

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

        rb.AddForce(
            dir.normalized * pushForce,
            ForceMode.Acceleration
        );

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

        // Draw trigger area
        Collider col = GetComponent<Collider>();

        if (col is BoxCollider box)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);

            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.DrawWireCube(box.center, box.size);

            Gizmos.matrix = oldMatrix;
        }
    }
}