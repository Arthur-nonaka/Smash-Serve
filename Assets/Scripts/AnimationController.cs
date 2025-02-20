using UnityEngine;
using System.Collections;

public class AnimationController : MonoBehaviour
{
    public Animator animator;

    public IEnumerator SetAnimatorBoolWithDelay(string parameter, bool value, float delay)
    {
        animator.SetBool(parameter, value);
        yield return new WaitForSeconds(delay);
        animator.SetBool(parameter, !value);
    }
}