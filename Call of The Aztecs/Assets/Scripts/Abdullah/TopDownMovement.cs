using UnityEngine;
using UnityEngine.InputSystem;

public class TopDownMovementNew : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float runMultiplier = 1.8f; // Speed multiplier when running
    public float jumpForce = 8f;
    public float gravity = -9.81f;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 3f;

[Header("Ground Check Settings")]
    public LayerMask groundMask;
    public float groundCheckRadius = 0.5f;
    public float groundCheckOffset = 1.0f;

    [Header("Jump Timing")]
    public float jumpCooldown = 0.3f;
    private float lastJumpTime = -999f;

    [Header("Camera Snap Settings")]
    public float snapAngle = 90f; // Change to 45f or 60f for smoother movement  

    private Rigidbody rb;
    private Animator animator;
    private Vector2 moveInput;
    private Vector3 moveDirection;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool isGrounded;
    private bool runHeld;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
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
            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }
        }
        else if (context.canceled)
        {
            jumpHeld = false;
        }
    }

    public void OnRun(InputAction.CallbackContext context)
    {
        runHeld = context.performed; // Shift pressed  
    }

    void FixedUpdate()
    {
        // Ground check  
        Vector3 groundCheckPos = transform.position + Vector3.down * groundCheckOffset;
        isGrounded = Physics.CheckSphere(groundCheckPos, groundCheckRadius, groundMask, QueryTriggerInteraction.Collide);

        // --- CAMERA RELATIVE MOVEMENT WITH SNAP ---  
        Transform cam = Camera.main.transform;
        float camYaw = cam.eulerAngles.y;
        float snappedYaw = Mathf.Round(camYaw / snapAngle) * snapAngle;

        Vector3 forward = Quaternion.Euler(0f, snappedYaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, snappedYaw, 0f) * Vector3.right;

        moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

        // --- FACE MOVEMENT DIRECTION (SMOOTH) ---  
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDirection, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 10f * Time.fixedDeltaTime));
        }

        // --- APPLY MOVEMENT ---  
        float currentSpeed = moveSpeed * (runHeld ? runMultiplier : 1f);
        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveDirection.x * currentSpeed;
        velocity.z = moveDirection.z * currentSpeed;

        // Jump with cooldown  
        if (jumpPressed && isGrounded && Time.time >= lastJumpTime + jumpCooldown)
        {
            velocity.y = jumpForce;
            lastJumpTime = Time.time;
            jumpPressed = false;
        }

        // Variable gravity  
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

        // Update animator parameters
        if (animator != null)
        {
            bool isMoving = moveDirection.sqrMagnitude > 0.001f;
            bool isFalling = rb.linearVelocity.y < -0.1f && !isGrounded;
            animator.SetBool("isRunning", isMoving && isGrounded);
            animator.SetBool("isFalling", isFalling);
            animator.SetBool("isGrounded", isGrounded);
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
