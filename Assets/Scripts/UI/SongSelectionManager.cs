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
        // Demo song data
        availableSongs = new SongData[]
        {
            new SongData
            {
                title = "Piano Dreams",
                artist = "Classical Composer",
                duration = "3:45",
                difficulty = DifficultyLevel.Easy,
                bpm = 120,
                songKey = "piano_dreams"
            },
            new SongData
            {
                title = "Guitar Hero",
                artist = "Rock Band",
                duration = "4:12",
                difficulty = DifficultyLevel.Medium,
                bpm = 140,
                songKey = "guitar_hero"
            },
            new SongData
            {
                title = "Harp Fantasy",
                artist = "Celtic Musicians",
                duration = "5:30",
                difficulty = DifficultyLevel.Hard,
                bpm = 85,
                songKey = "harp_fantasy"
            },
            new SongData
            {
                title = "Jazz Fusion",
                artist = "Jazz Collective",
                duration = "6:18",
                difficulty = DifficultyLevel.Expert,
                bpm = 180,
                songKey = "jazz_fusion"
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
        if (songTitle.Contains("Piano")) return "♪";
        if (songTitle.Contains("Guitar")) return "♫";
        if (songTitle.Contains("Harp")) return "♬";
        if (songTitle.Contains("Jazz")) return "♩";
        return "♪";
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

        // Rich text format ile tek text component'inde tüm bilgileri göster
        string formattedDetails = $@"♪ <size=28><color=#FFD700>Song Details</color></size>

<size=20><color=#87CEEB>♫ Title:</color></size>
<color=#FFFFFF>{selectedSong.title}</color>

<size=20><color=#87CEEB>♪ Artist:</color></size>
<color=#FFFFFF>{selectedSong.artist}</color>

<size=20><color=#87CEEB>♬ Duration:</color></size>
<color=#FFFFFF>{selectedSong.duration}</color>

<size=20><color=#87CEEB>♦ Difficulty:</color></size>
{GetDifficultyColoredText(selectedSong.difficulty)}

<size=20><color=#87CEEB>♫ BPM:</color></size>
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
        return difficulty switch
        {
            DifficultyLevel.Easy => "●",
            DifficultyLevel.Medium => "●",
            DifficultyLevel.Hard => "●",
            DifficultyLevel.Expert => "●",
            _ => "○"
        };
    }

    private void PlaySelectedSong()
    {
        int selectedIndex = songDropdown?.value ?? 0;
        if (selectedIndex < 0 || selectedIndex >= availableSongs.Length) return;

        SongData selectedSong = availableSongs[selectedIndex];

        Debug.Log($"► Starting song: {selectedSong.title} ({selectedSong.difficulty})");

        // GameManager'a selected song bilgisini gönder ve gameplay'e geç
        if (GameManager.Instance != null)
        {
            // Selected song data'sını GameManager'a set et
            // GameManager.Instance.SetSelectedSong(selectedSong); // Bu method'u eklemen gerekebilir

            // Playing state'ine geç
            GameManager.Instance.ChangeGameState(GameState.Playing);
        }
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