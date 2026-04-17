using UnityEngine;

public class JoelCameraScript : MonoBehaviour
{
    [SerializeField] Transform player;
    [SerializeField] Vector3 offset;

    void Start()
    {
        if (player == null)
        {
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo != null) player = pgo.transform;
        }

        if (player != null)
        {
            offset = transform.position - new Vector3(player.position.x, 0, player.position.z);
        }
        else
        {
            Debug.LogWarning("JoelCameraScript: 'player' is not assigned and no GameObject tagged 'Player' was found. Camera will remain static until a player is assigned.");
        }
    }

    void LateUpdate()
    {
        // Guard against the player reference being destroyed (MissingReferenceException).
        if (player == null)
        {
            // Try to find a replacement (useful if player was recreated).
            var pgo = GameObject.FindGameObjectWithTag("Player");
            if (pgo != null)
            {
                player = pgo.transform;
                // Recompute offset so camera won't jump unexpectedly.
                offset = transform.position - new Vector3(player.position.x, 0, player.position.z);
            }
            else
            {
                return; // Nothing to follow right now.
            }
        }

        transform.position = new Vector3(player.position.x, 0, player.position.z) + offset;
    }
}
