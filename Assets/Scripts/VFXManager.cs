using UnityEngine;
using UnityEngine.VFX;
using System.Collections;

public class VFXManager : MonoBehaviour
{
    public VisualEffect jumpVFXPrefab;

    public void PlayJumpVFX(Vector3 jumpPosition)
    {
        VisualEffect vfxInstance = Instantiate(jumpVFXPrefab, jumpPosition, Quaternion.identity);
        vfxInstance.Play();

        StartCoroutine(DeactivateAndDestroy(vfxInstance, 1f));
    }

    private IEnumerator DeactivateAndDestroy(VisualEffect vfxInstance, float delay)
    {
        yield return new WaitForSeconds(delay);
        vfxInstance.Stop();
        Destroy(vfxInstance.gameObject);
    }
}
