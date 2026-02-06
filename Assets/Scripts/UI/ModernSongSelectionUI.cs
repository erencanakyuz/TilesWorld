using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Modern Song Selection UI with scrollable card list
/// </summary>
public class ModernSongSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ScrollRect songScrollView;
    [SerializeField] private Transform songListContent;
    [SerializeField] private GameObject songCardPrefab;
    [SerializeField] private Button playButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI headerText;

    [Header("Selected Song Preview")]
    [SerializeField] private TextMeshProUGUI selectedTitleText;
    [SerializeField] private TextMeshProUGUI selectedArtistText;
    [SerializeField] private Image selectedPreviewImage;

    private UIConfig uiConfig;
    private List<SongCard> songCards = new List<SongCard>();
    private SongCard currentlySelected;
    private SongSelectionData[] songs;

    void Start()
    {
        uiConfig = Resources.Load<UIConfig>("UI/UIConfig");
        LoadSongs();
        SetupUI();
        ApplyTheme();
    }

    void LoadSongs()
    {
        // Get songs from SongDatabase
        if (SongDatabase.Instance != null && SongDatabase.Instance.IsLoaded())
        {
            var dbSongs = SongDatabase.Instance.GetAllSongs();
            var songList = new List<SongSelectionData>();

            foreach (var dbSong in dbSongs)
            {
                songList.Add(new SongSelectionData
                {
                    musicId = dbSong.musicId,
                    title = dbSong.title,
                    artist = dbSong.artist,
                    duration = GameConstants.FormatDuration(GameConstants.EstimateDurationSeconds(dbSong.tempo)),
                    difficulty = dbSong.difficulty,
                    bpm = dbSong.tempo,
                    songKey = dbSong.songKey
                });
            }

            songs = songList.ToArray();
        }
        else
        {
            Debug.LogWarning("SongDatabase not loaded yet");
            CreateFallbackSongs();
        }
    }

    void CreateFallbackSongs()
    {
        songs = new SongSelectionData[]
        {
            new SongSelectionData
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

    void SetupUI()
    {
        // Auto-find components if not assigned
        if (songScrollView == null)
            songScrollView = GetComponentInChildren<ScrollRect>();

        if (songListContent == null && songScrollView != null)
             songListContent = songScrollView.content;

        if (playButton == null)
            playButton = transform.Find("PlayButton")?.GetComponent<Button>();

        if (backButton == null)
            backButton = transform.Find("BackButton")?.GetComponent<Button>();

        // Setup buttons
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);

        // Create song cards
        CreateSongCards();
    }

    void CreateSongCards()
    {
        if (songListContent == null || songs == null) return;

        // Clear existing cards
        foreach (Transform child in songListContent)
        {
            Destroy(child.gameObject);
        }
        songCards.Clear();

        // Create card for each song
        foreach (var song in songs)
        {
            GameObject cardObj = CreateSongCardObject();
            cardObj.transform.SetParent(songListContent, false);

            SongCard card = cardObj.GetComponent<SongCard>();
            if (card == null)
                card = cardObj.AddComponent<SongCard>();

            card.Initialize(song, uiConfig, OnSongCardSelected);
            songCards.Add(card);
        }

        // Select first song by default
        if (songCards.Count > 0)
        {
            OnSongCardSelected(songCards[0]);
        }
    }

GameObject CreateSongCardObject()
    {
        if (songCardPrefab != null)
        {
            return Instantiate(songCardPrefab);
        }

        // Create card manually if no prefab
        GameObject card = new GameObject("SongCard");
        RectTransform rect = card.AddComponent<RectTransform>();
        // Make card wider and centered
        rect.sizeDelta = new Vector2(1000, 120);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        // Background
        Image bg = card.AddComponent<Image>();
        bg.color = uiConfig != null ? uiConfig.backgroundColor : new Color(0.1f, 0.1f, 0.15f);

        // Add LayoutElement for ScrollRect
        LayoutElement layout = card.AddComponent<LayoutElement>();
        layout.preferredHeight = 120;
        layout.minHeight = 120;
        layout.flexibleWidth = 1;

        // Add Button
        Button btn = card.AddComponent<Button>();
        btn.targetGraphic = bg;
        
        // Add SongCard component FIRST
        SongCard songCard = card.AddComponent<SongCard>();
        songCard.backgroundImage = bg;
        songCard.selectButton = btn;
        
        // Create child elements
        CreateCardElements(card.transform, songCard);

        return card;
    }

void CreateCardElements(Transform parent, SongCard card)
    {
        // Accent stripe (left side) - taller and more visible
        GameObject stripe = new GameObject("AccentStripe");
        stripe.transform.SetParent(parent, false);
        RectTransform stripeRect = stripe.AddComponent<RectTransform>();
        stripeRect.anchorMin = new Vector2(0, 0);
        stripeRect.anchorMax = new Vector2(0, 1);
        stripeRect.sizeDelta = new Vector2(8, 0);
        stripeRect.anchoredPosition = new Vector2(4, 0);
        Image stripeImg = stripe.AddComponent<Image>();
        stripeImg.color = uiConfig != null ? uiConfig.accentColor : Color.magenta;
        card.accentStripe = stripeImg;

        // Title text - larger and better positioned
        card.titleText = CreateTextElement(parent, "TitleText", new Vector2(24, -25), new Vector2(600, 50), 28,
            uiConfig != null ? uiConfig.textPrimaryColor : Color.white, TextAlignmentOptions.MidlineLeft);

        // Artist text  
        card.artistText = CreateTextElement(parent, "ArtistText", new Vector2(24, -65), new Vector2(500, 35), 20,
            uiConfig != null ? uiConfig.textSecondaryColor : new Color(0.7f, 0.7f, 0.8f), TextAlignmentOptions.MidlineLeft);

        // Duration text - right side
        card.durationText = CreateTextElement(parent, "DurationText", new Vector2(700, -25), new Vector2(120, 35), 18,
            uiConfig != null ? uiConfig.textSecondaryColor : Color.gray, TextAlignmentOptions.MidlineRight);

        // BPM text - right side, below duration
        card.bpmText = CreateTextElement(parent, "BpmText", new Vector2(700, -60), new Vector2(120, 35), 20,
            uiConfig != null ? uiConfig.accentColor : Color.cyan, TextAlignmentOptions.MidlineRight);

        // Difficulty text - far right
        card.difficultyText = CreateTextElement(parent, "DifficultyText", new Vector2(850, -45), new Vector2(140, 50), 22,
            uiConfig != null ? uiConfig.warningColor : Color.yellow, TextAlignmentOptions.Center);
    }

    TextMeshProUGUI CreateTextElement(Transform parent, string name, Vector2 pos, Vector2 size, int fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.text = name; // Placeholder - will be set by SongCard.Initialize
        
        return text;
    }

    void OnSongCardSelected(SongCard card)
    {
        // Deselect previous
        if (currentlySelected != null)
        {
            currentlySelected.SetSelected(false);
        }

        // Select new
        currentlySelected = card;
        card.SetSelected(true);

        // Update preview
        UpdateSelectedSongPreview(card.GetSongData());
    }

    void UpdateSelectedSongPreview(SongSelectionData song)
    {
        if (selectedTitleText != null)
            selectedTitleText.text = song.title;

        if (selectedArtistText != null)
            selectedArtistText.text = song.artist;
    }

    void OnPlayButtonClicked()
    {
        if (currentlySelected == null) return;

        var songData = currentlySelected.GetSongData();
        
        // Create GameplaySongData
        var gameplaySongData = new GameplaySongData
        {
            songName = songData.title,
            artist = songData.artist,
            duration = GameConstants.ParseDuration(songData.duration, 180f),
            bpm = songData.bpm,
            audioFilePath = $"Music/{songData.songKey}",
            chartFilePath = $"Song_Note_Jsons/Individual/{songData.songKey}",
            songKey = songData.songKey,
            difficulty = songData.difficulty
        };

        // Start gameplay
        GameplayManager gameplayManager = FindAnyObjectByType<GameplayManager>();
        if (gameplayManager != null)
        {
            gameplayManager.StartGameplay(gameplaySongData);
        }
    }

    void OnBackButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeGameState(GameState.MainMenu);
        }
    }

    void ApplyTheme()
    {
        if (uiConfig == null) return;

        if (headerText != null)
            headerText.color = uiConfig.primaryColor;
    }
}
