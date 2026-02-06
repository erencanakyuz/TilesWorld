using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// NotificationUI - Oyun içi bildirim popup sistemi
/// Başarım, seviye atlama, ödül gibi bildirimleri ekranda gösterir.
/// Kuyruk sistemi ile bildirimleri sırayla gösterir.
/// </summary>
public class NotificationUI : MonoBehaviour
{
    public static NotificationUI Instance { get; private set; }

    [Header("⚙️ Configuration")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float slideInDuration = 0.4f;
    [SerializeField] private float slideOutDuration = 0.3f;
    [SerializeField] private float delayBetweenNotifications = 0.5f;
    [SerializeField] private int maxQueueSize = 10;

    // Notification queue
    private Queue<GamificationNotification> notificationQueue = new Queue<GamificationNotification>();
    private bool isShowingNotification = false;

    // Currently active notification UI
    private GameObject currentNotificationGO;
    private Canvas parentCanvas;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        GamificationManager.OnNotification += EnqueueNotification;
    }

    void OnDisable()
    {
        GamificationManager.OnNotification -= EnqueueNotification;
    }

    void Start()
    {
        // Find a canvas to parent notifications to
        FindCanvas();
    }

    private void FindCanvas()
    {
        // Try to find overlay canvas first, then any canvas
        var canvasLocator = FindFirstObjectByType<CanvasLocator>();
        if (canvasLocator != null)
        {
            parentCanvas = canvasLocator.OverlayCanvas ?? canvasLocator.MainCanvas;
        }

        if (parentCanvas == null)
        {
            parentCanvas = FindFirstObjectByType<Canvas>();
        }
    }

    /// <summary>
    /// Bildirim kuyruğuna ekler
    /// </summary>
    public void EnqueueNotification(GamificationNotification notification)
    {
        if (notification == null) return;
        if (notificationQueue.Count >= maxQueueSize) return;

        notificationQueue.Enqueue(notification);

        if (!isShowingNotification)
        {
            ShowNextNotification();
        }
    }

    private async void ShowNextNotification()
    {
        if (notificationQueue.Count == 0)
        {
            isShowingNotification = false;
            return;
        }

        isShowingNotification = true;
        var notification = notificationQueue.Dequeue();

        // Ensure canvas exists
        if (parentCanvas == null) FindCanvas();
        if (parentCanvas == null)
        {
            isShowingNotification = false;
            return;
        }

        // Create notification UI
        currentNotificationGO = CreateNotificationUI(notification);
        if (currentNotificationGO == null)
        {
            isShowingNotification = false;
            return;
        }

        // Animate in
        RectTransform rect = currentNotificationGO.GetComponent<RectTransform>();
        Vector2 startPos = new Vector2(0, 200); // Start above screen
        Vector2 endPos = new Vector2(0, -100);   // Target position (top area)
        rect.anchoredPosition = startPos;

        rect.DOAnchorPos(endPos, slideInDuration).SetEase(Ease.OutBack).SetUpdate(true);

        // Wait for display duration
        await Awaitable.WaitForSecondsAsync(displayDuration);

        // Animate out
        if (currentNotificationGO != null)
        {
            rect.DOAnchorPos(startPos, slideOutDuration).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() =>
                {
                    if (currentNotificationGO != null)
                    {
                        Destroy(currentNotificationGO);
                        currentNotificationGO = null;
                    }
                });

            await Awaitable.WaitForSecondsAsync(slideOutDuration + delayBetweenNotifications);
        }

        // Show next in queue
        ShowNextNotification();
    }

    private GameObject CreateNotificationUI(GamificationNotification notification)
    {
        // Container
        GameObject container = new GameObject("Notification");
        container.transform.SetParent(parentCanvas.transform, false);

        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 1f);
        containerRect.anchorMax = new Vector2(0.5f, 1f);
        containerRect.pivot = new Vector2(0.5f, 1f);
        containerRect.sizeDelta = new Vector2(500, 100);

        // Background with rounded corners effect
        Image bg = container.AddComponent<Image>();
        Color bgColor = GetNotificationColor(notification.type);
        bg.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0.92f);

        // Add CanvasGroup for potential fade effects
        CanvasGroup cg = container.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(container.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0, 0.5f);
        iconRect.anchorMax = new Vector2(0, 0.5f);
        iconRect.pivot = new Vector2(0, 0.5f);
        iconRect.anchoredPosition = new Vector2(15, 0);
        iconRect.sizeDelta = new Vector2(60, 60);
        TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
        iconText.text = notification.icon;
        iconText.fontSize = 36;
        iconText.alignment = TextAlignmentOptions.Center;

        // Title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(container.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0, 1);
        titleRect.anchoredPosition = new Vector2(80, -8);
        titleRect.sizeDelta = new Vector2(-100, 30);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = notification.title;
        titleText.fontSize = 16;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;

        // Message
        GameObject messageObj = new GameObject("Message");
        messageObj.transform.SetParent(container.transform, false);
        RectTransform messageRect = messageObj.AddComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0, 0.5f);
        messageRect.anchorMax = new Vector2(1, 0.5f);
        messageRect.pivot = new Vector2(0, 0.5f);
        messageRect.anchoredPosition = new Vector2(80, 2);
        messageRect.sizeDelta = new Vector2(-100, 28);
        TextMeshProUGUI messageText = messageObj.AddComponent<TextMeshProUGUI>();
        messageText.text = notification.message;
        messageText.fontSize = 20;
        messageText.fontStyle = FontStyles.Bold;
        messageText.color = Color.white;

        // Sub-message
        if (!string.IsNullOrEmpty(notification.subMessage))
        {
            GameObject subObj = new GameObject("SubMessage");
            subObj.transform.SetParent(container.transform, false);
            RectTransform subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0, 0);
            subRect.anchorMax = new Vector2(1, 0);
            subRect.pivot = new Vector2(0, 0);
            subRect.anchoredPosition = new Vector2(80, 8);
            subRect.sizeDelta = new Vector2(-100, 22);
            TextMeshProUGUI subText = subObj.AddComponent<TextMeshProUGUI>();
            subText.text = notification.subMessage;
            subText.fontSize = 14;
            subText.color = new Color(1, 1, 1, 0.8f);
        }

        return container;
    }

    private Color GetNotificationColor(NotificationType type)
    {
        return type switch
        {
            NotificationType.Achievement => new Color(0.9f, 0.7f, 0.1f),   // Gold
            NotificationType.LevelUp => new Color(0.2f, 0.7f, 1f),         // Blue
            NotificationType.RankUp => new Color(0.8f, 0.3f, 0.9f),        // Purple
            NotificationType.CityCompleted => new Color(0.2f, 0.8f, 0.4f), // Green
            NotificationType.CityUnlocked => new Color(0.3f, 0.9f, 0.5f),  // Light Green
            NotificationType.ArtistDefeated => new Color(0.9f, 0.3f, 0.3f),// Red
            NotificationType.DailyChallenge => new Color(0.3f, 0.8f, 0.9f),// Cyan
            NotificationType.WeeklyBonus => new Color(1f, 0.5f, 0.1f),     // Orange
            _ => new Color(0.3f, 0.3f, 0.4f)                               // Gray
        };
    }

    void OnDestroy()
    {
        if (currentNotificationGO != null)
        {
            Destroy(currentNotificationGO);
        }

        if (Instance == this)
            Instance = null;
    }
}
