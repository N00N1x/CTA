using UnityEngine;

public class LogSpawner : MonoBehaviour
{
    public GameObject logPrefab;
    public float spawnInterval = 3f;

    [Header("Gizmo")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.yellow;
    public float gizmoRadius = 0.25f;
    public float forwardArrowLength = 1f;

    private void Start()
    {
        InvokeRepeating(nameof(SpawnLog), 0f, spawnInterval);
    }

    void SpawnLog()
    {
        LogTrap log = FindInactiveLog();
        if (log != null)
        {
            log.transform.position = transform.position;
            log.transform.rotation = transform.rotation;
            log.gameObject.SetActive(true);
        }
        else
        {
            Instantiate(logPrefab, transform.position, transform.rotation);
        }
    }

    LogTrap FindInactiveLog()
    {
        LogTrap[] logs = Object.FindObjectsByType<LogTrap>(FindObjectsSortMode.None);
        foreach (var log in logs)
        {
            if (!log.gameObject.activeInHierarchy)
                return log;
        }
        return null;
    }

    // Draw spawn gizmo in the Scene view so devs can see where logs will appear and their forward direction.
    private void OnDrawGizmos()
    {
        if (!showGizmo)
            return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);

        // forward line
        Vector3 forwardEnd = transform.position + transform.forward * forwardArrowLength;
        Gizmos.DrawLine(transform.position, forwardEnd);

        // simple arrowhead at the tip
        Vector3 tip = forwardEnd;
        float headSize = forwardArrowLength * 0.25f;
        Vector3 rightHead = Quaternion.Euler(0f, 160f, 0f) * (-transform.forward) * headSize;
        Vector3 leftHead = Quaternion.Euler(0f, -160f, 0f) * (-transform.forward) * headSize;
        Gizmos.DrawLine(tip, tip + rightHead);
        Gizmos.DrawLine(tip, tip + leftHead);
    }
}
