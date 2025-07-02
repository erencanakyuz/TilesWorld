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
    [Header("🎮 Gameplay Configuration")]
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private bool enablePauseOnFocusLoss = true;

    [Header("🎵 Song Management")]
    [SerializeField] private SongData currentSong;
    [SerializeField] private float songStartDelay = 1f;
    [SerializeField] private bool useAudioTimeSync = true;

    [Header("🎯 Gameplay Settings")]
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
    [SerializeField] private UIManager uiManager;

    [Header("🔧 Debugging")]
    [SerializeField] private bool showDebugLogs = false;

    // Game state management
    private bool isGameActive = false;
    private bool isGamePaused = false;
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
        if (noteCreator == null) Debug.LogError("GameplayManager Error: GameNoteCreator reference not set and could not be found in scene!");
        if (noteRenderer == null) Debug.LogError("GameplayManager Error: NoteRenderer reference not set and could not be found in scene!");
        if (musicSystem == null) Debug.LogError("GameplayManager Error: InteractiveMusicSystem reference not set and could not be found in scene!");
        if (audioManager == null) Debug.LogError("GameplayManager Error: AudioManager instance not available!");
        if (uiManager == null) Debug.LogError("GameplayManager Error: UIManager instance not available!");

        SetupGameplaySystems();
    }

    void SetupGameplaySystems()
    {
        // Reset gameplay stats to default values
        ResetGameplayStats();

        // Subscribe to game events for coordinated gameplay
        SubscribeToEvents();

        // Set initial game state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameState.SongSelection);
        }
    }

    void SubscribeToEvents()
    {
        //Debug.Log("🔗 SUBSCRIBING TO EVENTS...");

        // *** PREVENT DUPLICATE SUBSCRIPTION ***
        GameNoteCreator.OnNotesGenerated -= OnNotesSpawnRequest;

        // GameNoteCreator events - True Dynamic System ile events kullanıyoruz!
        GameNoteCreator.OnNotesGenerated += OnNotesSpawnRequest; // CRITICAL!
        GameNoteCreator.OnGenerationComplete += HandleSongComplete;

        // InteractiveMusicSystem events
        InteractiveMusicSystem.OnChordDetected += HandleChordDetected;
        InteractiveMusicSystem.OnMusicalEventCreated += HandleMusicalEvent;

        // AudioManager events
        if (audioManager != null)
        {
            audioManager.OnMusicFinished += HandleMusicFinished;
        }

        // GameManager events
        GameManager.OnGameStateChanged += HandleGameStateChange;

        //Debug.Log("✅ Event subscriptions completed!");
    }

    private void OnNotesSpawnRequest(List<GameNoteInfo> notes, double dspTime)
    {
        if (noteRenderer != null)
        {
            noteRenderer.SpawnNotes(notes, dspTime);
        }
    }

    IEnumerator DelayedGameStartCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentSong != null)
        {
            StartGameplay(currentSong);
        }
    }

    /// <summary>
    /// *** ORİJİNAL JAVA ALGORİTMASI RESTORE EDİLDİ! ***
    /// Update - Main Game Loop (like original World.java)
    /// CRITICAL: Sürekli noteCreator.GetNote() çağırması!
    /// </summary>
    void Update()
    {
        if (!isGameActive || isGamePaused) return;

        double dspTime = AudioSettings.dspTime;
        float deltaTime = Time.deltaTime;

        // *** ORİJİNAL JAVA: Ana zamanlama döngüsü ***
        UpdateGameTiming(deltaTime);

        // *** CRITICAL: Sürekli nota generation çağrısı! ***
        // Bu oldgame.md'deki en önemli mekanik!
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

        // Update game stats
        UpdateGameStats();
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
            Debug.LogError("🎮 SongDatabase not available! Cannot start gameplay.");
            return;
        }

        // NEW SYSTEM: Get song data from centralized database as planned
        SongDatabaseInfo songInfo = SongDatabase.Instance.GetSongById(musicId);
        if (songInfo == null)
        {
            Debug.LogError($"🎮 Song with ID {musicId} not found in database!");
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

        Debug.Log($"🎮 Starting gameplay via SongDatabase: {currentSong.songName} by {currentSong.artist} (Tempo: {songInfo.tempo})");
        StartCoroutine(StartGameplaySequence());
    }

    /// <summary>
    /// BACKWARD COMPATIBILITY: Start gameplay with GameplaySongData (for existing UI)
    /// </summary>
    public void StartGameplay(SongSelectionManager.GameplaySongData songData)
    {
        if (songData == null)
        {
            Debug.LogError("🎮 Cannot start gameplay with null song!");
            return;
        }

        // Try to find the song in database first for consistency
        if (SongDatabase.Instance != null && SongDatabase.Instance.IsLoaded())
        {
            var dbSong = SongDatabase.Instance.GetSongByKey(songData.songKey);
            if (dbSong != null)
            {
                Debug.Log("🎵 Found song in database, using SongDatabase system...");
                StartGameplay(dbSong.musicId);
                return;
            }
        }

        // Fallback to old system if not found in database
        Debug.LogWarning("🎵 Song not found in SongDatabase, using legacy system...");

        // Convert to internal SongData format using ScriptableObject
        currentSong = ScriptableObject.CreateInstance<SongData>();
        currentSong.songName = songData.songName;
        currentSong.artist = songData.artist;
        currentSong.duration = songData.duration;
        currentSong.bpm = songData.bpm;
        currentSong.audioFilePath = songData.audioFilePath;
        currentSong.noteChartPath = songData.chartFilePath;
        currentSong.songKey = songData.songKey;

        StartCoroutine(StartGameplaySequence());
    }

    // Overload for backward compatibility
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

    private IEnumerator StartGameplaySequence()
    {
        PrepareGameplaySystems();

        yield return StartCoroutine(ShowCountdown());

        // Music loading now happens in the background.
        StartCoroutine(StartMusicWithDelay(songStartDelay));

        // Gameplay logic starts immediately.
        BeginActiveGameplay();
    }

    /// <summary>
    /// *** ORİJİNAL JAVA ALGORİTMASI RESTORE EDİLDİ! ***
    /// PrepareGameplaySystems - oldgame.md'deki sırayla sistem hazırlığı
    /// </summary>
    void PrepareGameplaySystems()
    {
        if (showDebugLogs) Debug.Log("🎮 Preparing gameplay systems...");

        // 🎼 MUSICAL INTEGRITY SYSTEM KURULUMU
        if (MusicalIntegritySystem.Instance == null)
        {
            var musicalIntegrityGO = new GameObject("MusicalIntegritySystem");
            musicalIntegrityGO.AddComponent<MusicalIntegritySystem>();
            if (showDebugLogs) Debug.Log("🎼 Musical Integrity System created and initialized");
        }

        ResetGameplayStats();

        if (noteRenderer != null)
        {
            noteRenderer.ClearAllNotes();
        }

        // Wire up the note travel time to the note creator for perfect sync.
        if (noteRenderer != null && noteCreator != null)
        {
            // 🎼 MUSICAL INTEGRITY SYSTEM ENTEGRASYONU
            if (currentSong != null)
            {
                string songKey = currentSong.songName.Replace(" ", "_").ToLower(); // Song key oluştur

                // NoteCreator'a song key'i set et
                noteCreator.SetCurrentSong(songKey);

                // NoteRenderer'a tempo ve song key'i set et
                noteRenderer.SetTempo(currentSong.bpm, songKey);

                if (showDebugLogs)
                {
                    Debug.Log($"🎼 [MUSICAL INTEGRITY] Systems synchronized:");
                    Debug.Log($"   🎵 Song: {songKey} ({currentSong.bpm} BPM)");
                    Debug.Log($"   🎯 NoteCreator & NoteRenderer updated with Musical Integrity System");
                }
            }
            else
            {
                // Fallback - sadece tempo set et
                noteRenderer.SetTempo(120); // Default tempo
                if (showDebugLogs) Debug.Log($"⚠️ No song data available, using default tempo (120 BPM)");
            }

            float travelTimeMs = noteRenderer.GetNoteTravelTime() * 1000f;
            noteCreator.SetFirstDelay(travelTimeMs);
            if (showDebugLogs) Debug.Log($"🎵 [TIMING CHECK] Note travel time set for perfect sync: {travelTimeMs:F0}ms");
        }

        if (noteCreator != null && currentSong != null)
        {
            try
            {
                // NEW: Pass tempo information to GameNoteCreator as planned
                noteCreator.LoadSong(currentSong); // This will use compatibility layer

                // TODO: When GameNoteCreator is refactored per RefactorParse.md,
                // this should become: noteCreator.LoadAndPrepareSong(chartData, currentSong.bpm);

                if (showDebugLogs) Debug.Log($"🎵 Song prepared with tempo: {currentSong.bpm} BPM");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"🚨 Song loading failed: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("🚨 GameNoteCreator or currentSong not ready!");
        }

        if (musicSystem != null && GameManager.Instance != null)
        {
            musicSystem.SetInstrument(GameManager.Instance.GetSelectedInstrument());
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameState.Playing);
        }
    }

    /// <summary>
    /// Estimate song duration based on tempo (BPM)
    /// </summary>
    private float EstimateDuration(int tempo)
    {
        // Simple heuristic: slower songs are generally longer
        // This is a rough estimation until we have actual duration data
        if (tempo < 60) return 240f;      // Very slow: 4 minutes
        if (tempo < 80) return 210f;      // Slow: 3.5 minutes  
        if (tempo < 120) return 180f;     // Moderate: 3 minutes
        if (tempo < 140) return 150f;     // Fast: 2.5 minutes
        return 120f;                      // Very fast: 2 minutes
    }

    private IEnumerator ShowCountdown()
    {
        _isCountingDown = true;

        for (int i = (int)countdownDuration; i > 0; i--)
        {
            if (showDebugLogs) Debug.Log($"🎮 Starting in {i}...");

            // Show countdown UI number
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowCountdown(i);
            }

            yield return new WaitForSeconds(1f);
        }

        // Show "GO!" or "Start!" message
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowCountdown(0); // 0 = GO!
        }

        yield return new WaitForSeconds(0.5f); // Brief pause for "GO!" message

        // Hide countdown UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideCountdown();
        }

        _isCountingDown = false;
    }

    private IEnumerator StartMusicWithDelay(float delay)
    {
        if (string.IsNullOrEmpty(currentSong.audioFilePath))
        {
            if (showDebugLogs) Debug.Log("No background music path provided.");
            yield break;
        }

        ResourceRequest request = Resources.LoadAsync<AudioClip>(currentSong.audioFilePath);
        yield return request; // Wait for the async operation to complete

        AudioClip clip = request.asset as AudioClip;

        if (clip != null)
        {
            yield return new WaitForSeconds(delay);
            if (audioManager != null)
            {
                audioManager.PlayMusic(clip);
                if (showDebugLogs) Debug.Log($"🎵 Asynchronously loaded and playing: {clip.name}");
            }
        }
        else
        {
            Debug.LogWarning($"Audio clip not found at path: {currentSong.audioFilePath}.");
        }
    }

    /// <summary>
    /// *** ORİJİNAL JAVA ALGORİTMASI RESTORE EDİLDİ! ***
    /// BeginActiveGameplay - Oyunun aktif başlatılması
    /// </summary>
    void BeginActiveGameplay()
    {
        isGameActive = true;
        isGamePaused = false;
        gameStartTime = Time.time;
        OnGameplayStarted?.Invoke();
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

        //Debug.Log("⏸️ Gameplay paused");
    }

    public void ResumeGameplay()
    {
        if (!isGameActive || !isGamePaused)
        {
            //Debug.Log($"🎮 Resume blocked: isGameActive={isGameActive}, isGamePaused={isGamePaused}");
            return;
        }

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

        //Debug.Log($"▶️ Gameplay resumed! TimeScale: {Time.timeScale}");
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

        //Debug.Log($"🏁 Gameplay ended! Final score: Accuracy {accuracy:F1}%, Max combo: {maxCombo}");
    }
    #endregion

    #region Event Handlers (Original World.java onTap logic)

    // HandleNotesGenerated removed - using direct SpawnNotes call

    void HandleSongComplete()
    {
        //Debug.Log("🎵 Song generation complete!");
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

    void HandleMusicalEvent(MusicalEvent musicalEvent)
    {
        // Additional processing for musical events if needed
    }

    void HandleMusicFinished()
    {
        //Debug.Log("🎵 Background music finished");
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

        //Debug.Log($"🎮 Final Stats: {finalStats}");
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
#if !UNITY_EDITOR
        if (enablePauseOnFocusLoss && !hasFocus && isGameActive && !isGamePaused)
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
        InteractiveMusicSystem.OnMusicalEventCreated -= HandleMusicalEvent;
        GameManager.OnGameStateChanged -= HandleGameStateChange;

        if (audioManager != null)
        {
            audioManager.OnMusicFinished -= HandleMusicFinished;
        }
    }
}

// GameplayStats moved to DataStructures.cs to avoid duplicates