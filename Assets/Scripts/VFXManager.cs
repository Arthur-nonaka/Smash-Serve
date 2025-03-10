using UnityEngine;
using UnityEngine.VFX;
using Mirror;
using System.Collections;

public class VFXManager : NetworkBehaviour
{
    public VisualEffect jumpVFXPrefab;
    public ParticleSystem spikeVFXPrefab;

    [ClientRpc]
    public void RpcPlayJumpVFX(Vector3 jumpPosition)
    {
        VisualEffect vfxInstance = Instantiate(jumpVFXPrefab, jumpPosition, Quaternion.identity);
        vfxInstance.Play();
        StartCoroutine(DeactivateAndDestroy(vfxInstance, 1f));
    }

    [ClientRpc]
    public void RpcPlaySpikeVFX(Vector3 spikePosition)
    {
        ParticleSystem vfxInstance = Instantiate(spikeVFXPrefab, spikePosition, Quaternion.identity);
        vfxInstance.Play();
        StartCoroutine(DeactivateAndDestroy(vfxInstance, 1f));
    }

    private IEnumerator DeactivateAndDestroy(VisualEffect vfxInstance, float delay)
    {
        yield return new WaitForSeconds(delay);
        vfxInstance.Stop();
        Destroy(vfxInstance.gameObject);
    }

    private IEnumerator DeactivateAndDestroy(ParticleSystem vfxInstance, float delay)
    {
        yield return new WaitForSeconds(delay);
        vfxInstance.Stop();
        Destroy(vfxInstance.gameObject);
    }
}
