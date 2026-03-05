using System.Collections.Generic;
using UnityEngine;

public class LogSpawner : MonoBehaviour
{
    [Header("Pool")]
    public GameObject logPrefab;        // Your log prefab (must contain `LogTrap` component)
    public int poolSize = 5;            // Number of logs to keep in pool
    public float spawnInterval = 2f;    // Time between spawns

    [Header("Spawn Control")]
    public bool useSpawnPoint = false;          // if true, use `spawnPoint` as base transform
    public Transform spawnPoint;                // optional external spawn transform
    public Vector3 spawnOffset = Vector3.zero;  // local offset from chosen base (local space)
    public bool randomizeOffset = false;
    public Vector3 randomOffsetRange = Vector3.zero; // range for random offset per axis (local space)
    public bool alignToSurface = false;         // raycast down to surface to place log on ground
    public LayerMask groundMask = ~0;
    public float alignRaycastDistance = 5f;

    [Header("Rotation")]
    public bool overrideRotation = false;       // if true, use `rotationOffset` instead of base rotation
    public Vector3 rotationOffset = Vector3.zero; // euler offset applied after base rotation

    [Header("Gizmo")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.yellow;
    public float gizmoRadius = 0.25f;
    public float forwardArrowLength = 1f;

    [Header("Debug")]
    public bool debugMode = false;

    private List<LogTrap> logPool = new List<LogTrap>();
    private int currentIndex = 0;
    private Vector3 layDownRotation;

    void Start()
    {
        if (logPrefab == null)
        {
            Debug.LogError("[LogSpawner] logPrefab is not assigned.");
            return;
        }

        // Pre-instantiate log pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject logObj = Instantiate(logPrefab, transform.position, transform.rotation);
            logObj.SetActive(false);
            var lt = logObj.GetComponent<LogTrap>();
            if (lt == null)
                Debug.LogWarning("[LogSpawner] Instantiated prefab has no LogTrap component attached.");

            logPool.Add(lt);
        }

        // Start spawning repeatedly
        InvokeRepeating(nameof(SpawnLog), 0f, spawnInterval);
    }

    void SpawnLog()
    {
        if (logPool.Count == 0)
        {
            if (debugMode) Debug.LogWarning("[LogSpawner] Pool empty, aborting spawn.");
            return;
        }

        LogTrap log = logPool[currentIndex];
        if (log == null)
        {
            // fallback: attempt to instantiate replacement
            var go = Instantiate(logPrefab, transform.position, transform.rotation);
            log = go.GetComponent<LogTrap>();
            logPool[currentIndex] = log;
        }

        // compute final spawn position in world space
        Vector3 localOffset = spawnOffset;
        if (randomizeOffset)
        {
            localOffset += new Vector3(
                Random.Range(-randomOffsetRange.x, randomOffsetRange.x),
                Random.Range(-randomOffsetRange.y, randomOffsetRange.y),
                Random.Range(-randomOffsetRange.z, randomOffsetRange.z)
            );
        }

        Transform baseT = (useSpawnPoint && spawnPoint != null) ? spawnPoint : transform;
        Vector3 worldPos = baseT.TransformPoint(localOffset);

        if (alignToSurface)
        {
            // cast down from above the candidate position
            Vector3 rayOrigin = worldPos + Vector3.up * alignRaycastDistance;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, alignRaycastDistance * 2f, groundMask, QueryTriggerInteraction.Ignore))
            {
                worldPos = hit.point;
            }
            else if (debugMode)
            {
                Debug.Log("[LogSpawner] Align raycast did not hit any surface.");
            }
        }

        // compute final rotation
        Quaternion finalRotation;
        if (overrideRotation)
            finalRotation = Quaternion.Euler(rotationOffset);
        else
        {
            finalRotation = baseT.rotation * Quaternion.Euler(rotationOffset);
        }

        // place and activate
        log.transform.SetPositionAndRotation(worldPos, finalRotation);
        log.gameObject.SetActive(true);

        if (debugMode)
            Debug.Log($"[LogSpawner] Spawned log at {worldPos} rot={finalRotation.eulerAngles}");

        currentIndex = (currentIndex + 1) % poolSize;
    }

    // Allow runtime control of spawn location from other scripts
    public void SetSpawnPoint(Transform t)
    {
        spawnPoint = t;
        useSpawnPoint = t != null;
    }

    public void SetSpawnOffset(Vector3 offset)
    {
        spawnOffset = offset;
    }

    public void SetRandomOffsetRange(Vector3 range)
    {
        randomOffsetRange = range;
    }

    // Draw spawn gizmo in the Scene view so devs can see where logs will appear and their forward direction.
    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color = gizmoColor;

        Transform baseT = (useSpawnPoint && spawnPoint != null) ? spawnPoint : transform;
        Vector3 previewOffset = spawnOffset;
        if (randomizeOffset)
        {
            // show center point of random range
            previewOffset += Vector3.Scale(randomOffsetRange, Vector3.zero);
        }

        Vector3 center = baseT.TransformPoint(previewOffset);
        Gizmos.DrawWireSphere(center, gizmoRadius);

        // forward line (based on chosen base transform)
        Vector3 forwardEnd = center + baseT.forward * forwardArrowLength;
        Gizmos.DrawLine(center, forwardEnd);

        // arrowhead
        Vector3 tip = forwardEnd;
        float headSize = forwardArrowLength * 0.25f;
        Vector3 rightHead = Quaternion.Euler(0f, 160f, 0f) * (-baseT.forward) * headSize;
        Vector3 leftHead = Quaternion.Euler(0f, -160f, 0f) * (-baseT.forward) * headSize;
        Gizmos.DrawLine(tip, tip + rightHead);
        Gizmos.DrawLine(tip, tip + leftHead);
    }
}

