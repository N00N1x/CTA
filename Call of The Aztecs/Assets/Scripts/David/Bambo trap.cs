using System.Collections;
using System.Reflection;
using UnityEngine;

public class BamboTrap : MonoBehaviour
{
    public Animator animator;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            animator.SetTrigger("Activate");

                if (other.CompareTag("Player"))
                {
                    other.GetComponent<playerHealth>()?.TakeDamage(100);
                }
            }
        }
    }
 

