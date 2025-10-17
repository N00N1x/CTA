using UnityEngine;
using System.Collections;

public class Windtrap : MonoBehaviour
{
    public float windForce = 10f;
    public Vector3 windDirection = Vector3.forward;
    public Animator windtrapAnimator;
    public ParticleSystem windParticles;           // Assign particle system in Inspector
    public float windDuration = 3f;
    public float windCooldown = 2f;

    // Gizmo / area settings
    public float windAreaLength = 5f;
    public float windAreaWidth = 2f;
    public float windAreaHeight = 2f;
    public Color gizmoColor = new Color(0f, 0.5f, 1f, 0.3f);

    private Coroutine windCoroutine;
    private bool isActive = false;
    private bool isCoolingDown = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isActive || isCoolingDown)
            return;

        // only start if the player is actually inside the configured wind area (gizmo)
        if (!IsInsideWindArea(other.transform.position))
            return;

        // start blowing coroutine (animator/particles will be toggled inside coroutine)
        isActive = true;
        windCoroutine = StartCoroutine(BlowPlayer(other));
    }

    private void OnTriggerExit(Collider other)
    {
        // nothing special here; coroutine checks area each frame and will stop the blow (and animation/particles) if player leaves
    }

    private IEnumerator BlowPlayer(Collider player)
    {
        // turn on blow animation and particles when we actually start blowing
        if (windtrapAnimator != null)
            windtrapAnimator.SetBool("IsActive", true);

        if (windParticles != null)
        {
            if (!windParticles.isPlaying)
                windParticles.Play();
        }

        float timer = 0f;
        Rigidbody rb = player.GetComponent<Rigidbody>();

        while (timer < windDuration)
        {
            // stop blowing immediately if player leaves the wind area
            if (!IsInsideWindArea(player.transform.position))
                break;

            if (rb != null)
                rb.AddForce(windDirection.normalized * windForce * Time.deltaTime, ForceMode.VelocityChange);

            timer += Time.deltaTime;
            yield return null;
        }

        // stop applying force and immediately stop the animation and particles
        if (windtrapAnimator != null)
            windtrapAnimator.SetBool("IsActive", false);

        if (windParticles != null)
        {
            // stop emitting but let existing particles fade naturally
            windParticles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        isActive = false;

        // start cooldown (animation/particles remain off during cooldown)
        isCoolingDown = true;
        yield return new WaitForSeconds(windCooldown);
        isCoolingDown = false;

        windCoroutine = null;
    }

    // Returns true when world position 'pos' is inside the oriented box defined by the gizmo settings.
    private bool IsInsideWindArea(Vector3 pos)
    {
        Vector3 forward = windDirection.normalized;
        if (forward.sqrMagnitude < 0.0001f)
            forward = transform.forward;

        // build orthonormal basis (forward, right, up)
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, forward);
        if (right.sqrMagnitude < 0.0001f)
        {
            // forward parallel to up, pick arbitrary right
            right = Vector3.right;
        }
        right = right.normalized;
        up = Vector3.Cross(forward, right).normalized;

        // center of the box
        Vector3 center = transform.position + forward * (windAreaLength / 2f);

        Vector3 d = pos - center;

        float halfLength = windAreaLength / 2f;
        float halfWidth = windAreaWidth / 2f;
        float halfHeight = windAreaHeight / 2f;

        float projForward = Mathf.Abs(Vector3.Dot(d, forward));
        if (projForward > halfLength) return false;

        float projRight = Mathf.Abs(Vector3.Dot(d, right));
        if (projRight > halfWidth) return false;

        float projUp = Mathf.Abs(Vector3.Dot(d, up));
        if (projUp > halfHeight) return false;

        return true;
    }

    // Draw wind area gizmo in the editor, rotated to match windDirection
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;

        Vector3 forward = windDirection.normalized;
        if (forward.sqrMagnitude < 0.0001f)
            forward = transform.forward;

        Vector3 center = transform.position + forward * (windAreaLength / 2f);
        Vector3 size = new Vector3(windAreaWidth, windAreaHeight, windAreaLength);

        // create matrix so cube is oriented along forward
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, rot, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, size);
        Gizmos.matrix = old;
    }
}
