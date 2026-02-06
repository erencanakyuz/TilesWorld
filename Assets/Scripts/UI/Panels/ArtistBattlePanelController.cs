using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ArtistBattlePanelController — Populates the runtime Artist Battle panel
/// with live data from ArtistBattleSystem.
/// </summary>
public class ArtistBattlePanelController : MonoBehaviour
{
    void Start()
    {
        PopulateData();
    }

    public void PopulateData()
    {
        var abs = ArtistBattleSystem.Instance;
        if (abs == null) return;

        var artists = abs.GetAllArtists();
        if (artists == null) return;

        // Battle stats
        var (wins, losses) = abs.GetTotalBattleStats();
        SetText("battle-stats", $"Galibiyet: {wins}  |  Maglubiyet: {losses}");

        int playerLevel = PlayerProgressionSystem.Instance?.GetLevel() ?? 1;

        for (int i = 0; i < artists.Count && i < 8; i++)
        {
            var artist = artists[i];
            var card = FindDeep(transform, $"ArtistCard_{i}");
            if (card == null) continue;

            bool unlocked = abs.IsArtistUnlocked(artist.artistId);
            bool defeated = abs.IsArtistDefeated(artist.artistId);
            bool mastered = abs.IsArtistMastered(artist.artistId);

            SetText(card, $"artist-icon-{i}", artist.portraitEmoji);
            SetText(card, $"artist-name-{i}", artist.artistName);
            SetText(card, $"artist-era-{i}", artist.era);
            SetText(card, $"artist-ability-{i}", $"* {artist.specialAbility}");

            if (mastered)
            {
                SetText(card, $"artist-status-{i}", $"[USTA] {artist.masterTitle}");
                SetTextColor(card, $"artist-status-{i}", new Color(1f, 0.8f, 0.2f));
            }
            else if (defeated)
            {
                int battleWins = abs.GetBattleWinsAgainst(artist.artistId);
                SetText(card, $"artist-status-{i}", $"[OK] Yenildi! ({battleWins}x) - {artist.defeatTitle}");
                SetTextColor(card, $"artist-status-{i}", new Color(0.3f, 1f, 0.4f));
            }
            else if (unlocked)
            {
                SetText(card, $"artist-status-{i}", $"Hedef: %{artist.targetAccuracy:F0} dogruluk, {artist.targetScore} puan");
                SetTextColor(card, $"artist-status-{i}", new Color(0.2f, 0.78f, 1f));
            }
            else
            {
                SetText(card, $"artist-status-{i}", $"[Kilitli] Seviye {artist.requiredLevel} gerekli");
                SetTextColor(card, $"artist-status-{i}", new Color(0.5f, 0.5f, 0.6f));
            }

            // Wire fight button
            var fightBtn = FindDeep(card, $"FightButton_{i}");
            if (fightBtn != null)
            {
                var btn = fightBtn.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    string artistId = artist.artistId;

                    if (unlocked)
                    {
                        btn.onClick.AddListener(() => StartBattle(artistId));

                        var btnText = fightBtn.GetComponentInChildren<TextMeshProUGUI>();
                        if (btnText != null) btnText.text = mastered ? ">>" : "!!";
                    }
                    else
                    {
                        btn.interactable = false;
                        var btnText = fightBtn.GetComponentInChildren<TextMeshProUGUI>();
                        if (btnText != null) btnText.text = "--";
                    }
                }
            }
        }

        // Hide unused cards
        for (int i = artists.Count; i < 8; i++)
        {
            var card = FindDeep(transform, $"ArtistCard_{i}");
            if (card != null) card.gameObject.SetActive(false);
        }
    }

    private void StartBattle(string artistId)
    {
        var abs = ArtistBattleSystem.Instance;
        if (abs == null) return;

        var artists = abs.GetAllArtists();
        var artist = artists.Find(a => a.artistId == artistId);
        if (artist == null || artist.battleSongs.Count == 0) return;

        Debug.Log($"[ArtistBattle] Starting battle against: {artist.artistName}");

        // Set battle mode
        if (GamificationManager.Instance != null)
        {
            GamificationManager.Instance.SetBattleMode(artistId);
        }

        // Pick the first unplayed battle song, or signature song
        string songKey = artist.signatureSong ?? artist.battleSongs[0];
        var songInfo = SongDatabase.Instance?.GetSongByKey(songKey);
        if (songInfo != null)
        {
            var gsd = new GameplaySongData
            {
                songName = songInfo.title,
                artist = songInfo.artist,
                duration = songInfo.duration,
                bpm = songInfo.tempo,
                audioFilePath = $"Music/{songInfo.songKey}",
                chartFilePath = $"Song_Note_Jsons/Individual/{songInfo.songKey}",
                songKey = songInfo.songKey,
                difficulty = songInfo.difficulty
            };

            var gameplayManager = Object.FindAnyObjectByType<GameplayManager>();
            if (gameplayManager != null)
            {
                gameplayManager.StartGameplay(gsd);
            }
        }
        else
        {
            Debug.LogWarning($"[ArtistBattle] Song not found: {songKey}");
        }
    }

    private void SetText(string goName, string text) => SetText(transform, goName, text);

    private void SetText(Transform root, string goName, string text)
    {
        var t = FindDeep(root, goName);
        if (t != null)
        {
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
        }
    }

    private void SetTextColor(Transform root, string goName, Color color)
    {
        var t = FindDeep(root, goName);
        if (t != null)
        {
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.color = color;
        }
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
