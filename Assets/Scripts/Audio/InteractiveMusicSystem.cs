using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

/// <summary>
/// InteractiveMusicSystem - The Soul of the Game
/// Based on original Sounds.java with SOUND_RESOURCE_IDXS mapping
/// Implements: Real-time music creation, instrument mapping, musical scales
/// "The player doesn't just hit notes - they CREATE music!"
/// </summary>
public class InteractiveMusicSystem : MonoBehaviour
{
    public static InteractiveMusicSystem Instance { get; private set; }

    [Header("🎵 Interactive Music Configuration")]
    [SerializeField] private InstrumentType currentInstrument = InstrumentType.Piano;
    [Header("⚙️ System Configuration")]
    [SerializeField] private bool showDebugInfo = false;

    [Header("🎼 Musical Mapping (Original SOUND_RESOURCE_IDXS)")]
    [Header("📊 Musical Analysis")]
    [SerializeField] private int notesPlayedThisSession = 0;
    [SerializeField] private int chordsPlayedThisSession = 0;

    // Original Java SOUND_RESOURCE_IDXS mapping artık AudioConstants'da merkezi olarak tanımlı

    // Chord detection and harmony
    private List<PlayingNote> currentlyPlayingNotes;
    private Dictionary<int, float> laneLastPlayTime;
    private Queue<MusicalEvent> recentMusicalEvents;

    // Performance tracking
    private AudioManager audioManager;
    private float lastChordTime = 0f;
    private const float CHORD_DETECTION_WINDOW = 0.2f; // 200ms window for chord detection

