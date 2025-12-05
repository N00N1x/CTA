using System.Collections;
using System.Reflection;
using UnityEngine;

public class BamboTrap : MonoBehaviour
{
    public int damage = 20;
    public float activeTime = 1.0f;    // how long spikes are up
    public float inactiveTime = 1.0f;  // how long spikes are down
    public bool autoToggle = true;

    private bool isActive = true;

    void Start()
    {
        if (autoToggle)
            StartCoroutine(ToggleRoutine());
    }

    private IEnumerator ToggleRoutine()
    {
        while (true)
        {
            isActive = true;      // spikes up
            yield return new WaitForSeconds(activeTime);

            isActive = false;     // spikes down
            yield return new WaitForSeconds(inactiveTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        if (other.CompareTag("Player"))
        {
            // Replace with your health system
            other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
        }
    }
}