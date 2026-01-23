using System.Collections;
using UnityEngine;

public class SpikeTrigger : MonoBehaviour
{
    public Animator spikeAnimator;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            spikeAnimator.SetTrigger("BambooActivate");
        }
    }
}



