using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// GameplayManager - The Conductor of the Symphony
/// Based on original World.java - coordinates all game systems with EXACT timing
/// Implements: Game loop, timing sync, system coordination, song management
/// "The maestro that makes all systems work in harmony"
/// </summary>
public class GameplayManager : MonoBehaviour
{
    [Header("ğŸ® Gameplay Configuration")]
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private bool enablePauseOnFocusLoss = true;

    [Header("ğŸµ Song Management")]
    [SerializeField] private SongData currentSong;
    [SerializeField] private float songStartDelay = 1f;
    [SerializeField] private bool enableBackgroundMusic = false;
    [SerializeField] private bool useAudioTimeSync = true;

    [Header("ğŸ¯ Gameplay Settings")]
    [SerializeField] private float perfectTimingWindow = 50f;   // ms
    [SerializeField] private float goodTimingWindow = 100f;    // ms
    [SerializeField] private int maxCombo = 0;
    [SerializeField] private int currentCombo = 0;

    [Header("ğŸ“Š Game Statistics")]
    [SerializeField] private int totalNotesHit = 0;
    [SerializeField] private int perfectHits = 0;
    [SerializeField] private int goodHits = 0;
    [SerializeField] private int okayHits = 0;
    [SerializeField] private int missedNotes = 0;
    [SerializeField] private float accuracy = 0f;

    [Header("âš™ï¸ System References")]
    [SerializeField] private GameNoteCreator noteCreator;
    [SerializeField] private NoteRenderer noteRenderer;
    [SerializeField] private InteractiveMusicSystem musicSystem;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private UIManager uiManager;

    [Header("ğŸ”§ Debugging")]
    [SerializeField] private bool showDebugLogs = false;

    // Game state management
    private bool isGameActive = false;
    private bool _isCountingDown = false;

    // Public property for countdown state
    public bool IsCountingDown => _isCountingDown;

    // Timing synchronization (Original World.java timing logic)
    private float gameStartTime = 0f;
    private float currentGameTime = 0f;
    private float songDuration = 0f;

    // Events for system coordination
    public static System.Action OnGameplayStarted;
    public static System.Action OnGameplayEnded;
    public static System.Action<float> OnGameTimeUpdated;
    public static System.Action<int> OnComboUpdated;
    public static System.Action<float> OnAccuracyUpdated;

    void Awake()
    {
        // Get system references if not assigned in Inspector
        if (noteCreator == null)
            noteCreator = FindFirstObjectByType<GameNoteCreator>();
        if (noteRenderer == null)
            noteRenderer = FindFirstObjectByType<NoteRenderer>();
        if (musicSystem == null)
            musicSystem = FindFirstObjectByType<InteractiveMusicSystem>();
    }

    void Start()
    {
        // Use singletons where available for efficiency - moved to Start to wait for Bootstrap scene
        if (audioManager == null)
            audioManager = AudioManager.Instance;
        if (uiManager == null)
            uiManager = UIManager.Instance;

        // Log errors if critical components are missing after attempting to find them
        // if (noteCreator == null) Debug.LogError("GameplayManager Error: GameNoteCreator reference not set and could not be found in scene!");
        // if (noteRenderer == null) Debug.LogError("GameplayManager Error: NoteRenderer reference not set and could not be found in scene!");
        // if (musicSystem == null) Debug.LogError("GameplayManager Error: InteractiveMusicSystem reference not set and could not be found in scene!");
        // if (audioManager == null) Debug.LogError("GameplayManager Error: AudioManager instance not available!");
        // if (uiManager == null) Debug.LogError("GameplayManager Error: UIManager instance not available!");

        SetupGameplaySystems();
    }

    void SetupGameplaySystems()
    {
        // Reset gameplay stats to default values
        ResetGameplayStats();

        // Subscribe to game events for coordinated gameplay
        SubscribeToEvents();

        // NOTE: Do NOT call ChangeGameState here.
        // GameManager.InitializeGameManager() already sets the initial state (MainMenu).
        // The user navigates to SongSelection via the MainMenu "Start" button.
        // Forcing SongSelection here hijacks the state and skips the MainMenu.
    }

