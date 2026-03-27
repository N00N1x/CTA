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

        if (obj.CompareTag("Player"))
        {
            Destroy(obj);
        }
    }
}
