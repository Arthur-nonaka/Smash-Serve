using UnityEngine;
using System.Collections;

public class GreenBox : MonoBehaviour
{
    private bool isTriggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;
        if (other.CompareTag("Ball"))
        {
            isTriggered = true;
            FindFirstObjectByType<TutorialManager>().TargetHit();
            Debug.Log("Ball entered the green area!");
            Destroy(gameObject);

            StartCoroutine(ResetTrigger());
        }
    }

    private IEnumerator ResetTrigger()
    {
        yield return new WaitForSeconds(1f);
        isTriggered = false;
    }
}