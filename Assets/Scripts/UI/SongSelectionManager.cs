using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SongSelectionManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Dropdown songDropdown;
    public TextMeshProUGUI songDetails;
    public Button playButton;
    public Button backButton;

    [Header("Song Data")]
    public SongData[] availableSongs;

    [System.Serializable]
    public class SongData
    {
        public string title;
        public string artist;
        public string duration;
        public DifficultyLevel difficulty;
        public int bpm;
        public string songKey; // For loading the actual song
    }

    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard,
        Expert
    }

    private void Start()
    {
        InitializeSongData();
        SetupUI();
        SetupEventListeners();

        // Show default selection
        UpdateSongDetails(0);
    }

    private void InitializeSongData()
    {
        // Real song data from ClassicPlayer database
        availableSongs = new SongData[]
        {
            new SongData
            {
                title = "Cannon",
                artist = "Pachelbel",
                duration = "3:00",
                difficulty = DifficultyLevel.Easy,
                bpm = 77,
                songKey = "cannon"
            },
            new SongData
            {
                title = "The Entertainer",
                artist = "Scott Joplin",
                duration = "3:20",
                difficulty = DifficultyLevel.Medium,
                bpm = 80,
                songKey = "entertainer"
            },
            new SongData
            {
                title = "Air on a G String",
                artist = "Bach",
                duration = "2:40",
                difficulty = DifficultyLevel.Easy,
                bpm = 65,
                songKey = "air_g_string"
            },
            new SongData
            {
                title = "Vidalita",
                artist = "Traditional",
                duration = "2:30",
                difficulty = DifficultyLevel.Medium,
                bpm = 120,
                songKey = "vidalita"
            },
            new SongData
            {
                title = "Minuet",
                artist = "Bach",
                duration = "2:00",
                difficulty = DifficultyLevel.Easy,
                bpm = 140,
                songKey = "minuet"
            },
            new SongData
            {
                title = "Romance",
                artist = "Tarrega",
                duration = "3:00",
                difficulty = DifficultyLevel.Medium,
                bpm = 120,
                songKey = "romance"
            },
            new SongData
            {
                title = "Toccata and Fugue",
                artist = "Bach",
                duration = "5:00",
                difficulty = DifficultyLevel.Expert,
                bpm = 110,
                songKey = "toccata_fugue"
            },
            new SongData
            {
                title = "Moonlight Sonata",
                artist = "Beethoven",
                duration = "4:00",
                difficulty = DifficultyLevel.Easy,
                bpm = 50,
                songKey = "moonlight"
            },
            new SongData
            {
                title = "Fur Elise",
                artist = "Beethoven",
                duration = "3:00",
                difficulty = DifficultyLevel.Medium,
                bpm = 62,
                songKey = "fur_elise"
            },
            new SongData
            {
                title = "Turkish Delight",
                artist = "Mozart",
                duration = "3:20",
                difficulty = DifficultyLevel.Expert,
                bpm = 140,
                songKey = "turkish_delight"
            }
        };
    }

    private void SetupUI()
    {
        // Auto-find UI components if not assigned
        if (songDropdown == null)
            songDropdown = GetComponentInChildren<TMP_Dropdown>();

        if (songDetails == null)
        {
            // SongDetails text component'ini bul
            Transform rightPanel = transform.Find("RightPanel");
            if (rightPanel != null)
                songDetails = rightPanel.Find("SongDetails")?.GetComponent<TextMeshProUGUI>();
        }

        if (playButton == null)
        {
            Transform leftPanel = transform.Find("LeftPanel");
            if (leftPanel != null)
                playButton = leftPanel.GetComponentInChildren<Button>(true);
        }

        if (backButton == null)
        {
            Transform leftPanel = transform.Find("LeftPanel");
            if (leftPanel != null)
            {
                Button[] buttons = leftPanel.GetComponentsInChildren<Button>(true);
                if (buttons.Length > 1)
                    backButton = buttons[1]; // İkinci buton Back button olmalı
            }
        }

        // Populate dropdown with songs
        PopulateDropdown();
    }

    private void PopulateDropdown()
    {
        if (songDropdown == null) return;

        songDropdown.options.Clear();

        foreach (var song in availableSongs)
        {
            string icon = GetSongIcon(song.title);
            songDropdown.options.Add(new TMP_Dropdown.OptionData($"{icon} {song.title}"));
        }

        songDropdown.value = 0;
        songDropdown.RefreshShownValue();
    }

    private string GetSongIcon(string songTitle)
    {
        // Use ASCII-safe music symbols instead of Unicode
        if (songTitle.Contains("Piano") || songTitle.Contains("Sonata") || songTitle.Contains("Minuet")) return "*";
        if (songTitle.Contains("Guitar") || songTitle.Contains("Romance")) return "#";
        if (songTitle.Contains("Harp") || songTitle.Contains("Air")) return "+";
        if (songTitle.Contains("Jazz") || songTitle.Contains("Entertainer")) return "&";
        if (songTitle.Contains("Bach") || songTitle.Contains("Toccata")) return "@";
        if (songTitle.Contains("Beethoven") || songTitle.Contains("Fur")) return "%";
        return "*"; // Default piano symbol
    }

    private void SetupEventListeners()
    {
        // Dropdown change event
        if (songDropdown != null)
            songDropdown.onValueChanged.AddListener(UpdateSongDetails);

        // Button events
        if (playButton != null)
            playButton.onClick.AddListener(PlaySelectedSong);

        if (backButton != null)
            backButton.onClick.AddListener(GoBack);
    }

    private void UpdateSongDetails(int songIndex)
    {
        if (songDetails == null || songIndex < 0 || songIndex >= availableSongs.Length)
            return;

        SongData selectedSong = availableSongs[songIndex];

        // Rich text format ile tek text component'inde tüm bilgileri göster - ASCII safe
        string formattedDetails = $@"* <size=28><color=#FFD700>Song Details</color></size>

<size=20><color=#87CEEB># Title:</color></size>
<color=#FFFFFF>{selectedSong.title}</color>

<size=20><color=#87CEEB>* Artist:</color></size>
<color=#FFFFFF>{selectedSong.artist}</color>

<size=20><color=#87CEEB>+ Duration:</color></size>
<color=#FFFFFF>{selectedSong.duration}</color>

<size=20><color=#87CEEB>@ Difficulty:</color></size>
{GetDifficultyColoredText(selectedSong.difficulty)}

<size=20><color=#87CEEB># BPM:</color></size>
<color=#FFFFFF>{selectedSong.bpm}</color>";

        songDetails.text = formattedDetails;

        // Debug log
        Debug.Log($"♪ Selected Song: {selectedSong.title} - {selectedSong.difficulty}");
    }

    private string GetDifficultyColoredText(DifficultyLevel difficulty)
    {
        string difficultyText = difficulty.ToString().ToUpper();
        string color = difficulty switch
        {
            DifficultyLevel.Easy => "#00FF00",    // Yeşil
            DifficultyLevel.Medium => "#FFFF00",  // Sarı
            DifficultyLevel.Hard => "#FF8000",    // Turuncu
            DifficultyLevel.Expert => "#FF0000",  // Kırmızı
            _ => "#FFFFFF"
        };

        return $"<color={color}>{GetDifficultyIcon(difficulty)} {difficultyText}</color>";
    }

    private string GetDifficultyIcon(DifficultyLevel difficulty)
    {
        // ASCII-safe difficulty icons
        return difficulty switch
        {
            DifficultyLevel.Easy => "*",      // Easy = 1 star
            DifficultyLevel.Medium => "**",   // Medium = 2 stars  
            DifficultyLevel.Hard => "***",   // Hard = 3 stars
            DifficultyLevel.Expert => "****", // Expert = 4 stars
            _ => "-"
        };
    }

    private void PlaySelectedSong()
    {
        int selectedIndex = songDropdown?.value ?? 0;
        if (selectedIndex < 0 || selectedIndex >= availableSongs.Length) return;

        SongData selectedSong = availableSongs[selectedIndex];

        Debug.Log($"► Starting song: {selectedSong.title} ({selectedSong.difficulty})");

        // Convert SongSelectionManager.SongData to GameplayManager's expected format
        var gameplaySongData = new GameplaySongData
        {
            songName = selectedSong.title,
            artist = selectedSong.artist,
            duration = ParseDurationToSeconds(selectedSong.duration),
            bpm = selectedSong.bpm,
            audioFilePath = $"Music/{selectedSong.songKey}",
            chartFilePath = $"Song_Note_Jsons/{selectedSong.songKey}_notes"
        };

        // Start gameplay directly with song data
        GameplayManager gameplayManager = FindFirstObjectByType<GameplayManager>();
        if (gameplayManager != null)
        {
            gameplayManager.StartGameplay(gameplaySongData);
        }
        else
        {
            Debug.LogError("❌ GameplayManager not found!");
            // Fallback - just change state
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangeGameState(GameState.Playing);
            }
        }
    }

    private float ParseDurationToSeconds(string duration)
    {
        // Parse "3:45" format to seconds
        try
        {
            string[] parts = duration.Split(':');
            if (parts.Length == 2)
            {
                int minutes = int.Parse(parts[0]);
                int seconds = int.Parse(parts[1]);
                return minutes * 60 + seconds;
            }
        }
        catch
        {
            Debug.LogWarning($"⚠️ Could not parse duration: {duration}");
        }

        return 180f; // Default 3 minutes
    }

    // Helper class to match GameplayManager's expected SongData format
    [System.Serializable]
    public class GameplaySongData
    {
        public string songName;
        public string artist;
        public float duration;
        public int bpm;
        public string audioFilePath;
        public string chartFilePath;
    }

    private void GoBack()
    {
        Debug.Log("◄ Going back to main menu");

        // Main menu'ye geri dön
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameState.MainMenu);
        }
    }

    // Public method for external access
    public SongData GetSelectedSong()
    {
        int selectedIndex = songDropdown?.value ?? 0;
        if (selectedIndex >= 0 && selectedIndex < availableSongs.Length)
        {
            return availableSongs[selectedIndex];
        }
        return null;
    }

    private void OnDestroy()
    {
        // Event listener'ları temizle
        if (songDropdown != null)
            songDropdown.onValueChanged.RemoveAllListeners();

        if (playButton != null)
            playButton.onClick.RemoveAllListeners();

        if (backButton != null)
            backButton.onClick.RemoveAllListeners();
    }
}