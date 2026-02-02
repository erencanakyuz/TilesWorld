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
    private TextMeshProUGUI readyText;
    private bool usingExistingTexts = false;

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

                if (readyText != null)
                {
                    readyText.text = "Get Ready";
                    readyText.gameObject.SetActive(true);
                }
            }
            else
            {
                countdownText.text = "GO!";
                countdownText.color = config != null ? config.successColor : Color.green;
                countdownText.fontSize = 100;

                if (readyText != null)
                {
                    readyText.gameObject.SetActive(false);
                }
                
                // CRITICAL FIX: Auto-hide after showing GO! for robustness
                _ = AutoHideAfterDelayAsync(1f);
            }

            _ = CountdownPulseEffectAsync();
        }

        if (usingExistingTexts)
        {
            countdownText.gameObject.SetActive(true);
        }
        else if (countdownUI != null)
        {
            countdownUI.SetActive(true);
        }
    }

    public void HideCountdown()
    {
        if (usingExistingTexts)
        {
            if (countdownText != null) countdownText.gameObject.SetActive(false);
            if (readyText != null) readyText.gameObject.SetActive(false);
            return;
        }

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

        if (TryBindExistingCountdownTexts())
        {
            usingExistingTexts = true;
            if (countdownText != null) countdownText.gameObject.SetActive(false);
            if (readyText != null) readyText.gameObject.SetActive(false);
            return;
        }

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

    private bool TryBindExistingCountdownTexts()
    {
        TextMeshProUGUI foundCountdown = null;
        TextMeshProUGUI foundReady = null;

        Canvas[] canvases = new[] { hudCanvas, mainCanvas };
        foreach (var canvas in canvases)
        {
            if (canvas == null) continue;
            var texts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in texts)
            {
                if (text == null) continue;
                if (foundCountdown == null && text.name == "CountdownText")
                {
                    foundCountdown = text;
                }
                else if (foundReady == null && text.name == "ReadyText")
                {
                    foundReady = text;
                }

                if (foundCountdown != null && foundReady != null)
                {
                    break;
                }
            }

            if (foundCountdown != null && foundReady != null)
            {
                break;
            }
        }

        if (foundCountdown != null)
        {
            countdownText = foundCountdown;
            readyText = foundReady;
            return true;
        }

        return false;
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
