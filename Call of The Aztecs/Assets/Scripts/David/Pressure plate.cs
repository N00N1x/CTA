using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pressureplate: MonoBehaviour
{
    public float pressDistance = 0.3f;
    public float presspeed = 1f;
    public Transform platetemplate;
    


    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            float Distance = Vector3.Distance(platetemplate.position, transform.position);
            
        
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        
    }

    private void OnTriggerExit(Collider other)
    {
        
    }

}
