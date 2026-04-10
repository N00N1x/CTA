using UnityEngine;
using UnityEngine.InputSystem;

public class TopDownMovementNew : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float runMultiplier = 1.8f;
    public float jumpForce = 8f;
    public float gravity = -9.81f;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 3f;
    public float modelForwardAngle = 180f;

    [Header("Ground Check Settings")]
    public LayerMask groundMask;
    public float groundCheckRadius = 0.5f;
    public float groundCheckOffset = 1.0f;

    [Header("Obstacle / Wall Handling")]
    [Tooltip("Layers considered as obstacles/walls (use environment layers).")]
    public LayerMask obstacleMask = ~0;
    [Tooltip("Radius used for sphere-checking obstacles ahead.")]
    public float obstacleSphereRadius = 0.35f;
    [Tooltip("Distance ahead to check for an immediate blocking wall.")]
    public float obstacleCheckDistance = 0.6f;
    [Tooltip("If a contact normal's Y is >= this, it's considered ground; otherwise it's a wall.")]
    public float wallNormalYThreshold = 0.65f;

    [Header("Jump Timing")]
    public float jumpCooldown = 0.3f;
    private float lastJumpTime = -999f;

    [Header("Camera Snap Settings")]
    public float snapAngle = 90f;

    [Header("References")]
    public Animator animator; // DRAG the Character's Animator here

    private Rigidbody rb;
    private Vector2 moveInput;
    private Vector3 moveDirection;

    private bool jumpPressed;
    private bool jumpHeld;
    private bool isGrounded;
    private bool runHeld;

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

    public void OnRun(InputAction.CallbackContext context)
    {
        runHeld = context.performed;
    }

    void FixedUpdate()
    {
        // Ground check
        Vector3 groundCheckPos = transform.position + Vector3.down * groundCheckOffset;
        isGrounded = Physics.CheckSphere(
            groundCheckPos,
            groundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Collide
        );

        // Camera relative movement
        Transform cam = Camera.main.transform;
        float camYaw = cam.eulerAngles.y;
        float snappedYaw = Mathf.Round(camYaw / snapAngle) * snapAngle;

        // Snap input to 8 directions (every 45 degrees)
        if (moveInput.sqrMagnitude < 0.0001f)
        {
            moveDirection = Vector3.zero;
        }
        else
        {
            float inputAngle = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg;
            float snappedInputAngle = Mathf.Round(inputAngle / 45f) * 45f;
            float totalYaw = snappedYaw + snappedInputAngle;
            moveDirection = Quaternion.Euler(0f, totalYaw, 0f) * Vector3.forward;
        }

        // Prevent walking/sticking to immediate walls:
        // If moving and there's a steep obstacle right in front within obstacleCheckDistance,
        // project movement onto the obstacle plane (removes into-wall component). If projection
        // is nearly zero, cancel horizontal movement so the player doesn't "walk along" the wall.
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            RaycastHit hit;
            Vector3 origin = transform.position; // can be adjusted if character origin is at feet
            Vector3 dir = moveDirection.normalized;

            if (Physics.SphereCast(origin, obstacleSphereRadius, dir, out hit, obstacleCheckDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                // If the hit is a steep surface (not ground), treat it as a blocking wall
                if (hit.normal.y < wallNormalYThreshold)
                {
                    // Remove movement component into the wall
                    Vector3 projected = Vector3.ProjectOnPlane(moveDirection, hit.normal);

                    // If projection almost zero, cancel movement so player doesn't slide along wall
                    if (projected.sqrMagnitude < 0.01f)
                        moveDirection = Vector3.zero;
                    else
                        moveDirection = projected.normalized * moveDirection.magnitude;
                }
            }
        }

        // Face movement direction
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot =
                Quaternion.LookRotation(moveDirection, Vector3.up) *
                Quaternion.Euler(0f, modelForwardAngle, 0f);

            rb.MoveRotation(Quaternion.Slerp(
                rb.rotation,
                targetRot,
                10f * Time.fixedDeltaTime
            ));
        }

        float currentSpeed = moveSpeed * (runHeld ? runMultiplier : 1f);

        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveDirection.x * currentSpeed;
        velocity.z = moveDirection.z * currentSpeed;

        // Jump
        if (jumpPressed && isGrounded && Time.time >= lastJumpTime + jumpCooldown)
        {
            velocity.y = jumpForce;
            lastJumpTime = Time.time;
            jumpPressed = false;

            if (animator != null)
                animator.SetTrigger("jumped");
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

        // Animator updates
        if (animator != null)
        {
            bool isMoving = moveDirection.sqrMagnitude > 0.001f;

            animator.SetBool("isRunning", isMoving && isGrounded);
            animator.SetBool("onGround", isGrounded);
        }

        rb.linearVelocity = velocity;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 groundCheckPos = transform.position + Vector3.down * groundCheckOffset;
        Gizmos.DrawWireSphere(groundCheckPos, groundCheckRadius);

        // draw obstacle check ray when selected
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Vector3 dir = moveDirection.normalized;
            Gizmos.DrawWireSphere(transform.position + dir * obstacleCheckDistance, obstacleSphereRadius);
        }
    }
}