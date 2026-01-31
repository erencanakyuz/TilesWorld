using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance { get; private set; }

    private UIConfig config;
    private Canvas hudCanvas;

    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI comboText;
    private TextMeshProUGUI multiplierText;
    private Slider healthBar;
    private Image instrumentIcon;

    private readonly System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder(32);
    private int lastDisplayedScore = -1;

    private int currentScore;
    private int currentCombo;
    private int currentMultiplier = 1;
    private float currentHealth = 1f;

    public void Initialize(UIConfig config, Canvas hudCanvas)
    {
        Instance = this;
        this.config = config;
        this.hudCanvas = hudCanvas;

        FindHUDElements();
        SetupLandscapeHUDLayout();
    }

    public bool FindHUDElements()
    {
        if (hudCanvas == null) return false;

        var allTexts = hudCanvas.GetComponentsInChildren<TextMeshProUGUI>();

        scoreText = System.Array.Find(allTexts, t => t.name.ToLower().Contains("score"));
        comboText = System.Array.Find(allTexts, t => t.name.ToLower().Contains("combo"));
        multiplierText = System.Array.Find(allTexts, t =>
            t.name.ToLower().Contains("multiplier") || t.name.ToLower().Contains("multi"));

        healthBar = hudCanvas.GetComponentInChildren<Slider>();

        var allImages = hudCanvas.GetComponentsInChildren<Image>();
        instrumentIcon = System.Array.Find(allImages, img =>
            img.name.ToLower().Contains("instrument") || img.name.ToLower().Contains("icon"));

        return scoreText != null && comboText != null;
    }

    public void UpdateScore(int score)
    {
        currentScore = Mathf.RoundToInt(score);

        if (scoreText != null)
        {
            if (currentScore != lastDisplayedScore)
            {
                lastDisplayedScore = currentScore;

                stringBuilder.Clear();
                stringBuilder.Append(currentScore.ToString("N0"));
                scoreText.text = stringBuilder.ToString();

                _ = ScaleTextEffectAsync(scoreText.transform);
            }
        }
    }

    public void UpdateCombo(int combo)
    {
        currentCombo = combo;

        if (comboText != null)
        {
            comboText.text = combo > 0 ? $"COMBO x{combo}" : "";

            if (combo > 0 && combo % 10 == 0)
            {
                _ = ComboMilestoneEffectAsync();
            }
        }

        int newMultiplier = Mathf.Clamp(1 + combo / 10, 1, 8);
        if (newMultiplier != currentMultiplier)
        {
            currentMultiplier = newMultiplier;
            UpdateMultiplier();
        }
    }

    public void UpdateHealth(float health)
    {
        currentHealth = Mathf.Clamp01(health);

        if (healthBar != null)
        {
            healthBar.value = currentHealth;

            Image fill = healthBar.fillRect.GetComponent<Image>();
            if (fill != null && config != null)
            {
                if (currentHealth < 0.3f)
                    fill.color = config.dangerColor;
                else if (currentHealth < 0.6f)
                    fill.color = config.warningColor;
                else
                    fill.color = config.successColor;
            }
        }
    }

    public void UpdateInstrumentIcon(InstrumentType instrument)
    {
        if (instrumentIcon != null)
        {
            string iconPath = $"Icons/Instrument_{instrument}";
            Sprite icon = Resources.Load<Sprite>(iconPath);

            if (icon != null)
                instrumentIcon.sprite = icon;
        }
    }

    public void Reset()
    {
        currentScore = 0;
        currentCombo = 0;
        currentMultiplier = 1;
        currentHealth = 1.0f;

        UpdateScore(0);
        UpdateCombo(0);
        UpdateHealth(1.0f);
    }

    private void UpdateMultiplier()
    {
        if (multiplierText != null && config != null)
        {
            multiplierText.text = $"x{currentMultiplier}";

            if (currentMultiplier >= 5)
                multiplierText.color = config.dangerColor;
            else if (currentMultiplier >= 3)
                multiplierText.color = config.warningColor;
            else
                multiplierText.color = config.textPrimaryColor;
        }
    }

    private void SetupLandscapeHUDLayout()
    {
        if (config == null) return;

        if (scoreText != null)
        {
            RectTransform scoreRect = scoreText.GetComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0f, 1f);
            scoreRect.anchorMax = new Vector2(0f, 1f);
            scoreRect.anchoredPosition = config.scorePosition;
            scoreRect.sizeDelta = config.scoreSize;
            scoreText.fontSize = config.scoreFontSize;
            scoreText.alignment = TextAlignmentOptions.Left;
        }

        if (comboText != null)
        {
            RectTransform comboRect = comboText.GetComponent<RectTransform>();
            comboRect.anchorMin = new Vector2(0.5f, 1f);
            comboRect.anchorMax = new Vector2(0.5f, 1f);
            comboRect.anchoredPosition = config.comboPosition;
            comboRect.sizeDelta = config.comboSize;
            comboText.fontSize = config.comboFontSize;
            comboText.alignment = TextAlignmentOptions.Center;
        }

        if (healthBar != null)
        {
            RectTransform healthRect = healthBar.GetComponent<RectTransform>();
            healthRect.anchorMin = new Vector2(1f, 1f);
            healthRect.anchorMax = new Vector2(1f, 1f);
            healthRect.anchoredPosition = config.healthPosition;
            healthRect.sizeDelta = config.healthSize;
        }

        if (multiplierText != null)
        {
            RectTransform multiplierRect = multiplierText.GetComponent<RectTransform>();
            multiplierRect.anchorMin = new Vector2(0f, 1f);
            multiplierRect.anchorMax = new Vector2(0f, 1f);
            multiplierRect.anchoredPosition = config.multiplierPosition;
            multiplierRect.sizeDelta = config.multiplierSize;
            multiplierText.fontSize = config.multiplierFontSize;
            multiplierText.alignment = TextAlignmentOptions.Left;
        }
    }

    private async Awaitable ScaleTextEffectAsync(Transform textTransform)
    {
        Vector3 originalScale = textTransform.localScale;
        Vector3 targetScale = originalScale * 1.2f;

        float elapsed = 0f;
        float duration = 0.1f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            textTransform.localScale = Vector3.Lerp(originalScale, targetScale, elapsed / duration);
            await Awaitable.NextFrameAsync();
        }

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            textTransform.localScale = Vector3.Lerp(targetScale, originalScale, elapsed / duration);
            await Awaitable.NextFrameAsync();
        }

        textTransform.localScale = originalScale;
    }

    private async Awaitable ComboMilestoneEffectAsync()
    {
        if (comboText != null && config != null)
        {
            Color originalColor = comboText.color;

            for (int i = 0; i < 3; i++)
            {
                comboText.color = config.primaryColor;
                await Awaitable.WaitForSecondsAsync(0.1f);
                comboText.color = originalColor;
                await Awaitable.WaitForSecondsAsync(0.1f);
            }
        }
    }
}
