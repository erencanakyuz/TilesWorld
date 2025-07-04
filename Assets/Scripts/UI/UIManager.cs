using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;
using System.Linq; // For convenient searches

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // Bu referanslar artık Inspector'da görünmeyecek, çalışma anında doldurulacak.
    private Canvas mainCanvas;
    private Canvas overlayCanvas;
    private Canvas hudCanvas;

    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI comboText;
    private TextMeshProUGUI multiplierText;
    private Slider healthBar;
    private Image instrumentIcon;

    private Transform effectParent;

    private Button pauseButton;
    private Button settingsButton;
    private GameObject mobileControls;


    [Header("🎮 Game State Panel Prefabs")]
    [SerializeField] public GameObject mainMenuPanelPrefab;
    [SerializeField] public GameObject songSelectionPanelPrefab;
    [SerializeField] public GameObject gameplayPanelPrefab; // This might be used for a specific overlay during gameplay
    [SerializeField] public GameObject pausePanelPrefab;
    [SerializeField] public GameObject gameOverPanelPrefab;
    [SerializeField] public GameObject settingsPanelPrefab;

    [Header("🎵 Audio Feedback UI")]
    [SerializeField] private GameObject perfectHitEffect;
    [SerializeField] private GameObject goodHitEffect;
    [SerializeField] private GameObject missEffect;

    [Header("📱 Mobile UI")]
    // Mobile controls are found automatically in the scene, no prefab needed

    [Header("⚙️ UI Configuration")]
    [SerializeField] private float effectDuration = 1.0f;
    [SerializeField] private AnimationCurve fadeAnimation;
    [SerializeField] private bool enableDebugLogging = false;
    [SerializeField] private bool enableFallbackUI = true;

    // UI State Management
    private Dictionary<GameState, GameObject> statePanelPrefabs;
    private GameObject currentPanelInstance;
    private Queue<GameObject> hitEffectPool;
    private List<ActiveHitEffect> activeEffects;
    
    // PERFORMANCE: Cache UI elements to avoid repeated searches
    private readonly Dictionary<string, Component> cachedUIElements = new Dictionary<string, Component>();
    
    // PERFORMANCE: Cache string formatting to reduce GC allocations
    private readonly System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(32);
    private int lastDisplayedScore = -1;

    // UI Data
    private int currentScore = 0;
    private int currentCombo = 0;
    private int currentMultiplier = 1;
    private float currentHealth = 1.0f;

    // UI Events
    public System.Action OnPausePressed;
    public System.Action OnResumePressed;
    public System.Action OnRestartPressed;
    public System.Action OnMainMenuPressed;
    public System.Action OnSettingsPressed;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize collections here to prevent NullReferenceException
            hitEffectPool = new Queue<GameObject>();
            activeEffects = new List<ActiveHitEffect>();

            // Event subscription'ları burada yap ki GameManager'dan önce hazır ol
            // Debug.Log("🔔 UIManager Awake - Event subscription yapılıyor...");
            GameManager.OnGameStateChanged += HandleGameStateChange;
            GameManager.OnScoreChanged += UpdateScore;
            GameManager.OnComboChanged += UpdateCombo;
            // Debug.Log("✅ UIManager Awake - Event subscription tamamlandı!");
        }
        else
        {
            Destroy(gameObject);
        }
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
        InitializeUISystem();

        // Zaten yüklü olan sahneleri kontrol et
        CheckAlreadyLoadedScenes();
    }

    void CheckAlreadyLoadedScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            // Bootstrap dışındaki sahneleri işle
            if (scene.name != "Bootstrap" && scene.isLoaded)
            {
                ProcessScene(scene);
                break; // İlk non-bootstrap sahne ile devam et
            }
        }
    }

    void ProcessScene(Scene scene)
    {
        if (AutoFindUIElements())
        {
            ConfigureCanvasScalers();
            SetupEventListeners();
        }
        else
        {
            // Debug.LogWarning($"'{scene.name}' sahnesinde bazı UI elemanları bulunamadı! UI düzgün çalışmayabilir.");
        }
    }

    bool AutoFindUIElements()
    {
        if (enableDebugLogging) Debug.Log("🔍 UI System: Starting element discovery...");
        
        bool canvasSuccess = FindCanvases();
        bool hudSuccess = FindHUDElements();
        bool effectSuccess = FindEffectElements();
        bool mobileSuccess = FindMobileControls();
        
        // STABILITY: Create fallback UI if critical elements missing
        if (!canvasSuccess || !hudSuccess)
        {
            Debug.LogWarning("⚠️ UI System: Critical elements missing, creating fallbacks...");
            CreateFallbackUI();
        }
        
        bool overallSuccess = canvasSuccess && hudSuccess && effectSuccess;
        
        if (enableDebugLogging) 
        {
            Debug.Log($"🎯 UI Discovery Complete - Canvas: {canvasSuccess}, HUD: {hudSuccess}, Effects: {effectSuccess}, Mobile: {mobileSuccess}");
        }
        
        return overallSuccess;
    }

    bool FindCanvases()
    {
        // Debug.Log("🔍 Canvas'ları arıyor...");
        var allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        // Debug.Log($"   Bulunan Canvas sayısı: {allCanvases.Length}");

        for (int i = 0; i < allCanvases.Length; i++)
        {
            // Debug.Log($"   Canvas[{i}]: {allCanvases[i].name} (sortingOrder: {allCanvases[i].sortingOrder})");
        }

        // İsim veya pattern'e göre canvas'ları ayır
        mainCanvas = System.Array.Find(allCanvases, c =>
            c.name.ToLower().Contains("main") ||
            c.name.ToLower() == "canvas" ||
            c.sortingOrder == 0) ?? (allCanvases.Length > 0 ? allCanvases[0] : null); // Fallback: ilk canvas

        hudCanvas = System.Array.Find(allCanvases, c =>
            c.name.ToLower().Contains("hud"));

        overlayCanvas = System.Array.Find(allCanvases, c =>
            c.name.ToLower().Contains("overlay") ||
            c.sortingOrder > 10); // Yüksek sorting order = overlay

        // Debug.Log($"   ✅ mainCanvas: {(mainCanvas != null ? mainCanvas.name : "NULL")}");
        // Debug.Log($"   ✅ hudCanvas: {(hudCanvas != null ? hudCanvas.name : "NULL")}");
        // Debug.Log($"   ✅ overlayCanvas: {(overlayCanvas != null ? overlayCanvas.name : "NULL")}");

        return mainCanvas != null;
    }

    bool FindHUDElements()
    {
        if (hudCanvas == null) return false;

        // HUD Canvas içindeki text elemanlarını akıllı şekilde bul
        var allTexts = hudCanvas.GetComponentsInChildren<TextMeshProUGUI>();

        scoreText = System.Array.Find(allTexts, t =>
            t.name.ToLower().Contains("score"));

        comboText = System.Array.Find(allTexts, t =>
            t.name.ToLower().Contains("combo"));

        multiplierText = System.Array.Find(allTexts, t =>
            t.name.ToLower().Contains("multiplier") ||
            t.name.ToLower().Contains("multi"));

        // Health bar'ı bul
        healthBar = hudCanvas.GetComponentInChildren<Slider>();

        // Instrument icon'u bul
        var allImages = hudCanvas.GetComponentsInChildren<Image>();
        instrumentIcon = System.Array.Find(allImages, img =>
            img.name.ToLower().Contains("instrument") ||
            img.name.ToLower().Contains("icon"));

        return scoreText != null && comboText != null;
    }

    bool FindEffectElements()
    {
        // Effect parent'ı overlay canvas'ta ara, yoksa HUD canvas'ta ara
        Canvas targetCanvas = overlayCanvas ?? hudCanvas;
        if (targetCanvas == null) return false;

        var allTransforms = targetCanvas.GetComponentsInChildren<Transform>();
        effectParent = System.Array.Find(allTransforms, t =>
            t.name.ToLower().Contains("effect")) ?? targetCanvas.transform;

        return effectParent != null;
    }

    bool FindMobileControls()
    {
        // PERFORMANCE: Cache button search results
        if (cachedUIElements.ContainsKey("buttons_searched"))
        {
            // Use cached results
            cachedUIElements.TryGetValue("pause_button", out var cachedPause);
            cachedUIElements.TryGetValue("settings_button", out var cachedSettings);
            pauseButton = cachedPause as Button;
            settingsButton = cachedSettings as Button;
        }
        else
        {
            // First time search - find buttons more efficiently in canvas hierarchy
            var allButtons = new List<Button>();
            Canvas[] searchCanvases = { mainCanvas, hudCanvas, overlayCanvas };
            
            foreach (Canvas canvas in searchCanvases)
            {
                if (canvas != null)
                {
                    allButtons.AddRange(canvas.GetComponentsInChildren<Button>());
                }
            }

            pauseButton = allButtons.Find(b => b.name.ToLower().Contains("pause"));
            settingsButton = allButtons.Find(b => 
                b.name.ToLower().Contains("settings") || 
                b.name.ToLower().Contains("setting"));

            // Cache results for next time
            cachedUIElements["buttons_searched"] = pauseButton; // Just a marker
            if (pauseButton != null) cachedUIElements["pause_button"] = pauseButton;
            if (settingsButton != null) cachedUIElements["settings_button"] = settingsButton;
        }

        // PERFORMANCE: Search mobile controls more efficiently - avoid scanning ALL GameObjects
        mobileControls = null;
        
        // First try to find it in known canvases (much faster)
        Canvas[] mobileSearchCanvases = { mainCanvas, hudCanvas, overlayCanvas };
        foreach (Canvas canvas in mobileSearchCanvases)
        {
            if (canvas != null)
            {
                Transform found = canvas.transform.Find("MobileControls") ?? 
                                canvas.transform.Find("Mobile Controls") ??
                                canvas.transform.Find("MobileControl");
                if (found != null)
                {
                    mobileControls = found.gameObject;
                    break;
                }
            }
        }
        
        // Fallback: Search only in UI hierarchy if not found (still much faster than all GameObjects)
        if (mobileControls == null && mainCanvas != null)
        {
            Transform[] canvasChildren = mainCanvas.GetComponentsInChildren<Transform>();
            mobileControls = System.Array.Find(canvasChildren, t =>
                t.name.ToLower().Contains("mobile") &&
                t.name.ToLower().Contains("control"))?.gameObject;
        }

        return true; // Mobil kontroller opsiyonel
    }

    /// <summary>
    /// STABILITY: Creates fallback UI elements if main ones are missing
    /// </summary>
    private void CreateFallbackUI()
    {
        if (!enableFallbackUI) return;

        // Create main canvas if missing
        if (mainCanvas == null)
        {
            GameObject canvasGO = new GameObject("FallbackMainCanvas");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            
            Debug.Log("✅ UI System: Created fallback main canvas");
        }

        // Create HUD canvas if missing
        if (hudCanvas == null)
        {
            GameObject hudGO = new GameObject("FallbackHUDCanvas");
            hudCanvas = hudGO.AddComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.sortingOrder = 10;
            hudGO.AddComponent<CanvasScaler>();
            hudGO.AddComponent<GraphicRaycaster>();
            
            Debug.Log("✅ UI System: Created fallback HUD canvas");
        }

        // Create overlay canvas if missing
        if (overlayCanvas == null)
        {
            GameObject overlayGO = new GameObject("FallbackOverlayCanvas");
            overlayCanvas = overlayGO.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 100;
            overlayGO.AddComponent<CanvasScaler>();
            overlayGO.AddComponent<GraphicRaycaster>();
            
            Debug.Log("✅ UI System: Created fallback overlay canvas");
        }

        // Configure all canvases for consistent scaling
        ConfigureCanvasScalers();
    }



    void InitializeUISystem()
    {
        SetupCanvasReferences();
    }

    /// <summary>
    /// Configures all Canvas Scalers in the scene for a consistent landscape UI.
    /// This should be called once during initialization.
    /// </summary>
    void ConfigureCanvasScalers()
    {
        // Configure main canvas for primary UI panels
        if (mainCanvas != null)
        {
            SetupScaler(mainCanvas.GetComponent<CanvasScaler>());
        }

        // Configure HUD canvas for gameplay elements
        if (hudCanvas != null)
        {
            SetupScaler(hudCanvas.GetComponent<CanvasScaler>());
        }

        // Configure Overlay canvas if it exists
        if (overlayCanvas != null)
        {
            SetupScaler(overlayCanvas.GetComponent<CanvasScaler>());
        }
    }

    /// <summary>
    /// Applies standardized landscape scaling settings to a CanvasScaler.
    /// </summary>
    private void SetupScaler(CanvasScaler scaler)
    {
        if (scaler == null) return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); // Target landscape resolution
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f; // Balanced scaling for various aspect ratios
    }

    void SetupCanvasReferences()
    {
        // Initialize state panels dictionary
        statePanelPrefabs = new Dictionary<GameState, GameObject>
        {
            { GameState.MainMenu, mainMenuPanelPrefab },
            { GameState.SongSelection, songSelectionPanelPrefab },
            { GameState.Playing, gameplayPanelPrefab }, // Note: Gameplay HUD is handled separately
            { GameState.Paused, pausePanelPrefab },
            { GameState.GameOver, gameOverPanelPrefab }
        };

        // Initialization moved to Awake
        // hitEffectPool = new Queue<GameObject>();
        // activeEffects = new List<ActiveHitEffect>();

        InitializeHitEffectPool();

        // Setup button events
        if (pauseButton != null)
            pauseButton.onClick.AddListener(() => OnPausePressed?.Invoke());

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() => OnSettingsPressed?.Invoke());
    }

    void InitializeHitEffectPool()
    {
        if (perfectHitEffect != null && effectParent != null)
        {
            for (int i = 0; i < 10; i++)
            {
                GameObject effect = Instantiate(perfectHitEffect, effectParent);
                effect.SetActive(false);
                hitEffectPool.Enqueue(effect);
            }
        }
    }

    void SetupEventListeners()
    {
        // Debug.Log("🔔 SetupEventListeners başlatılıyor...");

        // GameManager events artık Awake()'de subscribe ediliyor
        // Burada sadece UI-specific event'ler kalıyor

        // Debug.Log("✅ SetupEventListeners tamamlandı (GameManager events Awake'de handle edildi)!");
    }

    #region Game State UI Management
    void HandleGameStateChange(GameState newState)
    {
        // Debug.Log($"🔔 UIManager.HandleGameStateChange ÇAĞRILDI! State: {newState}");

        // Canvas'lar henüz bulunmadıysa bekle
        if (mainCanvas == null || overlayCanvas == null || hudCanvas == null)
        {
            // Debug.Log("⏳ Canvas'lar henüz yok, state change'i pending yapılıyor...");
            StartCoroutine(WaitForCanvasAndHandleState(newState));
            return;
        }

        // Debug.Log($"🎮 UIManager: Handling state change to {newState}");

        HandleStateChangeImmediate(newState);
    }

    private System.Collections.IEnumerator WaitForCanvasAndHandleState(GameState newState)
    {
        // Debug.Log("⏳ Canvas'ların bulunması bekleniyor...");

        // Canvas'lar bulunana kadar bekle (max 5 saniye)
        float waitTime = 0f;
        while ((mainCanvas == null || overlayCanvas == null || hudCanvas == null) && waitTime < 5f)
        {
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
        }

        if (mainCanvas != null)
        {
            // Debug.Log("✅ Canvas'lar bulundu, state change işleniyor...");
            HandleStateChangeImmediate(newState);
        }
        else
        {
            // Debug.LogError("❌ Canvas'lar 5 saniye içinde bulunamadı!");
        }
    }

    private void HandleStateChangeImmediate(GameState newState)
    {
        // Cleanup current panel first to prevent duplicates
        if (currentPanelInstance != null)
        {
            // Debug.Log($"🗑️ Destroying existing panel: {currentPanelInstance.name}");
            Destroy(currentPanelInstance);
            currentPanelInstance = null;
        }

        // Create the appropriate panel for this state
        Transform parentCanvas = GetParentCanvasForState(newState);

        if (parentCanvas != null && statePanelPrefabs.ContainsKey(newState) && statePanelPrefabs[newState] != null)
        {
            currentPanelInstance = Instantiate(statePanelPrefabs[newState], parentCanvas);
            // Debug.Log($"✅ Created panel for state: {newState}");
        }

        // Handle specific logic for each state
        switch (newState)
        {
            case GameState.MainMenu:
                ShowMainMenuUI();
                break;
            case GameState.SongSelection:
                ShowSongSelectionUI();
                break;
            case GameState.Playing:
                ShowGameplayUI();
                break;
            case GameState.Paused:
                ShowPauseUI();
                break;
            case GameState.GameOver:
                ShowGameOverUI();
                break;
        }
    }

    private Transform GetParentCanvasForState(GameState state)
    {
        // Debug.Log($"🔍 GetParentCanvasForState({state}):");
        // Debug.Log($"   mainCanvas: {(mainCanvas != null ? mainCanvas.name : "NULL")}");
        // Debug.Log($"   overlayCanvas: {(overlayCanvas != null ? overlayCanvas.name : "NULL")}");
        // Debug.Log($"   hudCanvas: {(hudCanvas != null ? hudCanvas.name : "NULL")}");

        switch (state)
        {
            case GameState.Paused:
                // Pause panel should appear on top of everything
                if (overlayCanvas != null)
                    return overlayCanvas.transform;
                else
                {
                    // Debug.LogError("❌ OverlayCanvas is NULL! Falling back to MainCanvas");
                    return mainCanvas?.transform;
                }

            case GameState.MainMenu:
            case GameState.SongSelection:
            case GameState.GameOver:
            default:
                // Most panels go on the main canvas
                if (mainCanvas != null)
                    return mainCanvas.transform;
                else
                {
                    // Debug.LogError("❌ MainCanvas is NULL! Cannot create panel!");
                    return null;
                }
        }
    }

    void ShowGameplayUI()
    {
        if (hudCanvas != null)
            hudCanvas.gameObject.SetActive(true);

        if (mobileControls != null)
            mobileControls.SetActive(true);

        // Setup landscape-optimized HUD layout
        SetupLandscapeHUDLayout();
        SetupMobileLandscapeControls();

        ResetGameplayUI();
    }

    void SetupLandscapeHUDLayout()
    {
        // Position HUD elements for landscape mobile layout

        // Score - Top Left
        if (scoreText != null)
        {
            RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0f, 1f);
            scoreRect.anchorMax = new Vector2(0f, 1f);
            scoreRect.anchoredPosition = new Vector2(100f, -60f); // Top-left corner
            scoreRect.sizeDelta = new Vector2(300f, 80f);
            scoreText.fontSize = 36;
            scoreText.alignment = TMPro.TextAlignmentOptions.Left;
        }

        // Combo - Top Center
        if (comboText != null)
        {
            RectTransform comboRect = comboText.GetComponent<RectTransform>();
            comboRect.anchorMin = new Vector2(0.5f, 1f);
            comboRect.anchorMax = new Vector2(0.5f, 1f);
            comboRect.anchoredPosition = new Vector2(0f, -60f); // Top-center
            comboRect.sizeDelta = new Vector2(400f, 80f);
            comboText.fontSize = 32;
            comboText.alignment = TMPro.TextAlignmentOptions.Center;
        }

        // Health Bar - Top Right
        if (healthBar != null)
        {
            RectTransform healthRect = healthBar.GetComponent<RectTransform>();
            healthRect.anchorMin = new Vector2(1f, 1f);
            healthRect.anchorMax = new Vector2(1f, 1f);
            healthRect.anchoredPosition = new Vector2(-200f, -60f); // Top-right
            healthRect.sizeDelta = new Vector2(300f, 30f);
        }

        // Multiplier - Near score
        if (multiplierText != null)
        {
            RectTransform multiplierRect = multiplierText.GetComponent<RectTransform>();
            multiplierRect.anchorMin = new Vector2(0f, 1f);
            multiplierRect.anchorMax = new Vector2(0f, 1f);
            multiplierRect.anchoredPosition = new Vector2(100f, -120f); // Below score
            multiplierRect.sizeDelta = new Vector2(150f, 60f);
            multiplierText.fontSize = 28;
            multiplierText.alignment = TMPro.TextAlignmentOptions.Left;
        }
    }

    void SetupMobileLandscapeControls()
    {
        // Create or setup mobile control buttons for landscape orientation

        // Pause button - Top Right Corner (easily reachable)
        if (pauseButton != null)
        {
            RectTransform pauseRect = pauseButton.GetComponent<RectTransform>();
            pauseRect.anchorMin = new Vector2(1f, 1f);
            pauseRect.anchorMax = new Vector2(1f, 1f);
            pauseRect.anchoredPosition = new Vector2(-80f, -80f); // Top-right
            pauseRect.sizeDelta = new Vector2(60f, 60f);
        }

        // Settings button - Top Right, below pause
        if (settingsButton != null)
        {
            RectTransform settingsRect = settingsButton.GetComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(1f, 1f);
            settingsRect.anchorMax = new Vector2(1f, 1f);
            settingsRect.anchoredPosition = new Vector2(-80f, -160f); // Below pause
            settingsRect.sizeDelta = new Vector2(60f, 60f);
        }
    }

    void ShowPauseUI()
    {
        if (hudCanvas != null)
            hudCanvas.gameObject.SetActive(true); // Keep HUD visible on pause

        // Otomatik olarak Resume / Restart butonlarını dinamik bağla
        SetupPausePanelButtons();
    }

    /// <summary>
    /// Pause panelindeki Resume ve Restart tuşlarını bulup uygun event'lere bağlar.
    /// Böylece Inspector'da manuel ayar gerektirmez.
    /// </summary>
    private void SetupPausePanelButtons()
    {
        if (currentPanelInstance == null) return;

        // Panel içindeki tüm Button bileşenlerini bul
        var buttons = currentPanelInstance.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            string lowerName = btn.name.ToLower();

            // Mevcut listener'ları temizle ki tekrarlı eklenmesin
            btn.onClick.RemoveAllListeners();

            if (lowerName.Contains("resume"))
            {
                btn.onClick.AddListener(() => OnResumePressed?.Invoke());
            }
            else if (lowerName.Contains("restart"))
            {
                btn.onClick.AddListener(() => OnRestartPressed?.Invoke());
            }
        }
    }

    void ShowGameOverUI()
    {
        if (hudCanvas != null)
            hudCanvas.gameObject.SetActive(false);

        Transform parentCanvas = GetParentCanvasForState(GameState.GameOver);
        if (parentCanvas == null)
        {
            // Debug.LogError("❌ Cannot create GameOver panel - no parent canvas!");
            return;
        }

        // Ensure parent canvas has GraphicRaycaster for UI interactions
        Canvas canvas = parentCanvas.GetComponent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            // Debug.Log("✅ Added GraphicRaycaster to canvas for UI interactions");
        }

        currentPanelInstance = Instantiate(gameOverPanelPrefab, parentCanvas);
        SetupGameOverPanelButtons();
        DisplayFinalScore();
    }

    private void SetupGameOverPanelButtons()
    {
        if (currentPanelInstance == null)
        {
            // Debug.LogError("❌ SetupGameOverPanelButtons: currentPanelInstance is NULL!");
            return;
        }

        // Butonları isimlerine göre bul
        var buttons = currentPanelInstance.GetComponentsInChildren<Button>();
        // Debug.Log($"🔍 Found {buttons.Length} buttons in GameOver panel:");

        foreach (var btn in buttons)
        {
            // Debug.Log($"   - Button: {btn.name}");
        }

        Button restartButton = System.Array.Find(buttons, b => b.name.ToLower().Contains("restart") || b.name.ToLower().Contains("again"));
        Button mainMenuButton = System.Array.Find(buttons, b => b.name.ToLower().Contains("menu"));

        if (restartButton != null)
        {
            // Debug.Log($"✅ Found restart button: {restartButton.name}");
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());

            // Also update the text to be more descriptive, including the shortcut
            var restartText = restartButton.GetComponentInChildren<TextMeshProUGUI>();
            if (restartText != null)
            {
                restartText.text = "Restart ↻ (R)";
            }
        }
        else
        {
            // Debug.LogError("❌ Restart button NOT found! Looking for buttons containing 'restart' or 'again'");
        }

        if (mainMenuButton != null)
        {
            // Debug.Log($"✅ Found main menu button: {mainMenuButton.name}");
            mainMenuButton.onClick.RemoveAllListeners(); // Önceki listener'ları temizle
            mainMenuButton.onClick.AddListener(() =>
            {
                // Debug.Log("🏠 Main menu button clicked! Invoking OnMainMenuPressed event...");
                OnMainMenuPressed?.Invoke();
            });
        }
        else
        {
            // Debug.LogError("❌ Main menu button NOT found! Looking for buttons containing 'menu'");
        }
    }

    void ShowMainMenuUI()
    {
        if (hudCanvas != null)
            hudCanvas.gameObject.SetActive(false);

        if (mobileControls != null)
            mobileControls.SetActive(false);
    }

    void ShowSongSelectionUI()
    {
        if (hudCanvas != null)
            hudCanvas.gameObject.SetActive(false);

        if (mobileControls != null)
            mobileControls.SetActive(false);

        // Debug.Log("🎵 Song Selection UI activated");
    }
    #endregion

    #region Gameplay UI Updates
    void UpdateScore(float score)
    {
        currentScore = Mathf.RoundToInt(score);

        if (scoreText != null)
        {
            // PERFORMANCE: Only update UI text if score actually changed
            if (currentScore != lastDisplayedScore)
            {
                lastDisplayedScore = currentScore;
                
                // PERFORMANCE: Use StringBuilder to avoid GC allocations
                stringBuilder.Clear();
                stringBuilder.Append(currentScore.ToString("N0"));
                scoreText.text = stringBuilder.ToString();

                // Add score change animation
                StartCoroutine(ScaleTextEffect(scoreText.transform));
            }
        }
    }

    void UpdateCombo(int combo)
    {
        currentCombo = combo;

        if (comboText != null)
        {
            comboText.text = combo > 0 ? $"COMBO x{combo}" : "";

            // Special effects for high combos
            if (combo > 0 && combo % 10 == 0)
            {
                StartCoroutine(ComboMilestoneEffect());
            }
        }

        // Update multiplier based on combo
        int newMultiplier = Mathf.Clamp(1 + combo / 10, 1, 8);
        if (newMultiplier != currentMultiplier)
        {
            currentMultiplier = newMultiplier;
            UpdateMultiplier();
        }
    }

    void UpdateMultiplier()
    {
        if (multiplierText != null)
        {
            multiplierText.text = $"x{currentMultiplier}";

            // Color coding for multiplier
            if (currentMultiplier >= 5)
                multiplierText.color = Color.red;
            else if (currentMultiplier >= 3)
                multiplierText.color = Color.yellow;
            else
                multiplierText.color = Color.white;
        }
    }

    public void UpdateHealth(float health)
    {
        currentHealth = Mathf.Clamp01(health);

        if (healthBar != null)
        {
            healthBar.value = currentHealth;

            // Color warning for low health
            Image fill = healthBar.fillRect.GetComponent<Image>();
            if (fill != null)
            {
                if (currentHealth < 0.3f)
                    fill.color = Color.red;
                else if (currentHealth < 0.6f)
                    fill.color = Color.yellow;
                else
                    fill.color = Color.green;
            }
        }
    }

    public void UpdateInstrumentIcon(InstrumentType instrument)
    {
        if (instrumentIcon != null)
        {
            // Load appropriate instrument icon
            string iconPath = $"Icons/Instrument_{instrument}";
            Sprite icon = Resources.Load<Sprite>(iconPath);

            if (icon != null)
                instrumentIcon.sprite = icon;
        }
    }
    #endregion

    #region Hit Effects
    public void ShowHitEffect(HitAccuracy accuracy, Vector2 screenPosition)
    {
        GameObject effectPrefab = GetEffectPrefab(accuracy);
        if (effectPrefab != null && effectParent != null)
        {
            GameObject effect = GetPooledEffect();
            if (effect != null)
            {
                // Position effect at hit location
                RectTransform effectRect = effect.GetComponent<RectTransform>();
                if (effectRect != null)
                {
                    Vector2 localPoint;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        effectParent.GetComponent<RectTransform>(),
                        screenPosition,
                        null,
                        out localPoint);

                    effectRect.localPosition = localPoint;
                }

                effect.SetActive(true);

                // Add to our managed list instead of starting a coroutine
                activeEffects.Add(new ActiveHitEffect
                {
                    effectObject = effect,
                    elapsedTime = 0f,
                    originalScale = effect.transform.localScale,
                    canvasGroup = effect.GetComponent<CanvasGroup>()
                });
            }
        }
    }

    GameObject GetEffectPrefab(HitAccuracy accuracy)
    {
        return accuracy switch
        {
            HitAccuracy.Perfect => perfectHitEffect,
            HitAccuracy.Good => goodHitEffect,
            HitAccuracy.Miss => missEffect,
            _ => perfectHitEffect
        };
    }

    GameObject GetPooledEffect()
    {
        if (hitEffectPool.Count > 0)
        {
            return hitEffectPool.Dequeue();
        }

        // Create new effect if pool is empty
        if (perfectHitEffect != null)
        {
            return Instantiate(perfectHitEffect, effectParent);
        }

        return null;
    }
    #endregion

    #region UI Animations
    IEnumerator ScaleTextEffect(Transform textTransform)
    {
        Vector3 originalScale = textTransform.localScale;
        Vector3 targetScale = originalScale * 1.2f;

        // Scale up
        float elapsed = 0f;
        float duration = 0.1f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            textTransform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            yield return null;
        }

        // Scale down
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            textTransform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            yield return null;
        }

        textTransform.localScale = originalScale;
    }

    IEnumerator ComboMilestoneEffect()
    {
        if (comboText != null)
        {
            // Flash effect for combo milestones
            Color originalColor = comboText.color;

            for (int i = 0; i < 3; i++)
            {
                comboText.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                comboText.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
    #endregion

    #region Utility Methods
    void ResetGameplayUI()
    {
        currentScore = 0;
        currentCombo = 0;
        currentMultiplier = 1;
        currentHealth = 1.0f;

        UpdateScore(0);
        UpdateCombo(0);
        UpdateHealth(1.0f);
    }

    void DisplayFinalScore()
    {
        // This will be expanded when game over screen is implemented
        // Debug.Log($"🏁 Final Score: {currentScore}, Max Combo: {currentCombo}");
    }

    public void SetUIInteractable(bool interactable)
    {
        // Enable/disable all UI interactions
        if (mainCanvas != null)
        {
            GraphicRaycaster raycaster = mainCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = interactable;
        }
    }

    public void RefreshUIElements()
    {
        // Debug.Log("🔄 Manuel UI Elements refresh başlatılıyor...");

        if (AutoFindUIElements())
        {
            ConfigureCanvasScalers();
            SetupEventListeners();
            // Debug.Log("✅ UI Elements başarıyla yeniden bulundu!");
        }
        else
        {
            // Debug.LogError("❌ UI Elements bulunamadı!");
        }
    }
    #endregion

    #region Countdown System
    private GameObject countdownUI;
    private TextMeshProUGUI countdownText;

    public void ShowCountdown(int number)
    {
        CreateCountdownUIIfNeeded();

        if (countdownText != null)
        {
            if (number > 0)
            {
                countdownText.text = number.ToString();
                countdownText.color = Color.white;
                countdownText.fontSize = 120;
            }
            else
            {
                countdownText.text = "GO!";
                countdownText.color = Color.green;
                countdownText.fontSize = 100;
            }

            // Animate countdown number
            StartCoroutine(CountdownPulseEffect());
        }

        if (countdownUI != null)
        {
            countdownUI.SetActive(true);
        }
    }

    public void HideCountdown()
    {
        if (countdownUI != null)
        {
            countdownUI.SetActive(false);
        }
    }

    private void CreateCountdownUIIfNeeded()
    {
        if (countdownUI != null) return;

        // Create countdown UI on the HUD Canvas
        Transform parentCanvas = hudCanvas != null ? hudCanvas.transform : mainCanvas?.transform;
        if (parentCanvas == null) return;

        // Create main countdown GameObject
        countdownUI = new GameObject("CountdownUI");
        countdownUI.transform.SetParent(parentCanvas, false);

        // Add RectTransform
        RectTransform rectTransform = countdownUI.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // Create countdown text
        GameObject textObj = new GameObject("CountdownText");
        textObj.transform.SetParent(countdownUI.transform, false);

        countdownText = textObj.AddComponent<TextMeshProUGUI>();
        countdownText.text = "3";
        countdownText.fontSize = 120;
        countdownText.color = Color.white;
        countdownText.alignment = TextAlignmentOptions.Center;
        // Use TextMeshPro default font or load from Resources
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null)
        {
            countdownText.font = font;
        }
        // If font is null, TextMeshPro will use default font

        // Position countdown text in center
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // Initially hidden
        countdownUI.SetActive(false);

        // Debug.Log("🎯 Countdown UI created successfully!");
    }

    private IEnumerator CountdownPulseEffect()
    {
        if (countdownText == null) yield break;

        Vector3 originalScale = countdownText.transform.localScale;
        Vector3 largeScale = originalScale * 1.3f;

        // Scale up quickly
        float elapsed = 0f;
        float duration = 0.1f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            countdownText.transform.localScale = Vector3.Lerp(originalScale, largeScale, progress);
            yield return null;
        }

        // Scale down
        elapsed = 0f;
        duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            countdownText.transform.localScale = Vector3.Lerp(largeScale, originalScale, progress);
            yield return null;
        }

        countdownText.transform.localScale = originalScale;
    }
    #endregion

    void OnDestroy()
    {
        // STABILITY: Safe event unsubscription with try-catch
        try
        {
            GameManager.OnGameStateChanged -= HandleGameStateChange;
            GameManager.OnScoreChanged -= UpdateScore;
            GameManager.OnComboChanged -= UpdateCombo;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"⚠️ UI System: Error during event cleanup: {e.Message}");
        }

        // Clean up cached elements
        cachedUIElements?.Clear();
        
        // Clear active effects to prevent memory leaks
        if (activeEffects != null)
        {
            foreach (var effect in activeEffects)
            {
                if (effect?.effectObject != null)
                {
                    Destroy(effect.effectObject);
                }
            }
            activeEffects.Clear();
        }

        if (enableDebugLogging) Debug.Log("🧹 UI System: Cleanup completed");
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Bootstrap sahnesi yüklendiğinde bir şey yapma
        if (scene.name == "Bootstrap")
        {
            return;
        }

        // Yeni yüklenen sahneyi işle
        ProcessScene(scene);
    }

    void Update()
    {
        // PERFORMANCE: Skip processing when no effects are active
        if (activeEffects.Count == 0) return;
        
        // Animate all active hit effects in a single loop to avoid coroutine overhead
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var activeEffect = activeEffects[i];
            activeEffect.elapsedTime += Time.deltaTime;

            if (activeEffect.elapsedTime >= effectDuration)
            {
                // Animation finished, return to pool
                activeEffect.effectObject.SetActive(false);
                hitEffectPool.Enqueue(activeEffect.effectObject);
                activeEffects.RemoveAt(i);
            }
            else
            {
                // Animate based on progress
                float progress = activeEffect.elapsedTime / effectDuration;
                float scale = fadeAnimation.Evaluate(progress);
                activeEffect.effectObject.transform.localScale = activeEffect.originalScale * scale;

                if (activeEffect.canvasGroup != null)
                {
                    activeEffect.canvasGroup.alpha = 1f - progress;
                }
            }
        }
    }

    #region Private Data Structures
    // This private class holds the state for an active hit effect animation
    class ActiveHitEffect
    {
        public GameObject effectObject;
        public float elapsedTime;
        public Vector3 originalScale;
        public CanvasGroup canvasGroup;
    }
    #endregion
}

// HitAccuracy enum moved to DataStructures.cs