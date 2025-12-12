using UnityEngine;

public class Killplane : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        TryKillPlayer(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryKillPlayer(collision.gameObject);
    }

    private void TryKillPlayer(GameObject obj)
    {
        if (obj == null) return;

        var playerHealth = obj.GetComponent<playerHealth>() ?? obj.GetComponentInParent<playerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(float.MaxValue);
            return;
        }

        if (obj.CompareTag("Player"))
        {
            if (CheckpointManager.Instance != null)
            {
                CheckpointManager.Instance.RespawnPlayer(obj);
            }
            else
            {
                Debug.LogWarning("CheckpointManager not found in scene.");
            }
        }
    }
}
