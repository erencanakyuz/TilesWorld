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
    private Dictionary<int, Material> hitZoneMaterials = new Dictionary<int, Material>();
    private Dictionary<HitAccuracy, Color> hitColors;
    private UIConfig uiConfig;

    // PARTICLE POOLING - Reduces GC spikes during heavy note density
    private Queue<GameObject> perfectParticlePool = new Queue<GameObject>();
    private Queue<GameObject> goodParticlePool = new Queue<GameObject>();
    private const int PARTICLE_POOL_SIZE = 30;
    [SerializeField] private int maxParticlePoolSize = 64;
    [SerializeField] private int maxActiveParticlesPerType = 24;
    private int perfectParticleTotal = 0;
    private int goodParticleTotal = 0;
    private int activePerfectParticles = 0;
    private int activeGoodParticles = 0;
    private GameplayManager gameplayManager;

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

        // Cache gameplay manager once to avoid per-hit searches
        gameplayManager = FindFirstObjectByType<GameplayManager>();
    }

    void Start()
    {
        if (gameplayManager == null)
        {
            gameplayManager = FindFirstObjectByType<GameplayManager>();
        }
    }

    private void InitializeParticlePools()
    {
        // Pre-instantiate Perfect particles
        if (perfectHitEffectPrefab != null)
        {
            for (int i = 0; i < PARTICLE_POOL_SIZE; i++)
            {
                var particle = Instantiate(perfectHitEffectPrefab);
                PreparePooledParticle(particle);
                particle.SetActive(false);
                perfectParticlePool.Enqueue(particle);
                perfectParticleTotal++;
            }
        }

        // Pre-instantiate Good particles
        if (goodHitEffectPrefab != null)
        {
            for (int i = 0; i < PARTICLE_POOL_SIZE; i++)
            {
                var particle = Instantiate(goodHitEffectPrefab);
                PreparePooledParticle(particle);
                particle.SetActive(false);
                goodParticlePool.Enqueue(particle);
                goodParticleTotal++;
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
                var materialInstance = renderer.material;
                if (materialInstance != null)
                {
                    // Set initial color (bright white with high alpha)
                    materialInstance.color = new Color(1f, 1f, 1f, 0.8f);

                    // Set bright emission for glow effect
                    if (materialInstance.HasProperty("_EmissionColor"))
                    {
                        materialInstance.SetColor("_EmissionColor", new Color(0.5f, 0.5f, 2f, 1f));
                        materialInstance.EnableKeyword("_EMISSION");
                    }

                    // Ensure proper transparency
                    if (materialInstance.HasProperty("_Surface"))
                    {
                        materialInstance.SetFloat("_Surface", 1); // Transparent
                        materialInstance.SetFloat("_Blend", 0);   // Alpha blend
                        materialInstance.renderQueue = 3000;
                    }

                    // Cache material instance to avoid per-hit allocations
                    hitZoneMaterials[zone.laneIndex] = materialInstance;
                }

                // Ensure it renders on top
                renderer.sortingOrder = 20;
            }

            // Debug.Log($"✅ Created hit zone visual for Lane {zone.laneIndex} matching collider size: {collider.size}");
        }
    }

    void OnEnable()
    {
        // Use timestamped events for DSP-accurate hit judgment
        InputManager.OnLaneTappedTimestamped += HandleLaneTapTimestamped;
    }

    void OnDisable()
    {
        InputManager.OnLaneTappedTimestamped -= HandleLaneTapTimestamped;
    }

    void HandleLaneTapTimestamped(int lane, Vector2 screenPos, double inputTime)
    {
        // STABILITY: Add error handling for hit evaluation
        try
        {
            EvaluateHitWithTimestamp(lane, screenPos, inputTime);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"HitZoneManager: Error processing lane tap for lane {lane}: {e.Message}");
        }
    }

    // Legacy handler for backward compatibility
    void HandleLaneTap(int lane, Vector2 screenPos)
    {
        HandleLaneTapTimestamped(lane, screenPos, Time.realtimeSinceStartupAsDouble);
    }

    /// <summary>
    /// DSP-accurate hit evaluation using input timestamp from RhythmTimingSystem.
    /// </summary>
    void EvaluateHitWithTimestamp(int lane, Vector2 screenPos, double inputTime)
    {
        if (lane < 0 || lane >= zones.Length) return;
        var zone = zones[lane];
        if (zone == null || zone.insideNotes.Count == 0) return;

        // Convert input timestamp to DSP time for accurate comparison
        double inputDspTime;
        if (RhythmTimingSystem.Instance != null && RhythmTimingSystem.Instance.IsSongPlaying)
        {
            // Use RhythmTimingSystem for accurate DSP conversion
            double rtNow = Time.realtimeSinceStartupAsDouble;
            double dspNow = AudioSettings.dspTime;
            double timeSinceInput = rtNow - inputTime;
            inputDspTime = dspNow - timeSinceInput;
        }
        else
        {
            // Fallback to current DSP time
            inputDspTime = AudioSettings.dspTime;
        }

        GameObject bestCandidate = null;
        double bestTimeDiff = double.MaxValue;
        NoteWrapper bestWrapper = null;

        foreach (var noteObj in zone.insideNotes)
        {
            if (noteObj == null) continue;
            var noteWrapper = noteObj.GetComponent<NoteWrapper>();
            if (noteWrapper == null) continue;

            // Use input DSP time instead of current DSP time
            double timeDiff = System.Math.Abs(inputDspTime - noteWrapper.dspHitTime);
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

    // Legacy evaluation method for backward compatibility
    void EvaluateHit(int lane, Vector2 screenPos)
    {
        EvaluateHitWithTimestamp(lane, screenPos, Time.realtimeSinceStartupAsDouble);
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
        FlashHitZone(zone.laneIndex, acc);

        // 6. Update score using configurable point values
        int points = acc switch
        {
            HitAccuracy.Perfect => perfectHitPoints,
            HitAccuracy.Good => goodHitPoints,
            _ => okayHitPoints // Okay hit
        };
        GameManager.Instance?.UpdateScore(points);
        
        // Update stats only when hit occurs (performance optimization)
        gameplayManager?.UpdateStatsOnHit();
    }

    private void SpawnParticleEffect(Vector3 position, HitAccuracy accuracy)
    {
        if (!CanSpawnParticle(accuracy))
        {
            return;
        }

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
                var pooled = effect.GetComponent<PooledHitParticle>();
                if (pooled != null)
                {
                    pooled.Initialize(this, accuracy, effect);
                }
                particleSystem.Play(true);
            }
            else
            {
                var pooled = effect.GetComponentInChildren<PooledHitParticle>();
                if (pooled != null)
                {
                    pooled.Initialize(this, accuracy, effect);
                }
                var childPs = effect.GetComponentInChildren<ParticleSystem>();
                if (childPs != null)
                {
                    childPs.Play(true);
                }
            }

            if (accuracy == HitAccuracy.Perfect) activePerfectParticles++;
            else activeGoodParticles++;
        }
    }

    public void SpawnHitEffect(Vector3 position, HitAccuracy accuracy)
    {
        SpawnParticleEffect(position, accuracy);
    }

    private GameObject GetPooledParticle(HitAccuracy accuracy)
    {
        Queue<GameObject> pool = accuracy == HitAccuracy.Perfect ? perfectParticlePool : goodParticlePool;
        GameObject prefab = accuracy == HitAccuracy.Perfect ? perfectHitEffectPrefab : goodHitEffectPrefab;
        
        // Try to get from pool
        while (pool.Count > 0)
        {
            var particle = pool.Dequeue();
            if (particle != null)
            {
                PreparePooledParticle(particle);
                return particle;
            }
        }
        
        // Pool empty - create new (fallback) up to max cap
        if (prefab != null)
        {
            if (accuracy == HitAccuracy.Perfect && perfectParticleTotal >= maxParticlePoolSize)
            {
                return null;
            }
            if (accuracy != HitAccuracy.Perfect && goodParticleTotal >= maxParticlePoolSize)
            {
                return null;
            }

            var particle = Instantiate(prefab);
            PreparePooledParticle(particle);
            if (accuracy == HitAccuracy.Perfect) perfectParticleTotal++;
            else goodParticleTotal++;
            return particle;
        }
        return null;
    }

    public void ReturnParticleToPool(GameObject particle, HitAccuracy accuracy)
    {
        if (particle == null) return;
        var particleSystem = particle.GetComponent<ParticleSystem>() ?? particle.GetComponentInChildren<ParticleSystem>();
        if (particleSystem != null)
        {
            if (!particleSystem.isStopped)
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            particleSystem.Clear(true);
        }
        particle.SetActive(false);
        Queue<GameObject> pool = accuracy == HitAccuracy.Perfect ? perfectParticlePool : goodParticlePool;
        pool.Enqueue(particle);

        if (accuracy == HitAccuracy.Perfect)
        {
            activePerfectParticles = Mathf.Max(0, activePerfectParticles - 1);
        }
        else
        {
            activeGoodParticles = Mathf.Max(0, activeGoodParticles - 1);
        }
    }

    private void PreparePooledParticle(GameObject particle)
    {
        if (particle == null) return;

        var autoDestroy = particle.GetComponent<ParticleAutoDestroy>();
        if (autoDestroy != null)
        {
            Destroy(autoDestroy);
        }

        var particleSystem = particle.GetComponent<ParticleSystem>() ?? particle.GetComponentInChildren<ParticleSystem>();
        if (particleSystem != null)
        {
            var main = particleSystem.main;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.Callback;
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        var pooled = particleSystem != null ? particleSystem.GetComponent<PooledHitParticle>() : particle.GetComponent<PooledHitParticle>();
        if (pooled == null)
        {
            if (particleSystem != null)
            {
                pooled = particleSystem.gameObject.AddComponent<PooledHitParticle>();
            }
            else
            {
                pooled = particle.AddComponent<PooledHitParticle>();
            }
        }
        // Accuracy will be set on spawn since pools are per-accuracy
    }

    private void FlashHitZone(HitAccuracy accuracy)
    {
        Color flashColor = hitColors.ContainsKey(accuracy) ? hitColors[accuracy] : 
            (uiConfig != null ? uiConfig.textPrimaryColor : Color.white);

        // Flash all hit zones with the specified color
        foreach (var laneIndex in hitZoneRenderers.Keys)
        {
            FlashHitZone(laneIndex, accuracy);
        }

        // Log the hit zone flash event
        // Debug.Log($"💫 Hit zone {accuracy} flashed with {accuracy} color: {flashColor}");
    }

    private void FlashHitZone(int laneIndex, HitAccuracy accuracy)
    {
        if (!hitZoneMaterials.TryGetValue(laneIndex, out var material) || material == null) return;

        Color flashColor = hitColors.ContainsKey(accuracy) ?
            hitColors[accuracy] :
            (uiConfig != null ? uiConfig.textPrimaryColor : Color.white);

        // Use DOTween for a smooth, punchy flash effect
        material.DOKill(); // Kill previous tweens on this material
        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", flashColor * 2f); // Make it glow intensely
            material.DOColor(uiConfig != null ? uiConfig.textPrimaryColor : Color.white, "_EmissionColor", 0.5f)
                .SetEase(Ease.OutQuad);
        }
        else
        {
            material.color = flashColor;
        }
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

    public void GetParticlePoolCounts(out int perfectPoolCount, out int goodPoolCount)
    {
        perfectPoolCount = perfectParticlePool != null ? perfectParticlePool.Count : 0;
        goodPoolCount = goodParticlePool != null ? goodParticlePool.Count : 0;
    }

    public void GetParticleCounts(out int perfectPoolCount, out int goodPoolCount, out int perfectActiveCount, out int goodActiveCount)
    {
        GetParticlePoolCounts(out perfectPoolCount, out goodPoolCount);
        perfectActiveCount = activePerfectParticles;
        goodActiveCount = activeGoodParticles;
    }

    private bool CanSpawnParticle(HitAccuracy accuracy)
    {
        if (accuracy == HitAccuracy.Perfect)
        {
            return activePerfectParticles < maxActiveParticlesPerType;
        }
        return activeGoodParticles < maxActiveParticlesPerType;
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
