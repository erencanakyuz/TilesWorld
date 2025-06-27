using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Automatic Scene Setup Tool for Piano Game
/// Creates all GameObjects, managers, UI, and configurations needed for testing
/// </summary>
public class SceneSetupTool : EditorWindow
{
    [MenuItem("Tools/Piano Game/Setup Scene")]
    public static void SetupCompleteScene()
    {
        Debug.Log("🚀 Starting automatic scene setup...");

        SetupCamera();
        SetupLighting();
        SetupManagers();
        SetupUI();
        SetupGameplayObjects();
        SetupPrefabs();
        ConnectReferences();

        Debug.Log("✅ Scene setup complete! Ready to test!");
    }

    static void SetupCamera()
    {
        Debug.Log("🎥 Setting up Main Camera...");

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraGO = new GameObject("Main Camera");
            mainCamera = cameraGO.AddComponent<Camera>();
            cameraGO.tag = "MainCamera";
        }

        // Configure camera for 3D perspective gameplay
        mainCamera.transform.position = new Vector3(0, 8, -5);
        mainCamera.transform.rotation = Quaternion.Euler(45, 0, 0);
        mainCamera.fieldOfView = 60f;
        mainCamera.clearFlags = CameraClearFlags.Skybox;

        // Add audio listener if not present
        if (mainCamera.GetComponent<AudioListener>() == null)
            mainCamera.gameObject.AddComponent<AudioListener>();

