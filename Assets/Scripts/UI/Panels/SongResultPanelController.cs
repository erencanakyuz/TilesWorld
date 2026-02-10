using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// SongResultPanelController -- Animated UGUI song result screen with 3-star display.
/// Subscribes to GamificationManager.OnSongResultProcessed and populates the panel
/// with score, accuracy, hit breakdown, XP earned, and animated stars.
/// </summary>
public class SongResultPanelController : MonoBehaviour
{
    // Cached UI elements (found by name)
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI artistLabel;
    private TextMeshProUGUI accuracyLabel;
    private TextMeshProUGUI scoreLabel;
    private TextMeshProUGUI comboLabel;
    private TextMeshProUGUI perfectLabel;
    private TextMeshProUGUI goodLabel;
    private TextMeshProUGUI okayLabel;
    private TextMeshProUGUI missLabel;
    private TextMeshProUGUI xpLabel;
    private TextMeshProUGUI currencyLabel;
    private TextMeshProUGUI levelLabel;
    private TextMeshProUGUI gradeLabel;
    private Image[] starImages = new Image[3];
    private Image[] starGlowImages = new Image[3];
    private CanvasGroup panelCanvasGroup;
    private RectTransform contentRect;

    // State
    private bool hasResult = false;
    private SongResultPackage pendingResult;

    void Awake()
    {
        CacheElements();

        // Start invisible - will animate in
        panelCanvasGroup = GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        panelCanvasGroup.alpha = 0f;
    }

    void OnEnable()
    {
        GamificationManager.OnSongResultProcessed += OnResultReceived;

        // Check if GamificationManager already processed a result before we were created
        if (!hasResult)
        {
            // Try to get the last result from GamificationManager
            var gm = GamificationManager.Instance;
            if (gm != null && gm.LastSongResult != null)
            {
                OnResultReceived(gm.LastSongResult);
            }
        }
    }

    void OnDisable()
    {
        GamificationManager.OnSongResultProcessed -= OnResultReceived;
    }

    private void OnResultReceived(SongResultPackage result)
    {
        if (result == null) return;
        hasResult = true;
        pendingResult = result;

        // Populate data immediately, then animate
        PopulateData(result);
        StartCoroutine(AnimateEntrance(result));
    }

    private void CacheElements()
    {
        titleLabel = FindLabel("result-song-title");
        artistLabel = FindLabel("result-artist");
        accuracyLabel = FindLabel("result-accuracy");
        scoreLabel = FindLabel("result-score");
        comboLabel = FindLabel("result-combo");
        perfectLabel = FindLabel("result-perfect");
        goodLabel = FindLabel("result-good");
        okayLabel = FindLabel("result-okay");
        missLabel = FindLabel("result-miss");
        xpLabel = FindLabel("result-xp");
        currencyLabel = FindLabel("result-currency");
        levelLabel = FindLabel("result-level");
        gradeLabel = FindLabel("result-grade");

        for (int i = 0; i < 3; i++)
        {
            var starGo = FindChild($"Star_{i}");
            if (starGo != null)
            {
                starImages[i] = starGo.GetComponent<Image>();
                var glowGo = starGo.transform.Find("Glow");
                if (glowGo != null)
                    starGlowImages[i] = glowGo.GetComponent<Image>();
            }
        }

        contentRect = FindChild("ResultContent")?.GetComponent<RectTransform>();
    }

    private void PopulateData(SongResultPackage result)
    {
        var stats = result.stats;
        var reward = result.reward;

        if (titleLabel != null)
            titleLabel.text = stats?.songName ?? "Unknown Song";

        if (artistLabel != null)
            artistLabel.text = stats?.artist ?? "";

        float accuracy = stats?.accuracy ?? 0f;
        if (accuracyLabel != null)
        {
            accuracyLabel.text = $"%{accuracy:F1}";
            if (accuracy >= 90f) accuracyLabel.color = new Color(0.3f, 1f, 0.4f);
            else if (accuracy >= 70f) accuracyLabel.color = new Color(0.2f, 0.78f, 1f);
            else if (accuracy >= 50f) accuracyLabel.color = new Color(1f, 0.8f, 0.2f);
            else accuracyLabel.color = new Color(1f, 0.4f, 0.4f);
        }

        if (scoreLabel != null)
            scoreLabel.text = (stats?.totalScore ?? 0).ToString("N0");

        if (comboLabel != null)
            comboLabel.text = (stats?.maxCombo ?? 0).ToString();

        if (perfectLabel != null)
            perfectLabel.text = (stats?.perfectHits ?? 0).ToString();

        if (goodLabel != null)
            goodLabel.text = (stats?.goodHits ?? 0).ToString();

        if (okayLabel != null)
            okayLabel.text = (stats?.okayHits ?? 0).ToString();

        if (missLabel != null)
            missLabel.text = (stats?.missedNotes ?? 0).ToString();

        if (xpLabel != null)
            xpLabel.text = $"+{reward?.totalXP ?? 0} XP";

        if (currencyLabel != null)
            currencyLabel.text = $"+{reward?.totalCurrency ?? 0}";

        if (levelLabel != null)
            levelLabel.text = $"Seviye {result.newLevel}";

        // Grade based on accuracy
        if (gradeLabel != null)
        {
            string grade;
            Color gradeColor;
            if (accuracy >= 95f) { grade = "S"; gradeColor = new Color(1f, 0.85f, 0.1f); }
            else if (accuracy >= 85f) { grade = "A"; gradeColor = new Color(0.3f, 1f, 0.4f); }
            else if (accuracy >= 70f) { grade = "B"; gradeColor = new Color(0.2f, 0.78f, 1f); }
            else if (accuracy >= 50f) { grade = "C"; gradeColor = new Color(1f, 0.8f, 0.2f); }
            else { grade = "D"; gradeColor = new Color(1f, 0.4f, 0.4f); }
            gradeLabel.text = grade;
            gradeLabel.color = gradeColor;
        }

        // Initialize stars as empty (will animate in)
        for (int i = 0; i < 3; i++)
        {
            if (starImages[i] != null)
            {
                starImages[i].color = new Color(0.3f, 0.3f, 0.4f, 0.5f);
                starImages[i].transform.localScale = Vector3.zero;
            }
            if (starGlowImages[i] != null)
            {
                starGlowImages[i].color = new Color(1f, 0.85f, 0.1f, 0f);
            }
        }
    }

