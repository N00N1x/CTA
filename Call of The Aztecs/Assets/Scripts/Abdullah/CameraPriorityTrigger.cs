using UnityEngine;

public class CameraPriorityTrigger : MonoBehaviour
{
    public Unity.Cinemachine.CinemachineCamera targetCamera;
    public int activePriority = 50;
    public int defaultPriority = 10;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            targetCamera.Priority = activePriority;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            targetCamera.Priority = defaultPriority;
    }
}
