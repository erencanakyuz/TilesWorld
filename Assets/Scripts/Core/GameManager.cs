using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;


public class GameManager : MonoBehaviour
{
    #region Singleton

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
#if UNITY_EDITOR
        if (Instance == null)
        {
            var bootstrapScene = SceneManager.GetSceneByName("Bootstrap");
            if (!bootstrapScene.isLoaded)
            {
                SceneManager.LoadScene("Bootstrap", LoadSceneMode.Additive);
            }
        }
#endif
    }

    public static GameManager Instance { get; private set; }

    void Awake()
    {
        // Bootstrap tarafından yaratıldığı için sadece Instance'ı set et
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Initialize after all Awakes are done
        InitializeGameManager();
    }
    #endregion

    [Header("🎵 Game Configuration")]
    [SerializeField] private GameConfig gameConfig;

    [Header("🎮 Core Systems")]
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private UIManager uiManager;

    [Header("📊 Game State")]
    [SerializeField] private PlayerData currentPlayer;
    [SerializeField] private GameSession currentSession;

    [Header("🎼 Music System")]
    [SerializeField] private List<SongData> availableSongs;
    [SerializeField] private InstrumentType selectedInstrument = InstrumentType.Piano;

    // Events for cross-scene communication
    public static event Action<GameState> OnGameStateChanged;
    public static event Action<float> OnScoreChanged;
    public static event Action<int> OnComboChanged;

    // Game State Management
    public GameState CurrentGameState { get; private set; } = GameState.MainMenu;
    public bool IsGamePaused { get; private set; } = false;

    public float perfectTimingWindow = 50f; // ms
    public float goodTimingWindow = 100f;   // ms

    [Header("📱 Mobile Settings")]
    public bool enableHaptics = true;
    public int targetFrameRate = 60;

    void InitializeGameManager()
    {
        // Set target frame rate for smooth performance, especially on mobile
        Application.targetFrameRate = targetFrameRate;

        LoadPlayerData();
        InitializeCoreComponents();
    }

    void InitializeCoreComponents()
    {
        // CRITICAL: Ensure SongDatabase is initialized first
        EnsureSongDatabaseExists();

        // If references are not set in the inspector, find them automatically using the singleton pattern.
        if (audioManager == null)
            audioManager = AudioManager.Instance;
        if (inputManager == null)
            inputManager = InputManager.Instance;
        if (uiManager == null)
            uiManager = UIManager.Instance;

        // Subscribe to UI events
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnPausePressed += HandlePauseButtonPressed;
            UIManager.Instance.OnSettingsPressed += HandleSettingsButtonPressed;
            UIManager.Instance.OnResumePressed += HandleResumeButtonPressed;
            UIManager.Instance.OnRestartPressed += HandleRestartButtonPressed;
            UIManager.Instance.OnMainMenuPressed += HandleMainMenuButtonPressed;

            // Test if events are properly connected
            int restartListenerCount = UIManager.Instance.OnRestartPressed?.GetInvocationList()?.Length ?? 0;
            // Debug.Log($"🔢 OnRestartPressed has {restartListenerCount} listeners");
        }
        else
        {
        }

        // This just verifies they're working
        if (AudioManager.Instance != null &&
            UIManager.Instance != null &&
            InputManager.Instance != null)
        {
        }
    }

    #region Game State Management
    public void ChangeGameState(GameState newState)
    {
        if (CurrentGameState == newState) return;

        GameState previousState = CurrentGameState;
        CurrentGameState = newState;

        // Dynamically adjust frame rate based on the game state for power saving.
        switch (newState)
        {
            case GameState.Playing:
                Application.targetFrameRate = 60;
                break;

            case GameState.MainMenu:
            case GameState.SongSelection:
            case GameState.Paused:
            case GameState.GameOver:
            case GameState.Settings:
                Application.targetFrameRate = 30;
                break;

            default:
                Application.targetFrameRate = 60; // Default for any other states
                break;
        }

        OnGameStateChanged?.Invoke(newState);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
#endif

        // Handle state-specific logic
        switch (newState)
        {
            case GameState.MainMenu:
                HandleMainMenuState();
                break;
            case GameState.SongSelection:
                HandleSongSelectionState();
                break;
            case GameState.Playing:
                HandlePlayingState();
                break;
            case GameState.Paused:
                HandlePausedState();
                break;
            case GameState.GameOver:
                HandleGameOverState();
                break;
        }
    }

    public void PauseGame()
    {
        if (CurrentGameState == GameState.Playing)
        {
            IsGamePaused = true;
            Time.timeScale = 0f;
            ChangeGameState(GameState.Paused);
        }
    }

    public void ResumeGame()
    {
        if (CurrentGameState == GameState.Paused)
        {
            IsGamePaused = false;
            Time.timeScale = 1f;
            ChangeGameState(GameState.Playing);
        }
    }
    #endregion

    #region Scene Management
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public async void LoadSceneAsync(string sceneName)
    {

        var operation = SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            await System.Threading.Tasks.Task.Yield();
        }

    }
    #endregion

    #region Player Data Management
    void LoadPlayerData()
    {
        // Load from PlayerPrefs or save file
        if (currentPlayer == null)
        {
            currentPlayer = new PlayerData
            {
                playerName = PlayerPrefs.GetString("PlayerName", "Player"),
                totalScore = PlayerPrefs.GetInt("TotalScore", 0),
                highestCombo = PlayerPrefs.GetInt("HighestCombo", 0),
                preferredInstrument = (InstrumentType)PlayerPrefs.GetInt("PreferredInstrument", 0)
            };
        }

        selectedInstrument = currentPlayer.preferredInstrument;
    }

    /// <summary>
    /// Updates the player data in memory (PlayerPrefs). Does not write to disk.
    /// </summary>
    public void UpdatePlayerDataInMemory()
    {
        if (currentPlayer != null)
        {
            PlayerPrefs.SetString("PlayerName", currentPlayer.playerName);
            PlayerPrefs.SetInt("TotalScore", currentPlayer.totalScore);
            PlayerPrefs.SetInt("HighestCombo", currentPlayer.highestCombo);
            PlayerPrefs.SetInt("PreferredInstrument", (int)currentPlayer.preferredInstrument);
        }
    }

    /// <summary>
    /// Writes the current in-memory PlayerPrefs to disk.
    /// </summary>
    public void SavePlayerDataToDisk()
    {
        PlayerPrefs.Save();
    }

    [System.Obsolete("Use UpdatePlayerDataInMemory and SavePlayerDataToDisk instead for better performance control.")]
    public void SavePlayerData()
    {
        if (currentPlayer != null)
        {
            PlayerPrefs.SetString("PlayerName", currentPlayer.playerName);
            PlayerPrefs.SetInt("TotalScore", currentPlayer.totalScore);
            PlayerPrefs.SetInt("HighestCombo", currentPlayer.highestCombo);
            PlayerPrefs.SetInt("PreferredInstrument", (int)currentPlayer.preferredInstrument);
            PlayerPrefs.Save();

        }
    }
    #endregion

    #region Game Session Management
    public void StartNewGameSession(SongData song)
    {
        currentSession = new GameSession
        {
            songData = song,
            instrument = selectedInstrument,
            startTime = Time.time,
            currentScore = 0,
            currentCombo = 0,
            perfectHits = 0,
            goodHits = 0,
            missedNotes = 0
        };

        ChangeGameState(GameState.Playing);
    }

    public void EndGameSession()
    {
        if (currentSession != null)
        {
            currentSession.endTime = Time.time;
            currentSession.duration = currentSession.endTime - currentSession.startTime;

            // Update player stats
            if (currentSession.currentScore > currentPlayer.totalScore)
                currentPlayer.totalScore = currentSession.currentScore;

            if (currentSession.currentCombo > currentPlayer.highestCombo)
                currentPlayer.highestCombo = currentSession.currentCombo;

            UpdatePlayerDataInMemory();
            ChangeGameState(GameState.GameOver);

        }
    }

    public void UpdateScore(int points)
    {
        if (currentSession != null)
        {
            currentSession.currentScore += points;
            OnScoreChanged?.Invoke(currentSession.currentScore);
        }
    }

    public void UpdateCombo(int combo)
    {
        if (currentSession != null)
        {
            currentSession.currentCombo = combo;
            OnComboChanged?.Invoke(combo);
        }
    }
    #endregion

    #region State Handlers
    void HandleMainMenuState()
    {
        // Main menu specific logic
    }

    void HandleSongSelectionState()
    {
        // Song selection specific logic
    }

    void HandlePlayingState()
    {
        // Game playing specific logic
    }

    void HandlePausedState()
    {
        // Paused game specific logic
    }

    void HandleGameOverState()
    {
        // Game over specific logic
    }
    #endregion

    #region UI Event Handlers
    void HandlePauseButtonPressed()
    {
        PauseGame();
    }

    void HandleSettingsButtonPressed()
    {
        // TODO: Settings panel functionality will be added later
        // For now, just open a simple settings overlay
    }

    void HandleResumeButtonPressed()
    {
        ResumeGame();
    }

    void HandleRestartButtonPressed()
    {
        Time.timeScale = 1f; // Ensure time is running
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void HandleMainMenuButtonPressed()
    {
        Time.timeScale = 1f; // Ensure time is running
        SceneManager.LoadScene("MainScene"); // Assuming your main menu scene is named "MainScene"
    }
    #endregion

    #region Getters
    public PlayerData GetCurrentPlayer() => currentPlayer;
    public GameSession GetCurrentSession() => currentSession;
    public List<SongData> GetAvailableSongs() => availableSongs;
    public InstrumentType GetSelectedInstrument() => selectedInstrument;

    public void SetSelectedInstrument(InstrumentType instrument)
    {
        selectedInstrument = instrument;
        currentPlayer.preferredInstrument = instrument;
    }
    #endregion

    void OnApplicationPause(bool pauseStatus)
    {
#if !UNITY_EDITOR
        if (pauseStatus)
        {
            SavePlayerDataToDisk();
            if (CurrentGameState == GameState.Playing)
            {
                PauseGame();
            }
        }
#endif
    }

    void OnApplicationFocus(bool hasFocus)
    {
#if !UNITY_EDITOR
        if (!hasFocus && CurrentGameState == GameState.Playing)
        {
            ChangeGameState(GameState.Paused);
        }
#endif
    }

    /// <summary>
    /// Ensure SongDatabase singleton exists (fallback if Bootstrap didn't create it)
    /// </summary>
    void EnsureSongDatabaseExists()
    {
        if (SongDatabase.Instance == null)
        {
            GameObject songDbObject = new GameObject("SongDatabase");
            songDbObject.AddComponent<SongDatabase>();
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from UI events to prevent memory leaks
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnPausePressed -= HandlePauseButtonPressed;
            UIManager.Instance.OnSettingsPressed -= HandleSettingsButtonPressed;
            UIManager.Instance.OnResumePressed -= HandleResumeButtonPressed;
            UIManager.Instance.OnRestartPressed -= HandleRestartButtonPressed;
            UIManager.Instance.OnMainMenuPressed -= HandleMainMenuButtonPressed;
        }

        SavePlayerDataToDisk();
    }

    void Update()
    {
#if UNITY_EDITOR
        // Debug: K tuşuna basınca oyun durumu bilgilerini göster
        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            ShowGameStateDebug();
        }

        // Debug: S tuşuna basınca Song Selection'a geç
        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            ChangeGameState(GameState.SongSelection);
        }
