using UnityEngine;

public class Killplane : MonoBehaviour
{
    public void TryKillPlayer(GameObject obj)
    {
        if (obj == null) return;

        var ph = obj.GetComponent<playerHealth>();
        if (ph != null)
        {
            ph.ForceDeath();
            return;
        }

        ph = obj.GetComponentInChildren<playerHealth>();
        if (ph != null)
        {
            ph.ForceDeath();
            return;
        }

        Debug.LogWarning("[Killplane] No playerHealth found on object; disabling GameObject as fallback.");
        obj.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            TryKillPlayer(other.gameObject);
        }
    }
}