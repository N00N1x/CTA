using UnityEngine;

public class Traps : MonoBehaviour
{
    public int damageAmount = 1; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
           
           // var playerHealth = other.GetComponent<PlayerHealth>();
          //  if (playerHealth != null)
            {
         //      playerHealth.TakeDamage(damageAmount);
            }
        }
    }
}
