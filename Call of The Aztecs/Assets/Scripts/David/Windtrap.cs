using UnityEngine;
using System.Collections;

public class Windtrap : MonoBehaviour
{
    public float windForce = 10f;
    public Vector3 windDirection = Vector3.forward;
    public Animator windtrapAnimator;
    public float windDuration = 4f;
    public float windCooldown = 2f; // Rest period after blowing

    private Coroutine windCoroutine;
    private bool isCoolingDown = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCoolingDown)
        {
            if (windtrapAnimator != null)
                windtrapAnimator.SetBool("IsActive", true);

            if (windCoroutine == null)
                windCoroutine = StartCoroutine(BlowPlayer(other));
        }
    }

    private IEnumerator BlowPlayer(Collider player)
    {
        float timer = 0f;
        Rigidbody rb = player.GetComponent<Rigidbody>();
        while (timer < windDuration)
        {
            if (rb != null)
                rb.AddForce(windDirection.normalized * windForce * Time.deltaTime, ForceMode.VelocityChange);

            timer += Time.deltaTime;
            yield return null;
        }

        // Stop the wind animation
        if (windtrapAnimator != null)
            windtrapAnimator.SetBool("IsActive", false);

        // Start cooldown
        isCoolingDown = true;
        yield return new WaitForSeconds(windCooldown);
        isCoolingDown = false;

        windCoroutine = null;
    }
}
