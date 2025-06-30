using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

/// <summary>
/// NoteRenderer - Visual Heart of the Game
/// Based on original WorldRenderer.java with perspective "conveyor belt" effect
/// Implements: Z-depth movement, perspective scaling, rotation effects
/// </summary>
public class NoteRenderer : MonoBehaviour
{
    [Header("🔧 Debugging")]
    [SerializeField] private bool showDebugLogs = false;

    [Header("🎨 Rendering")]
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private Transform noteParent;
    [SerializeField] private Color[] laneColors;
    [SerializeField] private float noteLengthMultiplier = 1.0f;
    [SerializeField] private float cameraAngle = 45f;

    [Header("📏 Sizing and Positioning")]
    [SerializeField] private int laneCount = 6;
    [SerializeField] private float laneWidth = 2.4f;       // Genişletildi: 1.8f → 2.4f

    [Header("🚀 Note Movement")]
    [Tooltip("The constant speed at which notes travel towards the player.")]
    [SerializeField] private float speedMultiplier = 12.0f;    // Increased default speed

    [Header("📊 Performance & Debug")]
    [SerializeField] private bool enableObjectPooling = true;
    [SerializeField] private int poolSize = 50;

    // Object pooling system (from MD analysis)
    private Queue<GameObject> notePool;
    private List<RenderingNote> activeNotes;
    private int totalNotesRendered = 0;

    // Original algorithm state
    private Camera mainCamera;
    private Material noteMaterial;                            // Material for notes
    // PERFORMANCE FIX: These variables were part of the disabled highlighting system.
    // private bool isNoteHighlight = false;                     // Original: isInvaderHilight
    // private float noteTextureChangeTime = 0f;                 // Original: invaderTextureChangeTime

    // Lane positioning
    private Vector3[] lanePositions;
    private float worldWidth;

    // Performance tracking
    private int activeNoteCount = 0;

    private MaterialPropertyBlock mpb; // NEW: for per-note color without new materials

    void Awake()
    {
        InitializeRenderer();
    }

    void Start()
    {
        SetupLanes();
        SetupCamera();
        SubscribeToEvents();
        CheckSceneLighting();
    }

