using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// WorldTourPanelController — Populates the runtime World Tour panel
/// with live city data from WorldTourSystem.
/// </summary>
public class WorldTourPanelController : MonoBehaviour
{
    void Start()
    {
        PopulateData();
    }

    public void PopulateData()
    {
        var wts = WorldTourSystem.Instance;
        if (wts == null) return;

        var cities = wts.GetAllCities();
        if (cities == null) return;

        // Overall progress
        float progress = wts.GetTourProgress();
        SetText("tour-progress-text", $"Tur Ilerlemesi: %{progress * 100f:F0}");
        SetProgressBar("tour-progress-bar", progress);

        int playerLevel = PlayerProgressionSystem.Instance?.GetLevel() ?? 1;

        for (int i = 0; i < cities.Count && i < 10; i++)
        {
            var city = cities[i];
            var card = FindDeep(transform, $"CityCard_{i}");
            if (card == null) continue;

            bool unlocked = wts.IsCityUnlocked(city.cityId);
            bool completed = wts.IsCityCompleted(city.cityId);

            SetText(card, $"city-icon-{i}", city.iconEmoji);
            SetText(card, $"city-name-{i}", $"{city.cityName}, {city.country}");

            if (completed)
            {
                SetText(card, $"city-status-{i}", "[OK] Tamamlandi!");
                SetTextColor(card, $"city-status-{i}", new Color(0.3f, 1f, 0.4f));
            }
            else if (unlocked)
            {
                int totalStars = wts.GetCityTotalStars(city.cityId);
                int maxStars = city.concerts.Count * 5;
                SetText(card, $"city-status-{i}", $"{totalStars}/{maxStars} yildiz | {city.concerts.Count} konser");
                SetTextColor(card, $"city-status-{i}", new Color(0.2f, 0.78f, 1f));
            }
            else
            {
                SetText(card, $"city-status-{i}", $"[Kilitli] Seviye {city.requiredLevel} gerekli");
                SetTextColor(card, $"city-status-{i}", new Color(0.5f, 0.5f, 0.6f));
            }

            // Stars display
            int cityStars = wts.GetCityTotalStars(city.cityId);
            int maxCityStars = city.concerts.Count * 5;
            string starsText = completed ? "***" : $"{cityStars}/{maxCityStars}";
            SetText(card, $"city-stars-{i}", starsText);

            // Wire card click to select city
            if (unlocked && !completed)
            {
                int cityIndex = i;
                string cityId = city.cityId;
                var btn = card.GetComponent<Button>();
                if (btn == null) btn = card.gameObject.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SelectCity(cityId, cityIndex));
            }
        }

        // Hide unused city cards
        for (int i = cities.Count; i < 10; i++)
        {
            var card = FindDeep(transform, $"CityCard_{i}");
            if (card != null) card.gameObject.SetActive(false);
        }

        // Wire StartConcert button
        var startBtn = FindDeep(transform, "StartConcertButton");
        if (startBtn != null)
        {
            var btn = startBtn.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => StartSelectedConcert());
            }
        }
    }

    private string selectedCityId;

    private void SelectCity(string cityId, int index)
    {
        selectedCityId = cityId;
        Debug.Log($"[WorldTour] Selected city: {cityId}");
    }

    private void StartSelectedConcert()
    {
        if (string.IsNullOrEmpty(selectedCityId))
        {
            Debug.LogWarning("[WorldTour] No city selected!");
            return;
        }

        var wts = WorldTourSystem.Instance;
        if (wts == null) return;

        // Find next incomplete concert
        var cities = wts.GetAllCities();
        var city = cities.Find(c => c.cityId == selectedCityId);
        if (city == null) return;

        int concertIndex = 0;
        for (int i = 0; i < city.concerts.Count; i++)
        {
            int stars = wts.GetConcertStars(selectedCityId, i);
            if (stars == 0)
            {
                concertIndex = i;
                break;
            }
        }

        // Set tour mode and start song
        if (GamificationManager.Instance != null)
        {
            GamificationManager.Instance.SetTourMode(selectedCityId, concertIndex);
        }

        // Start the concert's song
        var concert = city.concerts[concertIndex];
        if (concert.songKeys.Count > 0)
        {
            string songKey = concert.songKeys[0];
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
                Debug.LogWarning($"[WorldTour] Song not found: {songKey}");
            }
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
