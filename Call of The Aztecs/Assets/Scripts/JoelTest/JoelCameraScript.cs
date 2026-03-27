using UnityEngine;

public class JoelCameraScript : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Vector3 offset;

    void Start()
    {
        offset = transform.position - new Vector3 (player.position.x, 0, player.position.z);
    }

    void Update()
    {
        transform.position = new Vector3 (player.position.x, 0, player.position.z) + offset;
    }
}