    void SubscribeToEvents()
    {
        //Debug.Log("ğŸ”— SUBSCRIBING TO EVENTS...");

        // *** PREVENT DUPLICATE SUBSCRIPTION — unsubscribe all first ***
        GameNoteCreator.OnNotesGenerated -= OnNotesSpawnRequest;
        GameNoteCreator.OnGenerationComplete -= HandleSongComplete;
        InteractiveMusicSystem.OnChordDetected -= HandleChordDetected;
        GameManager.OnGameStateChanged -= HandleGameStateChange;

        // GameNoteCreator events
        GameNoteCreator.OnNotesGenerated += OnNotesSpawnRequest; // CRITICAL!
        GameNoteCreator.OnGenerationComplete += HandleSongComplete;

        // InteractiveMusicSystem events
        InteractiveMusicSystem.OnChordDetected += HandleChordDetected;

        // AudioManager events
        if (audioManager != null)
        {
            audioManager.OnMusicFinished -= HandleMusicFinished;
            audioManager.OnMusicFinished += HandleMusicFinished;
        }

        // GameManager events
        GameManager.OnGameStateChanged += HandleGameStateChange;

        //Debug.Log("âœ… Event subscriptions completed!");
    }

    private void OnNotesSpawnRequest(List<GameNoteInfo> notes, double dspTime)
    {
        if (noteRenderer != null)
        {
            noteRenderer.SpawnNotes(notes, dspTime);
        }
    }

    async Awaitable DelayedGameStartAsync(float delay)
    {
        await Awaitable.WaitForSecondsAsync(delay);
        if (currentSong != null)
        {
            StartGameplay(currentSong);
        }
    }

    /// <summary>
    /// *** ORÄ°JÄ°NAL JAVA ALGORÄ°TMASI RESTORE EDÄ°LDÄ°! ***
    /// Update - Main Game Loop (like original World.java)
    /// CRITICAL: SÃ¼rekli noteCreator.GetNote() Ã§aÄŸÄ±rmasÄ±!
    /// </summary>
    void Update()
    {
        if (!isGameActive || GameManager.Instance?.CurrentGameState == GameState.Paused) return;

        double dspTime = AudioSettings.dspTime;
        float deltaTime = Time.deltaTime;

        // *** ORÄ°JÄ°NAL JAVA: Ana zamanlama dÃ¶ngÃ¼sÃ¼ ***
        UpdateGameTiming(deltaTime);

        // *** CRITICAL: SÃ¼rekli nota generation Ã§aÄŸrÄ±sÄ±! ***
        // Bu oldgame.md'deki en Ã¶nemli mekanik!
        UpdateNoteGeneration(deltaTime, dspTime);
    }

    void UpdateGameTiming(float deltaTime)
    {
        if (useAudioTimeSync && audioManager != null && audioManager.IsMusicPlaying)
        {
            currentGameTime = audioManager.CurrentMusicTime;
        }
        else
        {
            // Fallback to game time
            currentGameTime += deltaTime;
        }

        OnGameTimeUpdated?.Invoke(currentGameTime);

        // Check for song end
        if (currentGameTime >= songDuration && songDuration > 0)
        {
            EndGameplay();
        }

        // Stats are now updated only when hits/misses occur, not every frame
    }

    /// <summary>
    /// *** EXACT JAVA: Main game loop calls getNote() ***
    /// Original Java: World.java updateInvaders() calls gameNoteCreator.getNote()
    /// </summary>
    void UpdateNoteGeneration(float deltaTime, double dspTime)
    {
        if (noteCreator != null && isGameActive)
        {
            noteCreator.GetNote(deltaTime, dspTime);
        }
    }

    #region Song Management & Game Flow

    /// <summary>
    /// Start gameplay with a song - NEW: Uses SongDatabase system as planned
    /// </summary>
    public void StartGameplay(int musicId)
    {
        if (SongDatabase.Instance == null || !SongDatabase.Instance.IsLoaded())
        {
            Debug.LogError("ğŸ® SongDatabase not available! Cannot start gameplay.");
            return;
        }

        // NEW SYSTEM: Get song data from centralized database as planned
        SongDatabaseInfo songInfo = SongDatabase.Instance.GetSongById(musicId);
        if (songInfo == null)
        {
            Debug.LogError($"ğŸ® Song with ID {musicId} not found in database!");
            return;
        }

        // Convert to internal SongData format using ScriptableObject
        currentSong = ScriptableObject.CreateInstance<SongData>();
        currentSong.songName = songInfo.title;
        currentSong.artist = songInfo.artist;
        currentSong.duration = songInfo.duration > 0 ? songInfo.duration : EstimateDuration(songInfo.tempo); // Use real duration if available
        currentSong.bpm = songInfo.tempo;
        currentSong.audioFilePath = $"Audio/{songInfo.songKey}";
        currentSong.noteChartPath = $"Song_Note_Jsons/Individual/{songInfo.songKey}";
        currentSong.songKey = songInfo.songKey;
        currentSong.difficulty = songInfo.difficulty;

        // Debug.Log($"ğŸ® Starting gameplay via SongDatabase: {currentSong.songName} by {currentSong.artist} (Tempo: {songInfo.tempo})");
        _ = StartGameplaySequenceAsync();
    }

