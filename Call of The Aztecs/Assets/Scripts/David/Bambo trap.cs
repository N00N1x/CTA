using System.Collections;
using System.Reflection;
using UnityEngine;

public class BamboTrap : MonoBehaviour
{
    public Animator spikeAnimator;     // Drag the Spike child’s Animator here
    public float damageDelay = 0.1f;
    public int damage = 20;

    private bool isBusy = false;
    private GameObject playerRef;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isBusy)
        {
            playerRef = other.gameObject;
            StartCoroutine(SpikeRoutine());
        }
    }

    private System.Collections.IEnumerator SpikeRoutine()
    {
        isBusy = true;

        spikeAnimator.SetTrigger("Pop");   // play spike child animation

        yield return new WaitForSeconds(damageDelay);

        playerRef.GetComponent<playerHealth>()?.TakeDamage(damage);

        yield return new WaitForSeconds(0.4f);

        isBusy = false;
    }
}
 