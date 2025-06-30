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
    [Header("Timing Windows (milliseconds)")]
    public float perfectWindow = 100f;
    public float goodWindow = 200f;

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

        // Auto-hit for held fingers
        if (InputManager.Instance == null) return;
        foreach (int lane in InputManager.Instance.GetActiveLanes())
        {
            TryAutoHit(lane);
        }
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
        if (zone == null) return;

        var noteObj = zone.PeekEarliestNote();
        if (noteObj == null) return;

        var noteWrapper = noteObj.GetComponent<NoteWrapper>();
        if (noteWrapper == null || noteWrapper.gameNoteInfo == null)
        {
            Debug.LogWarning($"[HitZoneManager] Note in lane {lane} has no valid note info. Ignoring tap.");
            return;
        }

        // SIMPLE: If note is in trigger zone, it's a hit! No timing check.
        Debug.Log($"[HitZoneManager] HIT! Lane {lane} - Note is in trigger zone.");
        ProcessSuccessfulHit(zone, noteObj, noteWrapper.gameNoteInfo, HitAccuracy.Good, screenPos);
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
            _ => 50
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