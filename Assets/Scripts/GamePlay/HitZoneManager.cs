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
    private Renderer hitZoneRenderer;
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

        // Create hit zone visual
        CreateHitZoneVisual();
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

    private void CreateHitZoneVisual()
    {
        if (hitZoneVisualPrefab != null)
        {
            GameObject visual = Instantiate(hitZoneVisualPrefab, new Vector3(0, 0.1f, hitLineZ), Quaternion.identity, transform);
            hitZoneRenderer = visual.GetComponent<Renderer>();

            // Ensure visibility
            if (hitZoneRenderer != null)
            {
                // Set initial color with high visibility
                if (hitZoneRenderer.material != null)
                {
                    hitZoneRenderer.material.color = new Color(1f, 1f, 1f, 0.8f);
                    if (hitZoneRenderer.material.HasProperty("_EmissionColor"))
                    {
                        hitZoneRenderer.material.SetColor("_EmissionColor", new Color(0.5f, 0.5f, 1f, 1f));
                    }
                }

                // Ensure it renders on top
                hitZoneRenderer.sortingOrder = 10;
            }

            Debug.Log("✅ Hit zone visual created and should be visible!");
        }
        else
        {
            Debug.LogWarning("⚠️ HitZoneVisual prefab not found! Please assign it in the inspector or run the HitZone fixer tool.");
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
        if (hitZoneRenderer == null || !hitColors.ContainsKey(accuracy)) return;

        Color targetColor = hitColors[accuracy];

        // Kill any ongoing color animation
        hitZoneRenderer.material.DOKill();

        // Create a more visible flash sequence
        Sequence seq = DOTween.Sequence();

        // Flash to bright color
        seq.Append(hitZoneRenderer.material.DOColor(targetColor, "_BaseColor", 0.1f));

        // Flash back to normal
        seq.Append(hitZoneRenderer.material.DOColor(new Color(1f, 1f, 1f, 0.8f), "_BaseColor", 0.3f));

        // Also animate emission for extra visibility
        if (hitZoneRenderer.material.HasProperty("_EmissionColor"))
        {
            Color emissionColor = targetColor * 3f; // Make emission very bright
            seq.Join(hitZoneRenderer.material.DOColor(emissionColor, "_EmissionColor", 0.1f));
            seq.Append(hitZoneRenderer.material.DOColor(new Color(0.5f, 0.5f, 1f, 1f), "_EmissionColor", 0.3f));
        }

        Debug.Log($"💫 Hit zone flashed with {accuracy} color: {targetColor}");
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