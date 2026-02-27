using UnityEngine;

public class RandomCycleOffset : MonoBehaviour
{
    Animator animator;
    float randomCycleOffset;
    void Start()
    {
        animator = GetComponent<Animator>();
        setRCO(animator);
    }

    void setRCO(Animator anim)
    {
        randomCycleOffset = Random.value;
        anim.SetFloat("randomCycleOffset", randomCycleOffset);
    }
}
