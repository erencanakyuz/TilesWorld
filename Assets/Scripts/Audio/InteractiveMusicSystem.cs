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
    [SerializeField] private MusicalScale currentScale = MusicalScale.CMajor;
    [SerializeField] private bool enableJsonBasedMusic = true;  // Use JSON note data
    [SerializeField] private bool showDebugInfo = true;         // Debug note playing

    [Header("🎼 Musical Mapping (Original SOUND_RESOURCE_IDXS)")]
    [SerializeField] private int baseOctave = 4;

    [Header("📊 Musical Analysis")]
    [SerializeField] private int notesPlayedThisSession = 0;
    [SerializeField] private int chordsPlayedThisSession = 0;
    [SerializeField] private float currentMelodyComplexity = 0f;

    // Original Java SOUND_RESOURCE_IDXS mapping (from MD analysis)
    private static readonly int[][] SOUND_RESOURCE_IDXS = {
        // Piano notes mapping (Original Java)
        new int[] { 24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44 },
        // Harp notes mapping  
        new int[] { 19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39 },
        // Guitar notes mapping
        new int[] { 15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35 },
        // Extended mappings for more variety
        new int[] { 10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30 },
        new int[] { 5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25 },
        new int[] { 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21 }
    };

    // Musical scale definitions (Western music theory)
    private static readonly Dictionary<MusicalScale, int[]> MUSICAL_SCALES = new Dictionary<MusicalScale, int[]>
    {
        { MusicalScale.CMajor, new int[] { 0, 2, 4, 5, 7, 9, 11 } },      // C D E F G A B
        { MusicalScale.AMajor, new int[] { 0, 2, 4, 5, 7, 9, 11 } },      // A B C# D E F# G#
        { MusicalScale.GMajor, new int[] { 0, 2, 4, 5, 7, 9, 11 } },      // G A B C D E F#
        { MusicalScale.Pentatonic, new int[] { 0, 2, 4, 7, 9 } },         // C D E G A
        { MusicalScale.MinorPentatonic, new int[] { 0, 3, 5, 7, 10 } },   // C Eb F G Bb
        { MusicalScale.Chromatic, new int[] { 0,1,2,3,4,5,6,7,8,9,10,11 } } // All notes
    };

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
    public static System.Action<float> OnMelodyComplexityChanged;

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
    /// Original Java SOUND_RESOURCE_IDXS mapping enhanced with musical theory
    /// </summary>
    MusicalNoteInfo CalculateMusicalNote(int lane)
    {
        var noteInfo = new MusicalNoteInfo();

        if (lane < 0 || lane >= 6)
        {
            noteInfo.isValid = false;
            return noteInfo;
        }

        // Use original SOUND_RESOURCE_IDXS as base mapping
        int instrumentIndex = (int)currentInstrument;
        if (instrumentIndex >= SOUND_RESOURCE_IDXS.Length)
            instrumentIndex = 0;

        int soundIndex = SOUND_RESOURCE_IDXS[instrumentIndex][lane];

        // Enhanced with musical scale mapping
        if (MUSICAL_SCALES.ContainsKey(currentScale))
        {
            int[] scaleNotes = MUSICAL_SCALES[currentScale];
            int scaleNoteIndex = lane % scaleNotes.Length;
            int scaleNote = scaleNotes[scaleNoteIndex];

            // Calculate MIDI note (C4 = 60 as base)
            noteInfo.midiNote = 60 + (baseOctave - 4) * 12 + scaleNote;
            noteInfo.soundIndex = soundIndex;
            noteInfo.noteName = GetNoteName(noteInfo.midiNote);
            noteInfo.instrumentType = currentInstrument;
            noteInfo.isValid = true;
        }
        else
        {
            // Fallback to original direct mapping
            noteInfo.midiNote = soundIndex + 60; // Approximate MIDI mapping
            noteInfo.soundIndex = soundIndex;
            noteInfo.noteName = GetNoteName(noteInfo.midiNote);
            noteInfo.instrumentType = currentInstrument;
            noteInfo.isValid = true;
        }

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

    #region Musical Complexity Analysis

    void UpdateMelodyComplexity(MusicalEvent newEvent)
    {
        // Simple complexity calculation based on note variety and timing
        var recentEvents = new List<MusicalEvent>(recentMusicalEvents);

        if (recentEvents.Count > 1)
        {
            // Calculate interval variety
            var intervals = new HashSet<int>();
            for (int i = 1; i < recentEvents.Count; i++)
            {
                int interval = Mathf.Abs(recentEvents[i].noteInfo.midiNote - recentEvents[i - 1].noteInfo.midiNote);
                intervals.Add(interval);
            }

            // Calculate timing variety
            var timings = new List<float>();
            for (int i = 1; i < recentEvents.Count; i++)
            {
                float timeDiff = recentEvents[i].timestamp - recentEvents[i - 1].timestamp;
                timings.Add(timeDiff);
            }

            // Complexity = interval variety + timing variety + chord frequency
            float intervalComplexity = intervals.Count / 12f; // Normalize to max possible intervals
            float timingComplexity = CalculateTimingVariance(timings);
            float chordComplexity = chordsPlayedThisSession / (float)notesPlayedThisSession;

            currentMelodyComplexity = (intervalComplexity + timingComplexity + chordComplexity) / 3f;
            OnMelodyComplexityChanged?.Invoke(currentMelodyComplexity);
        }
    }

    float CalculateTimingVariance(List<float> timings)
    {
        if (timings.Count == 0) return 0f;

        float average = 0f;
        foreach (float timing in timings) average += timing;
        average /= timings.Count;

        float variance = 0f;
        foreach (float timing in timings)
        {
            variance += (timing - average) * (timing - average);
        }
        variance /= timings.Count;

        return Mathf.Min(1f, variance * 10f); // Normalize and cap at 1
    }

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

        if (enableJsonBasedMusic && showDebugInfo)
        {
            Debug.Log($"🎵 JSON-based music enabled for {instrument}");
        }
    }

    /// <summary>
    /// Enhanced note playing with JSON pitch data (for NoteRenderer integration)
    /// </summary>
    public void TriggerNoteAudio(GameNoteInfo noteInfo)
    {
        if (noteInfo == null) return;

        // Enhanced volume calculation based on note duration from JSON
        float noteVolume = CalculateNoteVolume((int)noteInfo.duration);

#if UNITY_EDITOR
        // Debug tracking to ensure only NoteRenderer calls this method
        var stackTrace = new System.Diagnostics.StackTrace();
        var callingMethod = stackTrace.GetFrame(1)?.GetMethod()?.Name ?? "Unknown";
        var callingClass = stackTrace.GetFrame(1)?.GetMethod()?.DeclaringType?.Name ?? "Unknown";

        if (showDebugInfo)
        {
            Debug.Log($"🎵 TriggerNoteAudio called by {callingClass}.{callingMethod} - Note: {noteInfo.pitch}");
        }

        // Alert if called from unexpected source
        if (callingClass != "NoteRenderer")
        {
            Debug.LogWarning($"⚠️ TriggerNoteAudio called from unexpected source: {callingClass}.{callingMethod}");
        }
#endif

        // Use unified processing method
        ProcessAndPlayNote(noteInfo.idx, noteInfo.pitch, noteVolume, true, noteInfo.instrumentType);
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
            Debug.Log($"🎵 Playing chord with {chordNotes.Count} notes");
        }

        foreach (var note in chordNotes)
        {
            TriggerNoteAudio(note);
        }

        // Detect and classify chord using JSON pitch data
        var pitches = chordNotes.Select(n => n.pitch).ToList();
        ChordType detectedChord = DetectEnhancedChordType(pitches);

        if (detectedChord != ChordType.None)
        {
            OnChordDetected?.Invoke(detectedChord);

            if (showDebugInfo)
            {
                Debug.Log($"🎵 Enhanced chord detected: {detectedChord}");
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

    public void SetMusicalScale(MusicalScale scale)
    {
        currentScale = scale;
    }

    public void SetBaseOctave(int octave)
    {
        baseOctave = Mathf.Clamp(octave, 1, 8);
    }

    public MusicalSessionStats GetSessionStats()
    {
        return new MusicalSessionStats
        {
            notesPlayed = notesPlayedThisSession,
            chordsPlayed = chordsPlayedThisSession,
            melodyComplexity = currentMelodyComplexity,
            currentInstrument = currentInstrument,
            currentScale = currentScale
        };
    }

    void ResetSessionStats()
    {
        notesPlayedThisSession = 0;
        chordsPlayedThisSession = 0;
        currentMelodyComplexity = 0f;
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

            // Track musical complexity
            UpdateMelodyComplexity(musicalEvent);

            // Update statistics
            notesPlayedThisSession++;
            laneLastPlayTime[lane] = Time.time;

            OnMusicalEventCreated?.Invoke(musicalEvent);
        }

        // 3. Debug logging (if enabled)
        if (showDebugInfo)
        {
            Debug.Log($"🎵 Playing {instrumentType} pitch {pitch} " +
                     $"lane {lane} volume {velocity:F2}");
        }
    }
}

// Data structures moved to DataStructures.cs to avoid duplicates