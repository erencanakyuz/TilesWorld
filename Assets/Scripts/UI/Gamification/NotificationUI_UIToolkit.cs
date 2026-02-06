using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

/// <summary>
/// NotificationUI_UIToolkit — Popup notification system using Unity UI Toolkit.
/// Displays achievement unlocks, level ups, city completions, etc.
/// Attach to a GameObject with UIDocument component whose Source Asset = GamificationNotification.uxml.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class NotificationUI_UIToolkit : MonoBehaviour
{
    [Header("Notification Template")]
    [SerializeField] private VisualTreeAsset notificationTemplate;

    [Header("Timing")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float delayBetweenNotifications = 0.5f;
    [SerializeField] private int maxQueueSize = 10;

    private UIDocument uiDocument;
    private VisualElement notificationRoot;

    private Queue<GamificationNotification> notificationQueue = new Queue<GamificationNotification>();
    private bool isShowing = false;

    void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogWarning("[NotificationUI_UIToolkit] rootVisualElement is null. UXML may not be loaded.");
            return;
        }

        // CRITICAL: Let clicks pass through to UGUI canvases underneath
        root.pickingMode = PickingMode.Ignore;

        notificationRoot = root.Q("notification-root");

        // Pick up template from bootstrap helper if not assigned in inspector
        if (notificationTemplate == null && NotificationUI_UIToolkit_Helper.PendingTemplate != null)
        {
            notificationTemplate = NotificationUI_UIToolkit_Helper.PendingTemplate;
            NotificationUI_UIToolkit_Helper.PendingTemplate = null;
        }

        GamificationManager.OnNotification += EnqueueNotification;
    }

    void OnDisable()
    {
        GamificationManager.OnNotification -= EnqueueNotification;
    }

    public void EnqueueNotification(GamificationNotification notification)
    {
        if (notification == null) return;
        if (notificationQueue.Count >= maxQueueSize) return;

        notificationQueue.Enqueue(notification);

        if (!isShowing)
        {
            ShowNext();
        }
    }

    private async void ShowNext()
    {
        if (notificationQueue.Count == 0 || notificationRoot == null)
        {
            isShowing = false;
            return;
        }

        isShowing = true;
        var notification = notificationQueue.Dequeue();

        // Create popup element from template or manually
        VisualElement popup = CreatePopup(notification);
        notificationRoot.Add(popup);

        // Force layout pass, then animate in via USS class toggle
        popup.schedule.Execute(() =>
        {
            popup.AddToClassList("visible");
        }).ExecuteLater(30);

        // Wait for display duration
        await Awaitable.WaitForSecondsAsync(displayDuration);

        // Animate out
        if (popup != null && notificationRoot.Contains(popup))
        {
            popup.RemoveFromClassList("visible");
            popup.AddToClassList("slide-out");

            // Wait for animation, then remove
            await Awaitable.WaitForSecondsAsync(0.5f + delayBetweenNotifications);

            if (popup != null && notificationRoot.Contains(popup))
            {
                notificationRoot.Remove(popup);
            }
        }

        ShowNext();
    }

    private VisualElement CreatePopup(GamificationNotification notification)
    {
        VisualElement popup;

        if (notificationTemplate != null)
        {
            popup = notificationTemplate.Instantiate();
            var inner = popup.Q("notification-popup") ?? popup;
            inner.AddToClassList(GetTypeClass(notification.type));

            var icon = inner.Q<Label>("notif-icon");
            var title = inner.Q<Label>("notif-title");
            var message = inner.Q<Label>("notif-message");
            var sub = inner.Q<Label>("notif-sub");

            if (icon != null) icon.text = notification.icon;
            if (title != null) title.text = notification.title;
            if (message != null) message.text = notification.message;
            if (sub != null)
            {
                sub.text = notification.subMessage ?? "";
                sub.style.display = string.IsNullOrEmpty(notification.subMessage)
                    ? DisplayStyle.None : DisplayStyle.Flex;
            }

            return inner;
        }

        // Fallback: build manually in C#
        popup = new VisualElement();
        popup.AddToClassList("notification-popup");
        popup.AddToClassList(GetTypeClass(notification.type));
        popup.pickingMode = PickingMode.Ignore;

        var iconLabel = new Label(notification.icon);
        iconLabel.AddToClassList("notification-icon");
        popup.Add(iconLabel);

        var textGroup = new VisualElement();
        textGroup.AddToClassList("notification-text-group");

        var titleLabel = new Label(notification.title);
        titleLabel.AddToClassList("notification-title");
        textGroup.Add(titleLabel);

        var msgLabel = new Label(notification.message);
        msgLabel.AddToClassList("notification-message");
        textGroup.Add(msgLabel);

        if (!string.IsNullOrEmpty(notification.subMessage))
        {
            var subLabel = new Label(notification.subMessage);
            subLabel.AddToClassList("notification-sub");
            textGroup.Add(subLabel);
        }

        popup.Add(textGroup);
        return popup;
    }

    private string GetTypeClass(NotificationType type)
    {
        return type switch
        {
            NotificationType.Achievement => "notification-achievement",
            NotificationType.LevelUp => "notification-levelup",
            NotificationType.RankUp => "notification-rankup",
            NotificationType.CityCompleted => "notification-city",
            NotificationType.CityUnlocked => "notification-city",
            NotificationType.ArtistDefeated => "notification-artist",
            NotificationType.DailyChallenge => "notification-daily",
            NotificationType.WeeklyBonus => "notification-weekly",
            _ => "notification-achievement"
        };
    }
}
