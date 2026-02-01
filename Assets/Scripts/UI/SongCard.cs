using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Modern song card component for scrollable song list
/// </summary>
public class SongCard : MonoBehaviour
{
    [Header("UI References")]
    public Image backgroundImage;
    public Image accentStripe;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI artistText;
    public TextMeshProUGUI durationText;
    public TextMeshProUGUI bpmText;
    public TextMeshProUGUI difficultyText;
    public Button selectButton;

    [Header("Data")]
    private SongSelectionData songData;
    private UIConfig uiConfig;
    private bool isSelected = false;

    public void Initialize(SongSelectionData data, UIConfig config, System.Action<SongCard> onSelect)
    {
        songData = data;
        uiConfig = config;

        // Set text content
        if (titleText != null) titleText.text = data.title;
        if (artistText != null) artistText.text = data.artist;
        if (durationText != null) durationText.text = data.duration;
        if (bpmText != null) bpmText.text = $"{data.bpm} BPM";
        if (difficultyText != null) difficultyText.text = GetDifficultyText(data.difficulty);

        // Apply theme colors
        ApplyTheme();

        // Setup button
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelect?.Invoke(this));
        }
    }

    void ApplyTheme()
    {
        if (uiConfig == null) return;

        if (backgroundImage != null)
            backgroundImage.color = uiConfig.backgroundColor;

        if (accentStripe != null)
            accentStripe.color = GetDifficultyColor(songData.difficulty);

        if (titleText != null)
            titleText.color = uiConfig.textPrimaryColor;

        if (artistText != null)
            artistText.color = uiConfig.textSecondaryColor;

        if (durationText != null)
            durationText.color = uiConfig.textSecondaryColor;

        if (bpmText != null)
            bpmText.color = uiConfig.accentColor;

        if (difficultyText != null)
            difficultyText.color = GetDifficultyColor(songData.difficulty);
    }

    string GetDifficultyText(DifficultyLevel difficulty)
    {
        // Using ASCII-compatible characters instead of Unicode stars
        string stars = difficulty switch
        {
            DifficultyLevel.Easy => "*",
            DifficultyLevel.Medium => "**",
            DifficultyLevel.Hard => "***",
            DifficultyLevel.Expert => "****",
            DifficultyLevel.Master => "*****",
            _ => "*"
        };

        return $"{stars} {difficulty.ToString().ToUpper()}";
    }

    Color GetDifficultyColor(DifficultyLevel difficulty)
    {
        if (uiConfig == null) return Color.white;

        return difficulty switch
        {
            DifficultyLevel.Easy => uiConfig.successColor,
            DifficultyLevel.Medium => uiConfig.warningColor,
            DifficultyLevel.Hard => new Color(1f, 0.5f, 0f), // Orange
            DifficultyLevel.Expert => uiConfig.dangerColor,
            DifficultyLevel.Master => new Color(1f, 0f, 1f), // Magenta
            _ => uiConfig.textPrimaryColor
        };
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (backgroundImage != null)
        {
            backgroundImage.color = selected ?
                (uiConfig != null ? uiConfig.primaryColor * 0.3f : new Color(0.2f, 0.2f, 0.3f)) :
                (uiConfig != null ? uiConfig.backgroundColor : new Color(0.1f, 0.1f, 0.15f));
        }

        if (accentStripe != null)
        {
            accentStripe.color = selected ?
                (uiConfig != null ? uiConfig.accentColor : Color.magenta) :
                GetDifficultyColor(songData.difficulty);
        }
    }

    public SongSelectionData GetSongData() => songData;
}
