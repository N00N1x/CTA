using UnityEngine;

public class Axe : MonoBehaviour
{
    [Tooltip("Animator containing the attack state.")]
    public Animator animator;

    [Tooltip("Full or short state name. Example: \"Attack\" or \"Base Layer.Attack\"")]
    public string attackStateName = "Attack";

    [Tooltip("Animator layer index to check.")]
    public int layerIndex = 0;

    [Tooltip("Reference to the AxeDamage component to enable/disable during attack.")]
    public AxeDamage axeDamage;

    bool wasInAttack;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (axeDamage == null)
            axeDamage = GetComponentInChildren<AxeDamage>();
    }

    void Update()
    {
        if (animator == null || axeDamage == null) return;

        var stateInfo = animator.GetCurrentAnimatorStateInfo(layerIndex);
        bool inAttack = stateInfo.IsName(attackStateName);

        if (inAttack && !wasInAttack)
        {
            // entered attack state
            axeDamage.AnimationStart();
        }
        else if (!inAttack && wasInAttack)
        {
            // exited attack state
            axeDamage.AnimationEnd();
        }

        wasInAttack = inAttack;
    }

    // Optional helper to trigger attack animations from code
    public void DoAttack(string triggerName = "Attack")
    {
        if (animator != null)
            animator.SetTrigger(triggerName);
    }
}
