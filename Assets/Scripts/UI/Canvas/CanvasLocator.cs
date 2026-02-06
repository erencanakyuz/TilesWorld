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
        // Destroy old fallback canvases before re-discovering
        // so they don't shadow real scene canvases
        DestroyFallbackCanvases();

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

    private void DestroyFallbackCanvases()
    {
        // Only destroy canvases that WE created as fallbacks
        if (MainCanvas != null && MainCanvas.name == "FallbackMainCanvas")
        {
            Destroy(MainCanvas.gameObject);
            MainCanvas = null;
        }
        if (HUDCanvas != null && HUDCanvas.name == "FallbackHUDCanvas")
        {
            Destroy(HUDCanvas.gameObject);
            HUDCanvas = null;
        }
        if (OverlayCanvas != null && OverlayCanvas.name == "FallbackOverlayCanvas")
        {
            Destroy(OverlayCanvas.gameObject);
            OverlayCanvas = null;
        }
    }

    private bool FindCanvases()
    {
        // CRITICAL: Include inactive objects - HUDCanvas starts as inactive!
        var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        Debug.Log($"[CanvasLocator] Found {allCanvases.Length} canvases in scene");

        // Filter out our own fallback canvases — prefer real scene canvases
        bool IsFallback(Canvas c) => c.name.StartsWith("Fallback");

        // Try to find real (non-fallback) canvases first
        MainCanvas = System.Array.Find(allCanvases, c =>
            !IsFallback(c) && (
            c.name.ToLower().Contains("main") ||
            c.name.ToLower() == "canvas" ||
            c.sortingOrder == 0));
        // Fallback: any non-fallback canvas, then any canvas at all
        if (MainCanvas == null)
            MainCanvas = System.Array.Find(allCanvases, c => !IsFallback(c))
                      ?? (allCanvases.Length > 0 ? allCanvases[0] : null);

        HUDCanvas = System.Array.Find(allCanvases, c =>
            !IsFallback(c) && c.name.ToLower().Contains("hud"))
            ?? System.Array.Find(allCanvases, c => c.name.ToLower().Contains("hud"));

        OverlayCanvas = System.Array.Find(allCanvases, c =>
            !IsFallback(c) && (c.name.ToLower().Contains("overlay") || c.sortingOrder > 10))
            ?? System.Array.Find(allCanvases, c =>
                c.name.ToLower().Contains("overlay") || c.sortingOrder > 10);

        Debug.Log($"[CanvasLocator] Main={MainCanvas?.name ?? "NULL"}, HUD={HUDCanvas?.name ?? "NULL"}, Overlay={OverlayCanvas?.name ?? "NULL"}");

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
