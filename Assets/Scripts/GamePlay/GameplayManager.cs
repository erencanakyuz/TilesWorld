using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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
    [SerializeField] private UIManager uiManager;
    [SerializeField] private JsonMusicParser jsonMusicParser;

    // Game state management
    private bool isGameActive = false;
    private bool isGamePaused = false;
    private bool isSongLoaded = false;
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
        InitializeGameplayManager();
    }

    void Start()
    {
        InitializeGameplayManager();
    }

    void InitializeGameplayManager()
    {
        // Get system references if not assigned in Inspector
        if (noteCreator == null)
            noteCreator = FindFirstObjectByType<GameNoteCreator>();
        if (noteRenderer == null)
            noteRenderer = FindFirstObjectByType<NoteRenderer>();
        if (musicSystem == null)
            musicSystem = FindFirstObjectByType<InteractiveMusicSystem>();
        if (audioManager == null)
            audioManager = FindFirstObjectByType<AudioManager>();
        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();
        if (jsonMusicParser == null)
            jsonMusicParser = FindFirstObjectByType<JsonMusicParser>();

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
        Debug.Log("🔗 SUBSCRIBING TO EVENTS...");

        // *** PREVENT DUPLICATE SUBSCRIPTION ***
        GameNoteCreator.OnNotesGenerated -= noteRenderer.SpawnNotes; // Remove if exists

        // GameNoteCreator events - True Dynamic System ile events kullanıyoruz!
        Debug.Log("🔗 Subscribing to GameNoteCreator.OnNotesGenerated (duplicate-safe)");
        GameNoteCreator.OnNotesGenerated += noteRenderer.SpawnNotes; // CRITICAL!
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

        Debug.Log("✅ Event subscriptions completed!");
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

        float deltaTime = Time.deltaTime;

        // *** ORİJİNAL JAVA: Ana zamanlama döngüsü ***
        UpdateGameTiming(deltaTime);

        // *** CRITICAL: Sürekli nota generation çağrısı! ***
        // Bu oldgame.md'deki en önemli mekanik!
        UpdateNoteGeneration(deltaTime);
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
    void UpdateNoteGeneration(float deltaTime)
    {
        if (noteCreator != null && isGameActive)
        {
            noteCreator.GetNote(deltaTime);
        }
    }

    #region Song Management & Game Flow

    /// <summary>
    /// Start gameplay with a song - Main entry point
    /// </summary>
    public void StartGameplay(SongSelectionManager.GameplaySongData songData)
    {
        if (songData == null)
        {
            Debug.LogError("🎮 Cannot start gameplay with null song!");
            return;
        }

        // Convert to internal SongData format using ScriptableObject
        currentSong = ScriptableObject.CreateInstance<SongData>();
        currentSong.songName = songData.songName;
        currentSong.artist = songData.artist;
        currentSong.duration = songData.duration;
        currentSong.bpm = songData.bpm;
        currentSong.audioFilePath = songData.audioFilePath;
        currentSong.noteChartPath = songData.chartFilePath;
        currentSong.songKey = songData.songKey;

        Debug.Log($"🎮 Starting gameplay sequence for: {currentSong.songName} by {currentSong.artist}");
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

    /// <summary>
    /// *** ORİJİNAL JAVA ALGORİTMASI RESTORE EDİLDİ! ***
    /// PrepareGameplaySystems - oldgame.md'deki sırayla sistem hazırlığı
    /// </summary>
    void PrepareGameplaySystems()
    {
        ResetGameplayStats();

        if (noteRenderer != null)
        {
            noteRenderer.ClearAllNotes();
        }

        if (noteCreator != null && currentSong != null)
        {
            Debug.Log($"🎵 Loading song with dynamic system: {currentSong.songName}");

            // Direkt JSON loading deniyoruz
            try
            {
                noteCreator.LoadSong(currentSong); // Bu metod kendi JSON loading'ini yapıyor
                Debug.Log($"✅ Song loaded successfully!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"🚨 Song loading failed: {e.Message}");

                // Fallback: JsonMusicParser deniyoruz
                if (jsonMusicParser != null)
                {
                    Debug.Log("🎵 Fallback: Using JsonMusicParser...");
                    noteCreator.LoadSong(currentSong);
                }
                else
                {
                    Debug.LogError("🚨 JsonMusicParser not found! Manual JSON loading required.");
                }
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

    IEnumerator ShowCountdown()
    {
        _isCountingDown = true;

        for (int i = (int)countdownDuration; i > 0; i--)
        {
            Debug.Log($"🎮 Starting in {i}...");

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

    IEnumerator LoadAndStartSong()
    {
        isSongLoaded = false; // Mark as loading

        // Basic song setup - no audio loading required here
        // Audio will be handled in BeginActiveGameplay
        yield return new WaitForSeconds(0.1f); // Small delay for setup

        isSongLoaded = true;
        songDuration = currentSong.duration;

        Debug.Log($"🎮 Song loaded: {currentSong.songName} (Duration: {songDuration}s)");
    }

    /// <summary>
    /// *** ORİJİNAL JAVA ALGORİTMASI RESTORE EDİLDİ! ***
    /// BeginActiveGameplay - Oyunun aktif başlatılması
    /// </summary>
    void BeginActiveGameplay()
    {
        if (!isSongLoaded)
        {
            Debug.LogWarning("🎮 Song not fully loaded yet, delaying gameplay start...");
            StartCoroutine(DelayedGameStartCoroutine(songStartDelay));
            return;
        }

        gameStartTime = Time.time;
        isGameActive = true;  // *** CRITICAL: Bu satır olmadan Update() çalışmaz! ***
        isGamePaused = false;

        Debug.Log("🎮 *** GAMEPLAY AKTİF! *** Update() döngüsü başladı!");

        // Start music with configured delay
        StartCoroutine(StartMusicWithDelay());

        // Trigger event for other systems  
        OnGameplayStarted?.Invoke();

        Debug.Log("🎮 Gameplay başarıyla başlatıldı! Notalar gelmeye başlayacak...");
    }

    IEnumerator StartMusicWithDelay()
    {
        // Apply song start delay if configured
        if (songStartDelay > 0)
        {
            Debug.Log($"🎵 Applying song start delay: {songStartDelay}s");
            yield return new WaitForSeconds(songStartDelay);
        }

        // Start music (using existing AudioManager functionality)
        if (audioManager != null && !string.IsNullOrEmpty(currentSong.audioFilePath))
        {
            // Try to load audio clip from Resources
            AudioClip musicClip = Resources.Load<AudioClip>(currentSong.audioFilePath);
            if (musicClip != null)
            {
                audioManager.PlayMusic(musicClip, 0f);
                Debug.Log($"🎵 Müzik başlatıldı: {musicClip.name}");
            }
            else
            {
                // Background music not found - this is normal, game works with note-based music only
                Debug.Log($"🎵 Background music not available: {currentSong.audioFilePath} (Playing with note-based music only)");
            }
        }
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
        if (!isGameActive || !isGamePaused)
        {
            Debug.Log($"🎮 Resume blocked: isGameActive={isGameActive}, isGamePaused={isGamePaused}");
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

        Debug.Log($"▶️ Gameplay resumed! TimeScale: {Time.timeScale}");
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

    // HandleNotesGenerated removed - using direct SpawnNotes call

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
        GameNoteCreator.OnNotesGenerated -= noteRenderer.SpawnNotes;
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