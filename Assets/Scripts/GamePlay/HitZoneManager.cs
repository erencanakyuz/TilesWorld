using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;

/// <summary>
/// HitZoneManager
///  - Mediates between InputManager (taps/swipes) and HitZoneTrigger caches.
///  - Decides if a note can be hit based on timing window and sends results to
///    InteractiveMusicSystem, GameManager scoring, and UIManager effects.
/// Attach once to a gameplay controller object (e.g., GameplayManager).
/// </summary>
public class HitZoneManager : MonoBehaviour
{
    [Header("🎯 Time-Based Hit Windows (milliseconds)")]
    [Tooltip("Time window in MS for a 'Perfect' hit.")]
    public float perfectWindowMs = 80f;
    [Tooltip("Time window in MS for a 'Good' hit.")]
    public float goodWindowMs = 160f;
    [Tooltip("Time window in MS for an 'Okay' hit. Taps outside this are misses.")]
    public float okayWindowMs = 250f;

    [Header("CONFIGURATION")]
    [Tooltip("The ideal Z-position for a note to be hit. Must match NoteRenderer's hitZoneZ.")]
    public float hitLineZ = 0.0f;

    [Header("🎯 Scoring Configuration")]
    [Tooltip("Points awarded for perfect hits")]
    [SerializeField] private int perfectHitPoints = 300;
    [Tooltip("Points awarded for good hits")]
    [SerializeField] private int goodHitPoints = 100;
    [Tooltip("Points awarded for okay hits")]
    [SerializeField] private int okayHitPoints = 50;

    [Header("✨ Visuals")]
    [Tooltip("Drag the Enhanced_HitZoneVisual prefab here.")]
    [SerializeField] private GameObject hitZoneVisualPrefab;

    [Header("🎆 Particle Effect Prefabs")]
    [Tooltip("Perfect hit particle effect prefab")]
    [SerializeField] private GameObject perfectHitEffectPrefab;
    [Tooltip("Good hit particle effect prefab")]
    [SerializeField] private GameObject goodHitEffectPrefab;
    [Tooltip("Miss particle effect prefab")]
    [SerializeField] private GameObject missEffectPrefab;

    [Header("🔧 Auto-Setup")]
    [Tooltip("Automatically try to find and assign prefabs if not set")]
    [SerializeField] private bool autoFindPrefabs = true;

    [Tooltip("Reference to active AudioManager clock (optional). If null, Time.time will be used.")]
    public AudioManager audioManager;

    // Internal
    private HitZoneTrigger[] zones;
    private float noteTravelTime;
    private Dictionary<int, Renderer> hitZoneRenderers = new Dictionary<int, Renderer>();
    private Dictionary<HitAccuracy, Color> hitColors;
    private UIConfig uiConfig;

    // PARTICLE POOLING - Reduces GC spikes during heavy note density
    private Queue<GameObject> perfectParticlePool = new Queue<GameObject>();
    private Queue<GameObject> goodParticlePool = new Queue<GameObject>();
    private const int PARTICLE_POOL_SIZE = 10;

    void Awake()
    {
        zones = FindObjectsByType<HitZoneTrigger>(FindObjectsSortMode.None);
        System.Array.Sort(zones, (a, b) => a.laneIndex.CompareTo(b.laneIndex));

        if (audioManager == null) audioManager = AudioManager.Instance;

        // Load UI config for hit colors
        uiConfig = Resources.Load<UIConfig>("UI/UIConfig");
        InitializeHitColors();

        // Auto-find prefabs if enabled and not assigned
        if (autoFindPrefabs)
        {
            AutoFindPrefabs();
        }

        // Create hit zone visuals
        CreateHitZoneVisuals();

        // Initialize particle pools
        InitializeParticlePools();
    }

    private void InitializeParticlePools()
    {
        // Pre-instantiate Perfect particles
        if (perfectHitEffectPrefab != null)
        {
            for (int i = 0; i < PARTICLE_POOL_SIZE; i++)
            {
                var particle = Instantiate(perfectHitEffectPrefab);
                particle.SetActive(false);
                perfectParticlePool.Enqueue(particle);
            }
        }

        // Pre-instantiate Good particles
        if (goodHitEffectPrefab != null)
        {
            for (int i = 0; i < PARTICLE_POOL_SIZE; i++)
            {
                var particle = Instantiate(goodHitEffectPrefab);
                particle.SetActive(false);
                goodParticlePool.Enqueue(particle);
            }
        }
    }

