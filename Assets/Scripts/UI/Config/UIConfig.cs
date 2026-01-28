using UnityEngine;

[CreateAssetMenu(fileName = "UIConfig", menuName = "TilesWorld/UI Config")]
public class UIConfig : ScriptableObject
{
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

    [Header("Debug")]
    public bool enableDebugLogging = false;
    public bool enableFallbackUI = true;
}
