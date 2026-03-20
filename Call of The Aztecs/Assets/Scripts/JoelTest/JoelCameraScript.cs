using UnityEngine;

public class JoelCameraScript : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Vector3 offset;

    void Start()
    {
        offset = transform.position - player.position;
    }

    void Update()
    {
        transform.position = player.position + offset;
    }
}
