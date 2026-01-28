using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Automatically creates UIConfig.asset from the existing UIManager in Bootstrap scene.
/// Run this once via Tools > TilesWorld > Auto Create UIConfig From Scene
/// </summary>
public class AutoCreateUIConfigFromScene : Editor
{
    [MenuItem("Tools/TilesWorld/Auto Create UIConfig From Bootstrap Scene")]
    public static void CreateUIConfigFromBootstrapScene()
    {
        string bootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        if (!File.Exists(bootstrapScenePath))
        {
            Debug.LogError($"Bootstrap scene not found at {bootstrapScenePath}");
            return;
        }
        
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

        UIConfig config = ScriptableObject.CreateInstance<UIConfig>();
        if (!TryCopyFromBootstrapScene(config))
        {
            Debug.LogError("Failed to populate UIConfig from Bootstrap scene.");
            return;
        }

        // Save the asset
        string assetPath = folderPath + "/UIConfig.asset";
        
        // Check if already exists
        UIConfig existingConfig = AssetDatabase.LoadAssetAtPath<UIConfig>(assetPath);
        if (existingConfig != null)
        {
            if (!EditorUtility.DisplayDialog("UIConfig Exists", 
                "UIConfig.asset already exists. Overwrite?", "Yes", "No"))
            {
                return;
            }
            AssetDatabase.DeleteAsset(assetPath);
        }

        AssetDatabase.CreateAsset(config, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = config;

        Debug.Log($"✅ UIConfig asset created at: {assetPath}");
        EditorUtility.DisplayDialog("Success!", 
            $"UIConfig asset created at:\n{assetPath}\n\nAll prefab references have been automatically copied from Bootstrap scene.", 
            "OK");

    }

    public static bool TryCopyFromBootstrapScene(UIConfig config)
    {
        string bootstrapScenePath = "Assets/Scenes/Bootstrap.unity";
        if (!File.Exists(bootstrapScenePath))
        {
            Debug.LogError($"Bootstrap scene not found at {bootstrapScenePath}");
            return false;
        }

        string sceneText = File.ReadAllText(bootstrapScenePath);

        config.mainMenuPanelPrefab = LoadPrefabByField(sceneText, "mainMenuPanelPrefab");
        config.songSelectionPanelPrefab = LoadPrefabByField(sceneText, "songSelectionPanelPrefab");
        config.gameplayPanelPrefab = LoadPrefabByField(sceneText, "gameplayPanelPrefab");
        config.pausePanelPrefab = LoadPrefabByField(sceneText, "pausePanelPrefab");
        config.gameOverPanelPrefab = LoadPrefabByField(sceneText, "gameOverPanelPrefab");
        config.settingsPanelPrefab = LoadPrefabByField(sceneText, "settingsPanelPrefab");

        config.perfectHitEffect = LoadPrefabByField(sceneText, "perfectHitEffect");
        config.goodHitEffect = LoadPrefabByField(sceneText, "goodHitEffect");
        config.missEffect = LoadPrefabByField(sceneText, "missEffect");

        if (TryExtractFloat(sceneText, "effectDuration", out float effectDuration))
        {
            config.effectDuration = effectDuration;
        }

        config.enableDebugLogging = false;
        config.enableFallbackUI = true;

        return true;
    }

    private static GameObject LoadPrefabByField(string sceneText, string fieldName)
    {
        string pattern = $@"{fieldName}:\s+\{{fileID:\s*\d+,\s*guid:\s*([0-9a-fA-F]+)";
        Match match = Regex.Match(sceneText, pattern);
        if (!match.Success)
        {
            Debug.LogWarning($"Could not find field '{fieldName}' in Bootstrap scene.");
            return null;
        }

        string guid = match.Groups[1].Value;
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning($"Could not resolve GUID '{guid}' for field '{fieldName}'.");
            return null;
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private static bool TryExtractFloat(string sceneText, string fieldName, out float value)
    {
        value = 0f;
        string pattern = $@"{fieldName}:\s*([0-9]+(\.[0-9]+)?)";
        Match match = Regex.Match(sceneText, pattern);
        if (!match.Success) return false;
        return float.TryParse(match.Groups[1].Value, out value);
    }
}
