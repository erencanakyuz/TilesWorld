using UnityEngine;
using UnityEditor;
using System.IO;

public class UIConfigSetupWizard : EditorWindow
{
    // References from old UIManager (will be assigned in Inspector)
    public GameObject mainMenuPanelPrefab;
    public GameObject songSelectionPanelPrefab;
    public GameObject gameplayPanelPrefab;
    public GameObject pausePanelPrefab;
    public GameObject gameOverPanelPrefab;
    public GameObject settingsPanelPrefab;

    public GameObject perfectHitEffect;
    public GameObject goodHitEffect;
    public GameObject missEffect;

    public float effectDuration = 1.0f;
    public AnimationCurve fadeAnimation = AnimationCurve.EaseInOut(0, 1, 1, 0);

    private SerializedObject serializedObject;

    [MenuItem("Tools/TilesWorld/Setup UIConfig Asset")]
    public static void ShowWindow()
    {
        var window = GetWindow<UIConfigSetupWizard>("UIConfig Setup");
        window.minSize = new Vector2(400, 600);
    }

    void OnEnable()
    {
        serializedObject = new SerializedObject(this);
    }

    void OnGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("UIConfig Asset Setup Wizard", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Drag and drop the prefabs from your Project window here.\n\n" +
            "These are the same prefabs that were assigned to the old UIManager in Bootstrap scene.",
            MessageType.Info);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Panel Prefabs", EditorStyles.boldLabel);
        mainMenuPanelPrefab = (GameObject)EditorGUILayout.ObjectField("Main Menu Panel", mainMenuPanelPrefab, typeof(GameObject), false);
        songSelectionPanelPrefab = (GameObject)EditorGUILayout.ObjectField("Song Selection Panel", songSelectionPanelPrefab, typeof(GameObject), false);
        gameplayPanelPrefab = (GameObject)EditorGUILayout.ObjectField("Gameplay Panel", gameplayPanelPrefab, typeof(GameObject), false);
        pausePanelPrefab = (GameObject)EditorGUILayout.ObjectField("Pause Panel", pausePanelPrefab, typeof(GameObject), false);
        gameOverPanelPrefab = (GameObject)EditorGUILayout.ObjectField("Game Over Panel", gameOverPanelPrefab, typeof(GameObject), false);
        settingsPanelPrefab = (GameObject)EditorGUILayout.ObjectField("Settings Panel", settingsPanelPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Effect Prefabs", EditorStyles.boldLabel);
        perfectHitEffect = (GameObject)EditorGUILayout.ObjectField("Perfect Hit Effect", perfectHitEffect, typeof(GameObject), false);
        goodHitEffect = (GameObject)EditorGUILayout.ObjectField("Good Hit Effect", goodHitEffect, typeof(GameObject), false);
        missEffect = (GameObject)EditorGUILayout.ObjectField("Miss Effect", missEffect, typeof(GameObject), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Effect Settings", EditorStyles.boldLabel);
        effectDuration = EditorGUILayout.FloatField("Effect Duration", effectDuration);
        fadeAnimation = EditorGUILayout.CurveField("Fade Animation", fadeAnimation);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("Auto Fill From Bootstrap Scene", GUILayout.Height(30)))
        {
            UIConfig tempConfig = ScriptableObject.CreateInstance<UIConfig>();
            if (AutoCreateUIConfigFromScene.TryCopyFromBootstrapScene(tempConfig))
            {
                mainMenuPanelPrefab = tempConfig.mainMenuPanelPrefab;
                songSelectionPanelPrefab = tempConfig.songSelectionPanelPrefab;
                gameplayPanelPrefab = tempConfig.gameplayPanelPrefab;
                pausePanelPrefab = tempConfig.pausePanelPrefab;
                gameOverPanelPrefab = tempConfig.gameOverPanelPrefab;
                settingsPanelPrefab = tempConfig.settingsPanelPrefab;
                perfectHitEffect = tempConfig.perfectHitEffect;
                goodHitEffect = tempConfig.goodHitEffect;
                missEffect = tempConfig.missEffect;
                effectDuration = tempConfig.effectDuration;
                fadeAnimation = tempConfig.fadeAnimation;
            }
            DestroyImmediate(tempConfig);
        }

        EditorGUILayout.Space();

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Create UIConfig Asset", GUILayout.Height(40)))
        {
            CreateUIConfigAsset();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Asset will be created at:\nAssets/Resources/UI/UIConfig.asset",
            MessageType.None);

        serializedObject.ApplyModifiedProperties();
    }

    void CreateUIConfigAsset()
    {
        // Ensure Resources/UI folder exists
        string folderPath = "Assets/Resources/UI";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "UI");
        }

        // Create the ScriptableObject
        UIConfig config = ScriptableObject.CreateInstance<UIConfig>();

        // Assign values
        config.mainMenuPanelPrefab = mainMenuPanelPrefab;
        config.songSelectionPanelPrefab = songSelectionPanelPrefab;
        config.gameplayPanelPrefab = gameplayPanelPrefab;
        config.pausePanelPrefab = pausePanelPrefab;
        config.gameOverPanelPrefab = gameOverPanelPrefab;
        config.settingsPanelPrefab = settingsPanelPrefab;

        config.perfectHitEffect = perfectHitEffect;
        config.goodHitEffect = goodHitEffect;
        config.missEffect = missEffect;

        config.effectDuration = effectDuration;
        config.fadeAnimation = fadeAnimation;

        // Save the asset
        string assetPath = folderPath + "/UIConfig.asset";
        AssetDatabase.CreateAsset(config, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = config;

        Debug.Log($"✅ UIConfig asset created at: {assetPath}");
        EditorUtility.DisplayDialog("Success!", $"UIConfig asset created at:\n{assetPath}", "OK");
    }
}