    private void InitializeHitColors()
    {
        hitColors = new Dictionary<HitAccuracy, Color>();
        
        if (uiConfig != null)
        {
            hitColors[HitAccuracy.Perfect] = uiConfig.successColor;
            hitColors[HitAccuracy.Good] = uiConfig.warningColor;
            hitColors[HitAccuracy.Okay] = uiConfig.textSecondaryColor;
        }
        else
        {
            // Fallback colors
            hitColors[HitAccuracy.Perfect] = new Color(0f, 1f, 1f, 0.8f);
            hitColors[HitAccuracy.Good] = new Color(0f, 1f, 0f, 0.8f);
            hitColors[HitAccuracy.Okay] = new Color(1f, 1f, 0f, 0.8f);
        }
    }

    private void AutoFindPrefabs()
    {
        if (hitZoneVisualPrefab == null)
        {
            hitZoneVisualPrefab = Resources.Load<GameObject>("Prefabs/Effects/Enhanced_HitZoneVisual");
            if (hitZoneVisualPrefab == null)
            {
                // Try alternative paths
                hitZoneVisualPrefab = Resources.Load<GameObject>("Enhanced_HitZoneVisual");
            }
        }

        if (perfectHitEffectPrefab == null)
        {
            perfectHitEffectPrefab = Resources.Load<GameObject>("Prefabs/Effects/PerfectHitEffect");
            if (perfectHitEffectPrefab == null)
            {
                perfectHitEffectPrefab = Resources.Load<GameObject>("PerfectHitEffect");
            }
        }

        if (goodHitEffectPrefab == null)
        {
            goodHitEffectPrefab = Resources.Load<GameObject>("Prefabs/Effects/GoodHitEffect");
            if (goodHitEffectPrefab == null)
            {
                goodHitEffectPrefab = Resources.Load<GameObject>("GoodHitEffect");
            }
        }

        if (missEffectPrefab == null)
        {
            missEffectPrefab = Resources.Load<GameObject>("Prefabs/Effects/MissEffect");
            if (missEffectPrefab == null)
            {
                missEffectPrefab = Resources.Load<GameObject>("MissEffect");
            }
        }

        // Summary of auto-assignment results
        int foundCount = 0;
        if (hitZoneVisualPrefab != null) foundCount++;
        if (perfectHitEffectPrefab != null) foundCount++;
        if (goodHitEffectPrefab != null) foundCount++;
        if (missEffectPrefab != null) foundCount++;

        // Debug.Log($"✅ HitZoneManager: Auto-found {foundCount}/4 prefabs - HitZone={hitZoneVisualPrefab != null}, Perfect={perfectHitEffectPrefab != null}, Good={goodHitEffectPrefab != null}, Miss={missEffectPrefab != null}");
    }

