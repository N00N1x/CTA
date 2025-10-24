using UnityEngine;

public class Pressureplate : MonoBehaviour
{
    public Animator pressurePlateAnimator;
    public Animator trapAnimator;
    public AudioSource trapCreaking;
    public int trapDamage = 1; 

    private bool isTrapActivated = false;

    private void OnTriggerEnter(Collider other)
    { 
        if (isTrapActivated)
        {
            Debug.Log("Trap already activated, ignoring trigger.");
            return;
        }

        if (other.CompareTag("Player"))
        {
            isTrapActivated = true;

            // Play pressure plate animation
            if (pressurePlateAnimator != null)
                pressurePlateAnimator.SetTrigger("Activate");

            // Play trap animation
            if (trapAnimator != null)
                trapAnimator.SetTrigger("Activate");

            // Play trap creaking sound
            if (trapCreaking != null)
                trapCreaking.Play();
            else
                Debug.LogWarning("Trap creaking AudioSource not assigned on Pressureplate script for " + gameObject.name);

           
        }
    }
}
