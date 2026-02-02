using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MobileFinder : MonoBehaviour
{
    public static MobileFinder Instance { get; private set; }

    private UIConfig config;
    private readonly Dictionary<string, Component> cachedUIElements = new Dictionary<string, Component>();

    public Button PauseButton { get; private set; }
    public Button SettingsButton { get; private set; }
    public GameObject MobileControls { get; private set; }

    public void Initialize(UIConfig config)
    {
        Instance = this;
        this.config = config;
    }

    public void DiscoverControls(Canvas[] canvases)
    {
        FindButtons(canvases);
        FindMobileControls(canvases);
    }

    public void SetupLandscapeLayout()
    {
        if (config == null) return;

        if (PauseButton != null)
        {
            RectTransform pauseRect = PauseButton.GetComponent<RectTransform>();
            pauseRect.anchorMin = new Vector2(1f, 1f);
            pauseRect.anchorMax = new Vector2(1f, 1f);
            pauseRect.anchoredPosition = config.pauseButtonPosition;
            pauseRect.sizeDelta = config.buttonSize;
        }

        if (SettingsButton != null)
        {
            RectTransform settingsRect = SettingsButton.GetComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(1f, 1f);
            settingsRect.anchorMax = new Vector2(1f, 1f);
            settingsRect.anchoredPosition = config.settingsButtonPosition;
            settingsRect.sizeDelta = config.buttonSize;
        }
    }

    private void FindButtons(Canvas[] canvases)
    {
        if (cachedUIElements.ContainsKey("buttons_searched"))
        {
            cachedUIElements.TryGetValue("pause_button", out var cachedPause);
            cachedUIElements.TryGetValue("settings_button", out var cachedSettings);
            PauseButton = cachedPause as Button;
            SettingsButton = cachedSettings as Button;
            // If cached references are missing or destroyed, re-scan the scene.
            if (PauseButton != null && SettingsButton != null)
            {
                return;
            }

            cachedUIElements.Remove("buttons_searched");
            cachedUIElements.Remove("pause_button");
            cachedUIElements.Remove("settings_button");
        }

        var allButtons = new List<Button>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null)
            {
                allButtons.AddRange(canvas.GetComponentsInChildren<Button>(true));
            }
        }

        PauseButton = allButtons.Find(b => b.name.ToLower().Contains("pause"));
        SettingsButton = allButtons.Find(b => b.name.ToLower().Contains("settings") || b.name.ToLower().Contains("setting"));

        cachedUIElements["buttons_searched"] = PauseButton;
        if (PauseButton != null) cachedUIElements["pause_button"] = PauseButton;
        if (SettingsButton != null) cachedUIElements["settings_button"] = SettingsButton;
    }

    private void FindMobileControls(Canvas[] canvases)
    {
        MobileControls = null;

        foreach (Canvas canvas in canvases)
        {
            if (canvas == null) continue;

            Transform found = canvas.transform.Find("MobileControls") ??
                             canvas.transform.Find("Mobile Controls") ??
                             canvas.transform.Find("MobileControl");
            if (found != null)
            {
                MobileControls = found.gameObject;
                break;
            }
        }

        if (MobileControls == null)
        {
            foreach (Canvas canvas in canvases)
            {
                if (canvas == null) continue;

                Transform[] canvasChildren = canvas.GetComponentsInChildren<Transform>(true);
                var found = System.Array.Find(canvasChildren, t =>
                    t.name.ToLower().Contains("mobile") && t.name.ToLower().Contains("control"));

                if (found != null)
                {
                    MobileControls = found.gameObject;
                    break;
                }
            }
        }
    }
}
