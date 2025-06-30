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

        Debug.Log($"🎯 [TIMING DEBUG] === LANE {lane} HIT EVALUATION ===");
        Debug.Log($"   📦 Notes in trigger zone: {zone.insideNotes.Count}");

        // Find the note closest to the hit line (z=0)
        GameObject bestCandidate = null;
        float minDistance = float.MaxValue;

        // *** DEBUG: Her notanın Z pozisyonunu göster ***
        Debug.Log($"   🔍 Checking all notes in zone:");
        for (int i = zone.insideNotes.Count - 1; i >= 0; i--)
        {
            var noteObj = zone.insideNotes[i];
            if (noteObj == null) continue;

            float noteZ = noteObj.transform.position.z;
            float distance = Mathf.Abs(noteZ - hitLineZ);
            Debug.Log($"     Note {i}: Z={noteZ:F3}, Distance from hit line={distance:F3}");

            if (distance < minDistance)
            {
                minDistance = distance;
                bestCandidate = noteObj;
                Debug.Log($"     ⭐ New best candidate! Distance={distance:F3}");
            }
        }

        if (bestCandidate == null)
        {
            Debug.Log($"   ❌ No valid candidate found!");
            return;
        }

        var noteWrapper = bestCandidate.GetComponent<NoteWrapper>();
        if (noteWrapper == null || noteWrapper.gameNoteInfo == null)
        {
            Debug.LogWarning($"[HitZoneManager] Best candidate note in lane {lane} has no valid info. Ignoring tap.");
            return;
        }

        // *** TIMING SİSTEMİNİN DETAYLI AÇIKLAMASI ***
        Debug.Log($"📊 [TIMING SYSTEM EXPLANATION]:");
        Debug.Log($"   🎯 Hit Line Z: {hitLineZ:F3} (player's position)");
        Debug.Log($"   📍 Best Note Z: {bestCandidate.transform.position.z:F3}");
        Debug.Log($"   📏 Distance: {minDistance:F3}");
        Debug.Log($"   🎪 Perfect Window: ≤{perfectWindowZ:F3}");
        Debug.Log($"   👍 Good Window: ≤{goodWindowZ:F3}");
        Debug.Log($"   💡 NASIL ÇALIŞIR: Nota hit line'a ne kadar yakınsa o kadar perfect!");

        // Determine accuracy based on Z-position distance
        HitAccuracy accuracy;
        if (minDistance <= perfectWindowZ)
        {
            accuracy = HitAccuracy.Perfect;
            Debug.Log($"   🏆 RESULT: PERFECT! (distance {minDistance:F3} ≤ {perfectWindowZ:F3})");
        }
        else if (minDistance <= goodWindowZ)
        {
            accuracy = HitAccuracy.Good;
            Debug.Log($"   👍 RESULT: GOOD! (distance {minDistance:F3} ≤ {goodWindowZ:F3})");
        }
        else
        {
            // Any note inside the trigger but outside the 'Good' window is 'Okay'
            accuracy = HitAccuracy.Okay;
            Debug.Log($"   👌 RESULT: OKAY! (distance {minDistance:F3} > {goodWindowZ:F3})");
        }

        Debug.Log($"🎵 [FINAL] Lane {lane} HIT with {accuracy} accuracy!");
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