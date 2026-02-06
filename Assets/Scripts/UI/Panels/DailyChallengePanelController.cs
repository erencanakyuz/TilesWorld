using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// DailyChallengePanelController — Populates the runtime Daily Challenge panel
/// with live data from DailyChallengeSystem.
/// </summary>
public class DailyChallengePanelController : MonoBehaviour
{
    void Start()
    {
        PopulateData();
    }

    public void PopulateData()
    {
        var dcs = DailyChallengeSystem.Instance;
        if (dcs == null) return;

        var challenges = dcs.GetTodayChallenges();

        // Streak
        int consecutiveDays = dcs.GetConsecutiveDays();
        SetText("daily-streak", $"Ardisik Gun: {consecutiveDays} / 7");
        SetProgressBar("daily-streak-bar", consecutiveDays / 7f);

        bool allCompleted = dcs.AreAllTodayCompleted();
        string weeklyStatus = consecutiveDays >= 7 ? "[OK] Haftalik Bonus Kazanildi!" :
            allCompleted ? $"[OK] Bugun tamamlandi! ({consecutiveDays}/7 gun)" :
            $"Haftalik Bonus: {consecutiveDays}/7 gun tamamla";
        SetText("daily-weekly-status", weeklyStatus);

        // Challenge Cards
        for (int i = 0; i < 3; i++)
        {
            if (i < challenges.Count)
            {
                var ch = challenges[i];
                PopulateChallenge(i, ch);
            }
            else
            {
                // Hide unused challenge cards
                var card = FindDeep(transform, $"ChallengeCard_{i}");
                if (card != null) card.gameObject.SetActive(false);
            }
        }

        // Summary
        int completedToday = dcs.GetCompletedToday();
        SetText("daily-completed-count", $"Bugun Tamamlanan: {completedToday} / {challenges.Count}");
        SetText("daily-total-completed", $"Toplam Tamamlanan Gorev: {dcs.GetTotalChallengesCompleted()}");

        // Wire play button to go to song selection
        var playBtn = FindDeep(transform, "PlayButton");
        if (playBtn != null)
        {
            var btn = playBtn.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    GameManager.Instance?.ChangeGameState(GameState.SongSelection);
                });
            }
        }
    }

    private void PopulateChallenge(int index, DailyChallenge ch)
    {
        SetText($"challenge-icon-{index}", ch.icon);
        SetText($"challenge-title-{index}", ch.title);
        SetText($"challenge-desc-{index}", ch.description);

        string progressText;
        if (ch.isCompleted)
        {
            progressText = "[OK] Tamamlandi!";
        }
        else
        {
            progressText = $"Ilerleme: {ch.currentProgress}/{ch.targetValue}";
        }
        SetText($"challenge-progress-{index}", progressText);
        SetText($"challenge-reward-{index}", $"+{ch.rewardXP} XP  +{ch.rewardCurrency} Altin");

        // Check mark
        SetText($"challenge-check-{index}", ch.isCompleted ? "[x]" : "[ ]");

        // Color the progress text
        if (ch.isCompleted)
        {
            SetTextColor($"challenge-progress-{index}", new Color(0.3f, 1f, 0.4f));
            SetTextColor($"challenge-check-{index}", new Color(0.3f, 1f, 0.4f));
        }
    }

    private void SetText(string goName, string text)
    {
        var t = FindDeep(transform, goName);
        if (t != null)
        {
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }
    }

    private void SetTextColor(string goName, Color color)
    {
        var t = FindDeep(transform, goName);
        if (t != null)
        {
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.color = color;
        }
    }

    private void SetProgressBar(string goName, float progress)
    {
        var bar = FindDeep(transform, goName);
        if (bar == null) return;
        var fill = bar.Find("Fill");
        if (fill == null) return;
        var rt = fill.GetComponent<RectTransform>();
        rt.anchorMax = new Vector2(Mathf.Clamp01(progress), 1);
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
