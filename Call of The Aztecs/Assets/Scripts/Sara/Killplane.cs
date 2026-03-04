using UnityEngine;

public class Killplane : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("enterrd: " + other.gameObject.name + " | tag: " + other.tag);
        TryKillPlayer(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryKillPlayer(collision.gameObject);
    }

    private void TryKillPlayer(GameObject obj)
    {
        if (obj == null) return;
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

            return;
        }

        var entityHealth = obj.GetComponent<playerHealth>() ?? obj.GetComponentInParent<playerHealth>();

        if (entityHealth != null)
        {
            entityHealth.TakeDamage(float.MaxValue);
        }
    }
}
