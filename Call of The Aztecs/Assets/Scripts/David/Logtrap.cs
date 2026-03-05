using UnityEngine;

public class LogTrap : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 6f;
    public float distance = 10f;

    [Header("Damage")]
    public float damage = 30f;
    public string playerTag = "Player";

    [Header("Animation Looping")]
    public bool loopAnimation = true;                  // if true the animation will be looped while the log is active
    public Animator logAnimator;                       // assign Animator on the log prefab (preferred)
    public string loopParameterName = "IsLooping";     // bool parameter on Animator used for looping
    public Animation legacyAnimation;                  // optional legacy Animation component
    public string legacyClipName = "";                 // name of legacy clip to play/loop

    [Header("Misc")]
    public bool debugMode = false;

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
            // clear motion on enable so pooled logs behave consistently
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // start looped animation if requested
        if (loopAnimation)
            StartLoopAnimation();
    }

    private void OnDisable()
    {
        // ensure animation stops when the log is deactivated
        if (loopAnimation)
            StopLoopAnimation();
    }

    private void Update()
    {
        // move forward in local space
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        // disable when max distance reached
        if (Vector3.Distance(startPos, transform.position) >= distance)
        {
            if (debugMode) Debug.Log($"[LogTrap] reached distance, deactivating {name}");
            // stop animation before deactivation
            if (loopAnimation) StopLoopAnimation();
            gameObject.SetActive(false); // deactivate for pooling
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            other.GetComponent<playerHealth>()?.TakeDamage(damage);

            if (debugMode) Debug.Log($"[LogTrap] hit player {other.name}, damage={damage}");

            // stop animation before deactivation
            if (loopAnimation) StopLoopAnimation();
            gameObject.SetActive(false);
        }
    }

    // Start looping animation (Animator bool or legacy clip loop)
    private void StartLoopAnimation()
    {
        if (logAnimator != null && !string.IsNullOrEmpty(loopParameterName))
        {
            logAnimator.SetBool(loopParameterName, true);
            if (debugMode) Debug.Log($"[LogTrap] Animator.SetBool({loopParameterName}, true) on {name}");
            return;
        }

        if (legacyAnimation != null && !string.IsNullOrEmpty(legacyClipName))
        {
            legacyAnimation.Play(legacyClipName);
            var clip = legacyAnimation.GetClip(legacyClipName);
            if (clip != null)
                clip.wrapMode = WrapMode.Loop;
            if (debugMode) Debug.Log($"[LogTrap] legacy animation Play({legacyClipName}) loop on {name}");
        }
    }

    // Stop looping animation
    private void StopLoopAnimation()
    {
        if (logAnimator != null && !string.IsNullOrEmpty(loopParameterName))
        {
            logAnimator.SetBool(loopParameterName, false);
            if (debugMode) Debug.Log($"[LogTrap] Animator.SetBool({loopParameterName}, false) on {name}");
            return;
        }

        if (legacyAnimation != null && !string.IsNullOrEmpty(legacyClipName))
        {
            legacyAnimation.Stop(legacyClipName);
            if (debugMode) Debug.Log($"[LogTrap] legacy animation Stop({legacyClipName}) on {name}");
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
