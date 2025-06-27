using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

/// <summary>
/// NoteRenderer - Visual Heart of the Game
/// Based on original WorldRenderer.java with perspective "conveyor belt" effect
/// Implements: Z-depth movement, perspective scaling, rotation effects
/// </summary>
public class NoteRenderer : MonoBehaviour
{
    [Header("🎨 Rendering Configuration")]
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private Transform noteParent;
    [SerializeField] private int laneCount = 6;
    [SerializeField] private float laneWidth = 1.8f;

    [Header("🚀 Perspective Movement (Original Algorithm)")]
    [SerializeField] private float worldDepth = 25f;           // Original: 25.0F depth
    [SerializeField] private float speedMultiplier = 0.1f;     // EXTREMELY SLOW for debugging
    [SerializeField] private float baseNoteSpeed = 0.5f;       // EXTREMELY SLOW to see notes
    [SerializeField] private bool enablePerspectiveScaling = true;
    [SerializeField] private bool enablePerspectiveRotation = true;

    [Header("🎯 Hit Zone Configuration")]
    [SerializeField] private float hitZoneZ = 3.0f;            // Original: 3.0F
    [SerializeField] private float hitZoneWidth = 10f;
    [SerializeField] private float noteDestroyZ = -5f;         // When to destroy missed notes

    [Header("📊 Performance & Debug")]
    [SerializeField] private bool enableObjectPooling = true;
    [SerializeField] private int poolSize = 50;
    [SerializeField] private bool showDebugInfo = true;  // Enable for debugging visibility issues
    [SerializeField] private bool showHitZone = true;

    // Object pooling system (from MD analysis)
    private Queue<GameObject> notePool;
    private List<RenderingNote> activeNotes;
    private int totalNotesRendered = 0;

    // Original algorithm state
    private Camera mainCamera;
    private Material noteMaterial;                            // Material for notes
    private float accInvaderY = 0f;                           // Original: accInvaderY
    private bool isNoteHighlight = false;                     // Original: isInvaderHilight
    private float noteTextureChangeTime = 0f;                 // Original: invaderTextureChangeTime

    // Lane positioning
    private Vector3[] lanePositions;
    private float worldWidth;

    // Performance tracking
    private int activeNoteCount = 0;

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

