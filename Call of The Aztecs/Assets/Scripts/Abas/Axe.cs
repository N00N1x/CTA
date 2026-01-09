using UnityEngine;

public class Axe : MonoBehaviour
{
    [Tooltip("Animator containing the attack state.")]
    public Animator animator;

    [Tooltip("Name of the attack state to monitor. Example: \"Attack\" or \"Base Layer.Attack\"")]
    public string attackStateName = "Attack";

    [Tooltip("Animator layer index to check.")]
    public int layerIndex = 0;

    [Tooltip("Reference to the AxeDamage component that will be enabled/disabled while in the attack state.")]
    public AxeDamage axeDamage;

    bool wasInAttack = false;

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
            // Entered attack state
            axeDamage.AnimationStart();
        }
        else if (!inAttack && wasInAttack)
        {
            // Exited attack state
            axeDamage.AnimationEnd();
        }

        wasInAttack = inAttack;
    }

    // Optional: trigger attack from code
    public void DoAttack(string triggerName = "Attack")
    {
        if (animator != null)
            animator.SetTrigger(triggerName);
    }
}
