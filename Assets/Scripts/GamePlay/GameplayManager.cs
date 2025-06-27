using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// GameplayManager - The Conductor of the Symphony
/// Based on original World.java - coordinates all game systems
/// Implements: Game loop, timing sync, system coordination, song management
/// "The maestro that makes all systems work in harmony"
/// </summary>
public class GameplayManager : MonoBehaviour
{
    [Header("🎮 Gameplay Configuration")]
    [SerializeField] private bool autoStartOnLoad = false;
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private bool enablePauseOnFocusLoss = true;

    [Header("🎵 Song Management")]
    [SerializeField] private SongData currentSong;
    [SerializeField] private float songStartDelay = 1f;
    [SerializeField] private bool useAudioTimeSync = true;

    [Header("🎯 Gameplay Settings")]
    [SerializeField] private float noteSpeed = 5f;
    [SerializeField] private float perfectTimingWindow = 50f;   // ms
    [SerializeField] private float goodTimingWindow = 100f;    // ms
    [SerializeField] private int maxCombo = 0;
    [SerializeField] private int currentCombo = 0;

    [Header("📊 Game Statistics")]
    [SerializeField] private int totalNotesHit = 0;
    [SerializeField] private int perfectHits = 0;
    [SerializeField] private int goodHits = 0;
    [SerializeField] private int missedNotes = 0;
    [SerializeField] private float accuracy = 0f;

    [Header("⚙️ System References")]
    [SerializeField] private GameNoteCreator noteCreator;
    [SerializeField] private NoteRenderer noteRenderer;
    [SerializeField] private InteractiveMusicSystem musicSystem;
    [SerializeField] private AudioManager audioManager;

    [Header("📱 Debug & Testing")]
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private bool enablePerformanceTracking = true;
    [SerializeField] private KeyCode testStartKey = KeyCode.Space;

    // Game state management
    private bool isGameActive = false;
    private bool isGamePaused = false;
    private bool isSongLoaded = false;
    private bool isCountingDown = false;

    // Timing synchronization (Original World.java timing logic)
    private float gameStartTime = 0f;
    private float currentGameTime = 0f;
    private float songDuration = 0f;
    private bool isUsingAudioSync = true;

    // Performance tracking
    private float lastUpdateTime = 0f;
    private int frameCount = 0;
    private float avgFPS = 60f;

    // Events for system coordination
    public static System.Action OnGameplayStarted;
    public static System.Action OnGameplayEnded;
    public static System.Action<float> OnGameTimeUpdated;
    public static System.Action<int> OnComboUpdated;
    public static System.Action<float> OnAccuracyUpdated;

    void Awake()
    {
        InitializeGameplayManager();
    }

    void Start()
    {
        SetupGameplaySystems();
        SubscribeToEvents();

        if (autoStartOnLoad && currentSong != null)
        {
            StartCoroutine(DelayedGameStart(1f));
        }

        if (showDebugInfo)
            Debug.Log("🎮 GameplayManager initialized - Ready for musical gameplay!");
    }

    void InitializeGameplayManager()
    {
        // Find system references if not assigned
        if (noteCreator == null)
            noteCreator = FindObjectOfType<GameNoteCreator>();

        if (noteRenderer == null)
            noteRenderer = FindObjectOfType<NoteRenderer>();

        if (musicSystem == null)
            musicSystem = FindObjectOfType<InteractiveMusicSystem>();

        if (audioManager == null)
            audioManager = AudioManager.Instance;

        ResetGameplayStats();
    }

