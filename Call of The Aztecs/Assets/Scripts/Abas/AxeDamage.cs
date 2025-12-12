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

    [Header("Debug / testing")]
    public bool debugMode = true;
    [Tooltip("Keep collider enabled for quick testing without animation events.")]
    public bool alwaysActiveForTesting = false;

    Collider axeCollider;
    readonly Dictionary<Transform, float> lastHitTime = new Dictionary<Transform, float>();
    readonly HashSet<Transform> hitThisSwing = new HashSet<Transform>();

    void Awake()
    {
        // Try to find collider on same GameObject first, then children.
        axeCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        if (axeCollider == null)
        {
            Debug.LogError("[AxeDamage] No Collider found on this GameObject or children. Add a Box/Capsule collider to the blade.");
            return;
        }

        if (debugMode) Debug.Log($"[AxeDamage] Found collider '{axeCollider.name}' (isTrigger={axeCollider.isTrigger})");

        // make sure trigger mode matches setting
        axeCollider.isTrigger = useTrigger;

        // default testing behavior
        if (alwaysActiveForTesting)
            axeCollider.enabled = true;
        else if (enableColliderOnlyDuringSwing)
            axeCollider.enabled = false;
    }

    // call from animation event
    public void BeginSwing()
    {
        if (debugMode) Debug.Log("[AxeDamage] BeginSwing()");
        StopAllCoroutines();
        StartCoroutine(SwingRoutine(swingActiveDuration));
    }

    IEnumerator SwingRoutine(float duration)
    {
        ClearExpiredHits();
        hitThisSwing.Clear();

        if (enableColliderOnlyDuringSwing && axeCollider != null)
            axeCollider.enabled = true;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (enableColliderOnlyDuringSwing && axeCollider != null && !alwaysActiveForTesting)
            axeCollider.enabled = false;

        hitThisSwing.Clear();
    }

    // Animation-driven full-state methods (public so an external controller can call them)
    // Call at attack state enter: enables collider and clears previous hit tracking.
    public void AnimationStart()
    {
        if (debugMode) Debug.Log("[AxeDamage] AnimationStart() - enabling collider for state");
        ClearExpiredHits();
        hitThisSwing.Clear();

        if (axeCollider != null)
            axeCollider.enabled = true;
    }

    // Call at attack state exit: disables collider and clears per-swing hits.
    public void AnimationEnd()
    {
        if (debugMode) Debug.Log("[AxeDamage] AnimationEnd() - disabling collider for state");
        hitThisSwing.Clear();

        if (axeCollider != null && !alwaysActiveForTesting)
            axeCollider.enabled = false;
    }

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

    // call from animation event instead of BeginSwing if you prefer manual control
    public void SetColliderEnabled(bool enabled)
    {
        if (axeCollider != null)
        {
            axeCollider.enabled = enabled;
            if (debugMode) Debug.Log($"[AxeDamage] SetColliderEnabled({enabled})");
        }
    }
}
