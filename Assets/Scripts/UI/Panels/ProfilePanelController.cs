using UnityEngine;
using TMPro;

/// <summary>
/// ProfilePanelController — Populates the runtime Profile panel with live data
/// from PlayerProgressionSystem and AchievementSystem.
/// </summary>
public class ProfilePanelController : MonoBehaviour
{
    void Start()
    {
        PopulateData();
    }

    public void PopulateData()
    {
        var pps = PlayerProgressionSystem.Instance;
        var achieveSys = AchievementSystem.Instance;
        if (pps == null) return;

        var profile = pps.GetProfile();
        if (profile == null) return;

        SetText("profile-level", $"LV.{profile.level}");
        SetText("profile-rank", $"Rank: {pps.GetRank()}");
        SetText("profile-xp", $"XP: {profile.currentXP}/{pps.GetXPForNextLevel()} ({pps.GetLevelProgress() * 100f:F0}%)");
        SetText("profile-currency", $"Altin: {profile.currency}");
        SetText("profile-streak", $"Giris Serisi: {profile.loginStreak} gun");

        // XP Progress Bar
        SetProgressBar("profile-xp-bar", pps.GetLevelProgress());

        // Stats
        SetText("profile-total-songs", $"Calinan Sarki: {profile.totalSongsPlayed}");
        SetText("profile-total-notes", $"Vurulan Nota: {profile.totalNotesHit}");
        SetText("profile-best-combo", $"En Iyi Kombo: {profile.bestCombo}");
        SetText("profile-avg-accuracy", $"En Iyi Dogruluk: %{profile.bestAccuracy:F1}");

        // Achievements
        if (achieveSys != null)
        {
            int unlocked = achieveSys.GetUnlockedCount();
            int total = achieveSys.GetTotalCount();
            float pct = achieveSys.GetCompletionPercentage();
            SetText("profile-achievements", $"Acilan: {unlocked} / {total} (%{pct:F0})");

            var recent = achieveSys.GetRecentlyUnlocked(1);
            if (recent.Count > 0)
            {
                SetText("profile-recent-achieve", $"Son: {recent[0].title}");
            }
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
