using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// GamificationUIBootstrap — Creates UIDocument GameObjects for the gamification UI Toolkit screens.
/// Must be called after GamificationManager is initialized.
/// Loads UXML/USS assets from Resources and creates UIDocument components at runtime.
/// </summary>
public static class GamificationUIBootstrap
{
    private const string UXML_PATH_HUD = "UI/Gamification/GamificationHUD";
    private const string UXML_PATH_NOTIFICATION = "UI/Gamification/GamificationNotification";
    private const string UXML_PATH_NOTIFICATION_TEMPLATE = "UI/Gamification/NotificationPopup";
    private const string UXML_PATH_RESULT = "UI/Gamification/SongResult";
    private const string PANEL_SETTINGS_PATH = "UI/Gamification/GamificationPanelSettings";

    /// <summary>
    /// Call from Bootstrap.cs to set up all gamification UI Toolkit documents
    /// </summary>
    public static void Initialize()
    {
        // Load Panel Settings (shared across all gamification UIDocuments)
        var panelSettings = Resources.Load<PanelSettings>(PANEL_SETTINGS_PATH);

        // If no PanelSettings in Resources, create one programmatically
        if (panelSettings == null)
        {
            Debug.LogWarning("[GamificationUIBootstrap] No PanelSettings found at Resources/UI/Gamification/. " +
                "Using first available PanelSettings or creating minimal fallback.");
            panelSettings = FindOrCreatePanelSettings();
        }

        // === 1. HUD ===
        SetupUIDocument(
            "GamificationHUD_UIToolkit",
            UXML_PATH_HUD,
            panelSettings,
            go => go.AddComponent<GamificationHUD_UIToolkit>(),
            sortOrder: 100
        );

        // === 2. Notifications ===
        var notifTemplate = Resources.Load<VisualTreeAsset>(UXML_PATH_NOTIFICATION_TEMPLATE);
        SetupUIDocument(
            "GamificationNotification_UIToolkit",
            UXML_PATH_NOTIFICATION,
            panelSettings,
            go =>
            {
                var notifUI = go.AddComponent<NotificationUI_UIToolkit>();
                // Assign the popup template
                if (notifTemplate != null)
                {
                    // Use serialized field via reflection-free approach: set in Awake of the component
                    // We'll use a static setter instead
                    NotificationUI_UIToolkit_Helper.PendingTemplate = notifTemplate;
                }
            },
            sortOrder: 200
        );

        // === 3. Song Result ===
        SetupUIDocument(
            "SongResult_UIToolkit",
            UXML_PATH_RESULT,
            panelSettings,
            go => go.AddComponent<SongResultUI_UIToolkit>(),
            sortOrder: 300
        );

        Debug.Log("🎨 [GamificationUIBootstrap] All UI Toolkit documents initialized.");
    }

    private static void SetupUIDocument(
        string objectName,
        string uxmlResourcePath,
        PanelSettings panelSettings,
        System.Action<GameObject> addComponents,
        float sortOrder = 0)
    {
        var uxmlAsset = Resources.Load<VisualTreeAsset>(uxmlResourcePath);
        if (uxmlAsset == null)
        {
            Debug.LogWarning($"[GamificationUIBootstrap] Could not load UXML: Resources/{uxmlResourcePath}. " +
                $"Skipping {objectName}.");
            return;
        }

        // Create GameObject but keep it inactive so UIDocument.OnEnable doesn't fire yet
        GameObject go = new GameObject(objectName);
        go.SetActive(false);
        Object.DontDestroyOnLoad(go);

        // Add UIDocument and configure it BEFORE enabling
        var uiDoc = go.AddComponent<UIDocument>();
        uiDoc.panelSettings = panelSettings;
        uiDoc.sortingOrder = sortOrder;
        uiDoc.visualTreeAsset = uxmlAsset;

        // Add controller components (they'll query elements in their OnEnable)
        addComponents?.Invoke(go);

        // NOW activate — UIDocument.OnEnable fires, instantiates UXML,
        // then controller OnEnable fires and can safely query elements
        go.SetActive(true);

        Debug.Log($"  ✅ {objectName} created (sortOrder={sortOrder})");
    }

    private static PanelSettings FindOrCreatePanelSettings()
    {
        // Try to find any existing PanelSettings in the project
        var existing = Resources.FindObjectsOfTypeAll<PanelSettings>();
        if (existing != null && existing.Length > 0)
        {
            return existing[0];
        }

        // Create a minimal one at runtime
        var ps = ScriptableObject.CreateInstance<PanelSettings>();
        ps.name = "GamificationPanelSettings_Runtime";
        ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        ps.referenceResolution = new Vector2Int(1920, 1080);
        ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        ps.match = 0.5f;
        return ps;
    }
}

/// <summary>
/// Helper to pass the notification template to the component after creation.
/// </summary>
public static class NotificationUI_UIToolkit_Helper
{
    public static VisualTreeAsset PendingTemplate;
}