    void CheckSceneLighting()
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        if (lights.Length == 0)
        {
            Debug.LogWarning("🎨 No lights found in scene! Notes may not be visible.");
        }
    }

    void InitializeRenderer()
    {
        notePool = new Queue<GameObject>();
        activeNotes = new List<RenderingNote>();

        CreateNoteMaterial();

        // NEW: initialize property block once
        mpb = new MaterialPropertyBlock();

        if (enableObjectPooling)
            CreateNotePool();
    }

    void CreateNoteMaterial()
    {
        if (notePrefab == null)
        {
            Debug.LogError("Note prefab is not assigned in NoteRenderer!");
            return;
        }

        var renderer = notePrefab.GetComponent<Renderer>();
        if (renderer != null && renderer.sharedMaterial != null)
        {
            noteMaterial = new Material(renderer.sharedMaterial);
            if (showDebugLogs) Debug.Log($"🎨 Created base material with shader: {noteMaterial.shader.name}");
        }
        else
        {
            Debug.LogError("Note prefab must have a Renderer with a valid material. Creating a fallback material.");
            // --- FALLBACK: Create a simple unlit material from scratch ---
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color"); // Secondary fallback
            if (shader == null) shader = Shader.Find("Standard"); // Ultimate fallback

            noteMaterial = new Material(shader);
            noteMaterial.color = Color.cyan;
        }
    }

    void CreateNotePool()
    {
        if (notePrefab == null || noteParent == null) return;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject note = Instantiate(notePrefab, noteParent);
            note.SetActive(false);
            notePool.Enqueue(note);
        }
    }

    void SetupLanes()
    {
        worldWidth = laneCount * laneWidth;
        lanePositions = new Vector3[laneCount];

        // Hit zone width is now determined by the physical trigger colliders in the scene.
        // hitZoneWidth = laneWidth; 

        for (int i = 0; i < laneCount; i++)
        {
            // Match existing HitZoneTrigger positions in scene:
            // Lane 0: x: -4.5, Lane 1: x: -2.7, Lane 2: x: -0.9
            // Lane 3: x: 0.9, Lane 4: x: 2.7, Lane 5: x: 4.5
            float xOffset = (i - 2.5f) * 1.8f; // 1.8f spacing between lanes
            lanePositions[i] = new Vector3(xOffset, 0, 0);
        }

        if (showDebugLogs) Debug.Log($"🎯 Lanes setup: {laneCount} lanes, {laneWidth:F2} width each, total world width: {worldWidth:F1}");
    }

    void SetupCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindFirstObjectByType<Camera>();

        if (mainCamera != null)
        {
            Debug.Log($"🎥 Camera setup: Position {mainCamera.transform.position}, Rotation {mainCamera.transform.rotation.eulerAngles}");
        }
        else
        {
            Debug.LogError("🎥 No main camera found!");
        }

        mainCamera.transform.rotation = Quaternion.Euler(cameraAngle, 0, 0);

        if (showDebugLogs) Debug.Log($"🎥 Camera setup: Position ({mainCamera.transform.position.x:F2}, {mainCamera.transform.position.y:F2}, {mainCamera.transform.position.z:F2}), Rotation ({mainCamera.transform.eulerAngles.x:F2}, {mainCamera.transform.eulerAngles.y:F2}, {mainCamera.transform.eulerAngles.z:F2})");
    }

    void SubscribeToEvents()
    {
        // Hit detection is now handled entirely by HitZoneManager.
    }

    #region Original WorldRenderer Algorithm

    /// <summary>
    /// Original Java: renderInvaders() - Core rendering loop with perspective effects
    /// </summary>
    void Update()
    {
        // PERFORMANCE FIX: As per performance.md, do not run update logic if the game is paused.
        if (Time.timeScale <= 0f) return;

        float deltaTime = Time.deltaTime;

        UpdateNoteTextures(deltaTime);
        UpdateActiveNotes(deltaTime);
        CleanupDestroyedNotes();

        // Fix from performance report: keep statistics updated.
        activeNoteCount = activeNotes.Count;
    }

    /// <summary>
    /// Original Java: invaderTextureChangeTime logic - Note texture animation
    /// </summary>
    void UpdateNoteTextures(float deltaTime)
    {
        // PERFORMANCE FIX: This entire highlighting logic creates new material instances per-frame
        // and is a significant performance bottleneck. It has been temporarily disabled.
        // TODO: Re-implement this using a more efficient method like MaterialPropertyBlock or by
        // modifying shader properties directly if highlighting is required in the future.
        /*
        noteTextureChangeTime += deltaTime;
        if (noteTextureChangeTime > 0.2f) // Original: 0.2F interval
        {
            isNoteHighlight = !isNoteHighlight;
            noteTextureChangeTime = 0f;

            // Clean up destroyed notes first
            activeNotes.RemoveAll(note => note == null || note.gameObject == null);

            // Apply texture changes to all active notes
            foreach (var activeNote in activeNotes)
            {
                if (activeNote != null && activeNote.gameObject != null)
                {
                    ApplyNoteHighlight(activeNote, isNoteHighlight);
                }
            }
        }
        */
    }

    /// <summary>
    /// *** EXACT JAVA ALGORITHM adapted for Unity coordinate system ***
    /// Notes move from POSITIVE Z (far) to NEGATIVE Z (near player)
    /// </summary>
    void UpdateActiveNotes(float deltaTime)
    {
        if (deltaTime <= 0f || Time.timeScale <= 0f) return;

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            var activeNote = activeNotes[i];
            if (activeNote == null || activeNote.gameObject == null) continue;

            // --- Constant Speed Movement (Acceleration Removed) ---
            // Notes now move at a single, consistent speed.
            float currentSpeed = speedMultiplier;

            // *** UNITY COORDINATE: Move notes TOWARD player (Z decreases) ***
            activeNote.currentPosition.z -= currentSpeed * deltaTime;

            // Update Unity transform directly (no coordinate conversion needed)
            activeNote.gameObject.transform.position = activeNote.currentPosition;

            // Destroy notes that go too far behind (-4 on Z axis)
            if (activeNote.currentPosition.z < -4f)
            {
                if (showDebugLogs) Debug.Log($"🗑️ Destroying note at Z={activeNote.currentPosition.z:F2}");
                HandleNoteMissed(activeNote);
                ReturnNoteToPool(activeNote.gameObject);
                activeNotes.RemoveAt(i);
                continue;
            }
        }
    }

    // void ApplyNoteHighlight(RenderingNote activeNote, bool highlight)
    // {
    //     ...
    // }

    /// <summary>
    /// Get material for specific pitch and instrument
    /// </summary>
    // PERFORMANCE FIX: This entire method is a performance killer as it creates a new material instance for each note.
    // It is kept here but commented out for future reference. If unique note colors are needed,
    // this should be replaced with a system that uses MaterialPropertyBlock.
    /*
    Material GetMaterialForPitch(int pitch, InstrumentType instrumentType)
    {
        // Create or reuse material with pitch-based color
        Material pitchMaterial = new Material(noteMaterial.shader);

        // Color based on pitch (musical note colors)
        Color noteColor = GetColorForPitch(pitch);

        // Tint based on instrument
        Color instrumentTint = GetColorForInstrument(instrumentType);

        // Combine pitch color with instrument tint
        pitchMaterial.color = Color.Lerp(noteColor, instrumentTint, 0.3f);

        // Ensure material is valid
        if (pitchMaterial == null || pitchMaterial.shader.name.Contains("Hidden"))
        {
            Debug.LogWarning("🎨 Pitch material creation failed, using base material");
            return noteMaterial;
        }

        return pitchMaterial;
    }
    */

    /// <summary>
    /// Get color based on musical pitch (chromatic scale)
    /// </summary>
    Color GetColorForPitch(int pitch)
    {
        // Map pitch to color wheel (12-tone chromatic scale)
        int chromaticNote = pitch % 12;

        switch (chromaticNote)
        {
            case 0: return Color.red;       // C
            case 1: return Color.Lerp(Color.red, Color.yellow, 0.5f);   // C#
            case 2: return Color.yellow;    // D
            case 3: return Color.Lerp(Color.yellow, Color.green, 0.5f); // D#
            case 4: return Color.green;     // E
            case 5: return Color.cyan;      // F
            case 6: return Color.Lerp(Color.cyan, Color.blue, 0.5f);    // F#
            case 7: return Color.blue;      // G
            case 8: return Color.Lerp(Color.blue, Color.magenta, 0.5f); // G#
            case 9: return Color.magenta;   // A
            case 10: return Color.Lerp(Color.magenta, Color.red, 0.5f);  // A#
            case 11: return Color.white;     // B
            default: return Color.gray;
        }
    }

    /// <summary>
    /// Get color tint for instrument type
    /// </summary>
    Color GetColorForInstrument(InstrumentType instrumentType)
    {
        switch (instrumentType)
        {
            case InstrumentType.Piano: return new Color(1f, 1f, 1f, 1f);     // White (neutral)
            case InstrumentType.Harp: return new Color(0.8f, 1f, 0.9f, 1f); // Light green
            case InstrumentType.Guitar: return new Color(1f, 0.9f, 0.7f, 1f); // Warm yellow
            default: return Color.white;
        }
    }

    /// <summary>
    /// Show pitch indicator at specific position
    /// </summary>
    public void ShowPitchIndicator(int pitch, Vector3 position)
    {
        // Create temporary visual indicator for pitch
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        indicator.transform.position = position + Vector3.up * 2f;
        indicator.transform.localScale = Vector3.one * 0.3f;

        Renderer indicatorRenderer = indicator.GetComponent<Renderer>();
        // PERFORMANCE FIX: The GetMaterialForPitch method was disabled for performance.
        // This indicator will now use the default material.
        // indicatorRenderer.material = GetMaterialForPitch(pitch, InstrumentType.Piano);

        // Auto-destroy after 1 second
        Destroy(indicator, 1f);
    }
    #endregion

    #region Note Management

    public void SpawnNotes(List<GameNoteInfo> notes)
    {
        // *** DEBUG: Kim çağırıyor? ***
        string caller = System.Environment.StackTrace.Split('\n')[1].Trim();


        // Called directly from GameplayManager (no events)
        foreach (var note in notes)
        {
            SpawnNote(note);
        }


    }

    void SpawnNote(GameNoteInfo noteInfo)
    {
        GameObject noteObject = GetPooledNote();
        if (noteObject == null)
        {
            Debug.LogError("🎨 No pooled note available! Check notePrefab assignment and noteParent setup.");
            return;
        }

        // This will be the base color for the note, used for highlighting.
        Color finalColor = Color.white;

        // *** CRITICAL: Ensure note has material ***
        Renderer noteRenderer = noteObject.GetComponent<Renderer>();
        if (noteRenderer != null)
        {
            // PERFORMANCE FIX: Disabled per-note material instancing to avoid excessive draw calls.
            // All notes will now use the prefab's default material.
            // TODO: To re-enable unique note colors, implement a MaterialPropertyBlock system
            // instead of creating new material instances.
            //
            // // USE SHARED MATERIAL (no instancing)
            // noteRenderer.sharedMaterial = noteMaterial;
            //
            // // Apply per-note color via MaterialPropertyBlock
            // mpb.Clear();
            // Color pitchColor = GetColorForPitch(noteInfo.pitch);
            // Color instrumentTint = GetColorForInstrument(noteInfo.instrumentType);
            // finalColor = Color.Lerp(pitchColor, instrumentTint, 0.3f);
            // // try _BaseColor first (URP Unlit), fall back to _Color
            // if (noteRenderer.sharedMaterial.HasProperty("_BaseColor"))
            //     mpb.SetColor("_BaseColor", finalColor);
            // else
            //     mpb.SetColor("_Color", finalColor);
            // noteRenderer.SetPropertyBlock(mpb);
        }
        else
        {
            Debug.LogError("🚨 Note prefab missing Renderer component!");
            return;
        }

        // Calculate spawn position
        Vector3 spawnPosition;
        if (lanePositions != null && noteInfo.idx >= 0 && noteInfo.idx < lanePositions.Length)
        {
            spawnPosition = lanePositions[noteInfo.idx];
        }
        else
        {
            // Fallback calculation using current laneWidth
            float xOffset = (noteInfo.idx - (laneCount - 1) * 0.5f) * laneWidth;
            spawnPosition = new Vector3(xOffset, 0, 0);
        }

        // *** UNITY COORDINATE: Spawn notes at FAR END (positive Z) ***
        spawnPosition.z = 25f; // Start at a fixed distance from the player
        spawnPosition.y = 0;

        // Set note properties - fit exactly to lane width
        float noteScale = laneWidth * 0.7f; // %70 of lane width for nice fit
        noteObject.transform.localScale = new Vector3(noteScale, 1.0f, noteScale * noteLengthMultiplier);
        noteObject.transform.position = spawnPosition;
        noteObject.SetActive(true);

        // NEW ► Ensure note has tag and NoteWrapper for HitZone system
        noteObject.tag = "Note";
        var wrapper = noteObject.GetComponent<NoteWrapper>();
        if (wrapper == null)
            wrapper = noteObject.AddComponent<NoteWrapper>();
        wrapper.gameNoteInfo = noteInfo;

        // Reverted to original Time.time based calculation.
        float travelTime = Mathf.Abs((spawnPosition.z - 0f)) / Mathf.Max(0.01f, speedMultiplier); // hitLineZ assumed 0
        wrapper.expectedHitTime = Time.time + travelTime;

        // Create active note tracking
        var activeNote = new RenderingNote
        {
            gameObject = noteObject,
            noteInfo = noteInfo,
            currentPosition = spawnPosition,
            spawnTime = Time.time, // This is game clock time, not song time
            baseColor = finalColor
        };

        activeNotes.Add(activeNote);
        totalNotesRendered++;

        // Debug only for first few spawns to check for duplicates
        /*
        if (totalNotesRendered <= 10)
        {
            Debug.Log($"🎵 SPAWN #{totalNotesRendered}: Lane {noteInfo.idx}, Pitch {noteInfo.pitch}, Pos {spawnPosition}");
        }
        */

        // Ensure collider & Rigidbody configuration for trigger detection
        BoxCollider col = noteObject.GetComponent<BoxCollider>();
        if (col == null)
        {
            col = noteObject.AddComponent<BoxCollider>();
            Debug.Log($"[NoteRenderer] Added BoxCollider to note {noteObject.name}");
        }
        col.isTrigger = false; // note acts as solid body entering trigger
        col.size = new Vector3(laneWidth * 0.7f, 1.0f, laneWidth * 0.7f * noteLengthMultiplier); // Match visual scale

        Rigidbody rb = noteObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = noteObject.AddComponent<Rigidbody>();

        }
        rb.isKinematic = true; // we move notes manually
        rb.useGravity = false; // no gravity
        rb.constraints = RigidbodyConstraints.FreezeAll; // prevent any physics movement

        if (showDebugLogs && totalNotesRendered <= 5)
        {
            Debug.Log($"[NoteRenderer] Note {noteObject.name} setup: Collider={col != null}, Rigidbody={rb != null}, Tag='{noteObject.tag}'");
        }
    }

    GameObject GetPooledNote()
    {
        if (enableObjectPooling)
        {
            // If pool is empty, create more notes
            if (notePool.Count == 0)
            {
                Debug.Log($"🎨 Pool empty, expanding by {poolSize} notes...");
                for (int i = 0; i < poolSize; i++)
                {
                    GameObject note = Instantiate(notePrefab, noteParent);
                    note.SetActive(false);
                    notePool.Enqueue(note);
                }
            }

            return notePool.Dequeue();
        }
        else if (notePrefab != null && noteParent != null)
        {
            return Instantiate(notePrefab, noteParent);
        }

        Debug.LogError("🎨 Cannot create note: notePrefab or noteParent is missing!");
        return null;
    }

    public void ReturnNoteToPool(GameObject noteObject)
    {
        if (noteObject == null) return;

        if (enableObjectPooling)
        {
            // Reset note state before returning to pool
            noteObject.transform.position = Vector3.zero;
            noteObject.transform.rotation = Quaternion.identity;
            noteObject.SetActive(false);
            notePool.Enqueue(noteObject);
        }
        else
        {
            Destroy(noteObject);
        }
    }

    void HandleNoteMissed(RenderingNote activeNote)
    {
        // Reset combo, reduce health, etc.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateCombo(0);
        }
    }

    void TriggerNoteAudio(GameNoteInfo noteInfo)
    {
        // Only use InteractiveMusicSystem to avoid duplicate calls
        if (InteractiveMusicSystem.Instance != null)
        {
            // Use enhanced music system with JSON pitch data - this handles all audio
            InteractiveMusicSystem.Instance.PlayNoteFromChart(noteInfo);
        }
        // Note: No fallback to prevent duplicate audio calls
    }

    int CalculateScore(float accuracy)
    {
        if (accuracy > 0.9f) return 300; // Perfect
        if (accuracy > 0.7f) return 200; // Great
        if (accuracy > 0.5f) return 100; // Good
        return 50; // OK
    }
    #endregion

    void CleanupDestroyedNotes()
    {
        // Additional cleanup for any orphaned notes
        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            if (activeNotes[i].gameObject == null)
            {
                activeNotes.RemoveAt(i);
            }
        }
    }

    #region Public Interface

    /// <summary>
    /// Processes a note that has been successfully hit by the player.
    /// This finds the corresponding active note, removes it from tracking, and returns its GameObject to the pool.
    /// </summary>
    public void ProcessHitNote(GameObject noteObject)
    {
        if (noteObject == null) return;

        // Find the active note entry that corresponds to this GameObject
        int index = activeNotes.FindIndex(n => n.gameObject == noteObject);

        if (index != -1)
        {
            // Remove it from the active list to stop it from being updated
            activeNotes.RemoveAt(index);
        }
        else
        {
            if (showDebugLogs) Debug.LogWarning($"[NoteRenderer] ProcessHitNote was called for a GameObject that is not in the activeNotes list. This can happen with rapid hits but should be monitored.");
        }

        // Now, return the object to the pool
        ReturnNoteToPool(noteObject);
    }

    public int GetActiveNoteCount() => activeNoteCount;
    public int GetPooledNoteCount() => notePool.Count;
    public int GetTotalNotesRendered() => totalNotesRendered;

    public void ClearAllNotes()
    {
        foreach (var activeNote in activeNotes)
        {
            ReturnNoteToPool(activeNote.gameObject);
        }
        activeNotes.Clear();
    }

    public void SetNoteSpeed(float speed)
    {
        // This method is obsolete as speed is now controlled by speedMultiplier.
        Debug.LogWarning("⚠️ SetNoteSpeed is deprecated - use SetSpeedMultiplier instead");
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0.1f, multiplier);
    }

    /// <summary>
    /// Calculates the time it takes for a note to travel from its spawn point to the hit line.
    /// </summary>
    /// <returns>Travel time in seconds.</returns>
    public float GetNoteTravelTime()
    {
        // Assumes a fixed spawn Z of 25f and a hit line Z of 0f.
        // This should be updated if these values become dynamic.
        float spawnZ = 25f;
        float hitZ = 0f;
        return Mathf.Abs(spawnZ - hitZ) / Mathf.Max(0.01f, speedMultiplier);
    }
    #endregion

    #region Debug Visualization

    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        // Gizmos for physical hit zones are now implicitly handled by their own GameObjects.
        // Active note debugging can be enabled here if needed.
        if (showDebugLogs && activeNotes != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var note in activeNotes)
            {
                if (note.gameObject != null)
                {
                    Gizmos.DrawWireSphere(note.currentPosition, 0.4f);
                }
            }
        }
#endif
    }
    #endregion

    void OnDestroy()
    {
        // Event subscriptions are no longer handled here.
    }
}

#region Data Structures

[System.Serializable]
public class RenderingNote
{
    public GameObject gameObject;
    public GameNoteInfo noteInfo;
    public Vector3 currentPosition;
    public float spawnTime;
    public Color baseColor;
}

#endregion