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

    // Internal
    private HitZoneTrigger[] zones;
    private float noteTravelTime;

    void Awake()
    {
        zones = FindObjectsByType<HitZoneTrigger>(FindObjectsSortMode.None);
        System.Array.Sort(zones, (a, b) => a.laneIndex.CompareTo(b.laneIndex));

        if (audioManager == null) audioManager = AudioManager.Instance;

        // NoteRenderer'a referans artık gerekli değil.
        // if (noteRenderer == null) noteRenderer = FindFirstObjectByType<NoteRenderer>();
        // if (noteRenderer != null)
        // {
        //     noteTravelTime = noteRenderer.GetNoteTravelTime();
        // }
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

    // DEĞİŞİKLİK: Bu metod artık NoteAnimator'ı çağırıyor.
    void ProcessSuccessfulHit(HitZoneTrigger zone, GameObject noteObj, GameNoteInfo noteInfo, HitAccuracy acc, Vector2 screenPos)
    {
        // 1. Notayı trigger listesinden çıkar (ÇİFT VURMA ÖNLEMİ - KRİTİK)
        zone.RemoveNote(noteObj);

        // 2. Notanın animatörünü al ve VURULMA ANİMASYONUNU OYNAT
        var animator = noteObj.GetComponent<NoteAnimator>();
        if (animator != null)
        {
            // Yeni animasyon sistemimizi çağırıyoruz! Notayı yok etme işini bu metod üstlenecek.
            animator.AnimateHit(acc);
        }
        else
        {
            // Fallback: Animatör yoksa eski usul yok et
            Destroy(noteObj);
        }

        // 3. Ses ve Müzik sistemlerini tetikle (BU DEĞİŞMİYOR)
        if (noteInfo != null)
        {
            InteractiveMusicSystem.Instance?.PlayNoteFromChart(noteInfo);
        }

        // 4. UIManager efektini ÇAĞIRMIYORUZ, çünkü kendi animasyonumuz var.
        // Bu satır artık gerekli değil ve çakışmalara yol açabilir.
        // UIManager.Instance?.ShowHitEffect(acc, screenPos);

        // 5. Skoru güncelle (BU DEĞİŞMİYOR)
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
    // The precise DSP time when this note is expected to be perfectly hit.
    public double dspHitTime;
    public GameNoteInfo gameNoteInfo;
}