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
    private Dictionary<HitAccuracy, Color> hitColors = new Dictionary<HitAccuracy, Color>()
    {
        { HitAccuracy.Perfect, new Color(0f, 1f, 1f, 0.8f) }, // Cyan
        { HitAccuracy.Good, new Color(0f, 1f, 0f, 0.8f) },    // Green
        { HitAccuracy.Okay, new Color(1f, 1f, 0f, 0.8f) }     // Yellow
    };

    void Awake()
    {
        zones = FindObjectsByType<HitZoneTrigger>(FindObjectsSortMode.None);
        System.Array.Sort(zones, (a, b) => a.laneIndex.CompareTo(b.laneIndex));

        if (audioManager == null) audioManager = AudioManager.Instance;

        // Auto-find prefabs if enabled and not assigned
        if (autoFindPrefabs)
        {
            AutoFindPrefabs();
        }

        // Create hit zone visuals
        CreateHitZoneVisuals();
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

        Debug.Log($"✅ HitZoneManager: Auto-found {foundCount}/4 prefabs - HitZone={hitZoneVisualPrefab != null}, Perfect={perfectHitEffectPrefab != null}, Good={goodHitEffectPrefab != null}, Miss={missEffectPrefab != null}");
    }

    private void CreateHitZoneVisuals()
    {
        if (hitZoneVisualPrefab == null)
        {
            Debug.LogWarning("⚠️ HitZoneVisual prefab not found! Please assign it in the inspector or run the HitZone fixer tool.");
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

            Debug.Log($"✅ Created hit zone visual for Lane {zone.laneIndex} matching collider size: {collider.size}");
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
        EvaluateHit(lane, screenPos);
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

        // 3. Trigger sound and music systems (UNCHANGED)
        if (noteInfo != null)
        {
            InteractiveMusicSystem.Instance?.PlayNoteFromChart(noteInfo);
        }

        // 4. Spawn proper particle effect
        SpawnParticleEffect(noteObj.transform.position, acc);

        // 5. Flash hit zone with appropriate color
        FlashHitZone(acc);

        // 6. Update score (UNCHANGED)
        int points = acc switch
        {
            HitAccuracy.Perfect => 300,
            HitAccuracy.Good => 100,
            _ => 50 // Okay hit
        };
        GameManager.Instance?.UpdateScore(points);
    }

    private void SpawnParticleEffect(Vector3 position, HitAccuracy accuracy)
    {
        GameObject prefabToSpawn = accuracy switch
        {
            HitAccuracy.Perfect => perfectHitEffectPrefab,
            HitAccuracy.Good => goodHitEffectPrefab,
            _ => goodHitEffectPrefab // Use good effect for okay hits too
        };

        if (prefabToSpawn != null)
        {
            // Instantiate the particle effect
            GameObject effect = Instantiate(prefabToSpawn, position, Quaternion.identity);

            // The ParticleAutoDestroy component will handle cleanup
            Debug.Log($"✨ Spawned {accuracy} particle effect at {position}");
        }
        else
        {
            Debug.LogWarning($"⚠️ No particle effect prefab assigned for {accuracy} hit!");
        }
    }

    private void FlashHitZone(HitAccuracy accuracy)
    {
        // Flash all lane visuals with the accuracy color
        foreach (var kvp in hitZoneRenderers)
        {
            Renderer renderer = kvp.Value;
            if (renderer != null && hitColors.ContainsKey(accuracy))
            {
                Color targetColor = hitColors[accuracy];

                // Kill any ongoing color animation
                renderer.material.DOKill();

                // Create a more visible flash sequence
                Sequence seq = DOTween.Sequence();

                // Flash to bright color
                seq.Append(renderer.material.DOColor(targetColor, "_BaseColor", 0.1f));

                // Flash back to normal
                seq.Append(renderer.material.DOColor(new Color(1f, 1f, 1f, 0.8f), "_BaseColor", 0.3f));

                // Also animate emission for extra visibility
                if (renderer.material.HasProperty("_EmissionColor"))
                {
                    Color emissionColor = targetColor * 3f; // Make emission very bright
                    seq.Join(renderer.material.DOColor(emissionColor, "_EmissionColor", 0.1f));
                    seq.Append(renderer.material.DOColor(new Color(0.5f, 0.5f, 1f, 1f), "_EmissionColor", 0.3f));
                }

                Debug.Log($"💫 Hit zone {kvp.Key} flashed with {accuracy} color: {targetColor}");
            }
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