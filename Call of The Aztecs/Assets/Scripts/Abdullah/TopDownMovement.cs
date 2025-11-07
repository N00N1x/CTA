using UnityEngine;
using UnityEngine.InputSystem;

public class TopDownMovementNew : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float gravity = -9.81f;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 3f;

    [Header("Ground Check Settings")]
    public LayerMask groundMask;
    public float groundCheckRadius = 0.5f;
    public float groundCheckOffset = 1.0f;

    [Header("Jump Timing")]
    public float jumpCooldown = 0.3f;   // ? seconds between jumps
    private float lastJumpTime = -999f; // ? tracks last jump time

    private Rigidbody rb;
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            jumpPressed = true;
            jumpHeld = true;
        }
        else if (context.canceled)
        {
            jumpHeld = false;
        }
    }

    void FixedUpdate()
    {
        Vector3 groundCheckPos = transform.position + Vector3.down * groundCheckOffset;
        isGrounded = Physics.CheckSphere(groundCheckPos, groundCheckRadius, groundMask, QueryTriggerInteraction.Collide);

        moveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveDirection.x * moveSpeed;
        velocity.z = moveDirection.z * moveSpeed;

        // ? Jump with cooldown
        if (jumpPressed && isGrounded && Time.time >= lastJumpTime + jumpCooldown)
        {
            velocity.y = jumpForce;
            lastJumpTime = Time.time; // ? record when the jump happened
            jumpPressed = false;
        }

        // Variable jump height logic
        if (rb.linearVelocity.y > 0 && !jumpHeld)
        {
            velocity.y += gravity * lowJumpMultiplier * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y < 0)
        {
            velocity.y += gravity * fallMultiplier * Time.fixedDeltaTime;
        }
        else
        {
            velocity.y += gravity * Time.fixedDeltaTime;
        }

        rb.linearVelocity = velocity;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 groundCheckPos = transform.position + Vector3.down * groundCheckOffset;
        Gizmos.DrawWireSphere(groundCheckPos, groundCheckRadius);
    }
}
