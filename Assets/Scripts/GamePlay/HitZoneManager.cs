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
    [Header("🎯 Position-Based Hit Windows (Z-axis distance)")]
    [Tooltip("Z-distance from hit line for a 'Perfect' hit. From oldgame.md: 0.3 units is a good start.")]
    public float perfectWindowZ = 0.3f;
    [Tooltip("Z-distance from hit line for a 'Good' hit. From oldgame.md: 0.6 units is a good start.")]
    public float goodWindowZ = 0.6f;

    [Header("CONFIGURATION")]
    [Tooltip("The ideal Z-position for a note to be hit. Must match NoteRenderer's hitZoneZ.")]
    public float hitLineZ = 0.0f;

    [Tooltip("Reference to active AudioManager clock (optional). If null, Time.time will be used.")]
    public AudioManager audioManager;

    // Internal
    private HitZoneTrigger[] zones;

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

        // AUTO-HIT DEVRE DIŞI - Bu sürekli tetiklemeye neden oluyor
        // Auto-hit for held fingers
        // if (InputManager.Instance == null) return;
        // foreach (int lane in InputManager.Instance.GetActiveLanes())
        // {
        //     TryAutoHit(lane);
        // }
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

        // Find the note closest to the hit line (z=0)
        GameObject bestCandidate = null;
        float minDistance = float.MaxValue;

        // Iterate backwards to avoid issues if a note is somehow removed during iteration
        for (int i = zone.insideNotes.Count - 1; i >= 0; i--)
        {
            var noteObj = zone.insideNotes[i];
            if (noteObj == null) continue;

            float distance = Mathf.Abs(noteObj.transform.position.z - hitLineZ);
            if (distance < minDistance)
            {
                minDistance = distance;
                bestCandidate = noteObj;
            }
        }

        if (bestCandidate == null) return;

        var noteWrapper = bestCandidate.GetComponent<NoteWrapper>();
        if (noteWrapper == null || noteWrapper.gameNoteInfo == null)
        {
            Debug.LogWarning($"[HitZoneManager] Best candidate note in lane {lane} has no valid info. Ignoring tap.");
            return;
        }

        // Determine accuracy based on Z-position distance
        HitAccuracy accuracy;
        if (minDistance <= perfectWindowZ)
        {
            accuracy = HitAccuracy.Perfect;
        }
        else if (minDistance <= goodWindowZ)
        {
            accuracy = HitAccuracy.Good;
        }
        else
        {
            // Any note inside the trigger but outside the 'Good' window is 'Okay'
            accuracy = (HitAccuracy)2; // Assuming 'Okay' or a third value exists. If not, this will be Good.
        }

        Debug.Log($"[HitZoneManager] HIT! Lane {lane} - Note '{bestCandidate.name}' with accuracy {accuracy} (Z-dist: {minDistance:F2})");
        ProcessSuccessfulHit(zone, bestCandidate, noteWrapper.gameNoteInfo, accuracy, screenPos);
    }

    float noteWrapperFallback(GameObject noteObj)
    {
        // If no wrapper, approximate using Z position vs speed. Fallback only.
        return Time.time;
    }

    void ProcessSuccessfulHit(HitZoneTrigger zone, GameObject noteObj, GameNoteInfo noteInfo, HitAccuracy acc, Vector2 screenPos)
    {
        // Remove & recycle note
        zone.RemoveNote(noteObj);
        noteObj.SetActive(false);

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