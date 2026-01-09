using UnityEngine;

public class LogTrap : MonoBehaviour
{
    public float speed = 6f;
    public float distance = 10f;
    public float damage = 30f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        if (Vector3.Distance(startPos, transform.position) >= distance)
        {
            Destroy(gameObject); // or reset
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<playerHealth>()?.TakeDamage(damage);
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
}