#endif
    }

    void ShowGameStateDebug()
    {
        Debug.Log("=== 🎮 OYUN DURUMU DEBUG ===");
        Debug.Log($"🎯 Current State: {CurrentGameState}");
        Debug.Log($"⏸️ Is Paused: {IsGamePaused}");
        Debug.Log($"🎵 Selected Instrument: {selectedInstrument}");

        if (currentPlayer != null)
        {
            Debug.Log($"👤 Player: {currentPlayer.playerName}");
            Debug.Log($"🏆 High Score: {currentPlayer.totalScore}");
            Debug.Log($"🔥 Best Combo: {currentPlayer.highestCombo}");
        }

        if (currentSession != null)
        {
            Debug.Log($"🎵 Current Song: {currentSession.songData?.songName ?? "None"}");
            Debug.Log($"📊 Score: {currentSession.currentScore}");
            Debug.Log($"🔥 Combo: {currentSession.currentCombo}");
            Debug.Log($"✨ Perfect: {currentSession.perfectHits}, Good: {currentSession.goodHits}, Miss: {currentSession.missedNotes}");
        }
        else
        {
            Debug.Log("🎵 No active session");
        }

        Debug.Log("========================");

        // Test: Switch to Song Selection state
        Debug.Log("🎵 Testing Song Selection - Press S to switch to SongSelection state");
        Debug.Log("🎮 Press K to show this debug info");
    }

    public void RestartGame()
    {
        // Reload the current scene to restart the game
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}

// Game-specific Data Structures (enums moved to DataStructures.cs)
[System.Serializable]
public class PlayerData
{
    public string playerName;
    public int totalScore;
    public int highestCombo;
    public InstrumentType preferredInstrument;
}

[System.Serializable]
public class GameSession
{
    public SongData songData;
    public InstrumentType instrument;
    public float startTime;
    public float endTime;
    public float duration;
    public int currentScore;
    public int currentCombo;
    public int perfectHits;
    public int goodHits;
    public int missedNotes;
}

[System.Serializable]
[CreateAssetMenu(fileName = "New Song", menuName = "Piano Game/Song Data", order = 1)]
public class SongData : ScriptableObject
{
    public string songName;
    public string artist;
    public int bpm;
    public float duration;
    public string audioFilePath;
    public string noteChartPath;
    public string songKey;
    public DifficultyLevel difficulty;
}

[System.Serializable]
public class GameConfig
{
    [Header("🎵 Audio Settings")]
    public int targetLatencyMs = 20;
    public float masterVolume = 1.0f;

    [Header("🎮 Gameplay Settings")]
    public int laneCount = 6;
}