using UnityEngine;

public class ThirdPersonMovement : MonoBehaviour
{
    public float moveSpeed = 5f;          // Movement speed
    public float turnSmoothTime = 0.1f;   // Smooth time for turning
    public float jumpForce = 5f;          // Jump height
    public Transform playerCamera;        // Reference to the player camera
    public Rigidbody rb;                  // Reference to the Rigidbody

    private float turnSmoothVelocity;     // velocity ref for SmoothDampAngle
    private bool isGrounded;

    void Update()
    {
        HandleMovementAndTurning();
        JumpPlayer();
    }

    void HandleMovementAndTurning()
    {
        // Read raw input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Build input direction (camera-relative rotation will be applied)
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            // Compute the target angle relative to the camera's y-rotation
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + playerCamera.eulerAngles.y;

            // Smoothly rotate the player
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // Move in the direction the player is facing (based on camera-aligned targetAngle)
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            rb.MovePosition(transform.position + moveDir * moveSpeed * Time.deltaTime);
        }
        else
        {
            // No input: optionally keep current velocity / do nothing
        }
    }

    void JumpPlayer()
    {
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        isGrounded = true;
    }

    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }
}
