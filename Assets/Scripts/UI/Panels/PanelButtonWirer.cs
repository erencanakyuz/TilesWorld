using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class PanelButtonWirer
{
    public static void WirePausePanel(GameObject panel, System.Action onResume, System.Action onRestart, System.Action onMainMenu = null)
    {
        if (panel == null) return;

        var buttons = panel.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            string lowerName = btn.name.ToLower();
            btn.onClick.RemoveAllListeners();

            if (lowerName.Contains("resume") || lowerName.Contains("continue"))
            {
                btn.onClick.AddListener(() => onResume?.Invoke());
                Debug.Log($"[PanelButtonWirer] Resume button wired: {btn.name}");
            }
            else if (lowerName.Contains("restart") || lowerName.Contains("retry"))
            {
                btn.onClick.AddListener(() => onRestart?.Invoke());
                Debug.Log($"[PanelButtonWirer] Restart button wired: {btn.name}");
            }
            else if (lowerName.Contains("menu") || lowerName.Contains("quit") || lowerName.Contains("exit"))
            {
                btn.onClick.AddListener(() => onMainMenu?.Invoke());
                Debug.Log($"[PanelButtonWirer] MainMenu button wired: {btn.name}");
            }
        }
    }

    public static void WireGameOverPanel(GameObject panel, System.Action onRestart, System.Action onMainMenu)
    {
        if (panel == null) return;

        var buttons = panel.GetComponentsInChildren<Button>(true);

        Button restartButton = System.Array.Find(buttons, b => b.name.ToLower().Contains("restart") || b.name.ToLower().Contains("again"));
        Button mainMenuButton = System.Array.Find(buttons, b => b.name.ToLower().Contains("menu"));

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => onRestart?.Invoke());

            var restartText = restartButton.GetComponentInChildren<TextMeshProUGUI>();
            if (restartText != null)
            {
                restartText.text = "Restart ? (R)";
            }
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(() => onMainMenu?.Invoke());
        }
    }


