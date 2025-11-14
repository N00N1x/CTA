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

        var player = obj.GetComponent<playerHealth>() ?? obj.GetComponentInParent<playerHealth>();
        if (player != null)
        {
            player.TakeDamage(float.MaxValue);
            return;
        }

        if (obj.CompareTag("Player") || obj.CompareTag("player"))
        {
            Destroy(obj);
        }
    }
}

