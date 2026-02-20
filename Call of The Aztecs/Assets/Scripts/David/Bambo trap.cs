using System.Collections;
using UnityEngine;

public class SpikeTrigger : MonoBehaviour
{
    public Animator spikeAnimator;

    public int damage = 100;
    public float damageDelay = 0.1f;
    public float cooldown = 0.6f;
    private bool canDamage = false;

    public void EnableDamage()
    {
        canDamage = true;
    }

    public void DisableDamage()
    {
        canDamage = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!canDamage) return;

        if (other.CompareTag("Player"))
        {
            other.GetComponent<playerHealth>()?.TakeDamage(damage);
        }
    }
}






