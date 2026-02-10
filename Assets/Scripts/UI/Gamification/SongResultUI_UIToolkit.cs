using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// SongResultUI_UIToolkit — Full-screen song result screen using Unity UI Toolkit.
/// Shows XP breakdown, stars, accuracy, hit stats, battle/tour results, and action buttons.
/// Attach to a GameObject with UIDocument component whose Source Asset = SongResult.uxml.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class SongResultUI_UIToolkit : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement root;
    private VisualElement resultRoot;

    // Cached references
    private Label songTitle;
    private Label songArtist;
    private Label[] starLabels = new Label[5];
    private Label accuracyLabel;
    private Label valPerfect, valGood, valOkay, valMiss, valCombo;
    private VisualElement xpBreakdown;
    private Label valTotalXP, valTotalCurrency;
    private Label levelInfo, xpProgress;
    private VisualElement battleBanner, tourBanner;
    private Label battleTitle, battleArtistName, battleUnlock, battleTip;
    private Button btnRetry, btnContinue;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogWarning("[SongResultUI_UIToolkit] rootVisualElement is null. UXML may not be loaded.");
            return;
        }

        // CRITICAL: Let clicks pass through to UGUI canvases underneath
        root.pickingMode = PickingMode.Ignore;

        resultRoot = root.Q("result-root");

        CacheElements();
        BindButtons();

        // Subscribe to result event
        // DISABLED: UGUI SongResultPanelController now handles this with animated 3-star display
        // GamificationManager.OnSongResultProcessed += ShowResult;

        // Start hidden and non-interactive
        if (resultRoot != null)
        {
            resultRoot.style.display = DisplayStyle.None;
            resultRoot.RemoveFromClassList("visible");
            resultRoot.pickingMode = PickingMode.Ignore;
        }
    }

    void OnDisable()
    {
        GamificationManager.OnSongResultProcessed -= ShowResult;
    }

    private void CacheElements()
    {
        songTitle = root.Q<Label>("song-title");
        songArtist = root.Q<Label>("song-artist");

        for (int i = 0; i < 5; i++)
            starLabels[i] = root.Q<Label>($"star-{i}");

        accuracyLabel = root.Q<Label>("accuracy-label");
        valPerfect = root.Q<Label>("val-perfect");
        valGood = root.Q<Label>("val-good");
        valOkay = root.Q<Label>("val-okay");
        valMiss = root.Q<Label>("val-miss");
        valCombo = root.Q<Label>("val-combo");
        xpBreakdown = root.Q("xp-breakdown");
        valTotalXP = root.Q<Label>("val-total-xp");
        valTotalCurrency = root.Q<Label>("val-total-currency");
        levelInfo = root.Q<Label>("level-info");
        xpProgress = root.Q<Label>("xp-progress");
        battleBanner = root.Q("battle-banner");
        tourBanner = root.Q("tour-banner");
        battleTitle = root.Q<Label>("battle-title");
        battleArtistName = root.Q<Label>("battle-artist-name");
        battleUnlock = root.Q<Label>("battle-unlock");
        battleTip = root.Q<Label>("battle-tip");
        btnRetry = root.Q<Button>("btn-retry");
        btnContinue = root.Q<Button>("btn-continue");
    }

    private void BindButtons()
    {
        btnRetry?.RegisterCallback<ClickEvent>(evt =>
        {
            HideResult();
            UIManager.Instance?.OnRestartPressed?.Invoke();
        });

        btnContinue?.RegisterCallback<ClickEvent>(evt =>
        {
            HideResult();
            UIManager.Instance?.OnMainMenuPressed?.Invoke();
        });
    }

    /// <summary>
    /// Called by GamificationManager.OnSongResultProcessed event
    /// </summary>
    public void ShowResult(SongResultPackage result)
    {
        if (result == null || resultRoot == null) return;

        PopulateData(result);

        // Make visible, enable picking on the overlay and buttons
        resultRoot.style.display = DisplayStyle.Flex;
        resultRoot.pickingMode = PickingMode.Position;
        resultRoot.AddToClassList("visible");

        // Ensure buttons are clickable
        if (btnRetry != null) btnRetry.pickingMode = PickingMode.Position;
        if (btnContinue != null) btnContinue.pickingMode = PickingMode.Position;
    }

    public void HideResult()
    {
        if (resultRoot == null) return;
        resultRoot.RemoveFromClassList("visible");

        // After transition, hide completely and disable picking
        resultRoot.schedule.Execute(() =>
        {
            if (resultRoot != null && !resultRoot.ClassListContains("visible"))
            {
                resultRoot.style.display = DisplayStyle.None;
                resultRoot.pickingMode = PickingMode.Ignore;
            }
        }).ExecuteLater(600); // After 0.5s transition
    }

    private void PopulateData(SongResultPackage result)
    {
        var stats = result.stats;
        var reward = result.reward;

        // Header
        if (songTitle != null) songTitle.text = stats?.songName ?? "Unknown";
        if (songArtist != null) songArtist.text = stats?.artist ?? "";

        // Stars
        int stars = reward?.starsEarned ?? 0;
        for (int i = 0; i < 5; i++)
        {
            if (starLabels[i] == null) continue;
            bool earned = i < stars;
            starLabels[i].text = earned ? "★" : "☆";
            starLabels[i].EnableInClassList("earned", earned);
        }

        // Accuracy
        float accuracy = stats?.accuracy ?? 0f;
        if (accuracyLabel != null)
        {
            accuracyLabel.text = $"Doğruluk: %{accuracy:F1}";
            accuracyLabel.RemoveFromClassList("accuracy-high");
            accuracyLabel.RemoveFromClassList("accuracy-mid");
            accuracyLabel.RemoveFromClassList("accuracy-low");
            if (accuracy >= 90f) accuracyLabel.AddToClassList("accuracy-high");
            else if (accuracy >= 70f) accuracyLabel.AddToClassList("accuracy-mid");
            else accuracyLabel.AddToClassList("accuracy-low");
        }

        // Hit breakdown
        if (valPerfect != null) valPerfect.text = stats?.perfectHits.ToString() ?? "0";
        if (valGood != null) valGood.text = stats?.goodHits.ToString() ?? "0";
        if (valOkay != null) valOkay.text = stats?.okayHits.ToString() ?? "0";
        if (valMiss != null) valMiss.text = stats?.missedNotes.ToString() ?? "0";
        if (valCombo != null) valCombo.text = stats?.maxCombo.ToString() ?? "0";

        // XP breakdown (dynamic rows)
        if (xpBreakdown != null)
        {
            xpBreakdown.Clear();
            if (reward != null)
            {
                if (reward.xpFromPerfects > 0)
                    AddXPRow("Perfect Bonus", $"+{reward.xpFromPerfects} XP", "stat-value-success");
                if (reward.xpFromGoods > 0)
                    AddXPRow("Good Bonus", $"+{reward.xpFromGoods} XP", "stat-value-primary");
                if (reward.xpFromCompletion > 0)
                    AddXPRow("Tamamlama", $"+{reward.xpFromCompletion} XP", "stat-value-text");
                if (reward.xpFromFullCombo > 0)
                    AddXPRow("🌟 Full Combo!", $"+{reward.xpFromFullCombo} XP", "stat-value-warning");
                if (reward.xpFromNoMiss > 0)
                    AddXPRow("✨ Hatasız!", $"+{reward.xpFromNoMiss} XP", "stat-value-success");
                if (reward.difficultyMultiplier > 1f)
                    AddXPRow("Zorluk Çarpanı", $"x{reward.difficultyMultiplier:F2}", "stat-value-accent");
            }
        }

        // Totals
        if (reward != null)
        {
            if (valTotalXP != null) valTotalXP.text = $"+{reward.totalXP}";
            if (valTotalCurrency != null) valTotalCurrency.text = $"+{reward.totalCurrency}";
        }

        // Level info
        if (levelInfo != null) levelInfo.text = $"Seviye {result.newLevel} • {result.rank}";
        int currentXP = result.newXP;
        int neededXP = PlayerProgressionSystem.Instance?.GetXPForNextLevel() ?? 500;
        float xpPercent = result.xpProgress * 100f;
        if (xpProgress != null) xpProgress.text = $"XP: {currentXP}/{neededXP} ({xpPercent:F0}%)";

        // Battle banner
        if (result.isBattleMode && result.battleResult != null && battleBanner != null)
        {
            battleBanner.RemoveFromClassList("hidden");
            var br = result.battleResult;

            battleBanner.RemoveFromClassList("battle-win");
            battleBanner.RemoveFromClassList("battle-lose");
            battleBanner.AddToClassList(br.isVictory ? "battle-win" : "battle-lose");

            if (battleTitle != null)
            {
                battleTitle.text = br.isVictory ? "⚔️ DÜELLO KAZANILDI!" : "⚔️ Düello Kaybedildi";
                battleTitle.style.color = br.isVictory
                    ? new StyleColor(new Color(0.3f, 1f, 0.4f))
                    : new StyleColor(new Color(1f, 0.3f, 0.3f));
            }

            if (battleArtistName != null)
                battleArtistName.text = $"vs {br.artist?.artistName ?? "Unknown"}";

            if (battleUnlock != null)
            {
                if (br.isVictory && !string.IsNullOrEmpty(br.unlockedTitle))
                {
                    battleUnlock.text = $"🏅 Ünvan: {br.unlockedTitle}";
                    battleUnlock.style.display = DisplayStyle.Flex;
                }
                else
                {
                    battleUnlock.style.display = DisplayStyle.None;
                }
            }

            if (battleTip != null)
            {
                if (br.performanceTips != null && br.performanceTips.Count > 0)
                {
                    battleTip.text = $"💡 {br.performanceTips[0]}";
                    battleTip.style.display = DisplayStyle.Flex;
                }
                else
                {
                    battleTip.style.display = DisplayStyle.None;
                }
            }
        }
        else if (battleBanner != null)
        {
            battleBanner.AddToClassList("hidden");
        }

        // Tour banner
        if (tourBanner != null)
        {
            if (result.isTourMode)
                tourBanner.RemoveFromClassList("hidden");
            else
                tourBanner.AddToClassList("hidden");
        }
    }

    private void AddXPRow(string label, string value, string valueClass)
    {
        var row = new VisualElement();
        row.AddToClassList("stat-row");

        var labelEl = new Label(label);
        labelEl.AddToClassList("stat-label");
        row.Add(labelEl);

        var valueEl = new Label(value);
        valueEl.AddToClassList("stat-value");
        valueEl.AddToClassList(valueClass);
        row.Add(valueEl);

        xpBreakdown.Add(row);
    }
}
