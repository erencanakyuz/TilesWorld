using UnityEngine;

[CreateAssetMenu(fileName = "UIConfig", menuName = "TilesWorld/UI Config")]
public class UIConfig : ScriptableObject
{
    [Header("🎨 Color Theme")]
    [Tooltip("Primary brand color - main UI elements")]
    public Color primaryColor = new Color(0.2f, 0.8f, 1.0f); // Cyan blue
    
    [Tooltip("Accent color - highlights, buttons")]
    public Color accentColor = new Color(1.0f, 0.4f, 0.6f); // Pink
    
    [Tooltip("Background color - dark mode")]
    public Color backgroundColor = new Color(0.08f, 0.08f, 0.12f); // Very dark blue
    
    [Tooltip("Text color - primary")]
    public Color textPrimaryColor = new Color(1.0f, 1.0f, 1.0f); // White
    
    [Tooltip("Text color - secondary (dimmed)")]
    public Color textSecondaryColor = new Color(0.7f, 0.7f, 0.8f); // Light gray
    
    [Tooltip("Success color - perfect hits")]
    public Color successColor = new Color(0.3f, 1.0f, 0.4f); // Bright green
    
    [Tooltip("Warning color - good hits")]
    public Color warningColor = new Color(1.0f, 0.8f, 0.2f); // Golden yellow
    
    [Tooltip("Danger color - miss/low health")]
    public Color dangerColor = new Color(1.0f, 0.2f, 0.3f); // Bright red
    
    [Header("🌈 Gradient Settings")]
    public bool useGradients = true;
    public Color gradientStart = new Color(0.15f, 0.15f, 0.25f);
    public Color gradientEnd = new Color(0.05f, 0.05f, 0.15f);

    [Header("Game State Panel Prefabs")]
    public GameObject mainMenuPanelPrefab;
    public GameObject songSelectionPanelPrefab;
    public GameObject gameplayPanelPrefab;
    public GameObject pausePanelPrefab;
    public GameObject gameOverPanelPrefab;
    public GameObject settingsPanelPrefab;

    [Header("Audio Feedback UI")]
    public GameObject perfectHitEffect;
    public GameObject goodHitEffect;
    public GameObject missEffect;

    [Header("Effect Settings")]
    public float effectDuration = 1.0f;
    public AnimationCurve fadeAnimation;

    [Header("HUD Layout")]
    public Vector2 scorePosition = new Vector2(100f, -60f);
    public Vector2 scoreSize = new Vector2(300f, 80f);
    public float scoreFontSize = 36f;

    public Vector2 comboPosition = new Vector2(0f, -60f);
    public Vector2 comboSize = new Vector2(400f, 80f);
    public float comboFontSize = 32f;

    public Vector2 healthPosition = new Vector2(-200f, -60f);
    public Vector2 healthSize = new Vector2(300f, 30f);

    public Vector2 multiplierPosition = new Vector2(100f, -120f);
    public Vector2 multiplierSize = new Vector2(150f, 60f);
    public float multiplierFontSize = 28f;

    [Header("Mobile Layout")]
    public Vector2 pauseButtonPosition = new Vector2(-80f, -80f);
    public Vector2 settingsButtonPosition = new Vector2(-80f, -160f);
    public Vector2 buttonSize = new Vector2(60f, 60f);

    [Header("🎭 Animation Settings")]
    public float buttonScalePunch = 1.15f;
    public float buttonAnimDuration = 0.1f;
    public float uiFadeSpeed = 0.3f;

    [Header("Debug")]
    public bool enableDebugLogging = false;
    public bool enableFallbackUI = true;
}

