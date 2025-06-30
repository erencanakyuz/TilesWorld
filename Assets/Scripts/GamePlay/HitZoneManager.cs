using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    public float perfectWindowMs = 50f;
    [Tooltip("Time window in MS for a 'Good' hit.")]
    public float goodWindowMs = 100f;
    [Tooltip("Time window in MS for an 'Okay' hit. Taps outside this are misses.")]
    public float okayWindowMs = 150f;

    [Header("CONFIGURATION")]
    [Tooltip("The ideal Z-position for a note to be hit. Must match NoteRenderer's hitZoneZ.")]
    public float hitLineZ = 0.0f;

    [Tooltip("Reference to active AudioManager clock (optional). If null, Time.time will be used.")]
    public AudioManager audioManager;

    [Tooltip("Reference to the NoteRenderer to calculate note travel time for misses.")]
    [SerializeField] private NoteRenderer noteRenderer;

    // Internal
    private HitZoneTrigger[] zones;
    private float noteTravelTime;

    void Awake()
    {
        // Find all triggers in the scene
        zones = FindObjectsByType<HitZoneTrigger>(FindObjectsSortMode.None);

        // IMPORTANT: Sort the zones array by laneIndex to ensure zones[i] corresponds to lane i
        System.Array.Sort(zones, (a, b) => a.laneIndex.CompareTo(b.laneIndex));

        // Optional: Log the sorted order to confirm
        for (int i = 0; i < zones.Length; i++)
        {
            if (zones[i].laneIndex != i)
            {
                Debug.LogWarning($"[HitZoneManager] Zone sorting mismatch: zones[{i}] has laneIndex {zones[i].laneIndex}");
            }
        }

        if (audioManager == null) audioManager = AudioManager.Instance;
        if (noteRenderer == null) noteRenderer = FindFirstObjectByType<NoteRenderer>();
        if (noteRenderer != null)
        {
            noteTravelTime = noteRenderer.GetNoteTravelTime();
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

    void Update()
    {
        // Lazy-resolve AudioManager in case it was spawned after this component.
        if (audioManager == null)
            audioManager = AudioManager.Instance != null ? AudioManager.Instance : FindFirstObjectByType<AudioManager>();

        // Auto-hit for held fingers was disabled as it caused constant re-triggering.
        // The current system requires a distinct tap for each hit.
        /*
        if (InputManager.Instance == null) return;
        foreach (int lane in InputManager.Instance.GetActiveLanes())
        {
            TryAutoHit(lane);
        }
        */
    }

    void HandleLaneTap(int lane, Vector2 screenPos)
    {
        EvaluateHit(lane, screenPos);
    }

    void TryAutoHit(int lane)
    {
        EvaluateHit(lane, Vector2.zero, auto: true);
    }

    void EvaluateHit(int lane, Vector2 screenPos, bool auto = false)
    {
        if (lane < 0 || lane >= zones.Length) return;
        var zone = zones[lane];
        if (zone == null || zone.insideNotes.Count == 0) return;

        // In a time-based system, we find the note with the smallest time difference to its expected hit time.
        GameObject bestCandidate = null;
        float bestTimeDiff = float.MaxValue;
        NoteWrapper bestWrapper = null;

        foreach (var noteObj in zone.insideNotes)
        {
            if (noteObj == null) continue;
            var noteWrapper = noteObj.GetComponent<NoteWrapper>();
            if (noteWrapper == null) continue;

            float timeDiff = Mathf.Abs(Time.time - noteWrapper.expectedHitTime);
            if (timeDiff < bestTimeDiff)
            {
                bestTimeDiff = timeDiff;
                bestCandidate = noteObj;
                bestWrapper = noteWrapper;
            }
        }

        if (bestCandidate == null) return;

        // Convert the time difference to milliseconds for judgement.
        float timeDiffMs = bestTimeDiff * 1000f;
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
            // The tap was too early or too late for the closest note, so it's a miss.
            // We don't process it as a hit.
            return;
        }

        // --- [TIMING CHECK] Log ---
        Debug.Log($"[TIMING CHECK] Hit on Lane {lane} | Time Diff: {timeDiffMs:F1}ms | Accuracy: {accuracy}");
        // -------------------------

        ProcessSuccessfulHit(zone, bestCandidate, bestWrapper.gameNoteInfo, accuracy, screenPos);
    }

    float noteWrapperFallback(GameObject noteObj)
    {
        // If no wrapper, approximate using Z position vs speed. Fallback only.
        return Time.time;
    }

    void ProcessSuccessfulHit(HitZoneTrigger zone, GameObject noteObj, GameNoteInfo noteInfo, HitAccuracy acc, Vector2 screenPos)
    {
        // Tell the trigger to remove this note from its internal list FIRST to prevent double-hits.
        zone.RemoveNote(noteObj);

        // Return the note to the pool instead of destroying it
        if (noteRenderer != null)
        {
            noteRenderer.ProcessHitNote(noteObj);
        }
        else
        {
            // Fallback if renderer is not found (should not happen)
            Destroy(noteObj);
        }

        // Play audio & musical logic
        if (noteInfo != null)
        {
            InteractiveMusicSystem.Instance?.PlayNoteFromChart(noteInfo);
        }

        // UI feedback
        UIManager.Instance?.ShowHitEffect(acc, screenPos);

        // Score
        int points = acc switch
        {
            HitAccuracy.Perfect => 300,
            HitAccuracy.Good => 100,
            _ => 50 // This will be our "Okay" hit
        };
        GameManager.Instance?.UpdateScore(points);
    }
}

/// <summary>
/// Wrapper attached to note prefab giving expectedHitTime populated by NoteRenderer
/// so HitZoneManager can judge timing without heavy calculation.
/// </summary>
public class NoteWrapper : MonoBehaviour
{
    public float expectedHitTime; // seconds (AudioManager clock)
    public GameNoteInfo gameNoteInfo;
}