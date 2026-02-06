using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// [DEPRECATED] SongResultUI - Replaced by SongResultUI_UIToolkit.
/// Do NOT add this component to any GameObject. Use the UI Toolkit version instead.
/// </summary>
[System.Obsolete("Use SongResultUI_UIToolkit instead. This UGUI version is deprecated.")]
public class SongResultUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas parentCanvas;

    private GameObject resultPanel;
    private UIConfig uiConfig;

    // Cached result data
    private SongResultPackage currentResult;

    void Awake()
    {
        uiConfig = Resources.Load<UIConfig>("UI/UIConfig");
    }

    void OnEnable()
    {
        GamificationManager.OnSongResultProcessed += ShowResult;
    }

    void OnDisable()
    {
        GamificationManager.OnSongResultProcessed -= ShowResult;
    }

    /// <summary>
    /// Shows the song result screen with animated reveal
    /// </summary>
    public void ShowResult(SongResultPackage result)
    {
        if (result == null) return;
        currentResult = result;

        // Find canvas
        if (parentCanvas == null)
        {
            var canvasLocator = FindFirstObjectByType<CanvasLocator>();
            parentCanvas = canvasLocator?.OverlayCanvas ?? canvasLocator?.MainCanvas;
            if (parentCanvas == null) parentCanvas = FindFirstObjectByType<Canvas>();
        }
        if (parentCanvas == null) return;

        // Destroy old panel if exists
        if (resultPanel != null) Destroy(resultPanel);

        BuildResultPanel(result);
    }

    private void BuildResultPanel(SongResultPackage result)
    {
        Color bgColor = uiConfig != null ? uiConfig.backgroundColor : new Color(0.08f, 0.08f, 0.12f);
        Color accentColor = uiConfig != null ? uiConfig.accentColor : new Color(1f, 0.4f, 0.6f);
        Color primaryColor = uiConfig != null ? uiConfig.primaryColor : new Color(0.2f, 0.8f, 1f);
        Color textColor = uiConfig != null ? uiConfig.textPrimaryColor : Color.white;
        Color textSecondary = uiConfig != null ? uiConfig.textSecondaryColor : new Color(0.7f, 0.7f, 0.8f);
        Color successColor = uiConfig != null ? uiConfig.successColor : new Color(0.3f, 1f, 0.4f);
        Color warningColor = uiConfig != null ? uiConfig.warningColor : new Color(1f, 0.8f, 0.2f);

        // Main panel (full screen overlay)
        resultPanel = new GameObject("SongResultPanel");
        resultPanel.transform.SetParent(parentCanvas.transform, false);

        RectTransform panelRect = resultPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;

        Image panelBg = resultPanel.AddComponent<Image>();
        panelBg.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0.95f);

        // Content container (centered, scrollable area)
        GameObject content = new GameObject("Content");
        content.transform.SetParent(resultPanel.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.1f, 0.05f);
        contentRect.anchorMax = new Vector2(0.9f, 0.95f);
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 12;
        vlg.padding = new RectOffset(20, 20, 15, 15);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ===== HEADER: Song Title & Artist =====
        AddText(content.transform, "header_title", result.stats?.songName ?? "Unknown", 32, textColor, FontStyles.Bold, 45);
        AddText(content.transform, "header_artist", result.stats?.artist ?? "", 20, textSecondary, FontStyles.Italic, 30);

        // ===== STARS =====
        int stars = result.reward?.starsEarned ?? 0;
        string starDisplay = "";
        for (int i = 0; i < 5; i++)
        {
            starDisplay += i < stars ? "★ " : "☆ ";
        }
        AddText(content.transform, "stars", starDisplay.Trim(), 40, warningColor, FontStyles.Normal, 55);

        // ===== ACCURACY BAR =====
        float accuracy = result.stats?.accuracy ?? 0f;
        AddText(content.transform, "accuracy_label", $"Doğruluk: %{accuracy:F1}", 24, 
            accuracy >= 90f ? successColor : accuracy >= 70f ? primaryColor : warningColor, FontStyles.Bold, 35);

        // ===== HIT BREAKDOWN =====
        AddSeparator(content.transform, accentColor);
        AddText(content.transform, "breakdown_title", "📊 İsabet Detayları", 20, primaryColor, FontStyles.Bold, 30);

        var stats = result.stats;
        if (stats != null)
        {
            AddStatRow(content.transform, "💎 Perfect", stats.perfectHits.ToString(), successColor, 26);
            AddStatRow(content.transform, "✅ Good", stats.goodHits.ToString(), primaryColor, 26);
            AddStatRow(content.transform, "⚠️ Okay", stats.okayHits.ToString(), warningColor, 26);
            AddStatRow(content.transform, "❌ Miss", stats.missedNotes.ToString(), 
                uiConfig != null ? uiConfig.dangerColor : Color.red, 26);
            AddStatRow(content.transform, "🔗 Max Combo", stats.maxCombo.ToString(), accentColor, 26);
        }

        // ===== XP BREAKDOWN =====
        AddSeparator(content.transform, accentColor);
        AddText(content.transform, "xp_title", "⚡ XP Kazanımı", 20, primaryColor, FontStyles.Bold, 30);

        var reward = result.reward;
        if (reward != null)
        {
            if (reward.xpFromPerfects > 0) AddStatRow(content.transform, "Perfect Bonus", $"+{reward.xpFromPerfects} XP", successColor, 24);
            if (reward.xpFromGoods > 0) AddStatRow(content.transform, "Good Bonus", $"+{reward.xpFromGoods} XP", primaryColor, 24);
            if (reward.xpFromCompletion > 0) AddStatRow(content.transform, "Tamamlama", $"+{reward.xpFromCompletion} XP", textColor, 24);
            if (reward.xpFromFullCombo > 0) AddStatRow(content.transform, "🌟 Full Combo!", $"+{reward.xpFromFullCombo} XP", warningColor, 24);
            if (reward.xpFromNoMiss > 0) AddStatRow(content.transform, "✨ Hatasız!", $"+{reward.xpFromNoMiss} XP", successColor, 24);
            if (reward.difficultyMultiplier > 1f) AddStatRow(content.transform, "Zorluk Çarpanı", $"x{reward.difficultyMultiplier:F2}", accentColor, 24);

            AddSeparator(content.transform, warningColor);
            AddStatRow(content.transform, "TOPLAM XP", $"+{reward.totalXP}", warningColor, 28);
            AddStatRow(content.transform, "TOPLAM 💰", $"+{reward.totalCurrency}", new Color(1f, 0.85f, 0.2f), 28);
        }

        // ===== LEVEL PROGRESS =====
        AddSeparator(content.transform, accentColor);
        AddText(content.transform, "level_info", $"Seviye {result.newLevel} • {result.rank}", 22, primaryColor, FontStyles.Bold, 30);

        // XP Progress text
        float xpProgress = result.xpProgress;
        int currentXP = result.newXP;
        int neededXP = PlayerProgressionSystem.Instance?.GetXPForNextLevel() ?? 500;
        AddText(content.transform, "xp_progress", $"XP: {currentXP}/{neededXP} ({xpProgress * 100:F0}%)", 18, textSecondary, FontStyles.Normal, 25);

        // ===== BATTLE RESULT (if applicable) =====
        if (result.isBattleMode && result.battleResult != null)
        {
            AddSeparator(content.transform, new Color(0.9f, 0.3f, 0.3f));
            var br = result.battleResult;
            string battleTitle = br.isVictory ? "⚔️ DÜELLO KAZANILDI!" : "⚔️ Düello Kaybedildi";
            Color battleColor = br.isVictory ? successColor : uiConfig != null ? uiConfig.dangerColor : Color.red;
            AddText(content.transform, "battle_result", battleTitle, 26, battleColor, FontStyles.Bold, 35);
            AddText(content.transform, "battle_artist", $"vs {br.artist?.artistName ?? "Unknown"}", 20, textSecondary, FontStyles.Italic, 28);
            
            if (br.isVictory && !string.IsNullOrEmpty(br.unlockedTitle))
            {
                AddText(content.transform, "battle_title", $"🏅 Ünvan: {br.unlockedTitle}", 18, warningColor, FontStyles.Normal, 25);
            }

            if (br.performanceTips != null && br.performanceTips.Count > 0)
            {
                AddText(content.transform, "battle_tip", $"💡 {br.performanceTips[0]}", 16, textSecondary, FontStyles.Normal, 24);
            }
        }

        // ===== TOUR RESULT (if applicable) =====
        if (result.isTourMode)
        {
            AddSeparator(content.transform, new Color(0.2f, 0.8f, 0.4f));
            AddText(content.transform, "tour_info", "🌍 Dünya Turu Konseri Tamamlandı!", 22, successColor, FontStyles.Bold, 30);
        }

        // ===== BUTTONS =====
        AddSeparator(content.transform, accentColor);
        
        GameObject buttonRow = new GameObject("Buttons");
        buttonRow.transform.SetParent(content.transform, false);
        RectTransform buttonRowRect = buttonRow.AddComponent<RectTransform>();
        buttonRowRect.sizeDelta = new Vector2(0, 60);
        LayoutElement buttonRowLE = buttonRow.AddComponent<LayoutElement>();
        buttonRowLE.preferredHeight = 60;
        
        HorizontalLayoutGroup hlg = buttonRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 30;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;

        AddButton(buttonRow.transform, "Tekrar Oyna", accentColor, () =>
        {
            HideResult();
            UIManager.Instance?.OnRestartPressed?.Invoke();
        });

        AddButton(buttonRow.transform, "Devam Et", primaryColor, () =>
        {
            HideResult();
            UIManager.Instance?.OnMainMenuPressed?.Invoke();
        });

        // ===== ANIMATE IN =====
        CanvasGroup cg = resultPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.DOFade(1f, 0.5f).SetUpdate(true);
        panelRect.localScale = Vector3.one * 0.9f;
        panelRect.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    private void AddText(Transform parent, string name, string text, int fontSize, Color color, FontStyles style, float height)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, height);
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = height;

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private void AddStatRow(Transform parent, string label, string value, Color valueColor, float height)
    {
        GameObject row = new GameObject($"Stat_{label}");
        row.transform.SetParent(parent, false);
        RectTransform rowRect = row.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(0, height);
        LayoutElement le = row.AddComponent<LayoutElement>();
        le.preferredHeight = height;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = true;

        Color textColor = uiConfig != null ? uiConfig.textSecondaryColor : new Color(0.7f, 0.7f, 0.8f);

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = height * 0.6f;
        labelText.color = textColor;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;

        // Value
        GameObject valueObj = new GameObject("Value");
        valueObj.transform.SetParent(row.transform, false);
        TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.text = value;
        valueText.fontSize = height * 0.65f;
        valueText.color = valueColor;
        valueText.fontStyle = FontStyles.Bold;
        valueText.alignment = TextAlignmentOptions.MidlineRight;
    }

    private void AddSeparator(Transform parent, Color color)
    {
        GameObject sep = new GameObject("Separator");
        sep.transform.SetParent(parent, false);
        RectTransform sepRect = sep.AddComponent<RectTransform>();
        sepRect.sizeDelta = new Vector2(0, 2);
        LayoutElement le = sep.AddComponent<LayoutElement>();
        le.preferredHeight = 2;

        Image sepImage = sep.AddComponent<Image>();
        sepImage.color = new Color(color.r, color.g, color.b, 0.3f);
    }

    private void AddButton(Transform parent, string text, Color color, System.Action onClick)
    {
        GameObject btnObj = new GameObject($"Btn_{text}");
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(180, 50);
        LayoutElement le = btnObj.AddComponent<LayoutElement>();
        le.preferredWidth = 180;
        le.preferredHeight = 50;

        Image bg = btnObj.AddComponent<Image>();
        bg.color = color;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = bg;

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        btn.onClick.AddListener(() => onClick?.Invoke());
    }

    public void HideResult()
    {
        if (resultPanel != null)
        {
            var cg = resultPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.DOFade(0f, 0.3f).SetUpdate(true).OnComplete(() =>
                {
                    if (resultPanel != null)
                        Destroy(resultPanel);
                });
            }
            else
            {
                Destroy(resultPanel);
            }
        }
    }
}
