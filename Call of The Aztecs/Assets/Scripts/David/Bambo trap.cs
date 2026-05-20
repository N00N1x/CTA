
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public class BamboTrap : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Animation clip to play when the trap is triggered (only one).")]
    public AnimationClip triggerAnimation;
    [Tooltip("Optional Animator to play the clip on. If not set, a legacy Animation component will be used if available.")]
    public Animator animator;

    [Header("Detection")]
    public string targetTag = "Player";    // only objects with this tag will trigger
    [Tooltip("If true, the trap will only trigger when the object is 'on top' of the collider (not passing over).")]
    public bool requireOnTop = true;
    [Tooltip("Tolerance used when deciding if the other collider is sitting on the trap top.")]
    public float topThreshold = 0.1f;

    [Header("Misc")]
    public bool debugMode = false;

    // tracking colliders currently occupying the trap so we only play once while occupied
    private readonly HashSet<Collider> occupants = new HashSet<Collider>();
    private Collider triggerCollider;

    // PlayableGraph used to play the clip on an Animator
    private PlayableGraph playableGraph;
    private bool isPlaying = false;

    // Legacy Animation fallback
    private Animation legacyAnimation;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
        if (triggerCollider == null)
        {
            Debug.LogError($"[BamboTrap] No Collider found on {name}. Please add a Collider and set it to 'Is Trigger'.");
        }
        else if (!triggerCollider.isTrigger)
        {
            if (debugMode) Debug.LogWarning($"[BamboTrap] Collider on {name} is not set to IsTrigger. For best results set it as a trigger.");
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        legacyAnimation = GetComponent<Animation>();
        if (animator == null && legacyAnimation == null && debugMode)
        {
            Debug.Log($"[BamboTrap] No Animator or legacy Animation component found on {name}. The clip will not play without one.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerCollider == null) return;
        if (!other.CompareTag(targetTag)) return;

        if (requireOnTop)
        {
            // Recalculate trap top based on current bounds (works for moving traps too)
            float trapTop = triggerCollider.bounds.max.y;
            float otherBottom = other.bounds.min.y;

            // Only treat as "stepped on" if the other collider's bottom is at or above the trap top minus tolerance
            if (otherBottom < trapTop - topThreshold)
            {
                if (debugMode) Debug.Log($"[BamboTrap] Ignoring {other.name} entering because it's not on top (otherBottom={otherBottom:F3}, trapTop={trapTop:F3}).");
                return;
            }
        }

        // Add to occupants; play only when the first occupant arrives
        bool added = occupants.Add(other);
        if (added)
        {
            if (occupants.Count == 1)
            {
                PlayTriggerAnimation();
            }
            if (debugMode) Debug.Log($"[BamboTrap] {other.name} stepped on {name}. occupants={occupants.Count}");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!occupants.Contains(other)) return;

        bool removed = occupants.Remove(other);
        if (removed)
        {
            if (debugMode) Debug.Log($"[BamboTrap] {other.name} left {name}. occupants={occupants.Count}");
            if (occupants.Count == 0)
            {
                // last object left -> stop animation
                StopAnimation();
            }
        }
    }

    private void PlayTriggerAnimation()
    {
        if (triggerAnimation == null)
        {
            if (debugMode) Debug.Log("[BamboTrap] No triggerAnimation assigned.");
            return;
        }

        if (isPlaying)
        {
            if (debugMode) Debug.Log("[BamboTrap] Animation already playing.");
            return;
        }

        // Preferred: play clip on Animator via PlayableGraph (works without requiring animator states)
        if (animator != null)
        {
            playableGraph = PlayableGraph.Create($"BamboTrap_{name}");
            var playableOutput = AnimationPlayableOutput.Create(playableGraph, "BamboTrapOutput", animator);
            var clipPlayable = AnimationClipPlayable.Create(playableGraph, triggerAnimation);
            clipPlayable.SetApplyFootIK(false);
            playableOutput.SetSourcePlayable(clipPlayable);
            playableGraph.Play();
            isPlaying = true;
            if (debugMode) Debug.Log($"[BamboTrap] Playing '{triggerAnimation.name}' on Animator via PlayableGraph.");
            return;
        }

        // Fallback: legacy Animation component
        if (legacyAnimation != null)
        {
            if (!legacyAnimation.GetClip(triggerAnimation.name))
            {
                legacyAnimation.AddClip(triggerAnimation, triggerAnimation.name);
            }
            legacyAnimation.Play(triggerAnimation.name);
            isPlaying = true;
            if (debugMode) Debug.Log($"[BamboTrap] Playing '{triggerAnimation.name}' via legacy Animation.");
            return;
        }

        if (debugMode) Debug.LogWarning("[BamboTrap] No Animator or legacy Animation available to play the clip.");
    }

    private void StopAnimation()
    {
        if (!isPlaying) return;

        if (playableGraph.IsValid())
        {
            playableGraph.Stop();
            playableGraph.Destroy();
            playableGraph = default;
            isPlaying = false;
            if (debugMode) Debug.Log("[BamboTrap] Stopped PlayableGraph animation.");
            return;
        }

        if (legacyAnimation != null)
        {
            legacyAnimation.Stop();
            isPlaying = false;
            if (debugMode) Debug.Log("[BamboTrap] Stopped legacy Animation.");
            return;
        }

        isPlaying = false;
    }

    private void OnDisable()
    {
        StopAnimation();
    }

    // Optional: expose a method to forcibly clear occupants (useful for respawn/pooling)
    public void ResetOccupants()
    {
        occupants.Clear();
        StopAnimation();
        if (debugMode) Debug.Log($"[BamboTrap] ResetOccupants called on {name}.");
    }
}