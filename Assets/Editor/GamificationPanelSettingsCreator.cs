#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO;

/// <summary>
/// Editor utility to auto-create PanelSettings for Gamification UI Toolkit.
/// Run from menu: TilesWorld > Create Gamification PanelSettings
/// Also runs automatically via [InitializeOnLoadMethod].
/// </summary>
public static class GamificationPanelSettingsCreator
{
    private const string PANEL_SETTINGS_PATH = "Assets/Resources/UI/Gamification/GamificationPanelSettings.asset";

    [MenuItem("TilesWorld/Create Gamification PanelSettings")]
    public static void CreatePanelSettings()
    {
        EnsurePanelSettingsExists();
    }

    [InitializeOnLoadMethod]
    private static void OnEditorLoad()
    {
        // Only auto-create if the directory exists (project uses UI Toolkit gamification)
        if (Directory.Exists("Assets/Resources/UI/Gamification"))
        {
            EditorApplication.delayCall += () => EnsurePanelSettingsExists();
        }
    }

    private static void EnsurePanelSettingsExists()
    {
        if (AssetDatabase.LoadAssetAtPath<PanelSettings>(PANEL_SETTINGS_PATH) != null)
        {
            return; // Already exists
        }

        // Ensure directory exists
        string dir = Path.GetDirectoryName(PANEL_SETTINGS_PATH);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Create PanelSettings
        var ps = ScriptableObject.CreateInstance<PanelSettings>();
        ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        ps.referenceResolution = new Vector2Int(1920, 1080);
        ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
        ps.match = 0.5f;
        ps.sortingOrder = 100; // Above regular Canvas

        AssetDatabase.CreateAsset(ps, PANEL_SETTINGS_PATH);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"🎨 [GamificationPanelSettingsCreator] Created PanelSettings at {PANEL_SETTINGS_PATH}");
    }
}
#endif
