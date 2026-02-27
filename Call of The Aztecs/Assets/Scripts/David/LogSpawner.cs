using System.Collections.Generic;
using UnityEngine;

public class LogSpawner : MonoBehaviour
{
    public GameObject logPrefab;        // Your log prefab
    public int poolSize = 5;            // Number of logs to keep in pool
    public float spawnInterval = 2f;    // Time between spawns

    [Header("Gizmo")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.yellow;
    public float gizmoRadius = 0.25f;
    public float forwardArrowLength = 1f;

    private List<LogTrap> logPool = new List<LogTrap>();
    private int currentIndex = 0;

    void Start()
    {
        // Pre-instantiate log pool
        for (int i = 0; i < poolSize; i++)
        {
            GameObject logObj = Instantiate(logPrefab, transform.position, transform.rotation);
            logObj.SetActive(false);
            logPool.Add(logObj.GetComponent<LogTrap>());
        }

        // Start spawning repeatedly
        InvokeRepeating(nameof(SpawnLog), 0f, spawnInterval);
    }

    void SpawnLog()
    {
        LogTrap log = logPool[currentIndex];

        // Reset position and rotation
        log.transform.position = transform.position;
        log.transform.rotation = transform.rotation;

        log.gameObject.SetActive(true);

        // Move to next in pool
        currentIndex = (currentIndex + 1) % poolSize;
    }
}