    /// <summary>
    /// BACKWARD COMPATIBILITY: Start gameplay with GameplaySongData (for existing UI)
    /// </summary>
    public void StartGameplay(GameplaySongData songData)
    {
        if (songData == null)
        {
            Debug.LogError("ğŸ® Cannot start gameplay with null song!");
            return;
        }

        // Try to find the song in database first for consistency
        if (SongDatabase.Instance != null && SongDatabase.Instance.IsLoaded())
        {
            var dbSong = SongDatabase.Instance.GetSongByKey(songData.songKey);
            if (dbSong != null)
            {
                Debug.Log("ğŸµ Found song in database, using SongDatabase system...");
                StartGameplay(dbSong.musicId);
                return;
            }
        }

        // Fallback to old system if not found in database
        Debug.LogWarning("ğŸµ Song not found in SongDatabase, using legacy system...");

        // Convert to internal SongData format using ScriptableObject
        currentSong = ScriptableObject.CreateInstance<SongData>();
        currentSong.songName = songData.songName;
        currentSong.artist = songData.artist;
        currentSong.duration = songData.duration;
        currentSong.bpm = songData.bpm;
        currentSong.audioFilePath = songData.audioFilePath;
        currentSong.noteChartPath = songData.chartFilePath;
        currentSong.songKey = songData.songKey;
        currentSong.difficulty = songData.difficulty;

        _ = StartGameplaySequenceAsync();
    }

    // Overload for backward compatibility
    public void StartGameplay(SongData song)
    {
        if (song == null)
        {
            Debug.LogError("ğŸ® Cannot start gameplay with null song!");
            return;
        }

        currentSong = song;
        _ = StartGameplaySequenceAsync();
    }

    private async Awaitable StartGameplaySequenceAsync()
    {
        var selectedInstrument = GameManager.Instance != null ? GameManager.Instance.GetSelectedInstrument() : InstrumentType.Piano;

        if (audioManager != null)
        {
            audioManager.ResetDropStats();
            _ = audioManager.PrewarmInstrumentClipsAsync(selectedInstrument);
        }

        PrepareGameplaySystems();

        await ShowCountdownAsync();

        if (audioManager != null)
        {
            await audioManager.EnsureInstrumentReadyAsync(selectedInstrument);
        }

        // Music loading now happens in the background.
        _ = StartMusicWithDelayAsync(songStartDelay);

        // Gameplay logic starts immediately.
        BeginActiveGameplay();
    }

