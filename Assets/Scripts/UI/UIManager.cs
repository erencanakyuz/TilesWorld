using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

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
    // Removed unused field adaptiveUISize

    // UI State Management
    private Dictionary<GameState, GameObject> statePanelPrefabs;
    private GameObject currentPanelInstance;
    private Queue<GameObject> hitEffectPool;
    private List<GameObject> activeEffects;

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

            // Event subscription'ları burada yap ki GameManager'dan önce hazır ol
            Debug.Log("🔔 UIManager Awake - Event subscription yapılıyor...");
            GameManager.OnGameStateChanged += HandleGameStateChange;
            GameManager.OnScoreChanged += UpdateScore;
            GameManager.OnComboChanged += UpdateCombo;
            Debug.Log("✅ UIManager Awake - Event subscription tamamlandı!");
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
            Debug.LogWarning($"'{scene.name}' sahnesinde bazı UI elemanları bulunamadı! UI düzgün çalışmayabilir.");
        }
    }

    bool AutoFindUIElements()
    {
        Debug.Log("🔍 AutoFindUIElements başlatılıyor...");
        bool success = true;

        // Canvas'ları akıllı şekilde bul
        success &= FindCanvases();
        Debug.Log($"   FindCanvases result: {success}");

        // HUD elemanlarını HUDCanvas içinde ara
        success &= FindHUDElements();
        Debug.Log($"   FindHUDElements result: {success}");

        // Efekt parent'ını overlay canvas'ta ara
        success &= FindEffectElements();
        Debug.Log($"   FindEffectElements result: {success}");

        // Mobil kontrolleri ara
        success &= FindMobileControls();
        Debug.Log($"   FindMobileControls result: {success}");

        Debug.Log($"🎯 AutoFindUIElements tamamlandı: {success}");
        return success;
    }

    bool FindCanvases()
    {
        Debug.Log("🔍 Canvas'ları arıyor...");
        var allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Debug.Log($"   Bulunan Canvas sayısı: {allCanvases.Length}");

        for (int i = 0; i < allCanvases.Length; i++)
        {
            Debug.Log($"   Canvas[{i}]: {allCanvases[i].name} (sortingOrder: {allCanvases[i].sortingOrder})");
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

        Debug.Log($"   ✅ mainCanvas: {(mainCanvas != null ? mainCanvas.name : "NULL")}");
        Debug.Log($"   ✅ hudCanvas: {(hudCanvas != null ? hudCanvas.name : "NULL")}");
        Debug.Log($"   ✅ overlayCanvas: {(overlayCanvas != null ? overlayCanvas.name : "NULL")}");

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
        // Butonları tüm canvas'larda ara
        var allButtons = FindObjectsByType<Button>(FindObjectsSortMode.None);

        pauseButton = System.Array.Find(allButtons, b =>
            b.name.ToLower().Contains("pause"));

        settingsButton = System.Array.Find(allButtons, b =>
            b.name.ToLower().Contains("settings") ||
            b.name.ToLower().Contains("setting"));

        // Mobile controls container'ını ara
        var allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        mobileControls = System.Array.Find(allGameObjects, go =>
            go.name.ToLower().Contains("mobile") &&
            go.name.ToLower().Contains("control"));

        return true; // Mobil kontroller opsiyonel
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

        // Initialize hit effect pool
        hitEffectPool = new Queue<GameObject>();
        activeEffects = new List<GameObject>();

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
        Debug.Log("🔔 SetupEventListeners başlatılıyor...");

        // GameManager events artık Awake()'de subscribe ediliyor
        // Burada sadece UI-specific event'ler kalıyor

        Debug.Log("✅ SetupEventListeners tamamlandı (GameManager events Awake'de handle edildi)!");
    }

    #region Game State UI Management
    void HandleGameStateChange(GameState newState)
    {
        Debug.Log($"🔔 UIManager.HandleGameStateChange ÇAĞRILDI! State: {newState}");

        // Canvas'lar henüz bulunmadıysa bekle
        if (mainCanvas == null || overlayCanvas == null || hudCanvas == null)
        {
            Debug.Log("⏳ Canvas'lar henüz yok, state change'i pending yapılıyor...");
            StartCoroutine(WaitForCanvasAndHandleState(newState));
            return;
        }

        Debug.Log($"🎮 UIManager: Handling state change to {newState}");

        HandleStateChangeImmediate(newState);
    }

    private System.Collections.IEnumerator WaitForCanvasAndHandleState(GameState newState)
    {
        Debug.Log("⏳ Canvas'ların bulunması bekleniyor...");

        // Canvas'lar bulunana kadar bekle (max 5 saniye)
        float waitTime = 0f;
        while ((mainCanvas == null || overlayCanvas == null || hudCanvas == null) && waitTime < 5f)
        {
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
        }

        if (mainCanvas != null)
        {
            Debug.Log("✅ Canvas'lar bulundu, state change işleniyor...");
            HandleStateChangeImmediate(newState);
        }
        else
        {
            Debug.LogError("❌ Canvas'lar 5 saniye içinde bulunamadı!");
        }
    }

    private void HandleStateChangeImmediate(GameState newState)
    {
        Debug.Log($"🎮 UIManager: Handling state change to {newState}");

        // Destroy the previous panel instance
        if (currentPanelInstance != null)
        {
            Debug.Log($"🗑️ Destroying previous panel: {currentPanelInstance.name}");
            Destroy(currentPanelInstance);
            currentPanelInstance = null;
        }

        // Activate the correct panel for the new state by instantiating it
        if (statePanelPrefabs.TryGetValue(newState, out GameObject prefab) && prefab != null)
        {
            Transform parentCanvas = GetParentCanvasForState(newState);
            if (parentCanvas != null)
            {
                currentPanelInstance = Instantiate(prefab, parentCanvas);
                Debug.Log($"✅ Created panel: {prefab.name} on canvas: {parentCanvas.name}");
            }
            else
            {
                Debug.LogError($"❌ Cannot create panel for {newState} - parentCanvas is NULL!");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ No panel prefab found for state: {newState}");
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
        Debug.Log($"🔍 GetParentCanvasForState({state}):");
        Debug.Log($"   mainCanvas: {(mainCanvas != null ? mainCanvas.name : "NULL")}");
        Debug.Log($"   overlayCanvas: {(overlayCanvas != null ? overlayCanvas.name : "NULL")}");
        Debug.Log($"   hudCanvas: {(hudCanvas != null ? hudCanvas.name : "NULL")}");

        switch (state)
        {
            case GameState.Paused:
                // Pause panel should appear on top of everything
                if (overlayCanvas != null)
                    return overlayCanvas.transform;
                else
                {
                    Debug.LogError("❌ OverlayCanvas is NULL! Falling back to MainCanvas");
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
                    Debug.LogError("❌ MainCanvas is NULL! Cannot create panel!");
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
    }

    void ShowGameOverUI()
    {
        if (hudCanvas != null)
            hudCanvas.gameObject.SetActive(false);

        DisplayFinalScore();
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

        Debug.Log("🎵 Song Selection UI activated");
    }
    #endregion

    #region Gameplay UI Updates
    void UpdateScore(float score)
    {
        currentScore = Mathf.RoundToInt(score);

        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString("N0");

            // Add score change animation
            StartCoroutine(ScaleTextEffect(scoreText.transform));
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
                activeEffects.Add(effect);

                StartCoroutine(AnimateHitEffect(effect));
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

    IEnumerator AnimateHitEffect(GameObject effect)
    {
        float elapsed = 0f;
        Vector3 originalScale = effect.transform.localScale;

        while (elapsed < effectDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / effectDuration;

            // Scale animation
            float scale = fadeAnimation.Evaluate(progress);
            effect.transform.localScale = originalScale * scale;

            // Fade animation
            CanvasGroup canvasGroup = effect.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - progress;
            }

            yield return null;
        }

        // Return to pool
        effect.SetActive(false);
        activeEffects.Remove(effect);
        hitEffectPool.Enqueue(effect);
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
        Debug.Log($"🏁 Final Score: {currentScore}, Max Combo: {currentCombo}");
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
        Debug.Log("🔄 Manuel UI Elements refresh başlatılıyor...");

        if (AutoFindUIElements())
        {
            ConfigureCanvasScalers();
            SetupEventListeners();
            Debug.Log("✅ UI Elements başarıyla yeniden bulundu!");
        }
        else
        {
            Debug.LogError("❌ UI Elements bulunamadı!");
        }
    }
    #endregion

    void OnDestroy()
    {
        // Unsubscribe from events
        GameManager.OnGameStateChanged -= HandleGameStateChange;
        GameManager.OnScoreChanged -= UpdateScore;
        GameManager.OnComboChanged -= UpdateCombo;
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
}

// Supporting enums
public enum HitAccuracy
{
    Miss,
    Good,
    Perfect
}