using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;

public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGameManager();
        }
        else
        {
            Destroy(gameObject);
        }
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

    void InitializeGameManager()
    {
        LoadPlayerData();
        InitializeCoreComponents();
        Debug.Log("🎮 GameManager initialized successfully");
    }

    void InitializeCoreComponents()
    {
        // Initialize audio system (already tested and working)
        if (audioManager == null)
            audioManager = FindObjectOfType<AudioManager>();

        // Initialize input system
        if (inputManager == null)
            inputManager = FindObjectOfType<InputManager>();

        // Initialize UI system
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>();

        Debug.Log("🎵 Core systems initialized");
    }

    #region Game State Management
    public void ChangeGameState(GameState newState)
    {
        GameState previousState = CurrentGameState;
        CurrentGameState = newState;

        Debug.Log($"🎮 Game State: {previousState} → {newState}");

        OnGameStateChanged?.Invoke(newState);

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
        Debug.Log($"🎭 Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }

    public async void LoadSceneAsync(string sceneName)
    {
        Debug.Log($"🎭 Loading scene async: {sceneName}");

        var operation = SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            await System.Threading.Tasks.Task.Yield();
        }

        Debug.Log($"✅ Scene loaded: {sceneName}");
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

    public void SavePlayerData()
    {
        if (currentPlayer != null)
        {
            PlayerPrefs.SetString("PlayerName", currentPlayer.playerName);
            PlayerPrefs.SetInt("TotalScore", currentPlayer.totalScore);
            PlayerPrefs.SetInt("HighestCombo", currentPlayer.highestCombo);
            PlayerPrefs.SetInt("PreferredInstrument", (int)currentPlayer.preferredInstrument);
            PlayerPrefs.Save();

            Debug.Log("💾 Player data saved");
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
        Debug.Log($"🎵 Started new session: {song.songName} with {selectedInstrument}");
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

            SavePlayerData();
            ChangeGameState(GameState.GameOver);

            Debug.Log($"🏁 Session ended - Score: {currentSession.currentScore}, Combo: {currentSession.currentCombo}");
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
        if (pauseStatus && CurrentGameState == GameState.Playing)
        {
            PauseGame();
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && CurrentGameState == GameState.Playing)
        {
            PauseGame();
        }
    }

    void OnDestroy()
    {
        SavePlayerData();
    }
}

// Enums and Data Structures
[System.Serializable]
public enum GameState
{
    MainMenu,
    SongSelection,
    Playing,
    Paused,
    GameOver
}

[System.Serializable]
public enum InstrumentType
{
    Piano,
    Harp,
    Guitar
}

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
public class SongData
{
    public string songName;
    public string artist;
    public float bpm;
    public float duration;
    public string audioFilePath;
    public string noteChartPath;
    public DifficultyLevel difficulty;
}

[System.Serializable]
public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard,
    Expert
}

[System.Serializable]
public class GameConfig
{
    [Header("🎵 Audio Settings")]
    public int targetLatencyMs = 20;
    public float masterVolume = 1.0f;

    [Header("🎮 Gameplay Settings")]
    public int laneCount = 6;
    public float noteSpeed = 500f;
    public float perfectTimingWindow = 50f; // ms
    public float goodTimingWindow = 100f;   // ms

    [Header("📱 Mobile Settings")]
    public bool enableHaptics = true;
    public int targetFrameRate = 60;
}