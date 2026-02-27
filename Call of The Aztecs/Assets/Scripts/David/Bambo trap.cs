using UnityEngine;
using System.Collections;

public class SpikeTrigger : MonoBehaviour
{
    public Animator spikeAnimator;   // Assign the Animator in the inspector
    public string animationTrigger = "BambooActivate"; // Animator trigger name

    public int damage = 40;
    public float damageDelay = 0.1f; 
    public float cooldown = 0.6f;   

    private bool canDamage = false;
    private bool isOnCooldown = false;

    public void ActivateSpike()
    {
        if (isOnCooldown) return;

        // Play spike animation
        if (spikeAnimator != null)
        {
            spikeAnimator.SetTrigger(animationTrigger);
        }

       
        StartCoroutine(DamageWindow());
    }

    private IEnumerator DamageWindow()
    {
        isOnCooldown = true;

        
        yield return new WaitForSeconds(damageDelay);
        canDamage = true;

        yield return new WaitForSeconds(cooldown);
        canDamage = false;

        isOnCooldown = false;
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


