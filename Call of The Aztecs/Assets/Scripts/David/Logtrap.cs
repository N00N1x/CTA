using UnityEngine;

public class LogTrap : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 6f;
    public float distance = 10f;

    [Header("Damage")]
    public float damage = 30f;
    public string playerTag = "Player";

    [Header("Animation")]
    public bool useAnimator = true;                  // prefer Animator (Mecanim)
    public Animator logAnimator;                     // Animator on the log prefab
    public string animatorTrigger = "Play";          // trigger to play clip/state
    public string animatorBoolForLoop = "IsLooping"; // optional bool parameter for looping

    [Header("Legacy / Clip")]
    public Animation legacyAnimation;                // optional legacy Animation component
    public AnimationClip animationClip;              // standalone clip to play via legacy Animation
    public bool clipLoop = true;                     // loop the clip if using legacy Animation

    [Header("Looping")]
    public bool loopAnimation = true;                // if true animation loops while log active

    [Header("Misc")]
    public bool debugMode = false;

    private Vector3 startPos;
    bool hasHit = false;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Ensure legacy Animation component exists if the user assigned an AnimationClip
        if (animationClip != null)
        {
            if (legacyAnimation == null)
            {
                legacyAnimation = GetComponent<Animation>();
                if (legacyAnimation == null)
                {
                    // add at runtime so the clip can be played
                    legacyAnimation = gameObject.AddComponent<Animation>();
                    legacyAnimation.playAutomatically = false;
                    if (debugMode) Debug.Log($"[LogTrap] Added Animation component at runtime on {name}");
                }
            }

            // Register clip under its name if it's not already present
            if (legacyAnimation != null)
            {
                string clipName = animationClip.name;
                if (legacyAnimation.GetClip(clipName) == null)
                {
                    legacyAnimation.AddClip(animationClip, clipName);
                    if (debugMode) Debug.Log($"[LogTrap] Added clip '{clipName}' to legacy Animation on {name}");
                }
            }
        }
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

        // start looped or playing animation if requested
        if (loopAnimation)
            StartLoopAnimation();
        else
            PlayOnceAnimation();
    }

    private void OnDisable()
    {
        // ensure animation stops when the log is deactivated
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
            StopLoopAnimation();
            gameObject.SetActive(false); // deactivate for pooling
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        if (other.CompareTag(playerTag))
        {
            hasHit = true;

            var health = other.GetComponent<playerHealth>();
            if (health != null)
                health.TakeDamage(damage);

            if (debugMode)
                Debug.Log($"[LogTrap] hit player {other.name}, damage={damage}");

            // Optional: stop animation before destroying
            StopLoopAnimation();

            // Destroy after short delay (so effects/animation can play)
            Destroy(gameObject, 0.1f);
        }
    }

    // Play animation continuously while active (Animator bool or legacy clip)
    private void StartLoopAnimation()
    {
        if (useAnimator && logAnimator != null)
        {
            // if animator uses a bool to drive looping state
            if (!string.IsNullOrEmpty(animatorBoolForLoop))
            {
                logAnimator.SetBool(animatorBoolForLoop, true);
                if (debugMode) Debug.Log($"[LogTrap] Animator.SetBool({animatorBoolForLoop}, true) on {name}");
                return;
            }

            // otherwise try a trigger to enter a looping state
            if (!string.IsNullOrEmpty(animatorTrigger))
            {
                logAnimator.SetTrigger(animatorTrigger);
                if (debugMode) Debug.Log($"[LogTrap] Animator.SetTrigger({animatorTrigger}) on {name}");
                return;
            }
        }

        // Legacy Animation path: ensure clip exists then play as loop
        if (animationClip != null && legacyAnimation != null)
        {
            string clipName = animationClip.name;
            if (legacyAnimation.GetClip(clipName) == null)
            {
                legacyAnimation.AddClip(animationClip, clipName);
                if (debugMode) Debug.Log($"[LogTrap] Added clip '{clipName}' to legacy Animation at StartLoopAnimation");


                var state = legacyAnimation[clipName];
                if (state != null)
                    state.wrapMode = clipLoop ? WrapMode.Loop : WrapMode.Once;

                legacyAnimation.Play(clipName);
                if (debugMode) Debug.Log($"[LogTrap] legacy Animation.Play({clipName}) loop on {name}");
            }
        }
    }
    // Play a single shot animation (non-looping) when log activates
    private void PlayOnceAnimation()
    {
        if (useAnimator && logAnimator != null && !string.IsNullOrEmpty(animatorTrigger))
        {
            logAnimator.SetTrigger(animatorTrigger);
            if (debugMode) Debug.Log($"[LogTrap] Animator.SetTrigger({animatorTrigger}) (once) on {name}");
            return;
        }

        if (animationClip != null && legacyAnimation != null)
        {
            string clipName = animationClip.name;
            if (legacyAnimation.GetClip(clipName) == null)
            {
                legacyAnimation.AddClip(animationClip, clipName);
                if (debugMode) Debug.Log($"[LogTrap] Added clip '{clipName}' to legacy Animation at PlayOnceAnimation");
            }

            legacyAnimation[clipName].wrapMode = WrapMode.Once;
            legacyAnimation.Play(clipName);
            if (debugMode) Debug.Log($"[LogTrap] legacy Animation.Play({clipName}) (once) on {name}");
        }
    }

    private void StopLoopAnimation()
    {
        if (useAnimator && logAnimator != null && !string.IsNullOrEmpty(animatorBoolForLoop))
        {
            logAnimator.SetBool(animatorBoolForLoop, false);
            if (debugMode) Debug.Log($"[LogTrap] Animator.SetBool({animatorBoolForLoop}, false) on {name}");
        }

        if (legacyAnimation != null && animationClip != null)
        {
            var clipName = animationClip.name;
            if (legacyAnimation.IsPlaying(clipName))
                legacyAnimation.Stop(clipName);
            if (debugMode) Debug.Log($"[LogTrap] legacy Animation.Stop({clipName}) on {name}");
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
