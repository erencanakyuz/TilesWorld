using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("🎨 UI References")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private Canvas overlayCanvas;
    [SerializeField] private Canvas hudCanvas;

    [Header("📊 HUD Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private TextMeshProUGUI multiplierText;
    [SerializeField] private Slider healthBar;
    [SerializeField] private Image instrumentIcon;

    [Header("🎮 Game State Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject songSelectionPanel;
    [SerializeField] private GameObject gameplayPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("🎵 Audio Feedback UI")]
    [SerializeField] private GameObject perfectHitEffect;
    [SerializeField] private GameObject goodHitEffect;
    [SerializeField] private GameObject missEffect;
    [SerializeField] private Transform effectParent;

    [Header("📱 Mobile UI")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private GameObject mobileControls;

    [Header("⚙️ UI Configuration")]
    [SerializeField] private float effectDuration = 1.0f;
    [SerializeField] private AnimationCurve fadeAnimation;
    // Removed unused field adaptiveUISize

    // UI State Management
    private Dictionary<GameState, GameObject> statePanels;
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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        InitializeUISystem();
        ConfigureCanvasScalers();
        SetupEventListeners();
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
        statePanels = new Dictionary<GameState, GameObject>
        {
            { GameState.MainMenu, mainMenuPanel },
            { GameState.SongSelection, songSelectionPanel },
            { GameState.Playing, gameplayPanel },
            { GameState.Paused, pausePanel },
            { GameState.GameOver, gameOverPanel }
        };

        // Initialize hit effect pool
        hitEffectPool = new Queue<GameObject>();
        activeEffects = new List<GameObject>();

        InitializeHitEffectPool();
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
        // Subscribe to GameManager events
        GameManager.OnGameStateChanged += HandleGameStateChange;
        GameManager.OnScoreChanged += UpdateScore;
        GameManager.OnComboChanged += UpdateCombo;

        // Setup button events
        if (pauseButton != null)
            pauseButton.onClick.AddListener(() => OnPausePressed?.Invoke());

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() => OnSettingsPressed?.Invoke());
    }

    #region Game State UI Management
    void HandleGameStateChange(GameState newState)
    {
        // Deactivate all panels first
        foreach (var panel in statePanels.Values)
        {
            if (panel != null) panel.SetActive(false);
        }

        // Activate the correct panel for the new state
        if (statePanels.TryGetValue(newState, out GameObject activePanel))
        {
            if (activePanel != null) activePanel.SetActive(true);
        }

        // Handle specific logic for each state
        switch (newState)
        {
            case GameState.MainMenu:
                ShowMainMenuUI();
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
    #endregion

    void OnDestroy()
    {
        // Unsubscribe from events
        GameManager.OnGameStateChanged -= HandleGameStateChange;
        GameManager.OnScoreChanged -= UpdateScore;
        GameManager.OnComboChanged -= UpdateCombo;
    }
}

// Supporting enums
public enum HitAccuracy
{
    Miss,
    Good,
    Perfect
}