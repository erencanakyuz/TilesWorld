using UnityEngine;
using System.Collections;
using TMPro;

public class CountdownController : MonoBehaviour
{
    public static CountdownController Instance { get; private set; }

    private UIConfig config;
    private Canvas hudCanvas;
    private Canvas mainCanvas;
    private GameObject countdownUI;
    private TextMeshProUGUI countdownText;

    public void Initialize(Canvas hudCanvas, Canvas mainCanvas)
    {
        Instance = this;
        this.hudCanvas = hudCanvas;
        this.mainCanvas = mainCanvas;
        this.config = Resources.Load<UIConfig>("UI/UIConfig");
    }

    public void ShowCountdown(int number)
    {
        CreateCountdownUIIfNeeded();

        if (countdownText != null)
        {
            if (number > 0)
            {
                countdownText.text = number.ToString();
                countdownText.color = config != null ? config.textPrimaryColor : Color.white;
                countdownText.fontSize = 120;
            }
            else
            {
                countdownText.text = "GO!";
                countdownText.color = config != null ? config.successColor : Color.green;
                countdownText.fontSize = 100;
                
                // CRITICAL FIX: Auto-hide after showing GO! for robustness
                _ = AutoHideAfterDelayAsync(1f);
            }

            _ = CountdownPulseEffectAsync();
        }

        if (countdownUI != null)
        {
            countdownUI.SetActive(true);
        }
    }

    public void HideCountdown()
    {
        if (countdownUI != null)
        {
            countdownUI.SetActive(false);
        }
    }

    private async Awaitable AutoHideAfterDelayAsync(float delay)
    {
        await Awaitable.WaitForSecondsAsync(delay);
        HideCountdown();
    }

    private void CreateCountdownUIIfNeeded()
    {
        if (countdownUI != null) return;

        Transform parentCanvas = hudCanvas != null ? hudCanvas.transform : mainCanvas?.transform;
        if (parentCanvas == null) return;

        countdownUI = new GameObject("CountdownUI");
        countdownUI.transform.SetParent(parentCanvas, false);

        RectTransform rectTransform = countdownUI.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        GameObject textObj = new GameObject("CountdownText");
        textObj.transform.SetParent(countdownUI.transform, false);

        countdownText = textObj.AddComponent<TextMeshProUGUI>();
        countdownText.text = "3";
        countdownText.fontSize = 120;
        countdownText.color = config != null ? config.textPrimaryColor : Color.white;
        countdownText.alignment = TextAlignmentOptions.Center;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null)
        {
            countdownText.font = font;
        }

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        countdownUI.SetActive(false);
    }

    private async Awaitable CountdownPulseEffectAsync()
    {
        if (countdownText == null) return;

        Vector3 originalScale = countdownText.transform.localScale;
        Vector3 largeScale = originalScale * 1.3f;

        float elapsed = 0f;
        float duration = 0.1f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            countdownText.transform.localScale = Vector3.Lerp(originalScale, largeScale, progress);
            await Awaitable.NextFrameAsync();
        }

        elapsed = 0f;
        duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;
            countdownText.transform.localScale = Vector3.Lerp(largeScale, originalScale, progress);
            await Awaitable.NextFrameAsync();
        }

        countdownText.transform.localScale = originalScale;
    }
}
