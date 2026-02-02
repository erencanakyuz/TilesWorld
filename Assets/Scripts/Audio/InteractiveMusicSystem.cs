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
    private readonly List<MusicalEvent> chordBuffer = new List<MusicalEvent>(16);
    private readonly List<int> chordMidiBuffer = new List<int>(8);
    private Queue<MusicalEvent> eventPool;
    private Queue<MusicalNoteInfo> noteInfoPool;
    [SerializeField] private int maxEventPoolSize = 64;
    [SerializeField] private int maxNoteInfoPoolSize = 64;

    // Performance tracking
    private AudioManager audioManager;
    private float lastChordTime = 0f;
    private const float CHORD_DETECTION_WINDOW = 0.2f; // 200ms window for chord detection

    // Chord detection system
    private Queue<MusicalEvent> recentMusicalEvents;
    
    // Events for musical feedback  
    public static System.Action<ChordType> OnChordDetected;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInteractiveMusic();
        recentMusicalEvents = new Queue<MusicalEvent>();
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
        eventPool = new Queue<MusicalEvent>();
        noteInfoPool = new Queue<MusicalNoteInfo>();

        // Initialize lane play times
        for (int i = 0; i < 6; i++)
        {
            laneLastPlayTime[i] = 0f;
        }
    }

    void SetupMusicSystem()
    {
        // Get references to other systems using singleton pattern
        if (audioManager == null)
            audioManager = AudioManager.Instance;

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


    #endregion

    #region Chord Detection & Harmony Analysis

    void CheckForChordCreation(MusicalEvent newEvent)
    {
        // Look for notes played within chord detection window
        chordBuffer.Clear();
        float timeWindow = Time.time - CHORD_DETECTION_WINDOW;

        foreach (var recentEvent in recentMusicalEvents)
        {
            if (recentEvent.timestamp >= timeWindow)
            {
                chordBuffer.Add(recentEvent);
            }
        }

        if (chordBuffer.Count >= 2) // At least 2 notes for a chord
        {
            ChordType detectedChord = AnalyzeChord(chordBuffer);
            if (detectedChord != ChordType.None)
            {
                HandleChordDetection(detectedChord, chordBuffer);
            }
        }
    }

    ChordType AnalyzeChord(List<MusicalEvent> notes)
    {
        if (notes.Count < 2) return ChordType.None;

        // Simple chord detection - can be enhanced
        chordMidiBuffer.Clear();
        foreach (var note in notes)
        {
            chordMidiBuffer.Add(note.noteInfo.midiNote % 12); // Normalize to octave
        }

        chordMidiBuffer.Sort();

        // Basic triad detection
        if (chordMidiBuffer.Count >= 3)
        {
            int root = chordMidiBuffer[0];
            int third = chordMidiBuffer[1];
            int fifth = chordMidiBuffer[2];

            // Major triad pattern (4 semitones + 3 semitones)
            if ((third - root) % 12 == 4 && (fifth - third) % 12 == 3)
                return ChordType.Major;

            // Minor triad pattern (3 semitones + 4 semitones)
            if ((third - root) % 12 == 3 && (fifth - third) % 12 == 4)
                return ChordType.Minor;
        }

        // Simple interval detection
        if (chordMidiBuffer.Count == 2)
        {
            int interval = (chordMidiBuffer[1] - chordMidiBuffer[0]) % 12;
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
            var oldEvent = recentMusicalEvents.Dequeue();
            RecycleMusicalEvent(oldEvent);
        }

        // Cap queue size for performance
        while (recentMusicalEvents.Count > 20)
        {
            var oldEvent = recentMusicalEvents.Dequeue();
            RecycleMusicalEvent(oldEvent);
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
                // Clear recent events for new session
                ClearRecentEvents();
                break;
            case GameState.GameOver:
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
    /// Müzikal event'i işle ve analiz et (separated from audio playing)
    /// </summary>
    private void ProcessMusicalEvent(GameNoteInfo noteInfo, float velocity)
    {
        // Create musical event for analysis
        var musicalEvent = GetPooledMusicalEvent();
        var noteDetails = GetPooledNoteInfo();
        noteDetails.midiNote = noteInfo.pitch;
        noteDetails.soundIndex = noteInfo.pitch;
        noteDetails.noteName = $"Note{noteInfo.pitch}";
        noteDetails.instrumentType = currentInstrument;
        noteDetails.isValid = true;

        musicalEvent.lane = noteInfo.line;
        musicalEvent.timestamp = Time.time;
        musicalEvent.velocity = velocity;
        musicalEvent.noteInfo = noteDetails;

        // Keep any optional fields clean
        musicalEvent.detectedChord = ChordType.None;
        musicalEvent.isPlayerTriggered = false;

        // Add to recent events for chord detection
        recentMusicalEvents.Enqueue(musicalEvent);

        // Update session statistics
        notesPlayedThisSession++;
        laneLastPlayTime[noteInfo.line] = Time.time;

        // Analyze musical patterns (chord detection only)
        CheckForChordCreation(musicalEvent);
        CleanupOldMusicalEvents();
    }

    private void ClearRecentEvents()
    {
        if (recentMusicalEvents == null) return;
        while (recentMusicalEvents.Count > 0)
        {
            var ev = recentMusicalEvents.Dequeue();
            RecycleMusicalEvent(ev);
        }
    }

    private MusicalEvent GetPooledMusicalEvent()
    {
        if (eventPool != null && eventPool.Count > 0)
        {
            return eventPool.Dequeue();
        }
        return new MusicalEvent();
    }

    private MusicalNoteInfo GetPooledNoteInfo()
    {
        if (noteInfoPool != null && noteInfoPool.Count > 0)
        {
            return noteInfoPool.Dequeue();
        }
        return new MusicalNoteInfo();
    }

    private void RecycleMusicalEvent(MusicalEvent musicalEvent)
    {
        if (musicalEvent == null) return;

        if (musicalEvent.activePitches != null)
        {
            musicalEvent.activePitches.Clear();
        }

        if (musicalEvent.noteInfo != null)
        {
            RecycleNoteInfo(musicalEvent.noteInfo);
            musicalEvent.noteInfo = null;
        }

        if (eventPool != null && eventPool.Count < maxEventPoolSize)
        {
            eventPool.Enqueue(musicalEvent);
        }
    }

    private void RecycleNoteInfo(MusicalNoteInfo noteInfo)
    {
        if (noteInfo == null) return;
        if (noteInfoPool != null && noteInfoPool.Count < maxNoteInfoPoolSize)
        {
            noteInfoPool.Enqueue(noteInfo);
        }
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
            // Updated to use AudioManager + ProcessChartNoteHit
            if (AudioManager.Instance != null)
            {
                float volume = AudioManager.Instance.CalculateNoteVolume(note.duration);
                AudioManager.Instance.PlayNote(currentInstrument, note.pitch, volume, useJavaMapping: true, line: note.line);
                ProcessChartNoteHit(note);
            }
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




    // NEW METHOD: Handle chart note hit for analysis only (no audio)
    public void ProcessChartNoteHit(GameNoteInfo noteInfo)
    {
        if (noteInfo == null) return;

        float noteVolume = AudioManager.Instance != null ? AudioManager.Instance.CalculateNoteVolume(noteInfo.duration) : 1.0f;
        ProcessMusicalEvent(noteInfo, noteVolume);
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
}

// Data structures moved to DataStructures.cs to avoid duplicates
