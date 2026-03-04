using UnityEngine;

public class LogTrap : MonoBehaviour
{
    public float speed = 6f;
    public float distance = 10f;
    public float damage = 30f;

    private Vector3 startPos;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        startPos = transform.position;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        if (Vector3.Distance(startPos, transform.position) >= distance)
        {
            gameObject.SetActive(false); // Deactivate instead of destroying
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<playerHealth>()?.TakeDamage(damage);
            gameObject.SetActive(false);
        }
    }
}

public class DirectionGizmo : MonoBehaviour
    {
        public float length = 2f;

        void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(
                transform.position,
                transform.position + transform.forward * length
            );

            Gizmos.DrawSphere(transform.position + transform.forward * length, 0.1f);
        }
    }
