using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Enhanced button with theme support and animations
/// </summary>
[RequireComponent(typeof(Button))]
public class StyledButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("Style")]
    [SerializeField] private ButtonStyle style = ButtonStyle.Primary;
    [SerializeField] private bool useThemeColors = true;

    [Header("Animation")]
    [SerializeField] private bool enablePunchAnimation = true;
    [SerializeField] private float punchScale = 1.15f;

    private Button button;
    private Image buttonImage;
    private TextMeshProUGUI buttonText;
    private UIConfig config;
    private Vector3 originalScale;

    public enum ButtonStyle
    {
        Primary,
        Accent,
        Success,
        Warning,
        Danger,
        Secondary
    }

    void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        buttonText = GetComponentInChildren<TextMeshProUGUI>();
        originalScale = transform.localScale;

        if (useThemeColors)
        {
            config = Resources.Load<UIConfig>("UI/UIConfig");
            ApplyTheme();
        }
    }

    void OnEnable()
    {
        if (useThemeColors && config != null)
        {
            ApplyTheme();
        }
    }

    void ApplyTheme()
    {
        if (config == null || button == null) return;

        Color baseColor = GetStyleColor();
        
        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = baseColor * 1.2f;
        colors.pressedColor = baseColor * 0.8f;
        colors.selectedColor = baseColor * 1.1f;
        colors.disabledColor = config.textSecondaryColor;
        button.colors = colors;

        if (buttonImage != null)
        {
            buttonImage.color = baseColor;
        }

        if (buttonText != null)
        {
            buttonText.color = config.textPrimaryColor;
        }
    }

    Color GetStyleColor()
    {
        if (config == null) return Color.white;

        return style switch
        {
            ButtonStyle.Primary => config.primaryColor,
            ButtonStyle.Accent => config.accentColor,
            ButtonStyle.Success => config.successColor,
            ButtonStyle.Warning => config.warningColor,
            ButtonStyle.Danger => config.dangerColor,
            ButtonStyle.Secondary => config.textSecondaryColor,
            _ => config.primaryColor
        };
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (enablePunchAnimation && config != null)
        {
            _ = AnimateScaleAsync(originalScale * punchScale, config.buttonAnimDuration);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (enablePunchAnimation && config != null)
        {
            _ = AnimateScaleAsync(originalScale, config.buttonAnimDuration);
        }
    }

    async Awaitable AnimateScaleAsync(Vector3 targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            await Awaitable.NextFrameAsync();
        }

        transform.localScale = targetScale;
    }

    public void SetStyle(ButtonStyle newStyle)
    {
        style = newStyle;
        ApplyTheme();
    }
}
