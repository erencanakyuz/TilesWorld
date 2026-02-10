using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor-only menu item to test the Song Result screen with mock data.
/// Access via menu: Tools > Test Song Result
/// </summary>
public static class SongResultTester
{
    [MenuItem("Tools/Test Song Result Screen")]
    public static void TestSongResult()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Enter Play Mode first, then use Tools > Test Song Result Screen");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found!");
            return;
        }

        GameManager.Instance.DebugTestSongResult();
        Debug.Log("[SongResultTester] Triggered mock song result!");
    }
}
