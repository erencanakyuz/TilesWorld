using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class AllPanelsCreator : EditorWindow
{
    [MenuItem("TilesWorld/Create All UI Panels")]
    public static void CreateAllPanels()
    {
        Debug.Log("🎯 Tüm UI Panel'ları oluşturuluyor...");

        CreateMainMenuPanel();
        CreatePausePanel();
        CreateGameOverPanel();
        CreateSettingsPanel();
        CreateGameplayPanel();

        Debug.Log("✅ Tüm UI Panel'ları başarıyla oluşturuldu!");
    }

    private static void CreateMainMenuPanel()
    {
        string prefabPath = "Assets/Prefabs/UI/panels/MainMenuPanel.prefab";

        GameObject panelRoot = new GameObject("MainMenuPanel");
        SetupBasicPanel(panelRoot, new Color(0.1f, 0.1f, 0.3f, 0.9f));

        // Title
        CreateTitle(panelRoot, "♪ TILES WORLD", new Vector2(0, 150));

        // Buttons
        CreateButton(panelRoot, "StartGameButton", "► START GAME", new Vector2(0, 50), new Vector2(300, 60));
        CreateButton(panelRoot, "SettingsButton", "⚙ SETTINGS", new Vector2(0, -20), new Vector2(300, 60));
        CreateButton(panelRoot, "ExitButton", "✕ EXIT", new Vector2(0, -90), new Vector2(300, 60));

        SavePrefab(panelRoot, prefabPath);
        Debug.Log($"✅ MainMenuPanel created: {prefabPath}");
    }

    private static void CreatePausePanel()
    {
        string prefabPath = "Assets/Prefabs/UI/panels/PausePanel.prefab";

        GameObject panelRoot = new GameObject("PausePanel");
        SetupBasicPanel(panelRoot, new Color(0, 0, 0, 0.8f));

        // Title
        CreateTitle(panelRoot, "⏸ PAUSED", new Vector2(0, 100));

        // Buttons
        CreateButton(panelRoot, "ResumeButton", "► RESUME", new Vector2(0, 20), new Vector2(250, 60));
        CreateButton(panelRoot, "RestartButton", "↻ RESTART", new Vector2(0, -50), new Vector2(250, 60));
        CreateButton(panelRoot, "MainMenuButton", "⌂ MAIN MENU", new Vector2(0, -120), new Vector2(250, 60));

        SavePrefab(panelRoot, prefabPath);
        Debug.Log($"✅ PausePanel created: {prefabPath}");
    }

    private static void CreateGameOverPanel()
    {
        string prefabPath = "Assets/Prefabs/UI/panels/GameOverPanel.prefab";

        GameObject panelRoot = new GameObject("GameOverPanel");
        SetupBasicPanel(panelRoot, new Color(0.2f, 0.05f, 0.05f, 0.9f));

        // Title
        CreateTitle(panelRoot, "♪ GAME OVER", new Vector2(0, 150));

        // Score Display
        CreateText(panelRoot, "FinalScoreText", "Score: 0", new Vector2(0, 80), new Vector2(400, 50), 24);
        CreateText(panelRoot, "FinalComboText", "Best Combo: 0", new Vector2(0, 40), new Vector2(400, 50), 24);
        CreateText(panelRoot, "AccuracyText", "Accuracy: 0%", new Vector2(0, 0), new Vector2(400, 50), 24);

        // Buttons
        CreateButton(panelRoot, "RestartButton", "↻ PLAY AGAIN", new Vector2(0, -60), new Vector2(280, 60));
        CreateButton(panelRoot, "MainMenuButton", "⌂ MAIN MENU", new Vector2(0, -130), new Vector2(280, 60));

        SavePrefab(panelRoot, prefabPath);
        Debug.Log($"✅ GameOverPanel created: {prefabPath}");
    }

    private static void CreateSettingsPanel()
    {
        string prefabPath = "Assets/Prefabs/UI/panels/SettingsPanel.prefab";

        GameObject panelRoot = new GameObject("SettingsPanel");
        SetupBasicPanel(panelRoot, new Color(0.1f, 0.2f, 0.1f, 0.9f));

        // Title
        CreateTitle(panelRoot, "⚙ SETTINGS", new Vector2(0, 180));

        // Volume Section
        CreateText(panelRoot, "VolumeLabel", "♪ Master Volume", new Vector2(0, 120), new Vector2(300, 40), 18);
        CreateSlider(panelRoot, "VolumeSlider", new Vector2(0, 90), new Vector2(300, 30));

        // Difficulty Section
        CreateText(panelRoot, "DifficultyLabel", "♦ Difficulty", new Vector2(0, 40), new Vector2(300, 40), 18);
        CreateDropdown(panelRoot, "DifficultyDropdown", new Vector2(0, 10), new Vector2(300, 30));

        // Instrument Section
        CreateText(panelRoot, "InstrumentLabel", "♫ Instrument", new Vector2(0, -40), new Vector2(300, 40), 18);
        CreateDropdown(panelRoot, "InstrumentDropdown", new Vector2(0, -70), new Vector2(300, 30));

        // Buttons
        CreateButton(panelRoot, "SaveButton", "✓ SAVE", new Vector2(-80, -130), new Vector2(140, 50));
        CreateButton(panelRoot, "CancelButton", "✕ CANCEL", new Vector2(80, -130), new Vector2(140, 50));

        SavePrefab(panelRoot, prefabPath);
        Debug.Log($"✅ SettingsPanel created: {prefabPath}");
    }

    private static void CreateGameplayPanel()
    {
        string prefabPath = "Assets/Prefabs/UI/panels/GameplayPanel.prefab";

        GameObject panelRoot = new GameObject("GameplayPanel");
        SetupBasicPanel(panelRoot, new Color(0, 0, 0, 0.3f)); // Semi-transparent overlay

        // Countdown Text (center screen)
        CreateText(panelRoot, "CountdownText", "3", new Vector2(0, 0), new Vector2(200, 200), 120);

        // Ready Text
        CreateText(panelRoot, "ReadyText", "GET READY!", new Vector2(0, -100), new Vector2(500, 80), 36);

        SavePrefab(panelRoot, prefabPath);
        Debug.Log($"✅ GameplayPanel created: {prefabPath}");
    }

    private static void SetupBasicPanel(GameObject panel, Color backgroundColor)
    {
        // RectTransform
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // Background Image
        Image bg = panel.AddComponent<Image>();
        bg.color = backgroundColor;
    }

    private static void CreateTitle(GameObject parent, string text, Vector2 position)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent.transform, false);

        RectTransform rect = titleObj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(600, 80);

        TextMeshProUGUI textComp = titleObj.AddComponent<TextMeshProUGUI>();
        textComp.text = text;
        textComp.fontSize = 36;
        textComp.fontStyle = FontStyles.Bold;
        textComp.color = Color.white;
        textComp.alignment = TextAlignmentOptions.Center;
    }

    private static void CreateText(GameObject parent, string name, string text, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
        textComp.text = text;
        textComp.fontSize = fontSize;
        textComp.color = Color.white;
        textComp.alignment = TextAlignmentOptions.Center;
    }

    private static void CreateButton(GameObject parent, string name, string text, Vector2 position, Vector2 size)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent.transform, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f);

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        // Button Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
        textComp.text = text;
        textComp.fontSize = 18;
        textComp.fontStyle = FontStyles.Bold;
        textComp.color = Color.white;
        textComp.alignment = TextAlignmentOptions.Center;
    }

    private static void CreateSlider(GameObject parent, string name, Vector2 position, Vector2 size)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent.transform, false);

        RectTransform rect = sliderObj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Slider slider = sliderObj.AddComponent<Slider>();
        slider.value = 1f;

        // Slider needs Background, Fill Area, Handle - simplified version
        Image sliderBg = sliderObj.AddComponent<Image>();
        sliderBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
    }

    private static void CreateDropdown(GameObject parent, string name, Vector2 position, Vector2 size)
    {
        GameObject dropdownObj = new GameObject(name);
        dropdownObj.transform.SetParent(parent.transform, false);

        RectTransform rect = dropdownObj.AddComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image dropdownBg = dropdownObj.AddComponent<Image>();
        dropdownBg.color = Color.white;

        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
        dropdown.options.Clear();
        dropdown.options.Add(new TMP_Dropdown.OptionData("Option 1"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("Option 2"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("Option 3"));
    }

    private static void SavePrefab(GameObject obj, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        DestroyImmediate(obj);
        Selection.activeObject = prefab;
    }
}