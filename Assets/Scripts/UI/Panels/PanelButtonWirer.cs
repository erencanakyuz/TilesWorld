using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class PanelButtonWirer
{
    public static void WirePausePanel(GameObject panel, System.Action onResume, System.Action onRestart)
    {
        if (panel == null) return;

        var buttons = panel.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            string lowerName = btn.name.ToLower();
            btn.onClick.RemoveAllListeners();

            if (lowerName.Contains("resume"))
            {
                btn.onClick.AddListener(() => onResume?.Invoke());
            }
            else if (lowerName.Contains("restart"))
            {
                btn.onClick.AddListener(() => onRestart?.Invoke());
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
}