        Debug.Log("✅ Main Camera configured");
    }

    static void SetupLighting()
    {
        Debug.Log("🌟 Setting up Lighting...");

        // Create directional light
        GameObject lightGO = GameObject.Find("Directional Light");
        if (lightGO == null)
        {
            lightGO = new GameObject("Directional Light");
            lightGO.AddComponent<Light>();
        }

        Light light = lightGO.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.2f;
        light.shadows = LightShadows.Soft;
        lightGO.transform.position = new Vector3(0, 10, 0);
        lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);

        Debug.Log("✅ Lighting configured");
    }

    static void SetupManagers()
    {
        Debug.Log("🎮 Setting up Manager GameObjects...");

        // Create core managers
        CreateManager("GameManager", typeof(GameManager));
        CreateManager("AudioManager", typeof(AudioManager));
        CreateManager("InputManager", typeof(InputManager));
        CreateManager("UIManager", typeof(UIManager));

        // Create gameplay systems
        CreateManager("GameplayManager", typeof(GameplayManager));
        CreateManager("GameNoteCreator", typeof(GameNoteCreator));
        CreateManager("NoteRenderer", typeof(NoteRenderer));
        CreateManager("InteractiveMusicSystem", typeof(InteractiveMusicSystem));

        Debug.Log("✅ All managers created");
    }

    static GameObject CreateManager(string name, System.Type componentType)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            Debug.Log($"🔄 {name} already exists, skipping...");
            return existing;
        }

        GameObject managerGO = new GameObject(name);
        managerGO.AddComponent(componentType);

        Debug.Log($"✅ Created {name}");
        return managerGO;
    }

    static void SetupUI()
    {
        Debug.Log("🎨 Setting up UI Canvas...");

        // Create main canvas
        GameObject canvasGO = GameObject.Find("MainCanvas");
        if (canvasGO == null)
        {
            canvasGO = new GameObject("MainCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Create HUD Canvas
        GameObject hudCanvasGO = new GameObject("HUDCanvas");
        hudCanvasGO.transform.SetParent(canvasGO.transform);
        Canvas hudCanvas = hudCanvasGO.AddComponent<Canvas>();
        hudCanvas.overrideSorting = true;
        hudCanvas.sortingOrder = 10;

        // Create main UI panels
        CreateUIPanel(canvasGO, "MainMenuPanel", new Vector2(0, 0));
        CreateUIPanel(canvasGO, "SongSelectionPanel", new Vector2(0, 0));
        CreateUIPanel(canvasGO, "GameplayPanel", new Vector2(0, 0));
        CreateUIPanel(canvasGO, "PausePanel", new Vector2(0, 0));
        CreateUIPanel(canvasGO, "GameOverPanel", new Vector2(0, 0));

        // Create HUD elements
        CreateHUDElements(hudCanvasGO);

        // Create debug text
        CreateDebugText(canvasGO);

        // Create effect parent
        GameObject effectParent = new GameObject("EffectParent");
        effectParent.transform.SetParent(canvasGO.transform);

        Debug.Log("✅ UI Canvas setup complete");
    }

    static GameObject CreateUIPanel(GameObject parent, string name, Vector2 position)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent.transform);

        RectTransform rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = Vector2.zero;

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.5f);

        // Start with panels inactive
        panel.SetActive(false);

        return panel;
    }

    static void CreateHUDElements(GameObject hudParent)
    {
        // Score Text
        GameObject scoreGO = new GameObject("ScoreText");
        scoreGO.transform.SetParent(hudParent.transform);
        RectTransform scoreRect = scoreGO.AddComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0, 1);
        scoreRect.anchorMax = new Vector2(0, 1);
        scoreRect.anchoredPosition = new Vector2(100, -50);
        scoreRect.sizeDelta = new Vector2(300, 60);

        TextMeshProUGUI scoreText = scoreGO.AddComponent<TextMeshProUGUI>();
        scoreText.text = "Score: 0";
        scoreText.fontSize = 24;
        scoreText.color = Color.white;

        // Combo Text
        GameObject comboGO = new GameObject("ComboText");
        comboGO.transform.SetParent(hudParent.transform);
        RectTransform comboRect = comboGO.AddComponent<RectTransform>();
        comboRect.anchorMin = new Vector2(1, 1);
        comboRect.anchorMax = new Vector2(1, 1);
        comboRect.anchoredPosition = new Vector2(-100, -50);
        comboRect.sizeDelta = new Vector2(300, 60);

        TextMeshProUGUI comboText = comboGO.AddComponent<TextMeshProUGUI>();
        comboText.text = "Combo: 0";
        comboText.fontSize = 24;
        comboText.color = Color.yellow;
        comboText.alignment = TextAlignmentOptions.TopRight;

        // Health Bar
        GameObject healthGO = new GameObject("HealthBar");
        healthGO.transform.SetParent(hudParent.transform);
        RectTransform healthRect = healthGO.AddComponent<RectTransform>();
        healthRect.anchorMin = new Vector2(0.5f, 1);
        healthRect.anchorMax = new Vector2(0.5f, 1);
        healthRect.anchoredPosition = new Vector2(0, -30);
        healthRect.sizeDelta = new Vector2(400, 20);

        Slider healthSlider = healthGO.AddComponent<Slider>();
        healthSlider.value = 1.0f;

        // Create slider background
        GameObject healthBG = new GameObject("Background");
        healthBG.transform.SetParent(healthGO.transform);
        RectTransform bgRect = healthBG.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        Image bgImage = healthBG.AddComponent<Image>();
        bgImage.color = Color.red;
        healthSlider.targetGraphic = bgImage;

        // Create slider fill
        GameObject healthFill = new GameObject("Fill");
        healthFill.transform.SetParent(healthGO.transform);
        RectTransform fillRect = healthFill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        fillRect.anchoredPosition = Vector2.zero;

        Image fillImage = healthFill.AddComponent<Image>();
        fillImage.color = Color.green;
        healthSlider.fillRect = fillRect;
        healthSlider.targetGraphic = fillImage;
    }

    static void CreateDebugText(GameObject parent)
    {
        GameObject debugGO = new GameObject("DebugText");
        debugGO.transform.SetParent(parent.transform);

        RectTransform rectTransform = debugGO.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(1, 0);
        rectTransform.anchoredPosition = new Vector2(0, 100);
        rectTransform.sizeDelta = new Vector2(0, 100);

        TextMeshProUGUI debugText = debugGO.AddComponent<TextMeshProUGUI>();
        debugText.text = "🎮 Piano Game Ready!\n🎵 Touch screen to test input\n🎹 Space to start test song";
        debugText.fontSize = 18;
        debugText.color = Color.white;
        debugText.alignment = TextAlignmentOptions.Center;
    }

    static void SetupGameplayObjects()
    {
        Debug.Log("🎯 Setting up Gameplay Objects...");

        // Create note container
        GameObject noteContainer = new GameObject("NoteContainer");
        noteContainer.transform.position = Vector3.zero;

        // Create lane visualization
        CreateLaneVisualization();

        // Create hit zone visualization  
        CreateHitZoneVisualization();

        Debug.Log("✅ Gameplay objects created");
    }

    static void CreateLaneVisualization()
    {
        GameObject laneParent = new GameObject("LaneVisualization");

        for (int i = 0; i < 6; i++)
        {
            GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            lane.name = $"Lane_{i}";
            lane.transform.SetParent(laneParent.transform);

            float laneWidth = 1.8f;
            float xOffset = (i - 2.5f) * laneWidth;
            lane.transform.position = new Vector3(xOffset, 0, 12.5f);
            lane.transform.localScale = new Vector3(0.18f, 1f, 2.5f);

            Renderer renderer = lane.GetComponent<Renderer>();
            Material laneMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            laneMaterial.color = new Color(0.2f, 0.2f + i * 0.1f, 0.8f, 0.3f);
            renderer.material = laneMaterial;

            // Remove colliders for performance
            DestroyImmediate(lane.GetComponent<Collider>());
        }
    }

    static void CreateHitZoneVisualization()
    {
        GameObject hitZone = GameObject.CreatePrimitive(PrimitiveType.Plane);
        hitZone.name = "HitZone";
        hitZone.transform.position = new Vector3(0, 0.1f, 3f);
        hitZone.transform.localScale = new Vector3(1.08f, 1f, 1f);

        Renderer renderer = hitZone.GetComponent<Renderer>();
        Material hitMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        hitMaterial.color = new Color(1f, 1f, 0f, 0.5f);
        renderer.material = hitMaterial;

        // Remove collider
        DestroyImmediate(hitZone.GetComponent<Collider>());
    }

    static void SetupPrefabs()
    {
        Debug.Log("🎨 Creating Note Prefab...");

        // Create note prefab
        GameObject notePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        notePrefab.name = "NotePrefab";
        notePrefab.transform.localScale = new Vector3(1.5f, 0.2f, 1.5f);

        // Configure note material
        Renderer noteRenderer = notePrefab.GetComponent<Renderer>();
        Material noteMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        noteMaterial.color = Color.cyan;
        noteRenderer.material = noteMaterial;

        // Remove collider (we'll use custom hit detection)
        DestroyImmediate(notePrefab.GetComponent<Collider>());

        // Create prefab directories
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Notes"))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Notes");
        }

        // Save as prefab
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(notePrefab, "Assets/Prefabs/Notes/NotePrefab.prefab");
        DestroyImmediate(notePrefab);

        Debug.Log("✅ Note prefab created and saved");

        // Create hit effect prefabs
        CreateHitEffectPrefabs();
    }

    static void CreateHitEffectPrefabs()
    {
        // Create Perfect Hit Effect
        GameObject perfectEffect = new GameObject("PerfectHitEffect");

        // Add particle system for visual effect
        ParticleSystem particles = perfectEffect.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startColor = Color.yellow;
        main.startLifetime = 0.5f;
        main.startSpeed = 2f;
        main.maxParticles = 20;

        var emission = particles.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0.0f, 20)
        });

        // Create effect directories
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Effects"))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Effects");
        }

        // Save effect prefabs
        PrefabUtility.SaveAsPrefabAsset(perfectEffect, "Assets/Prefabs/Effects/PerfectHitEffect.prefab");

        // Create variations for Good and Miss effects
        var goodEffect = Instantiate(perfectEffect);
        goodEffect.name = "GoodHitEffect";
        var goodMain = goodEffect.GetComponent<ParticleSystem>().main;
        goodMain.startColor = Color.green;
        PrefabUtility.SaveAsPrefabAsset(goodEffect, "Assets/Prefabs/Effects/GoodHitEffect.prefab");

        var missEffect = Instantiate(perfectEffect);
        missEffect.name = "MissEffect";
        var missMain = missEffect.GetComponent<ParticleSystem>().main;
        missMain.startColor = Color.red;
        PrefabUtility.SaveAsPrefabAsset(missEffect, "Assets/Prefabs/Effects/MissEffect.prefab");

        // Cleanup
        DestroyImmediate(perfectEffect);
        DestroyImmediate(goodEffect);
        DestroyImmediate(missEffect);

        Debug.Log("✅ Hit effect prefabs created");
    }

    static void ConnectReferences()
    {
        Debug.Log("🔗 Connecting component references...");

        // Get all managers
        GameplayManager gameplayManager = FindFirstObjectByType<GameplayManager>();
        GameNoteCreator noteCreator = FindFirstObjectByType<GameNoteCreator>();
        NoteRenderer noteRenderer = FindFirstObjectByType<NoteRenderer>();
        InteractiveMusicSystem musicSystem = FindFirstObjectByType<InteractiveMusicSystem>();
        UIManager uiManager = FindFirstObjectByType<UIManager>();

        // Connect GameplayManager references
        if (gameplayManager != null)
        {
            SerializedObject gameplayObj = new SerializedObject(gameplayManager);

            gameplayObj.FindProperty("noteCreator").objectReferenceValue = noteCreator;
            gameplayObj.FindProperty("noteRenderer").objectReferenceValue = noteRenderer;
            gameplayObj.FindProperty("musicSystem").objectReferenceValue = musicSystem;
            gameplayObj.FindProperty("audioManager").objectReferenceValue = FindFirstObjectByType<AudioManager>();

            gameplayObj.ApplyModifiedProperties();
        }

        // Connect NoteRenderer references
        if (noteRenderer != null)
        {
            SerializedObject rendererObj = new SerializedObject(noteRenderer);

            // Load and assign the note prefab
            GameObject notePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Notes/NotePrefab.prefab");
            if (notePrefab != null)
            {
                rendererObj.FindProperty("notePrefab").objectReferenceValue = notePrefab;
            }

            // Assign note container
            GameObject noteContainer = GameObject.Find("NoteContainer");
            if (noteContainer != null)
            {
                rendererObj.FindProperty("noteParent").objectReferenceValue = noteContainer.transform;
            }

            rendererObj.ApplyModifiedProperties();
        }

        // Connect UIManager references
        if (uiManager != null)
        {
            SerializedObject uiObj = new SerializedObject(uiManager);

            // Assign canvas references
            Canvas mainCanvas = GameObject.Find("MainCanvas")?.GetComponent<Canvas>();
            if (mainCanvas != null)
            {
                uiObj.FindProperty("mainCanvas").objectReferenceValue = mainCanvas;
            }

            Canvas hudCanvas = GameObject.Find("HUDCanvas")?.GetComponent<Canvas>();
            if (hudCanvas != null)
            {
                uiObj.FindProperty("hudCanvas").objectReferenceValue = hudCanvas;
            }

            // Assign UI panel references
            uiObj.FindProperty("mainMenuPanel").objectReferenceValue = GameObject.Find("MainMenuPanel");
            uiObj.FindProperty("songSelectionPanel").objectReferenceValue = GameObject.Find("SongSelectionPanel");
            uiObj.FindProperty("gameplayPanel").objectReferenceValue = GameObject.Find("GameplayPanel");
            uiObj.FindProperty("pausePanel").objectReferenceValue = GameObject.Find("PausePanel");
            uiObj.FindProperty("gameOverPanel").objectReferenceValue = GameObject.Find("GameOverPanel");

            // Assign HUD references
            uiObj.FindProperty("scoreText").objectReferenceValue = GameObject.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
            uiObj.FindProperty("comboText").objectReferenceValue = GameObject.Find("ComboText")?.GetComponent<TextMeshProUGUI>();
            uiObj.FindProperty("healthBar").objectReferenceValue = GameObject.Find("HealthBar")?.GetComponent<Slider>();

            // Assign effect references
            GameObject perfectEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/PerfectHitEffect.prefab");
            GameObject goodEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/GoodHitEffect.prefab");
            GameObject missEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/MissEffect.prefab");

            if (perfectEffect != null) uiObj.FindProperty("perfectHitEffect").objectReferenceValue = perfectEffect;
            if (goodEffect != null) uiObj.FindProperty("goodHitEffect").objectReferenceValue = goodEffect;
            if (missEffect != null) uiObj.FindProperty("missEffect").objectReferenceValue = missEffect;

            // Assign effect parent
            GameObject effectParent = GameObject.Find("EffectParent");
            if (effectParent != null)
            {
                uiObj.FindProperty("effectParent").objectReferenceValue = effectParent.transform;
            }

            uiObj.ApplyModifiedProperties();
        }

        Debug.Log("✅ Component references connected");
    }

    [MenuItem("Tools/Piano Game/Create Test Song Data")]
    public static void CreateTestSongData()
    {
        Debug.Log("🎵 Creating test song data...");

        // Create ScriptableObject for test song
        SongData testSong = ScriptableObject.CreateInstance<SongData>();
        testSong.songName = "Test Song";
        testSong.artist = "Piano Game";
        testSong.bpm = 120f;
        testSong.duration = 60f;
        testSong.audioFilePath = "Audio/TestSong";
        testSong.noteChartPath = "Charts/TestSong";
        testSong.difficulty = DifficultyLevel.Easy;

        // Create song data folder
        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Data/Songs"))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Songs");
        }

        AssetDatabase.CreateAsset(testSong, "Assets/Data/Songs/TestSong.asset");
        AssetDatabase.SaveAssets();

        Debug.Log("✅ Test song data created");
    }

    [MenuItem("Tools/Piano Game/Setup Complete Game")]
    public static void SetupCompleteGame()
    {
        SetupCompleteScene();
        CreateTestSongData();

        // Auto-save scene
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("🎉 COMPLETE GAME SETUP FINISHED!");
        Debug.Log("🎮 Press Play to test the game!");
        Debug.Log("🎵 Touch screen or press Space to interact");
    }
}