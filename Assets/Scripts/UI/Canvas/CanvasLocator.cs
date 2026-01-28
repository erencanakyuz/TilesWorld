using UnityEngine;
using UnityEngine.UI;

public class CanvasLocator : MonoBehaviour
{
    public static CanvasLocator Instance { get; private set; }

    public Canvas MainCanvas { get; private set; }
    public Canvas HUDCanvas { get; private set; }
    public Canvas OverlayCanvas { get; private set; }

    private UIConfig config;

    public void Initialize(UIConfig config)
    {
        Instance = this;
        this.config = config;
        DiscoverCanvases();
    }

    public bool DiscoverCanvases()
    {
        bool canvasSuccess = FindCanvases();

        if (!canvasSuccess)
        {
            CreateFallbackUI();
            canvasSuccess = FindCanvases();
        }

        if (canvasSuccess)
        {
            ConfigureCanvasScalers();
        }

        return canvasSuccess;
    }

    private bool FindCanvases()
    {
        var allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);

        MainCanvas = System.Array.Find(allCanvases, c =>
            c.name.ToLower().Contains("main") ||
            c.name.ToLower() == "canvas" ||
            c.sortingOrder == 0) ?? (allCanvases.Length > 0 ? allCanvases[0] : null);

        HUDCanvas = System.Array.Find(allCanvases, c =>
            c.name.ToLower().Contains("hud"));

        OverlayCanvas = System.Array.Find(allCanvases, c =>
            c.name.ToLower().Contains("overlay") ||
            c.sortingOrder > 10);

        return MainCanvas != null;
    }

    private void CreateFallbackUI()
    {
        if (config != null && !config.enableFallbackUI) return;

        if (MainCanvas == null)
        {
            GameObject canvasGO = new GameObject("FallbackMainCanvas");
            MainCanvas = canvasGO.AddComponent<Canvas>();
            MainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        if (HUDCanvas == null)
        {
            GameObject hudGO = new GameObject("FallbackHUDCanvas");
            HUDCanvas = hudGO.AddComponent<Canvas>();
            HUDCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            HUDCanvas.sortingOrder = 10;
            hudGO.AddComponent<CanvasScaler>();
            hudGO.AddComponent<GraphicRaycaster>();
        }

        if (OverlayCanvas == null)
        {
            GameObject overlayGO = new GameObject("FallbackOverlayCanvas");
            OverlayCanvas = overlayGO.AddComponent<Canvas>();
            OverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            OverlayCanvas.sortingOrder = 100;
            overlayGO.AddComponent<CanvasScaler>();
            overlayGO.AddComponent<GraphicRaycaster>();
        }
    }

    private void ConfigureCanvasScalers()
    {
        if (MainCanvas != null)
        {
            SetupScaler(MainCanvas.GetComponent<CanvasScaler>());
        }

        if (HUDCanvas != null)
        {
            SetupScaler(HUDCanvas.GetComponent<CanvasScaler>());
        }

        if (OverlayCanvas != null)
        {
            SetupScaler(OverlayCanvas.GetComponent<CanvasScaler>());
        }
    }

    private void SetupScaler(CanvasScaler scaler)
    {
        if (scaler == null) return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }
}
