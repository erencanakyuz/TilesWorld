using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Applies theme colors and styles to UI elements at runtime
/// </summary>
public class UIThemeManager : MonoBehaviour
{
    public static UIThemeManager Instance { get; private set; }

    [Header("Theme Configuration")]
    [SerializeField] private UIConfig config;

    [Header("Auto-Apply")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool findAllCanvases = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadConfig();
    }

    void Start()
    {
        if (applyOnStart)
        {
            ApplyThemeToAllUI();
        }
    }

    void LoadConfig()
    {
        if (config == null)
        {
            config = Resources.Load<UIConfig>("UI/UIConfig");
        }

        if (config == null)
        {
            Debug.LogError("[UIThemeManager] UIConfig not found! UI theming disabled.");
        }
    }

    public void ApplyThemeToAllUI()
    {
        if (config == null) return;

        Canvas[] canvases = findAllCanvases ? FindObjectsByType<Canvas>(FindObjectsSortMode.None) : GetComponentsInChildren<Canvas>();

        foreach (Canvas canvas in canvases)
        {
            ApplyThemeToCanvas(canvas);
        }
    }

    public void ApplyThemeToCanvas(Canvas canvas)
    {
        if (canvas == null || config == null) return;

        // Apply to all Images (backgrounds, panels, icons)
        Image[] images = canvas.GetComponentsInChildren<Image>(true);
        foreach (Image img in images)
        {
            ApplyThemeToImage(img);
        }

        // Apply to all TextMeshPro texts
        TextMeshProUGUI[] texts = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            ApplyThemeToText(text);
        }

        // Apply to all Buttons
        Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            ApplyThemeToButton(btn);
        }
    }

    void ApplyThemeToImage(Image img)
    {
        if (img == null) return;

        string objName = img.name.ToLower();

        // Background panels
        if (objName.Contains("background") || objName.Contains("panel"))
        {
            img.color = config.backgroundColor;
        }
        // Accent elements
        else if (objName.Contains("accent") || objName.Contains("highlight"))
        {
            img.color = config.accentColor;
        }
        // Primary elements
        else if (objName.Contains("primary") || objName.Contains("header"))
        {
            img.color = config.primaryColor;
        }
    }

    void ApplyThemeToText(TextMeshProUGUI text)
    {
        if (text == null) return;

        string objName = text.name.ToLower();

        // Title/header text
        if (objName.Contains("title") || objName.Contains("header"))
        {
            text.color = config.primaryColor;
        }
        // Secondary/subtitle text
        else if (objName.Contains("subtitle") || objName.Contains("secondary"))
        {
            text.color = config.textSecondaryColor;
        }
        // Default text
        else
        {
            text.color = config.textPrimaryColor;
        }
    }

    void ApplyThemeToButton(Button btn)
    {
        if (btn == null) return;

        ColorBlock colors = btn.colors;
        colors.normalColor = config.primaryColor;
        colors.highlightedColor = config.accentColor;
        colors.pressedColor = config.accentColor * 0.8f;
        colors.selectedColor = config.primaryColor * 1.2f;
        colors.disabledColor = config.textSecondaryColor;
        btn.colors = colors;

        // Apply to button text
        TextMeshProUGUI btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (btnText != null)
        {
            btnText.color = config.textPrimaryColor;
        }
    }

    public UIConfig GetConfig() => config;

    public void SetConfig(UIConfig newConfig)
    {
        config = newConfig;
        ApplyThemeToAllUI();
    }
}
