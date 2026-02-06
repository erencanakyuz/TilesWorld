using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// GamificationHUD_UIToolkit — Top bar HUD using Unity UI Toolkit (UXML/USS).
/// Shows level, XP bar, currency, rank, and login streak.
/// Attach to a GameObject with a UIDocument component whose Source Asset = GamificationHUD.uxml.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class GamificationHUD_UIToolkit : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement root;

    // Cached element references
    private Label levelLabel;
    private VisualElement xpFill;
    private Label xpText;
    private Label currencyLabel;
    private Label rankLabel;
    private Label streakLabel;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogWarning("[GamificationHUD_UIToolkit] rootVisualElement is null. UXML may not be loaded.");
            return;
        }

        // Query elements by name (like CSS selectors)
        levelLabel = root.Q<Label>("hud-level");
        xpFill = root.Q("hud-xp-fill");
        xpText = root.Q<Label>("hud-xp-text");
        currencyLabel = root.Q<Label>("hud-currency");
        rankLabel = root.Q<Label>("hud-rank");
        streakLabel = root.Q<Label>("hud-streak");

        // Subscribe to gamification events
        PlayerProgressionSystem.OnXPGained += OnXPChanged;
        PlayerProgressionSystem.OnLevelUp += OnLevelChanged;
        PlayerProgressionSystem.OnCurrencyChanged += OnCurrencyChanged;
        GameManager.OnGameStateChanged += OnGameStateChanged;

        UpdateDisplay();
    }

    void OnDisable()
    {
        PlayerProgressionSystem.OnXPGained -= OnXPChanged;
        PlayerProgressionSystem.OnLevelUp -= OnLevelChanged;
        PlayerProgressionSystem.OnCurrencyChanged -= OnCurrencyChanged;
        GameManager.OnGameStateChanged -= OnGameStateChanged;
    }

    private void OnXPChanged(int _) => UpdateDisplay();
    private void OnLevelChanged(int _) => UpdateDisplay();
    private void OnCurrencyChanged(int _) => UpdateDisplay();

    private void OnGameStateChanged(GameState state)
    {
        if (root == null) return;

        // Only show HUD during playing, song selection, main menu
        bool show = state == GameState.MainMenu
                 || state == GameState.SongSelection
                 || state == GameState.Playing
                 || state == GameState.Profile;

        root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void UpdateDisplay()
    {
        var pps = PlayerProgressionSystem.Instance;
        if (pps == null || root == null) return;

        var profile = pps.GetProfile();
        if (profile == null) return;

        int level = pps.GetLevel();
        int currentXP = pps.GetCurrentXP();
        int neededXP = pps.GetXPForNextLevel();
        float progress = pps.GetLevelProgress();
        int currency = pps.GetCurrency();
        string rank = pps.GetRank().ToString();
        int streak = pps.GetLoginStreak();

        // Update labels
        if (levelLabel != null) levelLabel.text = $"LV.{level}";
        if (xpText != null) xpText.text = $"{currentXP}/{neededXP}";
        if (currencyLabel != null) currencyLabel.text = $"💰 {currency}";
        if (rankLabel != null) rankLabel.text = $"🏆 {rank}";
        if (streakLabel != null) streakLabel.text = streak > 1 ? $"🔥{streak}" : "";

        // Update XP bar fill width via inline style (percentage)
        if (xpFill != null)
        {
            float fillPercent = Mathf.Clamp01(progress) * 100f;
            xpFill.style.width = new StyleLength(new Length(fillPercent, LengthUnit.Percent));
        }
    }
}
