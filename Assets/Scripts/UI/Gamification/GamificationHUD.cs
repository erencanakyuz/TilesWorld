using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// [DEPRECATED] GamificationHUD - Replaced by GamificationHUD_UIToolkit.
/// Do NOT add this component to any GameObject. Use the UI Toolkit version instead.
/// </summary>
[System.Obsolete("Use GamificationHUD_UIToolkit instead. This UGUI version is deprecated.")]
public class GamificationHUD : MonoBehaviour
{
    [Header("UI References (Auto-discovered)")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI currencyText;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private Slider xpBar;
    [SerializeField] private TextMeshProUGUI xpText;
    [SerializeField] private TextMeshProUGUI streakText;

    [Header("Configuration")]
    [SerializeField] private bool showDuringGameplay = false;
    [SerializeField] private bool showInMenus = true;

    private Canvas parentCanvas;
    private GameObject hudPanel;
    private UIConfig uiConfig;

    void Start()
    {
        uiConfig = Resources.Load<UIConfig>("UI/UIConfig");
        
        // Subscribe to events for real-time updates (named handlers for proper unsubscription)
        PlayerProgressionSystem.OnXPGained += OnXPGainedHandler;
        PlayerProgressionSystem.OnLevelUp += OnLevelUpHandler;
        PlayerProgressionSystem.OnCurrencyChanged += OnCurrencyChangedHandler;
        GameManager.OnGameStateChanged += OnGameStateChanged;

        // Initial display
        CreateHUDElements();
        UpdateDisplay();
    }

    // Named handlers for proper event unsubscription
    private void OnXPGainedHandler(int _) => UpdateDisplay();
    private void OnLevelUpHandler(int _) => UpdateDisplay();
    private void OnCurrencyChangedHandler(int _) => UpdateDisplay();

    private void CreateHUDElements()
    {
        // Find a canvas to attach to
        var canvasLocator = FindFirstObjectByType<CanvasLocator>();
        parentCanvas = canvasLocator?.MainCanvas;
        if (parentCanvas == null) parentCanvas = FindFirstObjectByType<Canvas>();
        if (parentCanvas == null) return;

        // Create panel container
        hudPanel = new GameObject("GamificationHUD_Panel");
        hudPanel.transform.SetParent(parentCanvas.transform, false);

        RectTransform panelRect = hudPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(0.5f, 1);
        panelRect.anchoredPosition = new Vector2(0, 0);
        panelRect.sizeDelta = new Vector2(0, 50);

        // Semi-transparent background
        Image bg = hudPanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.7f);

        // Layout
        HorizontalLayoutGroup layout = hudPanel.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 20;
        layout.padding = new RectOffset(20, 20, 5, 5);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;

        Color accentColor = uiConfig != null ? uiConfig.accentColor : Color.cyan;
        Color textColor = uiConfig != null ? uiConfig.textPrimaryColor : Color.white;
        Color secondaryColor = uiConfig != null ? uiConfig.textSecondaryColor : new Color(0.7f, 0.7f, 0.8f);

        // Level badge
        levelText = CreateTextElement(hudPanel.transform, "LevelText", "LV.1", 22, accentColor, 80);
        levelText.fontStyle = FontStyles.Bold;

        // XP Bar container
        GameObject xpContainer = new GameObject("XPBarContainer");
        xpContainer.transform.SetParent(hudPanel.transform, false);
        RectTransform xpContainerRect = xpContainer.AddComponent<RectTransform>();
        xpContainerRect.sizeDelta = new Vector2(200, 40);
        LayoutElement xpLayout = xpContainer.AddComponent<LayoutElement>();
        xpLayout.preferredWidth = 200;
        xpLayout.flexibleWidth = 1;

        // XP Bar
        xpBar = CreateSlider(xpContainer.transform, "XPBar", accentColor);

        // XP Text overlay
        xpText = CreateTextElement(xpContainer.transform, "XPText", "0/500", 14, textColor, 200);
        RectTransform xpTextRect = xpText.GetComponent<RectTransform>();
        xpTextRect.anchorMin = Vector2.zero;
        xpTextRect.anchorMax = Vector2.one;
        xpTextRect.sizeDelta = Vector2.zero;
        xpText.alignment = TextAlignmentOptions.Center;

        // Currency
        currencyText = CreateTextElement(hudPanel.transform, "CurrencyText", "💰 0", 20, 
            new Color(1f, 0.85f, 0.2f), 120);

        // Rank
        rankText = CreateTextElement(hudPanel.transform, "RankText", "🎵 Novice", 16, secondaryColor, 150);

        // Streak
        streakText = CreateTextElement(hudPanel.transform, "StreakText", "", 16, 
            new Color(1f, 0.5f, 0.2f), 100);
    }

    private TextMeshProUGUI CreateTextElement(Transform parent, string name, string defaultText, 
        int fontSize, Color color, float width)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, 40);

        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredWidth = width;

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = defaultText;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;

        return text;
    }

    private Slider CreateSlider(Transform parent, string name, Color fillColor)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0, 0.3f);
        sliderRect.anchorMax = new Vector2(1, 0.7f);
        sliderRect.sizeDelta = Vector2.zero;
        sliderRect.offsetMin = new Vector2(0, 0);
        sliderRect.offsetMax = new Vector2(0, 0);

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        // Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = fillColor;

        slider.fillRect = fillRect;
        slider.value = 0;

        return slider;
    }

    public void UpdateDisplay()
    {
        var progression = PlayerProgressionSystem.Instance;
        if (progression == null) return;

        if (levelText != null)
            levelText.text = $"LV.{progression.GetLevel()}";

        if (xpBar != null)
            xpBar.value = progression.GetLevelProgress();

        if (xpText != null)
            xpText.text = $"{progression.GetCurrentXP()}/{progression.GetXPForNextLevel()}";

        if (currencyText != null)
            currencyText.text = $"💰 {progression.GetCurrency():N0}";

        if (rankText != null)
            rankText.text = $"🏆 {progression.GetRank()}";

        if (streakText != null)
        {
            int streak = progression.GetLoginStreak();
            streakText.text = streak > 1 ? $"🔥 {streak} gün" : "";
        }
    }

    private void OnGameStateChanged(GameState newState)
    {
        if (hudPanel == null) return;

        bool shouldShow = newState switch
        {
            GameState.Playing => showDuringGameplay,
            GameState.MainMenu or GameState.SongSelection or GameState.WorldTour 
                or GameState.ArtistBattle or GameState.DailyChallenge or GameState.Profile => showInMenus,
            _ => false
        };

        hudPanel.SetActive(shouldShow);

        if (shouldShow) UpdateDisplay();
    }

    void OnDestroy()
    {
        GameManager.OnGameStateChanged -= OnGameStateChanged;
        PlayerProgressionSystem.OnXPGained -= OnXPGainedHandler;
        PlayerProgressionSystem.OnLevelUp -= OnLevelUpHandler;
        PlayerProgressionSystem.OnCurrencyChanged -= OnCurrencyChangedHandler;
    }
}
