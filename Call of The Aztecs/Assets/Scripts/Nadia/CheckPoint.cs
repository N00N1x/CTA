using NUnit.Framework;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.SetCheckpoint(transform.position);
        }
        else
        {
            Debug.LogWarning("CheckpointManager.Instance is null — make sure a CheckpointManager is present in the scene.", this);
            var found = FindFirstObjectByType<CheckpointManager>();
            if (found != null)
            {
                found.SetCheckpoint(transform.position);
                Debug.Log("Found CheckpointManager via FindFirstObjectByType and set checkpoint.");
            }
        }
    }
}

