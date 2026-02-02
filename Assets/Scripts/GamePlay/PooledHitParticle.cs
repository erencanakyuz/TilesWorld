using UnityEngine;

/// <summary>
/// Returns pooled hit particles back to HitZoneManager without per-hit async allocations.
/// </summary>
public class PooledHitParticle : MonoBehaviour
{
    private HitZoneManager owner;
    private HitAccuracy accuracy;
    private ParticleSystem cachedParticleSystem;
    private GameObject rootObject;

    public void Initialize(HitZoneManager ownerRef, HitAccuracy accuracyRef, GameObject root)
    {
        owner = ownerRef;
        accuracy = accuracyRef;
        rootObject = root;

        if (cachedParticleSystem == null)
        {
            cachedParticleSystem = GetComponent<ParticleSystem>();
        }

        if (cachedParticleSystem != null)
        {
            var main = cachedParticleSystem.main;
            main.stopAction = ParticleSystemStopAction.Callback;
            main.loop = false;
        }
    }

    private void OnParticleSystemStopped()
    {
        if (owner != null)
        {
            owner.ReturnParticleToPool(rootObject != null ? rootObject : gameObject, accuracy);
        }
        else
        {
            (rootObject != null ? rootObject : gameObject).SetActive(false);
        }
    }
}
