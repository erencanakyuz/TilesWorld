using UnityEngine;

/// <summary>
/// ParticleAutoDestroy - Auto-destroys particle system after it finishes playing
/// 
/// This component automatically destroys the GameObject when the particle system finishes.
/// Used for hit effects and other temporary particle systems.
/// </summary>
public class ParticleAutoDestroy : MonoBehaviour
{
    private ParticleSystem particles;
    private float destroyTime;

    void Start()
    {
        particles = GetComponent<ParticleSystem>();
        if (particles != null)
        {
            // Calculate when to destroy based on particle system settings
            var main = particles.main;
            destroyTime = main.duration + main.startLifetime.constantMax + 0.5f; // Add small buffer

            // Schedule destruction
            Destroy(gameObject, destroyTime);
        }
        else
        {
            // Fallback if no particle system found
            Destroy(gameObject, 2f);
        }
    }

    void Update()
    {
        // Additional safety check - destroy if particle system is no longer playing
        if (particles != null && !particles.isPlaying && particles.particleCount == 0)
        {
            Destroy(gameObject);
        }
    }
}