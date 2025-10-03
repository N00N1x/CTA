using UnityEngine;

public class Killplane : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Killplane"))
        {
            Destroy(other.gameObject);
        }
    }
}
