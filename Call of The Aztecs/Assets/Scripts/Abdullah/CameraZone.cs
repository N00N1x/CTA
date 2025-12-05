using UnityEngine;
using Unity.Cinemachine;

public class CameraZone : MonoBehaviour
{
    private CinemachineVirtualCamera vcam;

    void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Raise this camera's priority high so it becomes active
            vcam.Priority = 20;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Lower priority so it stops being active when leaving
            vcam.Priority = 0;
        }
    }
}
