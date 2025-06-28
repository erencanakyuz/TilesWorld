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
    [SerializeField] private UIManager uiManager;

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

        if (autoStartOnLoad && currentSong != null)
        {
            StartCoroutine(DelayedGameStartCoroutine(1f));
        }
        else if (autoStartOnLoad)
        {
            Debug.LogWarning("🎮 No song available for auto-start");
        }
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
        // GameNoteCreator events
        GameNoteCreator.OnNotesGenerated += HandleNotesGenerated;
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
    }

    IEnumerator DelayedGameStartCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentSong != null)
        {
            StartGameplay(currentSong);
        }
    }

    void Update()
    {
        if (!isGameActive || isGamePaused) return;

        float deltaTime = Time.deltaTime;
        UpdateGameTiming(deltaTime);
        UpdateNoteGeneration(deltaTime);
    }

    void UpdateGameTiming(float deltaTime)
    {
        if (isUsingAudioSync && audioManager != null && audioManager.IsMusicPlaying)
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
            }
        }
    }

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

            // Show countdown UI - using isCountingDown state
            if (UIManager.Instance != null && isCountingDown)
            {
                // UIManager would show countdown number based on isCountingDown
            }

            yield return new WaitForSeconds(1f);
        }

        isCountingDown = false;
    }

    IEnumerator LoadAndStartSong()
    {
        isSongLoaded = false; // Mark as loading

        // Load audio clip (this would be enhanced with actual asset loading)
        if (audioManager != null && !string.IsNullOrEmpty(currentSong.audioFilePath))
        {
            // For now, we'll use a placeholder - in real implementation,
            // this would load the actual audio file
            Debug.Log($"🎵 Loading audio: {currentSong.audioFilePath}");

            yield return new WaitForSeconds(0.1f); // Simulate loading time

            // Apply song start delay before audio playback
            if (songStartDelay > 0)
            {
                Debug.Log($"🎵 Applying song start delay: {songStartDelay}s");
                yield return new WaitForSeconds(songStartDelay);
            }

            // Start audio playback
            // audioManager.PlayMusic(loadedClip, songStartDelay);
            isUsingAudioSync = useAudioTimeSync;
        }

        songDuration = currentSong.duration;
        isSongLoaded = true; // Mark as loaded
        Debug.Log($"🎵 Song loaded successfully (isSongLoaded: {isSongLoaded})");
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