    /// <summary>
    /// *** ORÄ°JÄ°NAL JAVA ALGORÄ°TMASI RESTORE EDÄ°LDÄ°! ***
    /// PrepareGameplaySystems - oldgame.md'deki sÄ±rayla sistem hazÄ±rlÄ±ÄŸÄ±
    /// </summary>
    void PrepareGameplaySystems()
    {
        if (showDebugLogs) Debug.Log("ğŸ® Preparing gameplay systems...");

        // ğŸ¼ MUSICAL INTEGRITY SYSTEM - Bootstrap tarafÄ±ndan oluÅŸturuldu, sadece referansÄ± al
        // Bootstrap.cs artÄ±k bu sistemi garanti ediyor, manuel oluÅŸturmaya gerek yok

        ResetGameplayStats();

        if (noteRenderer != null)
        {
            noteRenderer.ClearAllNotes();
        }

        // Wire up the note travel time to the note creator for perfect sync.
        if (noteRenderer != null && noteCreator != null)
        {
            // MUSICAL INTEGRITY SYSTEM INTEGRATION
            if (currentSong != null)
            {
                // CRITICAL FIX: Use the actual songKey from database, not derived from songName
                // songName: "Vidalita" vs songKey: "vidalita_traditional"
                string songKey = !string.IsNullOrEmpty(currentSong.songKey) 
                    ? currentSong.songKey 
                    : currentSong.songName.Replace(" ", "_").ToLower(); // Fallback only

                // NoteCreator'a song key'i set et
                noteCreator.SetCurrentSong(songKey);

                // NoteRenderer'a tempo ve song key'i set et
                noteRenderer.SetTempo(currentSong.bpm, songKey);

                if (showDebugLogs)
                {
                    Debug.Log($"[MUSICAL INTEGRITY] Systems synchronized:");
                    Debug.Log($"   Song: {songKey} ({currentSong.bpm} BPM)");
                    Debug.Log($"   NoteCreator & NoteRenderer updated with Musical Integrity System");
                }
            }
            else
            {
                // Fallback - sadece tempo set et
                noteRenderer.SetTempo(120); // Default tempo
                if (showDebugLogs) Debug.Log($"âš ï¸ No song data available, using default tempo (120 BPM)");
            }

            float travelTimeMs = noteRenderer.GetNoteTravelTime() * 1000f;
            noteCreator.SetFirstDelay(travelTimeMs);
            if (showDebugLogs) Debug.Log($"ğŸµ [TIMING CHECK] Note travel time set for perfect sync: {travelTimeMs:F0}ms");
        }

        if (noteCreator != null && currentSong != null)
        {
            try
            {
                // NEW: Pass tempo information to GameNoteCreator as planned
                noteCreator.LoadSong(currentSong); // This will use compatibility layer

                // NEW: Set dynamic hit timing windows based on MusicalIntegritySystem
                var syncData = MusicalIntegritySystem.Instance?.CalculateOptimalSync(currentSong.songKey, currentSong.bpm);
                if (syncData != null)
                {
                    var hitZoneManager = FindFirstObjectByType<HitZoneManager>();
                    if (hitZoneManager != null)
                    {
                        hitZoneManager.perfectWindowMs = syncData.hitTimingWindows.perfectMs;
                        hitZoneManager.goodWindowMs = syncData.hitTimingWindows.goodMs;
                        hitZoneManager.okayWindowMs = syncData.hitTimingWindows.okayMs;
                        if (showDebugLogs)
                        {
                            Debug.Log($"ğŸ¯ Hit windows set by MusicalIntegritySystem: P={syncData.hitTimingWindows.perfectMs:F0}ms, G={syncData.hitTimingWindows.goodMs:F0}ms, O={syncData.hitTimingWindows.okayMs:F0}ms");
                        }
                    }
                }

                // TODO: When GameNoteCreator is refactored per RefactorParse.md,
                // this should become: noteCreator.LoadAndPrepareSong(chartData, currentSong.bpm);

                if (showDebugLogs) Debug.Log($"ğŸµ Song prepared with tempo: {currentSong.bpm} BPM");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ğŸš¨ Song loading failed: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("ğŸš¨ GameNoteCreator or currentSong not ready!");
        }

        if (musicSystem != null && GameManager.Instance != null)
        {
            var selectedInstrument = GameManager.Instance.GetSelectedInstrument();
            musicSystem.SetInstrument(selectedInstrument);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameState.Playing);
        }
    }

    /// <summary>
    /// Estimate song duration based on tempo (BPM)
    /// </summary>
    private float EstimateDuration(int tempo) => GameConstants.EstimateDurationSeconds(tempo);

    private async Awaitable ShowCountdownAsync()
    {
        _isCountingDown = true;

        for (int i = (int)countdownDuration; i > 0; i--)
        {
            if (showDebugLogs) Debug.Log($"Starting in {i}...");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowCountdown(i);
            }

            await Awaitable.WaitForSecondsAsync(1f);
        }