public static void WireMainMenuPanel(GameObject panel, System.Action onStartGame, System.Action onSettings, System.Action onExit)
    {
        if (panel == null) return;

        var buttons = panel.GetComponentsInChildren<Button>(true);

        foreach (var btn in buttons)
        {
            string lowerName = btn.name.ToLower();
            btn.onClick.RemoveAllListeners();

            if (lowerName.Contains("start") || lowerName.Contains("play"))
            {
                btn.onClick.AddListener(() => onStartGame?.Invoke());
                Debug.Log("[PanelButtonWirer] Start Game button wired");
            }
            else if (lowerName.Contains("setting"))
            {
                btn.onClick.AddListener(() => onSettings?.Invoke());
                Debug.Log("[PanelButtonWirer] Settings button wired");
            }
            else if (lowerName.Contains("exit") || lowerName.Contains("quit"))
            {
                btn.onClick.AddListener(() => onExit?.Invoke());
                Debug.Log("[PanelButtonWirer] Exit button wired");
            }
        }
    }

    /// <summary>
    /// Dynamically adds gamification navigation buttons to the MainMenu panel.
    /// These buttons are inserted before the Exit button.
    /// </summary>
    public static void AddGamificationButtons(GameObject panel, System.Action onProfile,
        System.Action onWorldTour, System.Action onArtistBattle, System.Action onDailyChallenge)
    {
        if (panel == null) return;

        // Find the container that holds existing buttons (VerticalLayoutGroup parent)
        Transform container = null;
        var existingButtons = panel.GetComponentsInChildren<Button>(true);
        if (existingButtons.Length > 0)
        {
            container = existingButtons[0].transform.parent;
        }

        if (container == null)
        {
            // Fallback: use panel transform directly
            container = panel.transform;
        }

        // Ensure container has a VerticalLayoutGroup so dynamically added buttons stack correctly
        // (the MainMenuPanel prefab uses manual RectTransform positioning, not a layout group)
        var vlg = container.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = container.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(20, 20, 15, 15);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            // Add LayoutElement to each existing child so they get proper height
            foreach (Transform child in container)
            {
                if (child.GetComponent<LayoutElement>() == null)
                {
                    var le = child.gameObject.AddComponent<LayoutElement>();
                    var rt = child.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        // Use current height as preferred height
                        le.preferredHeight = Mathf.Max(rt.rect.height, 45f);
                    }
                    else
                    {
                        le.preferredHeight = 45f;
                    }
                    le.flexibleWidth = 1f;
                }
            }

            // Add ContentSizeFitter if the container has a ScrollRect parent or needs auto-sizing
            if (container.GetComponent<ContentSizeFitter>() == null)
            {
                var csf = container.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        // Find exit button to insert before it
        Transform exitButton = null;
        foreach (var btn in existingButtons)
        {
            if (btn.name.ToLower().Contains("exit") || btn.name.ToLower().Contains("quit"))
            {
                exitButton = btn.transform;
                break;
            }
        }

        int insertIndex = exitButton != null ? exitButton.GetSiblingIndex() : container.childCount;

        // Create a separator label
        var separator = CreateMenuLabel(container, "-- Oyun Modlari --", 14, new Color(0.5f, 0.5f, 0.65f));
        separator.SetSiblingIndex(insertIndex);
        insertIndex++;

        // Create navigation buttons
        var gamifButtons = new (string name, string text, System.Action action, Color color)[]
        {
            ("DailyChallengeButton", "[!] Gunluk Gorevler", onDailyChallenge, new Color(1f, 0.8f, 0.2f, 1f)),
            ("WorldTourButton", "(*) Dunya Turu", onWorldTour, new Color(0.2f, 0.78f, 1f, 1f)),
            ("ArtistBattleButton", "[x] Besteci Duellosu", onArtistBattle, new Color(1f, 0.4f, 0.6f, 1f)),
            ("ProfileButton", "[o] Profil & Basarimlar", onProfile, new Color(0.7f, 0.5f, 1f, 1f)),
        };

        foreach (var (bName, bText, bAction, bColor) in gamifButtons)
        {
            var btnGo = CreateMenuButton(container, bName, bText, bColor, bAction);
            btnGo.SetSiblingIndex(insertIndex);
            insertIndex++;
            Debug.Log($"[PanelButtonWirer] Gamification button added: {bName}");
        }

        // Another separator before Exit
        var sep2 = CreateMenuLabel(container, "", 4, Color.clear);
        sep2.SetSiblingIndex(insertIndex);
    }

    /// <summary>
    /// Creates a styled button matching the MainMenu panel design.
    /// </summary>
    private static Transform CreateMenuButton(Transform parent, string goName, string text, Color textColor, System.Action onClick)
    {
        // Try to clone an existing button for visual consistency
        var existingBtn = parent.GetComponentInChildren<Button>(true);
        UnityEngine.GameObject btnGo;

        if (existingBtn != null)
        {
            btnGo = Object.Instantiate(existingBtn.gameObject, parent);
            btnGo.name = goName;

            var tmpTexts = btnGo.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmpTexts)
            {
                t.text = text;
                t.color = textColor;
            }

            // Also check for legacy Text
            var legacyTexts = btnGo.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            foreach (var t in legacyTexts)
            {
                t.text = text;
                t.color = textColor;
            }

            var btn = btnGo.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClick?.Invoke());
        }
        else
        {
            // Fallback: create from scratch
            btnGo = new UnityEngine.GameObject(goName, typeof(RectTransform), typeof(CanvasRenderer),
                typeof(UnityEngine.UI.Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);

            btnGo.GetComponent<UnityEngine.UI.Image>().color = new Color(0.15f, 0.15f, 0.25f, 0.8f);

            var textGo = new UnityEngine.GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(btnGo.transform, false);
            var rt = textGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20;
            tmp.color = textColor;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;

            var btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
        }

        return btnGo.transform;
    }

    /// <summary>
    /// Creates a small text label for visual separation in menus.
    /// </summary>
    private static Transform CreateMenuLabel(Transform parent, string text, int fontSize, Color color)
    {
        var go = new UnityEngine.GameObject("Separator", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = fontSize + 8;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Italic;

        return go.transform;
    }
}
