using UnityEngine;
using UnityEditor;

/// <summary>
/// Timing Analyzer - Analyzes and reports on current timing and hit zone configuration
/// </summary>
public class TimingAnalyzer : EditorWindow
{
    [MenuItem("TilesWorld/🔍 Timing Analyzer")]
    public static void ShowWindow()
    {
        TimingAnalyzer window = GetWindow<TimingAnalyzer>("Timing Analyzer");
        window.minSize = new Vector2(400, 300);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("🔍 TilesWorld Timing Analyzer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("📊 Analyze Current Setup", GUILayout.Height(40)))
        {
            AnalyzeCurrentSetup();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("⚡ Apply EASY Configuration", GUILayout.Height(30)))
        {
            ApplyEasyConfiguration();
        }

        if (GUILayout.Button("🎯 Apply MEDIUM Configuration", GUILayout.Height(30)))
        {
            ApplyMediumConfiguration();
        }

        if (GUILayout.Button("🔥 Apply HARD Configuration", GUILayout.Height(30)))
        {
            ApplyHardConfiguration();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Use this tool to analyze and fix timing issues. EASY mode is recommended for testing.", MessageType.Info);
    }

    private void AnalyzeCurrentSetup()
    {
        Debug.Log("🔍 === TIMING ANALYSIS REPORT ===");

        // Analyze HitZoneManager
        HitZoneManager hitZoneManager = FindFirstObjectByType<HitZoneManager>();
        if (hitZoneManager != null)
        {
            Debug.Log($"📊 TIMING WINDOWS:");
            Debug.Log($"  Perfect: {hitZoneManager.perfectWindowMs}ms");
            Debug.Log($"  Good: {hitZoneManager.goodWindowMs}ms");
            Debug.Log($"  Okay: {hitZoneManager.okayWindowMs}ms");

            // Analyze difficulty
            if (hitZoneManager.perfectWindowMs < 100)
                Debug.Log("⚠️ Perfect window is VERY STRICT (< 100ms)");
            if (hitZoneManager.goodWindowMs < 200)
                Debug.Log("⚠️ Good window is STRICT (< 200ms)");
            if (hitZoneManager.okayWindowMs < 300)
                Debug.Log("⚠️ Okay window is STRICT (< 300ms)");
        }
        else
        {
            Debug.LogError("❌ HitZoneManager not found in scene!");
        }

        // Analyze Hit Zones
        HitZoneTrigger[] hitZones = FindObjectsByType<HitZoneTrigger>(FindObjectsSortMode.None);
        Debug.Log($"📊 HIT ZONES FOUND: {hitZones.Length}");

        foreach (var hitZone in hitZones)
        {
            BoxCollider collider = hitZone.GetComponent<BoxCollider>();
            if (collider != null)
            {
                Vector3 size = collider.size;
                Vector3 worldSize = Vector3.Scale(size, hitZone.transform.localScale);

                Debug.Log($"  Lane {hitZone.laneIndex}: Size={size} WorldSize={worldSize} Position={hitZone.transform.position}");

                // Check for problems
                if (worldSize.x < 2f)
                    Debug.Log($"    ⚠️ Lane {hitZone.laneIndex} is NARROW (width < 2.0)");
                if (worldSize.z < 2f)
                    Debug.Log($"    ⚠️ Lane {hitZone.laneIndex} has SHORT depth (< 2.0)");
            }
        }

        // Analyze NoteRenderer speed using public accessor methods (no reflection needed)
        NoteRenderer noteRenderer = FindFirstObjectByType<NoteRenderer>();
        if (noteRenderer != null)
        {
            Debug.Log($"📊 NOTE RENDERER:");

            // OPTIMIZED: Use public accessor methods instead of reflection for better performance
            float speedMultiplier = noteRenderer.GetSpeedMultiplier();
            float hitZoneZ = noteRenderer.GetHitZoneZ();
            float spawnZ = noteRenderer.GetSpawnZ();

            Debug.Log($"  Speed Multiplier: {speedMultiplier}");
            Debug.Log($"  Hit Zone Z: {hitZoneZ}");
            Debug.Log($"  Spawn Z: {spawnZ}");

            // Calculate note travel time
            float distance = spawnZ - hitZoneZ;
            float travelTime = distance / speedMultiplier;
            Debug.Log($"  Travel Time: {travelTime:F2} seconds");

            if (speedMultiplier > 15)
                Debug.Log("⚠️ Notes are moving VERY FAST (speed > 15)");
        }
        else
        {
            Debug.Log("❌ NoteRenderer not found in scene!");
        }

        Debug.Log("🔍 === END ANALYSIS ===");
    }

    private void ApplyEasyConfiguration()
    {
        Debug.Log("⚡ Applying EASY configuration...");

        // Easy timing windows
        HitZoneManager hitZoneManager = FindFirstObjectByType<HitZoneManager>();
        if (hitZoneManager != null)
        {
            hitZoneManager.perfectWindowMs = 300f;
            hitZoneManager.goodWindowMs = 500f;
            hitZoneManager.okayWindowMs = 800f;
            Debug.Log("✅ Easy timing windows set: Perfect=300ms, Good=500ms, Okay=800ms");
            EditorUtility.SetDirty(hitZoneManager);
        }

        // Large hit zones
        ApplyHitZoneSize(new Vector3(3f, 1f, 5f));

        // Slower note speed
        NoteRenderer noteRenderer = FindFirstObjectByType<NoteRenderer>();
        if (noteRenderer != null)
        {
            // OPTIMIZED: Use public setter instead of reflection for better performance
            noteRenderer.SetSpeedMultiplier(6f);
            Debug.Log("✅ Note speed set to EASY (6.0)");
            EditorUtility.SetDirty(noteRenderer);
        }

        Debug.Log("🎮 EASY mode applied! Perfect for learning and testing.");
    }

    private void ApplyMediumConfiguration()
    {
        Debug.Log("🎯 Applying MEDIUM configuration...");

        HitZoneManager hitZoneManager = FindFirstObjectByType<HitZoneManager>();
        if (hitZoneManager != null)
        {
            hitZoneManager.perfectWindowMs = 150f;
            hitZoneManager.goodWindowMs = 300f;
            hitZoneManager.okayWindowMs = 500f;
            Debug.Log("✅ Medium timing windows set: Perfect=150ms, Good=300ms, Okay=500ms");
            EditorUtility.SetDirty(hitZoneManager);
        }

        ApplyHitZoneSize(new Vector3(2.2f, 1f, 3f));

        NoteRenderer noteRenderer = FindFirstObjectByType<NoteRenderer>();
        if (noteRenderer != null)
        {
            // OPTIMIZED: Use public setter instead of reflection for better performance
            noteRenderer.SetSpeedMultiplier(8f);
            Debug.Log("✅ Note speed set to MEDIUM (8.0)");
            EditorUtility.SetDirty(noteRenderer);
        }

        Debug.Log("🎮 MEDIUM mode applied! Good balance of challenge and playability.");
    }

    private void ApplyHardConfiguration()
    {
        Debug.Log("🔥 Applying HARD configuration...");

        HitZoneManager hitZoneManager = FindFirstObjectByType<HitZoneManager>();
        if (hitZoneManager != null)
        {
            hitZoneManager.perfectWindowMs = 80f;
            hitZoneManager.goodWindowMs = 160f;
            hitZoneManager.okayWindowMs = 250f;
            Debug.Log("✅ Hard timing windows set: Perfect=80ms, Good=160ms, Okay=250ms");
            EditorUtility.SetDirty(hitZoneManager);
        }

        ApplyHitZoneSize(new Vector3(1.8f, 1f, 2f));

        NoteRenderer noteRenderer = FindFirstObjectByType<NoteRenderer>();
        if (noteRenderer != null)
        {
            // OPTIMIZED: Use public setter instead of reflection for better performance
            noteRenderer.SetSpeedMultiplier(12f);
            Debug.Log("✅ Note speed set to HARD (12.0)");
            EditorUtility.SetDirty(noteRenderer);
        }

        Debug.Log("🎮 HARD mode applied! For experienced rhythm game players.");
    }

    private void ApplyHitZoneSize(Vector3 newSize)
    {
        HitZoneTrigger[] hitZones = FindObjectsByType<HitZoneTrigger>(FindObjectsSortMode.None);

        foreach (var hitZone in hitZones)
        {
            BoxCollider collider = hitZone.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.size = newSize;
                collider.center = Vector3.zero;
                EditorUtility.SetDirty(hitZone);
            }
        }

        Debug.Log($"✅ Applied hit zone size {newSize} to {hitZones.Length} zones");
    }
}