        // Show "GO!" message
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowCountdown(0);
        }

        await Awaitable.WaitForSecondsAsync(0.5f);

        // Hide countdown UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideCountdown();
        }

        _isCountingDown = false;
    }

    private async Awaitable StartMusicWithDelayAsync(float delay)
    {
        // TODO: Provide background music clips and enable this path when available.
        if (!enableBackgroundMusic)
        {
            if (showDebugLogs) Debug.Log("Background music disabled. TODO: Assign song audio clips and enable background music.");
            return;
        }

        if (string.IsNullOrEmpty(currentSong.audioFilePath))
        {
            if (showDebugLogs) Debug.Log("No background music path provided.");
            return;
        }

        // Load audio clip asynchronously
        ResourceRequest request = Resources.LoadAsync<AudioClip>(currentSong.audioFilePath);
        await Awaitable.FromAsyncOperation(request);

        AudioClip clip = request.asset as AudioClip;

        if (clip != null)
        {
            await Awaitable.WaitForSecondsAsync(delay);
            if (audioManager != null)
            {
                audioManager.PlayMusic(clip);
                if (showDebugLogs) Debug.Log($"Asynchronously loaded and playing: {clip.name}");
            }
        }
        else
        {
            Debug.LogWarning($"Audio clip not found at path: {currentSong.audioFilePath}.");
        }
    }

    /// <summary>
    /// *** ORÄ°JÄ°NAL JAVA ALGORÄ°TMASI RESTORE EDÄ°LDÄ°! ***
    /// BeginActiveGameplay - Oyunun aktif baÅŸlatÄ±lmasÄ±
    /// </summary>
    void BeginActiveGameplay()
    {
        isGameActive = true;
        gameStartTime = Time.time;
        
        // Start DSP timing system for accurate rhythm judgment
        if (RhythmTimingSystem.Instance != null)
        {
            RhythmTimingSystem.Instance.StartSong();
        }
        
        OnGameplayStarted?.Invoke();
    }

    public void PauseGameplay()
    {
        if (!isGameActive || GameManager.Instance?.CurrentGameState == GameState.Paused) return;
        Time.timeScale = 0f;

        if (audioManager != null)
        {
            audioManager.PauseMusic();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameState.Paused);
        }

        //Debug.Log("â¸ï¸ Gameplay paused");
    }

    public void ResumeGameplay()
    {
        if (!isGameActive || GameManager.Instance?.CurrentGameState != GameState.Paused)
        {
            return;
        }
        Time.timeScale = 1f;

        if (audioManager != null)
        {
            audioManager.ResumeMusic();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameState.Playing);
        }

        //Debug.Log($"Gameplay resumed! TimeScale: {Time.timeScale}");
    }

    /// <summary>
    /// Force stops gameplay without triggering state changes.
    /// Used for scene transitions (restart/menu) to avoid GameOver panel.
    /// </summary>
    public void ForceStopGameplay()
    {
        isGameActive = false;
        Time.timeScale = 1f;
        
        // Kill all active tweens
        DG.Tweening.DOTween.KillAll();
        
        // Stop audio but don't trigger any events
        if (audioManager != null)
        {
            audioManager.StopMusic();
        }
        
        // Don't call EndGameSession - we're reloading the scene
    }

    public void EndGameplay()
    {
        if (!isGameActive) return;

        isGameActive = false;
        Time.timeScale = 1f;

        // Kill all DOTween animations to prevent errors on scene reload
        DG.Tweening.DOTween.KillAll();

        // Stop audio
        if (audioManager != null)
        {
            audioManager.StopMusic();
        }
        
        // Stop DSP timing system
        if (RhythmTimingSystem.Instance != null)
        {
            RhythmTimingSystem.Instance.StopSong();
        }

        // Calculate final stats
        CalculateFinalStats();

        // Feed stats through gamification pipeline
        if (GameManager.Instance != null && lastFinalStats != null)
        {
            GameManager.Instance.EndGameSessionWithStats(lastFinalStats);
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.EndGameSession();
        }

        OnGameplayEnded?.Invoke();
    }
    #endregion

    #region Event Handlers (Original World.java onTap logic)

    // HandleNotesGenerated removed - using direct SpawnNotes call

    void HandleSongComplete()
    {
        //Debug.Log("ğŸµ Song generation complete!");
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
    }


    void HandleMusicFinished()
    {
        //Debug.Log("ğŸµ Background music finished");
        EndGameplay();
    }

    void HandleGameStateChange(GameState newState)
    {
        // CRITICAL FIX: Don't compare newState with CurrentGameState - they're already the same!
        // Just check if we're active and respond to the state change directly
        if (!isGameActive) return;
        
        switch (newState)
        {
            case GameState.Paused:
                PauseGameplay();
                break;
            case GameState.Playing:
                // Only resume if we were paused (isPaused flag check happens inside ResumeGameplay)
                ResumeGameplay();
                break;
        }
    }
    #endregion

    #region Statistics & UI Updates

    void UpdateGameStats()
    {
        // Weighted accuracy: Perfect=100%, Good=75%, Okay=50%, Miss=0%
        int totalNotes = perfectHits + goodHits + okayHits + missedNotes;
        if (totalNotes > 0)
        {
            accuracy = ((perfectHits * 1.0f + goodHits * 0.75f + okayHits * 0.5f) / totalNotes) * 100f;
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

    // Call this when hits/misses occur instead of every frame
    public void UpdateStatsOnHit()
    {
        UpdateGameStats();
    }

    void CalculateFinalStats()
    {
        UpdateGameStats();

        var finalStats = new GameplayStats
        {
            totalNotesHit = totalNotesHit,
            perfectHits = perfectHits,
            goodHits = goodHits,
            okayHits = okayHits,
            missedNotes = missedNotes,
            maxCombo = maxCombo,
            accuracy = accuracy,
            totalScore = GameManager.Instance?.GetCurrentSession()?.currentScore ?? 0,
            songName = currentSong != null ? currentSong.songName : "Unknown",
            songKey = currentSong != null ? currentSong.songKey : "",
            artist = currentSong != null ? currentSong.artist : "",
            difficulty = currentSong != null ? currentSong.difficulty : DifficultyLevel.Easy,
            songDuration = currentSong != null ? currentSong.duration : 0f
        };

        lastFinalStats = finalStats;
    }

    // Cached final stats for gamification integration
    private GameplayStats lastFinalStats;

    /// <summary>
    /// Returns the last calculated final stats (available after EndGameplay)
    /// </summary>
    public GameplayStats GetFinalStats() => lastFinalStats;

    /// <summary>
    /// Register a successful hit. Called by HitZoneManager when a note is hit.
    /// </summary>
    public void RegisterHit(HitAccuracy accuracy)
    {
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
            case HitAccuracy.Okay:
                okayHits++;
                currentCombo++;
                break;
        }

        if (currentCombo > maxCombo)
            maxCombo = currentCombo;

        UpdateStatsOnHit();
    }

    /// <summary>
    /// Register a missed note. Called by NoteRenderer when a note passes without being hit.
    /// </summary>
    public void RegisterMiss()
    {
        missedNotes++;
        currentCombo = 0;

        UpdateStatsOnHit();
    }

    void ResetGameplayStats()
    {
        totalNotesHit = 0;
        perfectHits = 0;
        goodHits = 0;
        okayHits = 0;
        missedNotes = 0;
        currentCombo = 0;
        maxCombo = 0;
        accuracy = 0f;
        currentGameTime = 0f;
    }
    #endregion

    #region Public Interface

    public bool IsGameActive() => isGameActive;
    public bool IsGamePaused() => GameManager.Instance != null && GameManager.Instance.CurrentGameState == GameState.Paused;
    public float GetCurrentGameTime() => currentGameTime;
    public int GetCurrentCombo() => currentCombo;
    public float GetAccuracy() => accuracy;

    public void SetNoteSpeed(float speed)
    {
        // DEPRECATED: Directly control NoteRenderer.speedMultiplier instead
        if (noteRenderer != null)
            noteRenderer.SetSpeedMultiplier(speed);
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
            okayHits = okayHits,
            missedNotes = missedNotes,
            maxCombo = maxCombo,
            accuracy = accuracy,
            songName = currentSong != null ? currentSong.songName : "None"
        };
    }
    #endregion

    void OnApplicationPause(bool pauseStatus)
    {
        if (enablePauseOnFocusLoss && pauseStatus && isGameActive && GameManager.Instance?.CurrentGameState != GameState.Paused)
        {
            PauseGameplay();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
#if !UNITY_EDITOR
        if (enablePauseOnFocusLoss && !hasFocus && isGameActive && GameManager.Instance?.CurrentGameState != GameState.Paused)
        {
            PauseGameplay();
        }
#endif
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        GameNoteCreator.OnNotesGenerated -= OnNotesSpawnRequest;
        GameNoteCreator.OnGenerationComplete -= HandleSongComplete;
        InteractiveMusicSystem.OnChordDetected -= HandleChordDetected;
        GameManager.OnGameStateChanged -= HandleGameStateChange;

        if (audioManager != null)
        {
            audioManager.OnMusicFinished -= HandleMusicFinished;
        }
    }
}

// GameplayStats moved to DataStructures.cs to avoid duplicates