    // Events for musical feedback
    public static System.Action<MusicalEvent> OnMusicalEventCreated;
    public static System.Action<ChordType> OnChordDetected;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInteractiveMusic();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        SetupMusicSystem();
        SubscribeToEvents();
    }

    void InitializeInteractiveMusic()
    {
        currentlyPlayingNotes = new List<PlayingNote>();
        laneLastPlayTime = new Dictionary<int, float>();
        recentMusicalEvents = new Queue<MusicalEvent>();

        // Initialize lane play times
        for (int i = 0; i < 6; i++)
        {
            laneLastPlayTime[i] = 0f;
        }
    }

    void SetupMusicSystem()
    {
        // Get references to other systems
        if (audioManager == null)
            audioManager = FindFirstObjectByType<AudioManager>();

        // Set initial instrument
        if (GameManager.Instance != null)
        {
            currentInstrument = GameManager.Instance.GetSelectedInstrument();
        }
    }

    void SubscribeToEvents()
    {
        // Removed direct input handling to prevent duplicate audio
        // Audio is now handled only through NoteRenderer hit detection
        GameManager.OnGameStateChanged += HandleGameStateChange;
    }

    #region Core Interactive Music Logic (From Original Sounds.java)

    /// <summary>
    /// Original Java: playSound(int line, int pitch) - The heart of interactive music
    /// Now enhanced with musical theory and real-time composition
    /// </summary>
    public void PlayInteractiveNote(int lane, float velocity = 1.0f, bool isPlayerTriggered = true)
    {
        // Get musical note information for this lane
        MusicalNoteInfo noteInfo = CalculateMusicalNote(lane);

        if (noteInfo.isValid)
        {
            // Use unified processing method
            ProcessAndPlayNote(lane, noteInfo.midiNote, velocity, isPlayerTriggered, noteInfo.instrumentType);
        }
    }

    /// <summary>
    /// Original Java SOUND_RESOURCE_IDXS mapping - simplified
    /// </summary>
    MusicalNoteInfo CalculateMusicalNote(int lane)
    {
        var noteInfo = new MusicalNoteInfo();

        if (lane < 0 || lane >= 6)
        {
            noteInfo.isValid = false;
            return noteInfo;
        }

        // Use AudioConstants for centralized sound mapping
        int soundIndex = AudioConstants.GetSoundIndex(lane, 0); // Use lane as base, pitch 0 for this context

        // Simple direct mapping - no complex musical theory
        noteInfo.midiNote = soundIndex + 60; // Direct mapping
        noteInfo.soundIndex = soundIndex;
        noteInfo.noteName = GetNoteName(noteInfo.midiNote);
        noteInfo.instrumentType = currentInstrument;
        noteInfo.isValid = true;

        return noteInfo;
    }

    string GetNoteName(int midiNote)
    {
        string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        int noteIndex = midiNote % 12;
        int octave = midiNote / 12 - 1;
        return $"{noteNames[noteIndex]}{octave}";
    }
    #endregion

    #region Chord Detection & Harmony Analysis

    void CheckForChordCreation(MusicalEvent newEvent)
    {
        // Look for notes played within chord detection window
        var simultaneousNotes = new List<MusicalEvent>();
        float timeWindow = Time.time - CHORD_DETECTION_WINDOW;

        foreach (var recentEvent in recentMusicalEvents)
        {
            if (recentEvent.timestamp >= timeWindow)
            {
                simultaneousNotes.Add(recentEvent);
            }
        }

        if (simultaneousNotes.Count >= 2) // At least 2 notes for a chord
        {
            ChordType detectedChord = AnalyzeChord(simultaneousNotes);
            if (detectedChord != ChordType.None)
            {
                HandleChordDetection(detectedChord, simultaneousNotes);
            }
        }
    }

    ChordType AnalyzeChord(List<MusicalEvent> notes)
    {
        if (notes.Count < 2) return ChordType.None;

        // Simple chord detection - can be enhanced
        var midiNotes = new List<int>();
        foreach (var note in notes)
        {
            midiNotes.Add(note.noteInfo.midiNote % 12); // Normalize to octave
        }

        midiNotes.Sort();

        // Basic triad detection
        if (midiNotes.Count >= 3)
        {
            int root = midiNotes[0];
            int third = midiNotes[1];
            int fifth = midiNotes[2];

            // Major triad pattern (4 semitones + 3 semitones)
            if ((third - root) % 12 == 4 && (fifth - third) % 12 == 3)
                return ChordType.Major;

            // Minor triad pattern (3 semitones + 4 semitones)
            if ((third - root) % 12 == 3 && (fifth - third) % 12 == 4)
                return ChordType.Minor;
        }

        // Simple interval detection
        if (midiNotes.Count == 2)
        {
            int interval = (midiNotes[1] - midiNotes[0]) % 12;
            if (interval == 7) return ChordType.Perfect5th;
            if (interval == 4) return ChordType.Major3rd;
            if (interval == 3) return ChordType.Minor3rd;
        }

        return ChordType.Unison;
    }

    void HandleChordDetection(ChordType chordType, List<MusicalEvent> chordNotes)
    {
        chordsPlayedThisSession++;
        lastChordTime = Time.time;

        OnChordDetected?.Invoke(chordType);

        // Visual feedback for chord
        TriggerChordVisualEffect(chordType, chordNotes);
    }

    void TriggerChordVisualEffect(ChordType chordType, List<MusicalEvent> chordNotes)
    {
        // Enhanced visual feedback for musical chords
        if (UIManager.Instance != null)
        {
            foreach (var note in chordNotes)
            {
                Vector2 effectPos = InputManager.Instance != null ?
                    new Vector2(note.lane * Screen.width / 6f, Screen.height * 0.8f) :
                    Vector2.zero;

                // Special chord effect - different from single note
                HitAccuracy chordAccuracy = chordType == ChordType.Major || chordType == ChordType.Minor ?
                    HitAccuracy.Perfect : HitAccuracy.Good;

                UIManager.Instance.ShowHitEffect(chordAccuracy, effectPos);
            }
        }
    }
    #endregion

    #region Event Cleanup (Simplified)

    void CleanupOldMusicalEvents()
    {
        float cutoffTime = Time.time - 2f; // Keep 2 seconds of history

        while (recentMusicalEvents.Count > 0 && recentMusicalEvents.Peek().timestamp < cutoffTime)
        {
            recentMusicalEvents.Dequeue();
        }

        // Cap queue size for performance
        while (recentMusicalEvents.Count > 20)
        {
            recentMusicalEvents.Dequeue();
        }
    }
    #endregion

    #region Event Handlers

    // Direct input handling removed to prevent duplicate audio
    // Audio is now triggered only through NoteRenderer hit detection
    // This ensures notes only play when actually hitting chart notes, not random key presses

    void HandleGameStateChange(GameState newState)
    {
        switch (newState)
        {
            case GameState.Playing:
                ResetSessionStats();
                break;
            case GameState.GameOver:
                LogSessionSummary();
                break;
        }
    }
    #endregion

    #region Public Interface

    public void SetInstrument(InstrumentType instrument)
    {
        currentInstrument = instrument;
        /*
                if (enableJsonBasedMusic && showDebugInfo)
                {
                    Debug.Log($"🎵 JSON-based music enabled for {instrument}");
                }
        */
    }

    /// <summary>
    /// Enhanced note playing with JSON pitch data (for NoteRenderer integration)
    /// </summary>
    public void PlayNoteFromChart(GameNoteInfo noteInfo)
    {
        if (noteInfo == null) return;

        // Enhanced volume calculation based on note duration from JSON
        float noteVolume = CalculateNoteVolume((int)noteInfo.duration);

#if UNITY_EDITOR
        // Debug tracking to ensure only HitZoneManager calls this method
        var stackTrace = new System.Diagnostics.StackTrace();
        var callingMethod = stackTrace.GetFrame(1)?.GetMethod()?.Name ?? "Unknown";
        var callingClass = stackTrace.GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "Unknown";

        if (showDebugInfo)
        {
            // Debug.Log($"🎵 TriggerNoteAudio called by {callingClass}.{callingMethod} - Note: {noteInfo.pitch}");
        }

        // Alert if called from unexpected source - now expects HitZoneManager
        if (callingClass != "HitZoneManager")
        {
            Debug.LogWarning($"⚠️ TriggerNoteAudio called from unexpected source: {callingClass}.{callingMethod}");
        }
#endif

        // ENHANCED: AudioManager'ın unified metodunu Java mapping ile kullan
        if (audioManager != null)
        {
            audioManager.PlayNote(currentInstrument, noteInfo.pitch, noteVolume, useJavaMapping: true, line: noteInfo.line);
        }
        else
        {
            Debug.LogWarning("🎵 AudioManager referansı bulunamadı!");
        }

        // Müzikal event ve analiz sistemini koru
        ProcessMusicalEvent(noteInfo, noteVolume);
    }

    /// <summary>
    /// Müzikal event'i işle ve analiz et (separated from audio playing)
    /// </summary>
    private void ProcessMusicalEvent(GameNoteInfo noteInfo, float velocity)
    {
        // Create musical event for analysis
        var musicalEvent = new MusicalEvent
        {
            lane = noteInfo.line,
            timestamp = Time.time,
            velocity = velocity,
            noteInfo = new MusicalNoteInfo
            {
                midiNote = noteInfo.pitch,
                soundIndex = noteInfo.pitch,
                noteName = GetNoteName(noteInfo.pitch),
                instrumentType = currentInstrument,
                isValid = true
            }
        };

        // Add to recent events for chord detection
        recentMusicalEvents.Enqueue(musicalEvent);
        OnMusicalEventCreated?.Invoke(musicalEvent);

        // Update session statistics
        notesPlayedThisSession++;
        laneLastPlayTime[noteInfo.line] = Time.time;

        // Analyze musical patterns (chord detection only)
        CheckForChordCreation(musicalEvent);
        CleanupOldMusicalEvents();
    }

    /// <summary>
    /// Calculate note volume based on JSON duration value
    /// </summary>
    float CalculateNoteVolume(int duration)
    {
        // Duration from JSON (1-9) maps to volume (0.3-1.0)
        return Mathf.Lerp(0.3f, 1.0f, (duration - 1) / 8f);
    }

    /// <summary>
    /// Play multiple notes simultaneously (chord) - enhanced with JSON data
    /// </summary>
    public void PlayChord(List<GameNoteInfo> chordNotes)
    {
        if (chordNotes == null || chordNotes.Count == 0) return;

        if (showDebugInfo)
        {
            // Debug.Log($"🎵 Playing chord with {chordNotes.Count} notes");
        }

        foreach (var note in chordNotes)
        {
            PlayNoteFromChart(note);
        }

        // Detect and classify chord using JSON pitch data
        var pitches = chordNotes.Select(n => n.pitch).ToList();
        ChordType detectedChord = DetectEnhancedChordType(pitches);

        if (detectedChord != ChordType.None)
        {
            OnChordDetected?.Invoke(detectedChord);

            if (showDebugInfo)
            {
                // Debug.Log($"🎵 Enhanced chord detected: {detectedChord}");
            }
        }
    }

    /// <summary>
    /// Enhanced chord detection with JSON pitch data
    /// </summary>
    ChordType DetectEnhancedChordType(List<int> pitches)
    {
        if (pitches.Count < 2) return ChordType.None;

        // Convert to chromatic notes (0-11)
        var chromaticNotes = pitches.Select(p => p % 12).Distinct().OrderBy(n => n).ToList();

        if (chromaticNotes.Count < 2) return ChordType.Unison;

        // Check for intervals first (2 notes)
        if (chromaticNotes.Count == 2)
        {
            int interval = (chromaticNotes[1] - chromaticNotes[0] + 12) % 12;
            switch (interval)
            {
                case 3: return ChordType.Minor3rd;
                case 4: return ChordType.Major3rd;
                case 7: return ChordType.Perfect5th;
                default: return ChordType.Unison;
            }
        }

        // Check common chord patterns (3+ notes)
        var intervals = new List<int>();
        for (int i = 1; i < chromaticNotes.Count; i++)
        {
            intervals.Add((chromaticNotes[i] - chromaticNotes[0] + 12) % 12);
        }

        // Major chord (0, 4, 7)
        if (intervals.Contains(4) && intervals.Contains(7))
            return ChordType.Major;

        // Minor chord (0, 3, 7)
        if (intervals.Contains(3) && intervals.Contains(7))
            return ChordType.Minor;

        // Diminished chord (0, 3, 6)
        if (intervals.Contains(3) && intervals.Contains(6))
            return ChordType.Diminished;

        // Augmented chord (0, 4, 8)
        if (intervals.Contains(4) && intervals.Contains(8))
            return ChordType.Augmented;

        return ChordType.None;
    }

    public MusicalSessionStats GetSessionStats()
    {
        return new MusicalSessionStats
        {
            notesPlayed = notesPlayedThisSession,
            chordsPlayed = chordsPlayedThisSession,
            melodyComplexity = 0f, // Simplified - no longer calculated
            currentInstrument = currentInstrument,
            currentScale = MusicalScale.CMajor // Default - no longer changeable
        };
    }

    void ResetSessionStats()
    {
        notesPlayedThisSession = 0;
        chordsPlayedThisSession = 0;
        recentMusicalEvents.Clear();
    }

    void LogSessionSummary()
    {
        // Session logging removed for performance
    }
    #endregion

    void OnDestroy()
    {
        // Removed input unsubscription since we no longer handle direct input
        GameManager.OnGameStateChanged -= HandleGameStateChange;
    }

    /// <summary>
    /// UNIFIED NOTE PROCESSING - Eliminates code duplication
    /// Handles all note playing, musical analysis, and event creation
    /// </summary>
    private void ProcessAndPlayNote(int lane, int pitch, float velocity, bool isPlayerTriggered, InstrumentType instrumentType)
    {
        // 1. Play the sound using AudioManager
        if (audioManager != null)
        {
            audioManager.PlayNote(instrumentType, pitch, velocity);
        }

        // 2. Create musical event for analysis (only for interactive notes)
        if (isPlayerTriggered)
        {
            var musicalEvent = new MusicalEvent
            {
                lane = lane,
                noteInfo = CalculateMusicalNote(lane), // Recalculate for consistency
                velocity = velocity,
                timestamp = Time.time,
                isPlayerTriggered = isPlayerTriggered
            };

            // Add to recent events for chord detection
            recentMusicalEvents.Enqueue(musicalEvent);
            CleanupOldMusicalEvents();

            // Detect chords and harmonies
            CheckForChordCreation(musicalEvent);

            // Update statistics
            notesPlayedThisSession++;
            laneLastPlayTime[lane] = Time.time;

            OnMusicalEventCreated?.Invoke(musicalEvent);
        }

        // 3. Debug logging (if enabled)
        if (showDebugInfo)
        {
            // Debug.Log($"🎵 Playing {instrumentType} pitch {pitch} " +
            //         $"lane {lane} volume {velocity:F2}");
        }
    }
}

// Data structures moved to DataStructures.cs to avoid duplicates