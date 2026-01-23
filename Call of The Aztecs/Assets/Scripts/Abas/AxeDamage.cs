using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AxeDamage : MonoBehaviour
{
    [Header("Damage")]
    public int damageAmount = 100;
    public string targetTag = "Player";
    public LayerMask targetLayer = ~0; // all layers by default

    [Header("Swing / activation")]
    public bool useTrigger = true;
    public bool enableColliderOnlyDuringSwing = true;
    public float swingActiveDuration = 0.25f;
    public float perTargetCooldown = 0.25f;
    public bool hitOncePerSwing = true;

    [Header("Blade collider selection")]
    [Tooltip("Assign the exact collider on the blade you want to use (preferred).")]
    public Collider bladeCollider;
    [Tooltip("Fallback: name of child GameObject that holds the blade collider.")]
    public string bladeColliderName = "BladeCollider";
    [Tooltip("Fallback: tag of child GameObject that holds the blade collider.")]
    public string bladeColliderTag = "WeaponBlade";

    [Header("Debug / testing")]
    public bool debugMode = true;
    [Tooltip("Keep collider enabled for quick testing without animation events.")]
    public bool alwaysActiveForTesting = false;

    Collider axeCollider;
    readonly Dictionary<Transform, float> lastHitTime = new Dictionary<Transform, float>();
    readonly HashSet<Transform> hitThisSwing = new HashSet<Transform>();

    void Awake()
    {
        // root collider (may be the old static box)
        Collider rootCollider = GetComponent<Collider>();

        // If inspector-assigned bladeCollider is set, use it; otherwise try to find a child collider
        if (bladeCollider != null)
        {
            axeCollider = bladeCollider;
        }
        else
        {
            // search children for tag/name or first child collider
            Collider[] all = GetComponentsInChildren<Collider>(true);
            foreach (var c in all)
            {
                if (c.gameObject != gameObject && !string.IsNullOrEmpty(bladeColliderTag) && c.gameObject.CompareTag(bladeColliderTag))
                {
                    axeCollider = c;
                    break;
                }
            }

            if (axeCollider == null)
            {
                foreach (var c in all)
                {
                    if (c.gameObject != gameObject && !string.IsNullOrEmpty(bladeColliderName) && c.gameObject.name == bladeColliderName)
                    {
                        axeCollider = c;
                        break;
                    }
                }
            }

            if (axeCollider == null)
            {
                foreach (var c in all)
                {
                    if (c.gameObject != gameObject)
                    {
                        axeCollider = c;
                        break;
                    }
                }
            }

            // fallback: use root collider
            if (axeCollider == null)
                axeCollider = rootCollider;
        }

        if (axeCollider == null)
        {
            Debug.LogError("[AxeDamage] No Collider found on this GameObject or children. Add a Box/Capsule collider to the blade.");
            return;
        }

        if (debugMode) Debug.Log($"[AxeDamage] Using collider '{axeCollider.name}' (isTrigger={axeCollider.isTrigger})");

        // ensure trigger mode matches setting
        axeCollider.isTrigger = useTrigger;

        // If we selected a child blade collider and there is a root (stale) collider, disable/remove it
        if (rootCollider != null && rootCollider != axeCollider)
        {
            if (debugMode) Debug.Log("[AxeDamage] Disabling root collider to avoid interfering with blade hitbox.");
            rootCollider.enabled = false;
#if UNITY_EDITOR
            // Remove the component in editor so inspector no longer shows it
            UnityEngine.Object.DestroyImmediate(rootCollider);
#else
            UnityEngine.Object.Destroy(rootCollider);
#endif
        }

        // If the selected collider is on another GameObject (child), add a proxy to forward triggers
        if (axeCollider.gameObject != gameObject)
        {
            var proxy = axeCollider.gameObject.GetComponent<ColliderProxy>();
            if (proxy == null)
                proxy = axeCollider.gameObject.AddComponent<ColliderProxy>();

            proxy.owner = this;
        }

        // default testing behavior
        if (alwaysActiveForTesting)
            axeCollider.enabled = true;
        else if (enableColliderOnlyDuringSwing)
            axeCollider.enabled = false;
    }

    // This will be called by ColliderProxy when the child collider triggers
    public void OnProxyTriggerEnter(Collider other)
    {
        if (debugMode) Debug.Log($"[AxeDamage] OnProxyTriggerEnter from '{axeCollider.name}' with '{other.gameObject.name}'");
        TryHit(other.gameObject);
    }

    public void OnProxyCollisionEnter(Collision collision)
    {
        if (debugMode) Debug.Log($"[AxeDamage] OnProxyCollisionEnter from '{axeCollider.name}' with '{collision.gameObject.name}'");
        TryHit(collision.gameObject);
    }

    // Direct callbacks (if the collider and this MonoBehaviour are on the same GameObject)
    private void OnTriggerEnter(Collider other)
    {
        if (!useTrigger) return;
        if (debugMode) Debug.Log($"[AxeDamage] OnTriggerEnter with '{other.gameObject.name}' layer:{other.gameObject.layer} tag:{other.gameObject.tag}");
        TryHit(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (useTrigger) return;
        if (debugMode) Debug.Log($"[AxeDamage] OnCollisionEnter with '{collision.gameObject.name}' layer:{collision.gameObject.layer} tag:{collision.gameObject.tag}");
        TryHit(collision.gameObject);
    }

    private void TryHit(GameObject obj)
    {
        if (obj == null) return;

        // layer filter
        if (((1 << obj.layer) & targetLayer) == 0)
        {
            if (debugMode) Debug.Log("[AxeDamage] Ignored by layer");
            return;
        }

        // tag filter
        if (!string.IsNullOrEmpty(targetTag) && !obj.CompareTag(targetTag))
        {
            if (debugMode) Debug.Log($"[AxeDamage] Ignored by tag (expected '{targetTag}', got '{obj.tag}')");
            return;
        }

        Transform t = obj.transform;
        float now = Time.time;

        if (hitOncePerSwing && hitThisSwing.Contains(t))
        {
            if (debugMode) Debug.Log("[AxeDamage] Ignored: already hit this swing");
            return;
        }

        if (lastHitTime.TryGetValue(t, out float last) && now - last < perTargetCooldown)
        {
            if (debugMode) Debug.Log("[AxeDamage] Ignored: per-target cooldown");
            return;
        }

        var ph = obj.GetComponent<playerHealth>() ?? obj.GetComponentInParent<playerHealth>();
        if (ph != null && damageAmount > 0)
        {
            if (debugMode) Debug.Log($"[AxeDamage] Hitting '{obj.name}' for {damageAmount}");
            ph.TakeDamage(damageAmount);
        }
        else
        {
            if (debugMode) Debug.Log("[AxeDamage] No playerHealth found on target");
        }

        lastHitTime[t] = now;
        if (hitOncePerSwing) hitThisSwing.Add(t);
    }

    private void ClearExpiredHits()
    {
        float now = Time.time;
        var keys = new List<Transform>(lastHitTime.Keys);
        foreach (var k in keys)
        {
            if (now - lastHitTime[k] >= perTargetCooldown)
                lastHitTime.Remove(k);
        }
    }

    // Animation-driven full-state methods
    public void AnimationStart()
    {
        if (debugMode) Debug.Log("[AxeDamage] AnimationStart() - enabling collider for state");
        ClearExpiredHits();
        hitThisSwing.Clear();

        if (axeCollider != null)
            axeCollider.enabled = true;
    }

    public void AnimationEnd()
    {
        if (debugMode) Debug.Log("[AxeDamage] AnimationEnd() - disabling collider for state");
        hitThisSwing.Clear();

        if (axeCollider != null && !alwaysActiveForTesting)
            axeCollider.enabled = false;
    }

    // call from animation event instead of BeginSwing if you prefer manual control
    public void SetColliderEnabled(bool enabled)
    {
        if (axeCollider != null)
        {
            axeCollider.enabled = enabled;
            if (debugMode) Debug.Log($"[AxeDamage] SetColliderEnabled({enabled})");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (axeCollider == null) return;
        Gizmos.color = Color.red;                    
        if (axeCollider is BoxCollider box)
        {
            Gizmos.matrix = axeCollider.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
        else if (axeCollider is SphereCollider sphere)
        {
            Gizmos.DrawWireSphere(axeCollider.transform.TransformPoint(sphere.center), sphere.radius * Mathf.Max(
                axeCollider.transform.lossyScale.x,
                axeCollider.transform.lossyScale.y,
                axeCollider.transform.lossyScale.z));
        }
        else if (axeCollider is CapsuleCollider cap)
        {
            Gizmos.DrawWireSphere(axeCollider.transform.TransformPoint(cap.center + Vector3.up * (cap.height / 2f - cap.radius)), cap.radius);
            Gizmos.DrawWireSphere(axeCollider.transform.TransformPoint(cap.center - Vector3.up * (cap.height / 2f - cap.radius)), cap.radius);
        }
    }
}

// Small helper component that forwards collision/trigger events from the blade collider gameobject to the AxeDamage owner.
public class ColliderProxy : MonoBehaviour
{
    [HideInInspector] public AxeDamage owner;

    void OnTriggerEnter(Collider other)
    {
        owner?.OnProxyTriggerEnter(other);
    }

    void OnCollisionEnter(Collision collision)
    {
        owner?.OnProxyCollisionEnter(collision);
    }
}
