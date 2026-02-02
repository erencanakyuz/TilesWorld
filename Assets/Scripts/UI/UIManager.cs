using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public System.Action OnPausePressed;
    public System.Action OnResumePressed;
    public System.Action OnRestartPressed;
    public System.Action OnMainMenuPressed;
    public System.Action OnSettingsPressed;

    private UIConfig config;

    private CanvasLocator canvasLocator;
    private HUDController hudController;
    private PanelManager panelManager;
    private UIEffectPool effectPool;
    private CountdownController countdownController;
    private MobileFinder mobileFinder;

    private System.Action<GameState> onGameStateChanged;
    private System.Action<float> onScoreChanged;
    private System.Action<int> onComboChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        config = Resources.Load<UIConfig>("UI/UIConfig");
        if (config == null)
        {
            Debug.LogError("UIConfig not found at Resources/UI/UIConfig. UI system will be limited.");
        }

        canvasLocator = GetOrAddComponent<CanvasLocator>();
        hudController = GetOrAddComponent<HUDController>();
        panelManager = GetOrAddComponent<PanelManager>();
        effectPool = GetOrAddComponent<UIEffectPool>();
        countdownController = GetOrAddComponent<CountdownController>();
        mobileFinder = GetOrAddComponent<MobileFinder>();

        onGameStateChanged = HandleGameStateChange;
        onScoreChanged = score => hudController?.UpdateScore((int)score);
        onComboChanged = combo => hudController?.UpdateCombo(combo);

        GameManager.OnGameStateChanged += onGameStateChanged;
        GameManager.OnScoreChanged += onScoreChanged;
        GameManager.OnComboChanged += onComboChanged;
        
        // Initialize sub-managers immediately for current scene (before any Start() calls)
        InitializeSubManagersForCurrentScene();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        // Initialize immediately for current scene
        InitializeSubManagersForCurrentScene();
    }
    
    void InitializeSubManagersForCurrentScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        ProcessScene(activeScene);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Bootstrap") return;
        ProcessScene(scene);
    }

    void ProcessScene(Scene scene)
    {
        EnsureEventSystem();

        if (canvasLocator != null)
        {
            canvasLocator.Initialize(config);
        }

        Canvas hudCanvas = canvasLocator?.HUDCanvas;
        Canvas mainCanvas = canvasLocator?.MainCanvas;
        Canvas overlayCanvas = canvasLocator?.OverlayCanvas;

        if (hudCanvas != null)
        {
            hudController?.Initialize(config, hudCanvas);
        }

        if (countdownController != null)
        {
            countdownController.Initialize(hudCanvas, mainCanvas);
        }

        if (effectPool != null)
        {
            Transform effectParent = effectPool.DiscoverEffectParent(overlayCanvas, hudCanvas);
            if (effectParent != null)
            {
                effectPool.Initialize(config, effectParent);
            }
        }

        if (panelManager != null)
        {
            panelManager.Initialize(config, canvasLocator);
            // These are lambda assignments, not additive - safe to reassign
            panelManager.OnPausePressed = () => OnPausePressed?.Invoke();
            panelManager.OnResumePressed = () => OnResumePressed?.Invoke();
            panelManager.OnRestartPressed = () => OnRestartPressed?.Invoke();
            panelManager.OnMainMenuPressed = () => OnMainMenuPressed?.Invoke();
        }

        if (mobileFinder != null)
        {
            mobileFinder.Initialize(config);
            mobileFinder.DiscoverControls(new[] { mainCanvas, hudCanvas, overlayCanvas });

            // CRITICAL FIX: Remove old listeners before adding new ones to prevent duplicates
            if (mobileFinder.PauseButton != null)
            {
                mobileFinder.PauseButton.onClick.RemoveAllListeners();
                mobileFinder.PauseButton.onClick.AddListener(() => 
                {
                    // Try event first, fallback to direct call
                    if (OnPausePressed != null)
                    {
                        OnPausePressed.Invoke();
                    }
                    else if (GameManager.Instance != null)
                    {
                        Debug.Log("[UIManager] Pause button - fallback to direct GameManager call");
                        GameManager.Instance.ChangeGameState(GameState.Paused);
                    }
                });
            }
            if (mobileFinder.SettingsButton != null)
            {
                mobileFinder.SettingsButton.onClick.RemoveAllListeners();
                mobileFinder.SettingsButton.onClick.AddListener(() => 
                {
                    // Try event first, fallback to direct call
                    if (OnSettingsPressed != null)
                    {
                        OnSettingsPressed.Invoke();
                    }
                    else if (GameManager.Instance != null)
                    {
                        Debug.Log("[UIManager] Settings button - fallback to direct GameManager call");
                        GameManager.Instance.ChangeGameState(GameState.Settings);
                    }
                });
            }
        }
    }

    void EnsureEventSystem()
    {
        var existing = FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (existing != null) return;

        GameObject eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<EventSystem>();
        eventSystemGO.AddComponent<InputSystemUIInputModule>();
    }

    void HandleGameStateChange(GameState newState)
    {
        panelManager?.ShowPanelForState(newState);

        if (newState == GameState.Playing)
        {
            // Ensure HUD canvas is found and activated
            if (canvasLocator?.HUDCanvas != null)
            {
                canvasLocator.HUDCanvas.gameObject.SetActive(true);
                Debug.Log($"[UIManager] HUD Canvas activated for Playing state");
            }
            else
            {
                Debug.LogWarning("[UIManager] HUDCanvas is NULL - cannot activate HUD!");
            }
            
            if (mobileFinder?.MobileControls != null)
                mobileFinder.MobileControls.SetActive(true);

            hudController?.Reset();
            mobileFinder?.SetupLandscapeLayout();
        }
        else if (newState == GameState.Paused)
        {
            // Keep HUD visible during pause
            if (canvasLocator?.HUDCanvas != null)
                canvasLocator.HUDCanvas.gameObject.SetActive(true);
        }
        else
        {
            // Hide HUD for other states (MainMenu, SongSelection, GameOver)
            if (canvasLocator?.HUDCanvas != null)
                canvasLocator.HUDCanvas.gameObject.SetActive(false);
            if (mobileFinder?.MobileControls != null)
                mobileFinder.MobileControls.SetActive(false);
        }
    }

    public void ShowCountdown(int number) => countdownController?.ShowCountdown(number);

    public void HideCountdown() => countdownController?.HideCountdown();

    public void ShowHitEffect(HitAccuracy accuracy, Vector2 pos) => effectPool?.ShowEffect(accuracy, pos);

    public void UpdateScore(float score) => hudController?.UpdateScore((int)score);

    public void UpdateCombo(int combo) => hudController?.UpdateCombo(combo);

    public void UpdateHealth(float health) => hudController?.UpdateHealth(health);

    public void SetUIInteractable(bool interactable)
    {
        if (canvasLocator?.MainCanvas == null) return;
        GraphicRaycaster raycaster = canvasLocator.MainCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster != null)
            raycaster.enabled = interactable;
    }

    public void RefreshUIElements()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.name != "Bootstrap")
            {
                ProcessScene(scene);
            }
        }
    }

    void OnDestroy()
    {
        GameManager.OnGameStateChanged -= onGameStateChanged;
        GameManager.OnScoreChanged -= onScoreChanged;
        GameManager.OnComboChanged -= onComboChanged;
    }

    private T GetOrAddComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }
        return component;
    }
}
