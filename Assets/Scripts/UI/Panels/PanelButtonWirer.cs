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
}