    void SetupGameplaySystems()
    {
        // Configure note renderer speed
        if (noteRenderer != null)
        {
            noteRenderer.SetNoteSpeed(noteSpeed);
        }

        // Set initial game state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameState.SongSelection);
        }
    }

    void SubscribeToEvents()
    {
        // Subscribe to input events for note hitting
        InputManager.OnLaneTapped += HandleNoteHit;

        // Subscribe to note generation events
        GameNoteCreator.OnNotesGenerated += HandleNotesGenerated;
        GameNoteCreator.OnGenerationComplete += HandleSongComplete;

        // Subscribe to musical events
        InteractiveMusicSystem.OnChordDetected += HandleChordDetected;
        InteractiveMusicSystem.OnMusicalEventCreated += HandleMusicalEvent;

        // Subscribe to audio events
        if (audioManager != null)
        {
            audioManager.OnMusicFinished += HandleMusicFinished;
        }

        // Subscribe to game manager events
        GameManager.OnGameStateChanged += HandleGameStateChange;
    }

    #region Main Game Loop (Original World.java update logic)

    /// <summary>
    /// Original Java: update(float deltaTime) - Main game loop
    /// Enhanced with modern Unity patterns and system coordination
    /// </summary>
    void Update()
    {
        if (!isGameActive || isGamePaused) return;

        float deltaTime = Time.deltaTime;

        // Update game timing (original World timing logic)
        UpdateGameTiming(deltaTime);

        // Update note generation (original getNote calls)
        UpdateNoteGeneration(deltaTime);

        // Update performance tracking
        if (enablePerformanceTracking)
            UpdatePerformanceTracking(deltaTime);

        // Debug input handling
        HandleDebugInput();

        // Update UI with current stats
        UpdateGameplayUI();
    }

    /// <summary>
    /// Original World.java timing system enhanced with audio synchronization
    /// </summary>
    void UpdateGameTiming(float deltaTime)
    {
        if (isUsingAudioSync && audioManager != null && audioManager.IsMusicPlaying)
        {
            // Use audio time as source of truth (critical for rhythm games)
            currentGameTime = audioManager.CurrentMusicTime;
        }
        else
        {
            // Fallback to game time
            currentGameTime = Time.time - gameStartTime;
        }

        OnGameTimeUpdated?.Invoke(currentGameTime);

        // Check if song should end
        if (songDuration > 0 && currentGameTime >= songDuration)
        {
            EndGameplay();
        }
    }

    /// <summary>
    /// Original Java: noteCreator.getNote() calls in main loop
    /// </summary>
    void UpdateNoteGeneration(float deltaTime)
    {
        if (noteCreator != null)
        {
            var newNotes = noteCreator.GetNote(deltaTime);

            if (newNotes != null && newNotes.Count > 0)
            {
                // Notes will be handled by NoteRenderer through events
                if (showDebugInfo)
                    Debug.Log($"🎵 Generated {newNotes.Count} notes at time {currentGameTime:F2}s");
            }
        }
    }

    void UpdatePerformanceTracking(float deltaTime)
    {
        frameCount++;
        lastUpdateTime += deltaTime;

        if (lastUpdateTime >= 1f) // Update every second
        {
            avgFPS = frameCount / lastUpdateTime;
            frameCount = 0;
            lastUpdateTime = 0f;

            if (showDebugInfo && Time.frameCount % 60 == 0)
            {
                Debug.Log($"🎮 Performance: {avgFPS:F1} FPS, Notes: {totalNotesHit}, Combo: {currentCombo}");
            }
        }
    }

    void HandleDebugInput()
    {
        if (Input.GetKeyDown(testStartKey) && !isGameActive)
        {
            if (currentSong != null)
            {
                StartGameplay(currentSong);
            }
            else
            {
                Debug.LogWarning("🎮 No song loaded for testing!");
            }
        }

        if (Input.GetKeyDown(KeyCode.P) && isGameActive)
        {
            if (isGamePaused)
                ResumeGameplay();
            else
                PauseGameplay();
        }
    }
    #endregion

    #region Song Management & Game Flow

    /// <summary>
    /// Start gameplay with a song - Main entry point
    /// </summary>
    public void StartGameplay(SongData song)
    {
        if (song == null)
        {
            Debug.LogError("🎮 Cannot start gameplay with null song!");
            return;
        }

        currentSong = song;
        StartCoroutine(StartGameplaySequence());
    }

    IEnumerator StartGameplaySequence()
    {
        Debug.Log($"🎮 Starting gameplay: {currentSong.songName}");

        // Prepare all systems
        PrepareGameplaySystems();

        // Show countdown
        if (countdownDuration > 0)
        {
            yield return StartCoroutine(ShowCountdown());
        }

        // Load and start song
        yield return StartCoroutine(LoadAndStartSong());

        // Begin gameplay
        BeginActiveGameplay();
    }

    void PrepareGameplaySystems()
    {
        // Reset all stats
        ResetGameplayStats();

        // Load song into note creator
        if (noteCreator != null)
        {
            noteCreator.LoadSongData(currentSong);
        }

        // Set instrument for music system
        if (musicSystem != null && GameManager.Instance != null)
        {
            musicSystem.SetInstrument(GameManager.Instance.GetSelectedInstrument());
        }

        // Clear any existing notes
        if (noteRenderer != null)
        {
            noteRenderer.ClearAllNotes();
        }

        // Update game state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameState.Playing);
        }
    }

    IEnumerator ShowCountdown()
    {
        isCountingDown = true;

        for (int i = (int)countdownDuration; i > 0; i--)
        {
            Debug.Log($"🎮 Starting in {i}...");

            // Show countdown UI
            if (UIManager.Instance != null)
            {
                // UIManager would show countdown number
            }

            yield return new WaitForSeconds(1f);
        }

        isCountingDown = false;
    }

    IEnumerator LoadAndStartSong()
    {
        // Load audio clip (this would be enhanced with actual asset loading)
        if (audioManager != null && !string.IsNullOrEmpty(currentSong.audioFilePath))
        {
            // For now, we'll use a placeholder - in real implementation,
            // this would load the actual audio file
            Debug.Log($"🎵 Loading audio: {currentSong.audioFilePath}");

            yield return new WaitForSeconds(0.1f); // Simulate loading time

            // Start audio playback
            // audioManager.PlayMusic(loadedClip, songStartDelay);
            isUsingAudioSync = useAudioTimeSync;
        }

        songDuration = currentSong.duration;
        isSongLoaded = true;
    }

    void BeginActiveGameplay()
    {
        gameStartTime = Time.time;
        currentGameTime = 0f;
        isGameActive = true;
        isGamePaused = false;

        OnGameplayStarted?.Invoke();

        Debug.Log("🎮 Gameplay started! Let the music begin!");
    }

    public void PauseGameplay()
    {
        if (!isGameActive || isGamePaused) return;

        isGamePaused = true;
        Time.timeScale = 0f;

        if (audioManager != null)
        {
            audioManager.PauseMusic();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameState.Paused);
        }

        Debug.Log("⏸️ Gameplay paused");
    }

    public void ResumeGameplay()
    {
        if (!isGameActive || !isGamePaused) return;

        isGamePaused = false;
        Time.timeScale = 1f;

        if (audioManager != null)
        {
            audioManager.ResumeMusic();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameState.Playing);
        }

        Debug.Log("▶️ Gameplay resumed");
    }

    public void EndGameplay()
    {
        if (!isGameActive) return;

        isGameActive = false;
        isGamePaused = false;
        Time.timeScale = 1f;

        // Stop audio
        if (audioManager != null)
        {
            audioManager.StopMusic();
        }

        // Calculate final stats
        CalculateFinalStats();

        // Update game state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndGameSession();
        }

        OnGameplayEnded?.Invoke();

        Debug.Log($"🏁 Gameplay ended! Final score: Accuracy {accuracy:F1}%, Max combo: {maxCombo}");
    }
    #endregion

    #region Event Handlers (Original World.java onTap logic)

    /// <summary>
    /// Original Java: onTap(int lane, boolean isTap) enhanced with timing analysis
    /// </summary>
    void HandleNoteHit(int lane, Vector2 screenPosition)
    {
        if (!isGameActive || isGamePaused) return;

        // Find notes in hit zone for this lane
        var hitNotes = FindHittableNotes(lane);

        if (hitNotes.Count > 0)
        {
            // Get the closest note to perfect timing
            var bestNote = GetBestTimingNote(hitNotes);
            ProcessNoteHit(bestNote, lane);
        }
        else
        {
            // No notes to hit - miss
            ProcessMiss(lane);
        }
    }

    List<GameNoteInfo> FindHittableNotes(int lane)
    {
        // This would integrate with NoteRenderer to find notes in hit zone
        // For now, return empty list - NoteRenderer handles actual hit detection
        return new List<GameNoteInfo>();
    }

    GameNoteInfo GetBestTimingNote(List<GameNoteInfo> notes)
    {
        // Return note with best timing (closest to perfect hit zone)
        // This is a placeholder - actual implementation would calculate timing differences
        return notes.Count > 0 ? notes[0] : null;
    }

    void ProcessNoteHit(GameNoteInfo note, int lane)
    {
        if (note == null) return;

        // Calculate hit accuracy based on timing
        float timingDifference = CalculateTimingDifference(note);
        HitAccuracy accuracy = CalculateHitAccuracy(timingDifference);

        // Update stats
        totalNotesHit++;

        switch (accuracy)
        {
            case HitAccuracy.Perfect:
                perfectHits++;
                currentCombo++;
                break;
            case HitAccuracy.Good:
                goodHits++;
                currentCombo++;
                break;
            case HitAccuracy.Miss:
                missedNotes++;
                currentCombo = 0;
                break;
        }

        if (currentCombo > maxCombo)
            maxCombo = currentCombo;

        // Update UI and game manager
        UpdateGameStats();

        // Show hit effect
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowHitEffect(accuracy, screenPosition);
        }

        if (showDebugInfo)
            Debug.Log($"🎯 Note hit: Lane {lane}, Accuracy: {accuracy}, Combo: {currentCombo}");
    }

    void ProcessMiss(int lane)
    {
        currentCombo = 0;

        // Update UI to show miss
        if (UIManager.Instance != null)
        {
            Vector2 missPosition = new Vector2(lane * Screen.width / 6f, Screen.height * 0.8f);
            UIManager.Instance.ShowHitEffect(HitAccuracy.Miss, missPosition);
        }

        UpdateGameStats();

        if (showDebugInfo)
            Debug.Log($"❌ Miss: Lane {lane}, Combo reset");
    }

    float CalculateTimingDifference(GameNoteInfo note)
    {
        // Calculate difference between note timing and current game time
        return Mathf.Abs(note.timeMs - (currentGameTime * 1000f));
    }

    HitAccuracy CalculateHitAccuracy(float timingDifference)
    {
        if (timingDifference <= perfectTimingWindow)
            return HitAccuracy.Perfect;
        else if (timingDifference <= goodTimingWindow)
            return HitAccuracy.Good;
        else
            return HitAccuracy.Miss;
    }

    void HandleNotesGenerated(List<GameNoteInfo> notes)
    {
        // Notes are handled by NoteRenderer through events
        // This can be used for additional processing if needed
    }

    void HandleSongComplete()
    {
        Debug.Log("🎵 Song generation complete!");
        // Song will end when audio finishes or time runs out
    }

    void HandleChordDetected(ChordType chordType)
    {
        // Bonus points for chords
        if (GameManager.Instance != null)
        {
            int chordBonus = chordType == ChordType.Major || chordType == ChordType.Minor ? 500 : 200;
            GameManager.Instance.UpdateScore(chordBonus);
        }

        if (showDebugInfo)
            Debug.Log($"🎼 Chord bonus: {chordType} - Extra points awarded!");
    }

    void HandleMusicalEvent(MusicalEvent musicalEvent)
    {
        // Additional processing for musical events if needed
    }

    void HandleMusicFinished()
    {
        Debug.Log("🎵 Background music finished");
        EndGameplay();
    }

    void HandleGameStateChange(GameState newState)
    {
        switch (newState)
        {
            case GameState.Paused:
                if (isGameActive && !isGamePaused)
                    PauseGameplay();
                break;
            case GameState.Playing:
                if (isGameActive && isGamePaused)
                    ResumeGameplay();
                break;
        }
    }
    #endregion

    #region Statistics & UI Updates

    void UpdateGameStats()
    {
        // Calculate accuracy
        int totalNotes = perfectHits + goodHits + missedNotes;
        if (totalNotes > 0)
        {
            accuracy = ((float)(perfectHits + goodHits) / totalNotes) * 100f;
        }

        // Update game manager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateCombo(currentCombo);
        }

        // Fire events
        OnComboUpdated?.Invoke(currentCombo);
        OnAccuracyUpdated?.Invoke(accuracy);
    }

    void UpdateGameplayUI()
    {
        // This runs every frame to keep UI updated
        // In a real implementation, this would be more optimized
    }

    void CalculateFinalStats()
    {
        UpdateGameStats();

        var finalStats = new GameplayStats
        {
            totalNotesHit = totalNotesHit,
            perfectHits = perfectHits,
            goodHits = goodHits,
            missedNotes = missedNotes,
            maxCombo = maxCombo,
            accuracy = accuracy,
            songName = currentSong != null ? currentSong.songName : "Unknown"
        };

        Debug.Log($"🎮 Final Stats: {finalStats}");
    }

    void ResetGameplayStats()
    {
        totalNotesHit = 0;
        perfectHits = 0;
        goodHits = 0;
        missedNotes = 0;
        currentCombo = 0;
        maxCombo = 0;
        accuracy = 0f;
        currentGameTime = 0f;
    }
    #endregion

    #region Public Interface

    public bool IsGameActive() => isGameActive;
    public bool IsGamePaused() => isGamePaused;
    public float GetCurrentGameTime() => currentGameTime;
    public int GetCurrentCombo() => currentCombo;
    public float GetAccuracy() => accuracy;

    public void SetNoteSpeed(float speed)
    {
        noteSpeed = Mathf.Max(0.1f, speed);
        if (noteRenderer != null)
            noteRenderer.SetNoteSpeed(noteSpeed);
    }

    public void SetTimingWindows(float perfectMs, float goodMs)
    {
        perfectTimingWindow = perfectMs;
        goodTimingWindow = goodMs;
    }

    public GameplayStats GetCurrentStats()
    {
        return new GameplayStats
        {
            totalNotesHit = totalNotesHit,
            perfectHits = perfectHits,
            goodHits = goodHits,
            missedNotes = missedNotes,
            maxCombo = maxCombo,
            accuracy = accuracy,
            songName = currentSong != null ? currentSong.songName : "None"
        };
    }
    #endregion

    void OnApplicationPause(bool pauseStatus)
    {
        if (enablePauseOnFocusLoss && pauseStatus && isGameActive && !isGamePaused)
        {
            PauseGameplay();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (enablePauseOnFocusLoss && !hasFocus && isGameActive && !isGamePaused)
        {
            PauseGameplay();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        InputManager.OnLaneTapped -= HandleNoteHit;
        GameNoteCreator.OnNotesGenerated -= HandleNotesGenerated;
        GameNoteCreator.OnGenerationComplete -= HandleSongComplete;
        InteractiveMusicSystem.OnChordDetected -= HandleChordDetected;
        InteractiveMusicSystem.OnMusicalEventCreated -= HandleMusicalEvent;
        GameManager.OnGameStateChanged -= HandleGameStateChange;

        if (audioManager != null)
        {
            audioManager.OnMusicFinished -= HandleMusicFinished;
        }
    }
}

#region Data Structures

[System.Serializable]
public struct GameplayStats
{
    public int totalNotesHit;
    public int perfectHits;
    public int goodHits;
    public int missedNotes;
    public int maxCombo;
    public float accuracy;
    public string songName;

    public override string ToString()
    {
        return $"Song: {songName}, Accuracy: {accuracy:F1}%, Max Combo: {maxCombo}, " +
               $"Perfect: {perfectHits}, Good: {goodHits}, Miss: {missedNotes}";
    }
}

#endregion