    /// <summary>
    /// Convert 5-star system to 3-star:
    /// 5 stars (95%+) = 3 stars, 4 stars (85%+) = 3 stars,
    /// 3 stars (70%+) = 2 stars, 2 stars (50%+) = 1 star,
    /// 0-1 stars = 0 stars
    /// </summary>
    private int ConvertTo3Stars(int fiveStars)
    {
        if (fiveStars >= 4) return 3;
        if (fiveStars >= 3) return 2;
        if (fiveStars >= 2) return 1;
        return 0;
    }

    private IEnumerator AnimateEntrance(SongResultPackage result)
    {
        // Let the panel settle for one frame
        yield return null;

        int starsEarned = ConvertTo3Stars(result.reward?.starsEarned ?? 0);

        // 1. Fade in the panel background
        panelCanvasGroup.alpha = 0f;
        panelCanvasGroup.DOFade(1f, 0.4f).SetEase(Ease.OutQuad).SetUpdate(true);

        // 2. Slide content up
        if (contentRect != null)
        {
            Vector2 startPos = contentRect.anchoredPosition;
            contentRect.anchoredPosition = startPos + new Vector2(0, -60f);
            contentRect.DOAnchorPos(startPos, 0.5f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.5f);

        // 3. Animate grade label
        if (gradeLabel != null)
        {
            gradeLabel.transform.localScale = Vector3.zero;
            gradeLabel.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        yield return new WaitForSecondsRealtime(0.3f);

        // 4. Animate stars one by one with delay
        Color starEarnedColor = new Color(1f, 0.85f, 0.1f, 1f);
        Color starEmptyColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);

        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSecondsRealtime(0.35f);

            bool earned = i < starsEarned;
            Color targetColor = earned ? starEarnedColor : starEmptyColor;
            float targetScale = earned ? 1f : 0.7f;

            if (starImages[i] != null)
            {
                var star = starImages[i];

                // Bounce in from zero
                star.transform.localScale = Vector3.zero;
                star.transform.DOScale(targetScale, 0.5f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);

                // Color transition
                star.DOColor(targetColor, 0.3f).SetDelay(0.1f).SetUpdate(true);

                if (earned)
                {
                    // Overshoot bounce for earned stars
                    star.transform.DOScale(targetScale * 1.3f, 0.15f)
                        .SetDelay(0.5f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true)
                        .OnComplete(() =>
                        {
                            star.transform.DOScale(targetScale, 0.2f)
                                .SetEase(Ease.OutBounce)
                                .SetUpdate(true);
                        });

                    // Glow pulse for earned stars
                    if (starGlowImages[i] != null)
                    {
                        var glow = starGlowImages[i];
                        glow.DOColor(new Color(1f, 0.85f, 0.1f, 0.6f), 0.3f)
                            .SetDelay(0.2f)
                            .SetUpdate(true)
                            .OnComplete(() =>
                            {
                                // Pulse loop
                                glow.DOColor(new Color(1f, 0.85f, 0.1f, 0.15f), 0.8f)
                                    .SetLoops(-1, LoopType.Yoyo)
                                    .SetEase(Ease.InOutSine)
                                    .SetUpdate(true);
                            });
                    }

                    // Rotation kick for flair
                    star.transform.DORotate(new Vector3(0, 0, -10f), 0.1f)
                        .SetDelay(0.15f)
                        .SetEase(Ease.OutQuad)
                        .SetUpdate(true)
                        .OnComplete(() =>
                        {
                            star.transform.DORotate(Vector3.zero, 0.3f)
                                .SetEase(Ease.OutBack)
                                .SetUpdate(true);
                        });
                }
            }
        }

        // 5. Flash the accuracy number
        yield return new WaitForSecondsRealtime(0.3f);
        if (accuracyLabel != null)
        {
            accuracyLabel.transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0), 0.4f, 4, 0.5f)
                .SetUpdate(true);
        }
    }

    // ===== Helpers =====

    private TextMeshProUGUI FindLabel(string goName)
    {
        var t = FindChild(goName);
        return t != null ? t.GetComponent<TextMeshProUGUI>() : null;
    }

    private GameObject FindChild(string goName)
    {
        var all = GetComponentsInChildren<Transform>(true);
        foreach (var t in all)
        {
            if (t.name == goName) return t.gameObject;
        }
        return null;
    }
}
