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

    [Header("Acceleration")]
    public float acceleration = 30f;      // how fast the player speeds up
    public float deceleration = 40f;      // how fast the player slows down
    public float stopThreshold = 0.05f;   // speed below which we treat as stopped

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

    // current horizontal speed (smoothed)
    private float currentHorizontalSpeed = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
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
        if (context.performed)
            runHeld = true;
        else if (context.canceled)
            runHeld = false;
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;

        // Ground check
        Vector3 groundCheckPos = transform.position + Vector3.down * groundCheckOffset;
        isGrounded = Physics.CheckSphere(
            groundCheckPos,
            groundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Collide
        );

        // Camera relative movement
        if (Camera.main == null)
            return;

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
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            RaycastHit hit;
            Vector3 origin = transform.position;
            Vector3 dir = moveDirection.normalized;

            if (Physics.SphereCast(origin, obstacleSphereRadius, dir, out hit, obstacleCheckDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.normal.y < wallNormalYThreshold)
                {
                    Vector3 projected = Vector3.ProjectOnPlane(moveDirection, hit.normal);

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

        // Smooth acceleration / deceleration
        float targetSpeed = (moveDirection.sqrMagnitude > 0.001f) ? moveSpeed * (runHeld ? runMultiplier : 1f) : 0f;
        float rate = (targetSpeed > currentHorizontalSpeed) ? acceleration : deceleration;
        currentHorizontalSpeed = Mathf.MoveTowards(currentHorizontalSpeed, targetSpeed, rate * Time.fixedDeltaTime);

        if (currentHorizontalSpeed < stopThreshold)
            currentHorizontalSpeed = 0f;

        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontal = (moveDirection.sqrMagnitude > 0.001f) ? moveDirection.normalized * currentHorizontalSpeed : Vector3.zero;
        velocity.x = horizontal.x;
        velocity.z = horizontal.z;

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
            bool isMoving = currentHorizontalSpeed > stopThreshold;

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

        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Vector3 dir = moveDirection.normalized;
            Gizmos.DrawWireSphere(transform.position + dir * obstacleCheckDistance, obstacleSphereRadius);
        }
    }
}