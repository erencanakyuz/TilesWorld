using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Creates smooth gradient backgrounds for panels
/// </summary>
[RequireComponent(typeof(Image))]
public class GradientBackground : MonoBehaviour
{
    [Header("Gradient Settings")]
    [SerializeField] private bool useThemeGradient = true;
    [SerializeField] private Color topColor = new Color(0.15f, 0.15f, 0.25f);
    [SerializeField] private Color bottomColor = new Color(0.05f, 0.05f, 0.15f);
    [SerializeField] private float angle = 0f; // 0 = vertical, 90 = horizontal

    private Image image;
    private Material gradientMaterial;

    void Awake()
    {
        image = GetComponent<Image>();
        
        if (useThemeGradient)
        {
            LoadThemeColors();
        }
        
        CreateGradient();
    }

    void LoadThemeColors()
    {
        UIConfig config = Resources.Load<UIConfig>("UI/UIConfig");
        if (config != null && config.useGradients)
        {
            topColor = config.gradientStart;
            bottomColor = config.gradientEnd;
        }
    }

    void CreateGradient()
    {
        if (image == null) return;

        // Create gradient texture
        Texture2D gradientTexture = new Texture2D(1, 256);
        gradientTexture.wrapMode = TextureWrapMode.Clamp;
        gradientTexture.filterMode = FilterMode.Bilinear;

        for (int i = 0; i < 256; i++)
        {
            float t = i / 255f;
            Color color = Color.Lerp(bottomColor, topColor, t);
            gradientTexture.SetPixel(0, i, color);
        }

        gradientTexture.Apply();

        // Apply to image
        image.sprite = Sprite.Create(
            gradientTexture,
            new Rect(0, 0, gradientTexture.width, gradientTexture.height),
            new Vector2(0.5f, 0.5f)
        );
        
        // Rotate if needed
        if (angle != 0)
        {
            transform.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    public void SetColors(Color top, Color bottom)
    {
        topColor = top;
        bottomColor = bottom;
        CreateGradient();
    }

    public void SetAngle(float newAngle)
    {
        angle = newAngle;
        transform.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