    private void CreateHitZoneVisuals()
    {
        if (hitZoneVisualPrefab == null)
        {
            // Debug.LogWarning("⚠️ HitZoneVisual prefab not found! Please assign it in the inspector or run the HitZone fixer tool.");
            return;
        }

        // Create visuals for each lane
        foreach (var zone in zones)
        {
            if (zone == null) continue;

            // Get the collider to match size
            BoxCollider collider = zone.GetComponent<BoxCollider>();
            if (collider == null) continue;

            // Create visual for this lane
            GameObject visual = Instantiate(hitZoneVisualPrefab, zone.transform);
            visual.name = $"HitZoneVisual_Lane{zone.laneIndex}";

            // Match position to collider center
            visual.transform.localPosition = collider.center;

            // Match rotation (90 degrees on X to face up)
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            // Match scale to collider size
            // The visual is a quad (plane), so we map:
            // - collider.size.x to visual.scale.x (width)
            // - collider.size.z to visual.scale.y (depth)
            visual.transform.localScale = new Vector3(
                collider.size.x * zone.transform.localScale.x,
                collider.size.z * zone.transform.localScale.z,
                1f
            );

            // Get and configure the renderer
            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Store reference for later use
                hitZoneRenderers[zone.laneIndex] = renderer;

                // Configure material
                if (renderer.material != null)
                {
                    // Set initial color (bright white with high alpha)
                    renderer.material.color = new Color(1f, 1f, 1f, 0.8f);

                    // Set bright emission for glow effect
                    if (renderer.material.HasProperty("_EmissionColor"))
                    {
                        renderer.material.SetColor("_EmissionColor", new Color(0.5f, 0.5f, 2f, 1f));
                        renderer.material.EnableKeyword("_EMISSION");
                    }

                    // Ensure proper transparency
                    if (renderer.material.HasProperty("_Surface"))
                    {
                        renderer.material.SetFloat("_Surface", 1); // Transparent
                        renderer.material.SetFloat("_Blend", 0);   // Alpha blend
                        renderer.material.renderQueue = 3000;
                    }
                }

                // Ensure it renders on top
                renderer.sortingOrder = 20;
            }

            // Debug.Log($"✅ Created hit zone visual for Lane {zone.laneIndex} matching collider size: {collider.size}");
        }
    }

    void OnEnable()
    {
        InputManager.OnLaneTapped += HandleLaneTap;
    }

    void OnDisable()
    {
        InputManager.OnLaneTapped -= HandleLaneTap;
    }

    void HandleLaneTap(int lane, Vector2 screenPos)
    {
        // STABILITY: Add error handling for hit evaluation
        try
        {
            EvaluateHit(lane, screenPos);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HitZoneManager: Error processing lane tap for lane {lane}: {e.Message}");
        }
    }

    void EvaluateHit(int lane, Vector2 screenPos)
    {
        if (lane < 0 || lane >= zones.Length) return;
        var zone = zones[lane];
        if (zone == null || zone.insideNotes.Count == 0) return;

        GameObject bestCandidate = null;
        double bestTimeDiff = double.MaxValue;
        NoteWrapper bestWrapper = null;

        foreach (var noteObj in zone.insideNotes)
        {
            if (noteObj == null) continue;
            var noteWrapper = noteObj.GetComponent<NoteWrapper>();
            if (noteWrapper == null) continue;

            double timeDiff = System.Math.Abs(AudioSettings.dspTime - noteWrapper.dspHitTime);
            if (timeDiff < bestTimeDiff)
            {
                bestTimeDiff = timeDiff;
                bestCandidate = noteObj;
                bestWrapper = noteWrapper;
            }
        }

        if (bestCandidate == null) return;

        double timeDiffMs = bestTimeDiff * 1000.0;
        HitAccuracy accuracy;

        if (timeDiffMs <= perfectWindowMs)
        {
            accuracy = HitAccuracy.Perfect;
        }
        else if (timeDiffMs <= goodWindowMs)
        {
            accuracy = HitAccuracy.Good;
        }
        else if (timeDiffMs <= okayWindowMs)
        {
            accuracy = HitAccuracy.Okay;
        }
        else
        {
            return;
        }

        ProcessSuccessfulHit(zone, bestCandidate, bestWrapper.gameNoteInfo, accuracy, screenPos);
    }

    void ProcessSuccessfulHit(HitZoneTrigger zone, GameObject noteObj, GameNoteInfo noteInfo, HitAccuracy acc, Vector2 screenPos)
    {
        // 1. Remove note from trigger list (ANTI-DOUBLE HIT - CRITICAL)
        zone.RemoveNote(noteObj);

        // 2. Get note animator and PLAY HIT ANIMATION
        var animator = noteObj.GetComponent<NoteAnimator>();
        if (animator != null)
        {
            animator.AnimateHit(acc);
        }
        else
        {
            Destroy(noteObj);
        }

        // 3. Trigger sound and music systems (ENHANCED - Centralized Volume)
        if (noteInfo != null)
        {
            // Play audio directly with centralized volume calculation
            var instrument = GameManager.Instance != null ? GameManager.Instance.GetSelectedInstrument() : InstrumentType.Piano;
            // Use centralized volume calculation based on note duration
            float calculatedVolume = AudioManager.Instance?.CalculateNoteVolume(noteInfo.duration) ?? 1.0f;
            AudioManager.Instance?.PlayNote(instrument, noteInfo.pitch, volume: calculatedVolume, useJavaMapping: true, line: noteInfo.line, noteDuration: noteInfo.duration);

            // Notify IMS for musical analysis only
            InteractiveMusicSystem.Instance?.ProcessChartNoteHit(noteInfo);
        }

        // 4. Spawn proper particle effect
        SpawnParticleEffect(noteObj.transform.position, acc);

        // 5. Flash hit zone with appropriate color
        FlashHitZone(acc);

        // 6. Update score using configurable point values
        int points = acc switch
        {
            HitAccuracy.Perfect => perfectHitPoints,
            HitAccuracy.Good => goodHitPoints,
            _ => okayHitPoints // Okay hit
        };
        GameManager.Instance?.UpdateScore(points);
        
        // Update stats only when hit occurs (performance optimization)
        var gameplayManager = FindFirstObjectByType<GameplayManager>();
        gameplayManager?.UpdateStatsOnHit();
    }

    private void SpawnParticleEffect(Vector3 position, HitAccuracy accuracy)
    {
        // PERF FIX: Use pooled particles instead of Instantiate
        GameObject effect = GetPooledParticle(accuracy);
        
        if (effect != null)
        {
            effect.transform.position = position;
            effect.transform.rotation = Quaternion.identity;
            effect.SetActive(true);
            
            // Auto-return to pool after particle finishes
            var particleSystem = effect.GetComponent<ParticleSystem>();
            if (particleSystem != null)
            {
                _ = ReturnParticleAfterDelay(effect, accuracy, particleSystem.main.duration + 0.5f);
            }
            else
            {
                // Fallback: return after 2 seconds
                _ = ReturnParticleAfterDelay(effect, accuracy, 2f);
            }
        }
    }

    private GameObject GetPooledParticle(HitAccuracy accuracy)
    {
        Queue<GameObject> pool = accuracy == HitAccuracy.Perfect ? perfectParticlePool : goodParticlePool;
        GameObject prefab = accuracy == HitAccuracy.Perfect ? perfectHitEffectPrefab : goodHitEffectPrefab;
        
        // Try to get from pool
        while (pool.Count > 0)
        {
            var particle = pool.Dequeue();
            if (particle != null) return particle;
        }
        
        // Pool empty - create new (fallback)
        if (prefab != null)
        {
            return Instantiate(prefab);
        }
        return null;
    }

    private async Awaitable ReturnParticleAfterDelay(GameObject particle, HitAccuracy accuracy, float delay)
    {
        await Awaitable.WaitForSecondsAsync(delay);
        
        if (particle != null)
        {
            particle.SetActive(false);
            Queue<GameObject> pool = accuracy == HitAccuracy.Perfect ? perfectParticlePool : goodParticlePool;
            pool.Enqueue(particle);
        }
    }

    private void FlashHitZone(HitAccuracy accuracy)
    {
        Color flashColor = hitColors.ContainsKey(accuracy) ? hitColors[accuracy] : 
            (uiConfig != null ? uiConfig.textPrimaryColor : Color.white);

        // Flash all hit zones with the specified color
        foreach (var renderer in hitZoneRenderers.Values)
        {
            if (renderer == null) continue;

            // Use DOTween for a smooth, punchy flash effect
            renderer.material.DOKill(); // Kill previous tweens on this material
            renderer.material.SetColor("_EmissionColor", flashColor * 2f); // Make it glow intensely
            renderer.material.DOColor(uiConfig != null ? uiConfig.textPrimaryColor : Color.white, "_EmissionColor", 0.5f)
                .SetEase(Ease.OutQuad);
        }

        // Log the hit zone flash event
        // Debug.Log($"💫 Hit zone {accuracy} flashed with {accuracy} color: {flashColor}");
    }

    // Debug helper
    [ContextMenu("Test Perfect Hit Effect")]
    private void TestPerfectHitEffect()
    {
        SpawnParticleEffect(transform.position, HitAccuracy.Perfect);
        FlashHitZone(HitAccuracy.Perfect);
    }

    [ContextMenu("Test Good Hit Effect")]
    private void TestGoodHitEffect()
    {
        SpawnParticleEffect(transform.position, HitAccuracy.Good);
        FlashHitZone(HitAccuracy.Good);
    }

    /// <summary>
    /// Public metot - Test scriptleri için particle prefab'ını döndürür
    /// Reflection kullanımını önlemek için eklendi
    /// </summary>
    public GameObject GetParticlePrefabForAccuracy(HitAccuracy accuracy)
    {
        return accuracy switch
        {
            HitAccuracy.Perfect => perfectHitEffectPrefab,
            HitAccuracy.Good => goodHitEffectPrefab,
            HitAccuracy.Okay => goodHitEffectPrefab, // Okay için de Good efekti kullanılıyor
            HitAccuracy.Miss => missEffectPrefab,
            _ => null
        };
    }
}

/// <summary>
/// Wrapper attached to note prefab giving expectedHitTime populated by NoteRenderer
/// so HitZoneManager can judge timing without heavy calculation.
/// </summary>
public class NoteWrapper : MonoBehaviour
{
    // The precise DSP time when this note is expected to be perfectly hit.
    public double dspHitTime;
    public GameNoteInfo gameNoteInfo;
}