        if (showDebugInfo)
            Debug.Log("🎨 NoteRenderer initialized with original WorldRenderer algorithms");
    }

    void CheckSceneLighting()
    {
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        if (lights.Length == 0)
        {
            Debug.LogWarning("🎨 No lights found in scene! Notes may not be visible. URP/Lit material needs lighting.");
        }
        else
        {
            Debug.Log($"🎨 Found {lights.Length} lights in scene: {string.Join(", ", lights.Select(l => l.name + "(" + l.type + ")"))}");
        }

        // Check for UI Canvas that might block notes
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            Debug.Log($"🎨 Found Canvas: {canvas.name}, RenderMode: {canvas.renderMode}, SortingOrder: {canvas.sortingOrder}");
        }
    }

    void InitializeRenderer()
    {
        notePool = new Queue<GameObject>();
        activeNotes = new List<RenderingNote>();

        CreateNoteMaterial();

        if (enableObjectPooling)
            CreateNotePool();
    }

    void CreateNoteMaterial()
    {
        // Create a bright, visible material for notes - using Unlit for guaranteed visibility
        noteMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        noteMaterial.color = Color.cyan;

        // Fallback to Standard Unlit if URP not available
        if (noteMaterial.shader.name.Contains("Hidden"))
        {
            noteMaterial = new Material(Shader.Find("Unlit/Color"));
            noteMaterial.color = Color.cyan;
            Debug.Log("🎨 Created note material: Cyan Standard/Unlit (fallback)");
        }
        else
        {
            Debug.Log("🎨 Created note material: Cyan URP/Unlit");
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

        Debug.Log($"🎨 Note pool created: {poolSize} objects");
    }

    void SetupLanes()
    {
        worldWidth = laneCount * laneWidth;
        lanePositions = new Vector3[laneCount];

        for (int i = 0; i < laneCount; i++)
        {
            // Center lanes around world origin
            float xOffset = (i - (laneCount - 1) * 0.5f) * laneWidth;
            lanePositions[i] = new Vector3(xOffset, 0, 0);
        }

        if (showDebugInfo)
            Debug.Log($"🎯 Lanes setup: {laneCount} lanes, {laneWidth} width each, total world width: {worldWidth}");
    }

    void SetupCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindFirstObjectByType<Camera>();

        if (mainCamera != null)
        {
            // CRITICAL: Set to perspective mode for 3D depth effect
            bool wasOrthographic = mainCamera.orthographic;
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 60f;

            // ADJUSTED camera position for better visibility of notes
            mainCamera.transform.position = new Vector3(0, 12, -10); // Higher and further back
            mainCamera.transform.rotation = Quaternion.Euler(35, 0, 0); // Less steep angle

            Debug.Log($"🎥 Camera configured: {(wasOrthographic ? "Orthographic→" : "")}Perspective mode, FOV 60°, Position (0,12,-10), Rotation (35,0,0)");
        }
        else
        {
            Debug.LogError("🎥 No main camera found!");
        }
    }

    void SubscribeToEvents()
    {
        GameNoteCreator.OnNotesGenerated += HandleNotesGenerated;
        InputManager.OnLaneTapped += HandleLaneTapped;

        Debug.Log("🎨 NoteRenderer subscribed to events");
    }

    #region Original WorldRenderer Algorithm

    /// <summary>
    /// Original Java: renderInvaders() - Core rendering loop with perspective effects
    /// </summary>
    void Update()
    {
        float deltaTime = Time.deltaTime;

        // Debug Update method is being called when notes are active
        if (showDebugInfo && activeNotes.Count > 0 && Time.frameCount % 5 == 0)
        {
            Debug.Log($"🎨 UPDATE: {activeNotes.Count} active notes, deltaTime: {deltaTime:F3}, TimeScale: {Time.timeScale}");
        }

        UpdateNoteTextures(deltaTime);
        UpdateActiveNotes(deltaTime);
        CleanupDestroyedNotes();

        UpdateDebugInfo();

        // Debug Update loop more frequently to confirm it's running  
        if (showDebugInfo && Time.frameCount % 60 == 0) // Every second
        {
            if (activeNotes.Count > 0)
            {
                Debug.Log($"🎨 Update loop active: {activeNotes.Count} notes being processed, TimeScale: {Time.timeScale}");
            }
            else if (Time.frameCount % 300 == 0) // Every 5 seconds when no notes
            {
                Debug.Log($"🎨 Update loop running but no active notes, TimeScale: {Time.timeScale}");
            }
        }
    }

    /// <summary>
    /// Original Java: invaderTextureChangeTime logic - Note texture animation
    /// </summary>
    void UpdateNoteTextures(float deltaTime)
    {
        noteTextureChangeTime += deltaTime;
        if (noteTextureChangeTime > 0.2f) // Original: 0.2F interval
        {
            isNoteHighlight = !isNoteHighlight;
            noteTextureChangeTime = 0f;

            // Apply texture changes to all active notes
            foreach (var activeNote in activeNotes)
            {
                ApplyNoteHighlight(activeNote.gameObject, isNoteHighlight);
            }
        }
    }

    /// <summary>
    /// Original Java: updateInvaders() - Note movement and perspective calculation
    /// </summary>
    void UpdateActiveNotes(float deltaTime)
    {
        // ULTRA-AGGRESSIVE debugging - log EVERY frame when we have notes
        if (showDebugInfo && activeNotes.Count > 0)
        {
            Debug.Log($"🎨 UpdateActiveNotes called: {activeNotes.Count} notes to process, deltaTime: {deltaTime:F4}, TimeScale: {Time.timeScale}");
        }

        // CRITICAL: Check if time is paused (deltaTime will be 0)
        if (deltaTime <= 0f || Time.timeScale <= 0f)
        {
            if (showDebugInfo && activeNotes.Count > 0)
            {
                Debug.Log($"🎨 Movement paused! deltaTime: {deltaTime:F4}, TimeScale: {Time.timeScale}, {activeNotes.Count} notes waiting");
            }
            return; // Don't move notes when paused
        }

        // DISABLED: This was making notes fly upward!
        // accInvaderY += 0.01f; // Original: slight Y offset accumulation

        for (int i = activeNotes.Count - 1; i >= 0; i--) // Reverse iteration (original)
        {
            var activeNote = activeNotes[i];

            // Original movement calculation with perspective speed adjustment
            float zPosition = activeNote.currentPosition.z;
            float speedFactor = CalculateSpeedFactor(zPosition); // Original algorithm
            float currentSpeed = baseNoteSpeed * speedFactor * speedMultiplier;

            // Move note towards player (original Z-axis movement)
            float oldZ = activeNote.currentPosition.z;
            activeNote.currentPosition.z -= currentSpeed * deltaTime;

            // Apply original perspective effects
            ApplyPerspectiveEffects(activeNote, zPosition);

            // Update position
            activeNote.gameObject.transform.position = activeNote.currentPosition;

            // ULTRA-AGGRESSIVE: Debug ALL notes EVERY frame for the first 5 seconds
            if (showDebugInfo && (Time.time - activeNote.spawnTime) < 5.0f)
            {
                Debug.Log($"🎨 NOTE {i} MOVING: Z {oldZ:F2} → {activeNote.currentPosition.z:F2}, Y: {activeNote.currentPosition.y:F2}, Speed: {currentSpeed:F4}, DeltaTime: {deltaTime:F4}");
            }

            // Check if note passed hit zone or should be destroyed
            if (activeNote.currentPosition.z < noteDestroyZ)
            {
                if (showDebugInfo)
                    Debug.Log($"🎨 Note destroyed at Z={activeNote.currentPosition.z:F1} (below {noteDestroyZ})");

                HandleNoteMissed(activeNote);
                ReturnNoteToPool(activeNote.gameObject);
                activeNotes.RemoveAt(i);
            }
        }

        activeNoteCount = activeNotes.Count;
    }

    /// <summary>
    /// Original Java speed calculation: f = (invader.position.z - 3.0F) / -25.0F * this.speedMultiplier
    /// Creates accelerating perspective effect as notes approach player
    /// </summary>
    float CalculateSpeedFactor(float zPosition)
    {
        // Original algorithm - speed increases as note approaches hit zone
        float factor = (zPosition - hitZoneZ) / -worldDepth * speedMultiplier;
        return Mathf.Max(0.1f, factor); // Prevent negative or zero speed
    }

    /// <summary>
    /// Original Java: glTranslatef + glRotatef - Perspective transformation
    /// </summary>
    void ApplyPerspectiveEffects(RenderingNote activeNote, float zPosition)
    {
        var transform = activeNote.gameObject.transform;

        // Original perspective scaling
        if (enablePerspectiveScaling)
        {
            float perspective = 1.0f + zPosition * 0.1f; // Closer notes appear larger
            transform.localScale = Vector3.one * Mathf.Max(0.1f, perspective);
        }

        // Original rotation calculation: -(90.0F - 90.0F * Math.abs(invader.position.z) / 25.0F)
        if (enablePerspectiveRotation)
        {
            float rotationX = -(90f - 90f * Mathf.Abs(zPosition) / worldDepth);
            transform.rotation = Quaternion.Euler(rotationX, 0, 0);
        }

        // DISABLED: This was making notes fly upward!
        // Original Y offset accumulation for layering
        // Vector3 position = activeNote.currentPosition;
        // position.y += accInvaderY;
        // activeNote.currentPosition = position;
    }

    void ApplyNoteHighlight(GameObject noteObject, bool highlight)
    {
        var renderer = noteObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            // Simple highlight effect - could be enhanced with materials
            renderer.material.color = highlight ? Color.white : Color.gray;
        }
    }
    #endregion

    #region Note Management

    void HandleNotesGenerated(List<GameNoteInfo> notes)
    {
        Debug.Log($"🎨 NoteRenderer received {notes.Count} notes to spawn! Time: {Time.time:F1}, Frame: {Time.frameCount}");

        foreach (var note in notes)
        {
            SpawnNote(note);
        }

        Debug.Log($"🎨 Finished spawning {notes.Count} notes. Active count now: {activeNotes.Count}");
    }

    void SpawnNote(GameNoteInfo noteInfo)
    {
        GameObject noteObject = GetPooledNote();
        if (noteObject == null)
        {
            Debug.LogWarning("🎨 No pooled note available!");
            return;
        }

        // CRITICAL: Assign material to make note visible
        Renderer noteRenderer = noteObject.GetComponent<Renderer>();
        if (noteRenderer != null && noteMaterial != null)
        {
            noteRenderer.material = noteMaterial;
            Debug.Log($"🎨 Material assigned to note: {noteMaterial.color}, Enabled: {noteRenderer.enabled}");
        }
        else
        {
            Debug.LogWarning($"🎨 Material assignment failed! Renderer: {noteRenderer != null}, Material: {noteMaterial != null}");
        }

        // Position note at far end of conveyor belt
        Vector3 spawnPosition;
        if (lanePositions != null && noteInfo.idx >= 0 && noteInfo.idx < lanePositions.Length)
        {
            spawnPosition = lanePositions[noteInfo.idx];
        }
        else
        {
            // Fallback positioning
            float laneWidth = 1.8f;
            float xOffset = (noteInfo.idx - 2.5f) * laneWidth;
            spawnPosition = new Vector3(xOffset, 0, 0);
            Debug.Log($"🎨 Using fallback position for lane {noteInfo.idx}");
        }

        spawnPosition.z = worldDepth; // Start at far end (25 units away)
        spawnPosition.y = 0;

        // Make notes MUCH larger and more visible
        noteObject.transform.localScale = new Vector3(2.0f, 1.0f, 2.0f); // Much bigger for visibility
        noteObject.transform.position = spawnPosition;
        noteObject.SetActive(true);

        // Create active note tracking
        var activeNote = new RenderingNote
        {
            gameObject = noteObject,
            noteInfo = noteInfo,
            currentPosition = spawnPosition,
            spawnTime = Time.time
        };

        activeNotes.Add(activeNote);
        totalNotesRendered++;

        Debug.Log($"🎨 Note spawned: Lane {noteInfo.idx}, Position {spawnPosition}, Active: {noteObject.activeSelf}");
        Debug.Log($"🎨 Active notes count: {activeNotes.Count}, Total rendered: {totalNotesRendered}");
        Debug.Log($"🎨 Note spawned at frame {Time.frameCount}, TimeScale: {Time.timeScale}, spawnTime: {activeNote.spawnTime:F2}");
    }

    GameObject GetPooledNote()
    {
        if (enableObjectPooling && notePool.Count > 0)
        {
            Debug.Log($"🎨 Getting note from pool (remaining: {notePool.Count - 1})");
            return notePool.Dequeue();
        }
        else if (notePrefab != null && noteParent != null)
        {
            Debug.Log($"🎨 Creating new note from prefab");
            return Instantiate(notePrefab, noteParent);
        }
        else if (notePrefab == null)
        {
            Debug.LogWarning("🎨 No note prefab assigned!");
        }
        else if (noteParent == null)
        {
            Debug.LogWarning("🎨 No note parent assigned!");
        }

        Debug.LogWarning("🎨 No available notes in pool!");
        return null;
    }

    void ReturnNoteToPool(GameObject noteObject)
    {
        if (enableObjectPooling)
        {
            noteObject.SetActive(false);
            notePool.Enqueue(noteObject);
        }
        else
        {
            Destroy(noteObject);
        }
    }

    void HandleLaneTapped(int lane, Vector2 screenPosition)
    {
        // Find notes in hit zone for this lane
        var hitNotes = FindNotesInHitZone(lane);

        foreach (var activeNote in hitNotes)
        {
            HandleNoteHit(activeNote);
            ReturnNoteToPool(activeNote.gameObject);
            activeNotes.Remove(activeNote);

            // Trigger audio (interactive music creation from MD)
            TriggerNoteAudio(activeNote.noteInfo);
        }
    }

    List<RenderingNote> FindNotesInHitZone(int lane)
    {
        var hitNotes = new List<RenderingNote>();

        foreach (var activeNote in activeNotes)
        {
            if (activeNote.noteInfo.idx == lane &&
                activeNote.currentPosition.z <= hitZoneZ + hitZoneWidth * 0.5f &&
                activeNote.currentPosition.z >= hitZoneZ - hitZoneWidth * 0.5f)
            {
                hitNotes.Add(activeNote);
            }
        }

        return hitNotes;
    }

    void HandleNoteHit(RenderingNote activeNote)
    {
        // Calculate hit accuracy based on distance from perfect hit zone
        float distance = Mathf.Abs(activeNote.currentPosition.z - hitZoneZ);
        float accuracy = 1f - (distance / (hitZoneWidth * 0.5f));

        // Update game stats
        if (GameManager.Instance != null)
        {
            int score = CalculateScore(accuracy);
            GameManager.Instance.UpdateScore(score);
        }

        if (showDebugInfo)
            Debug.Log($"🎯 Note hit! Lane: {activeNote.noteInfo.idx}, Accuracy: {accuracy:F2}");
    }

    void HandleNoteMissed(RenderingNote activeNote)
    {
        if (showDebugInfo)
            Debug.Log($"❌ Note missed: Lane {activeNote.noteInfo.idx}");

        // Reset combo, reduce health, etc.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateCombo(0);
        }
    }

    void TriggerNoteAudio(GameNoteInfo noteInfo)
    {
        // Interactive music creation (MD's "secret sauce")
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayNote(
                noteInfo.instrumentType,
                noteInfo.pitch,
                1.0f
            );
        }
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

    void UpdateDebugInfo()
    {
        if (showDebugInfo && Time.frameCount % 60 == 0) // Every second
        {
            Debug.Log($"🎨 Renderer Stats: {activeNoteCount} active, {notePool.Count} pooled, {totalNotesRendered} total rendered");
        }
    }

    #region Public Interface

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
        accInvaderY = 0f;
    }

    public void SetNoteSpeed(float speed)
    {
        baseNoteSpeed = Mathf.Max(0.1f, speed);
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0.1f, multiplier);
    }
    #endregion

    #region Debug Visualization

    void OnDrawGizmos()
    {
        if (!showHitZone && !showDebugInfo) return;

        // Draw hit zone
        if (showHitZone)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < laneCount; i++)
            {
                Vector3 lanePos = lanePositions != null && i < lanePositions.Length ?
                    lanePositions[i] : new Vector3((i - (laneCount - 1) * 0.5f) * laneWidth, 0, 0);

                Vector3 hitZonePos = lanePos + Vector3.forward * hitZoneZ;

                // Draw hit zone box
                Gizmos.DrawWireCube(hitZonePos, new Vector3(laneWidth * 0.8f, 1f, hitZoneWidth));
            }

            // Draw note destroy line
            Gizmos.color = Color.red;
            Vector3 destroyLineStart = new Vector3(-worldWidth * 0.5f, 0, noteDestroyZ);
            Vector3 destroyLineEnd = new Vector3(worldWidth * 0.5f, 0, noteDestroyZ);
            Gizmos.DrawLine(destroyLineStart, destroyLineEnd);

            // Draw world depth
            Gizmos.color = Color.blue;
            Vector3 worldDepthStart = new Vector3(-worldWidth * 0.5f, 0, worldDepth);
            Vector3 worldDepthEnd = new Vector3(worldWidth * 0.5f, 0, worldDepth);
            Gizmos.DrawLine(worldDepthStart, worldDepthEnd);
        }

        // Draw active notes as spheres for debugging
        if (showDebugInfo && activeNotes != null)
        {
            Gizmos.color = Color.yellow;
            foreach (var note in activeNotes)
            {
                if (note.gameObject != null)
                {
                    Gizmos.DrawWireSphere(note.currentPosition, 0.5f);
                    // Draw note direction arrow
                    Gizmos.DrawLine(note.currentPosition, note.currentPosition + Vector3.back * 2f);
                }
            }
        }
    }
    #endregion

    void OnDestroy()
    {
        // Unsubscribe from events
        GameNoteCreator.OnNotesGenerated -= HandleNotesGenerated;
        InputManager.OnLaneTapped -= HandleLaneTapped;
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
}

#endregion