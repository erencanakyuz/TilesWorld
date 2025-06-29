using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

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
        public int musicId;
        public string title;
        public string artist;
        public string duration;
        public DifficultyLevel difficulty;
        public int bpm;
        public string songKey; // For loading the actual song
    }

    // DifficultyLevel enum moved to DataStructures.cs to avoid duplicates

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
        // Load real songs from CSV database
        LoadSongsFromDatabase();
    }

    private void LoadSongsFromDatabase()
    {
        try
        {
            // Load MUSIC.csv from Resources
            TextAsset musicCsv = Resources.Load<TextAsset>("Database csv/MUSIC");
            if (musicCsv == null)
            {
                Debug.LogError("❌ MUSIC.csv not found in Resources/Database csv/");
                CreateFallbackSongs();
                return;
            }

            var songList = new List<SongData>();
            string[] lines = musicCsv.text.Split('\n');

            // Skip header line (line 0)
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] values = ParseCSVLine(line);
                if (values.Length >= 4)
                {
                    try
                    {
                        int musicId = int.Parse(values[0].Trim('"'));
                        string title = values[2].Trim('"');
                        string artist = values[3].Trim('"');
                        int bpm = int.Parse(values[4].Trim('"'));

                        // Determine difficulty from stars in title
                        DifficultyLevel difficulty = GetDifficultyFromTitle(title);

                        // Clean title (remove stars)
                        string cleanTitle = title.Replace("★", "").Replace("☆", "").Trim();

                        var songData = new SongData
                        {
                            musicId = musicId,
                            title = cleanTitle,
                            artist = artist,
                            duration = CalculateDuration(bpm), // Estimate based on BPM
                            difficulty = difficulty,
                            bpm = bpm,
                            songKey = GetSongKeyFromTitle(cleanTitle, musicId) // Proper JSON mapping
                        };

                        songList.Add(songData);
                        Debug.Log($"🎵 Loaded: {cleanTitle} by {artist} (BPM: {bpm}, Difficulty: {difficulty})");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"⚠️ Failed to parse song line {i}: {e.Message}");
                    }
                }
            }

            availableSongs = songList.ToArray();
            Debug.Log($"🎼 Successfully loaded {availableSongs.Length} songs from database");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error loading songs from database: {e.Message}");
            CreateFallbackSongs();
        }
    }

    private string[] ParseCSVLine(string line)
    {
        // Simple CSV parser that handles quoted fields
        var result = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField);
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        result.Add(currentField); // Add last field
        return result.ToArray();
    }

    private DifficultyLevel GetDifficultyFromTitle(string title)
    {
        int starCount = title.Count(c => c == '★');
        return starCount switch
        {
            0 => DifficultyLevel.Easy,
            1 => DifficultyLevel.Easy,
            2 => DifficultyLevel.Medium,
            3 => DifficultyLevel.Hard,
            4 => DifficultyLevel.Expert,
            >= 5 => DifficultyLevel.Master,
            _ => DifficultyLevel.Easy
        };
    }

    private string CalculateDuration(int bpm)
    {
        // Estimate song duration based on BPM (rough calculation)
        int estimatedSeconds = Mathf.Clamp(300 - bpm, 60, 240); // 1-4 minutes
        int minutes = estimatedSeconds / 60;
        int seconds = estimatedSeconds % 60;
        return $"{minutes}:{seconds:D2}";
    }

    private void CreateFallbackSongs()
    {
        // Fallback songs if CSV loading fails
        availableSongs = new SongData[]
        {
            new SongData
            {
                musicId = 1,
                title = "Cannon",
                artist = "Pachelbel",
                duration = "3:00",
                difficulty = DifficultyLevel.Easy,
                bpm = 77,
                songKey = "music_1"
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
            DifficultyLevel.Master => "#FF00FF",  // Mor
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
            DifficultyLevel.Master => "*****", // Master = 5 stars
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
            chartFilePath = $"Song_Note_Jsons/Individual/{selectedSong.songKey}", // NEW: Points to Individual folder
            songKey = selectedSong.songKey
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
        public string songKey; // CRITICAL: For JSON loading!
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

    private string GetSongKeyFromTitle(string title, int musicId)
    {
        // NEW SYSTEM: Map to individual JSON filenames (without .json extension)
        // These match the filenames created by SongJSONSplitter

        string titleLower = title.ToLower().Trim();

        // Map each song to its individual JSON filename (generated by splitter)
        switch (titleLower)
        {
            case "cannon": return "cannon_pachelbel";
            case "the entertainer": return "the_entertainer_scott_joplin";
            case "air on a g string": return "air_on_a_g_string_bach";
            case "vidalita": return "vidalita_traditional";
            case "minuet": return "minuet_bach";
            case "romance": return "romance_tarrega";
            case "toccata and fugue": return "toccata_and_fugue_bach";
            case "moon light": return "moon_light_beethoven";
            case "noel": return "noel_bach";
            case "turkish delight": return "turkish_delight_mozart";
            case "fur elise": return "fur_elise_beethoven";
            case "ciacona": return "ciacona_s_l_weiss";
            case "hungarian danse no. 5": return "hungarian_danse_no_5_brahms";
            case "cathedral": return "cathedral_agustin_barrios";
            case "greensleeves": return "greensleeves_anonymous";
            case "asturias": return "asturias_isaac_albeniz";
            case "el noi de la mare": return "el_noi_de_la_mare_miguel_llobet";
            case "sinfonia 40": return "sinfonia_40_mozart";
            case "gymnopedie no. 1": return "gymnopedie_no_1_erik_satie";
            case "the bees": return "the_bees_agustin_barrios";
            case "salut d`amour": return "salut_d`amour_elgar";
            case "moonlight sonata op. 27, no. 2.": return "moonlight_sonata_op_27_no_2_beethoven";

            // Fallback to generated format if not found
            default:
                Debug.LogWarning($"⚠️ Unknown song title: '{title}', generating filename");
                return GenerateFilenameFromTitle(title, musicId);
        }
    }

    /// <summary>
    /// Generate filename from title as fallback (matches SongJSONSplitter logic)
    /// </summary>
    private string GenerateFilenameFromTitle(string title, int musicId)
    {
        // Clean title similar to SongJSONSplitter
        string cleaned = title.ToLower()
            .Replace(" ", "_")
            .Replace("★", "")
            .Replace("☆", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace("'", "")
            .Replace("`", "")
            .Replace("ü", "u")
            .Replace("é", "e")
            .Replace("ñ", "n");

        // Default artist based on music_id (fallback)
        string artist = musicId switch
        {
            1 => "pachelbel",
            2 => "scott_joplin",
            3 => "bach",
            8 => "beethoven",
            10 => "mozart",
            11 => "beethoven",
            _ => "unknown"
        };

        return $"{cleaned}_{artist}";
    }
}