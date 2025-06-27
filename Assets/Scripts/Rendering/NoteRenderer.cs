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
    [SerializeField] private float speedMultiplier = 0.1f;     // Debug: extremely slow for visibility
    [SerializeField] private float baseNoteSpeed = 0.5f;       // Base movement speed
    [SerializeField] private bool enablePerspectiveScaling = true;
    [SerializeField] private bool enablePerspectiveRotation = true;

    [Header("🎯 Hit Zone Configuration")]
    [SerializeField] private float hitZoneZ = 3.0f;            // Original: 3.0F
    [SerializeField] private float hitZoneWidth = 2f;          // Reduced for better precision
    [SerializeField] private float noteDestroyZ = -2f;         // Closer destruction for better performance

    [Header("📊 Performance & Debug")]
    [SerializeField] private bool enableObjectPooling = true;
    [SerializeField] private int poolSize = 50;
    [SerializeField] private bool showDebugInfo = false; // Debug disabled for performance
    [SerializeField] private bool showHitZone = true;

    // Object pooling system (from MD analysis)
    private Queue<GameObject> notePool;
    private List<RenderingNote> activeNotes;
    private int totalNotesRendered = 0;

    // Original algorithm state
    private Camera mainCamera;
    private Material noteMaterial;                            // Material for notes
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

        if (enableObjectPooling)
            CreateNotePool();
    }

    void CreateNoteMaterial()
    {
        // Create a bright, visible material for notes
        noteMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        noteMaterial.color = Color.cyan;

        // Fallback to Standard Unlit if URP not available
        if (noteMaterial.shader.name.Contains("Hidden"))
        {
            noteMaterial = new Material(Shader.Find("Unlit/Color"));
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
            // Minimal camera adjustments - keep original table/ground appearance
            if (mainCamera.orthographic)
            {
                mainCamera.orthographic = false; // Enable perspective for 3D depth
                mainCamera.fieldOfView = 60f;
            }

            // Only adjust if camera is at default position (don't override custom setups)
            if (mainCamera.transform.position == Vector3.zero || mainCamera.transform.position.z >= 0)
            {
                mainCamera.transform.position = new Vector3(0, 8, -8); // Less aggressive positioning
                mainCamera.transform.rotation = Quaternion.Euler(25, 0, 0); // Gentler angle
            }

            Debug.Log($"🎥 Camera setup: Position {mainCamera.transform.position}, Rotation {mainCamera.transform.rotation.eulerAngles}");
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
    }

    #region Original WorldRenderer Algorithm

    /// <summary>
    /// Original Java: renderInvaders() - Core rendering loop with perspective effects
    /// </summary>
    void Update()
    {
        float deltaTime = Time.deltaTime;

        UpdateNoteTextures(deltaTime);
        UpdateActiveNotes(deltaTime);
        CleanupDestroyedNotes();

        UpdateDebugInfo();
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
        if (deltaTime <= 0f || Time.timeScale <= 0f)
        {
            return;
        }

        for (int i = activeNotes.Count - 1; i >= 0; i--)
        {
            var activeNote = activeNotes[i];
            if (activeNote == null || activeNote.gameObject == null || !activeNote.gameObject.activeInHierarchy) continue;

            // Move note towards player (positive Z to negative Z)
            activeNote.currentPosition.z -= baseNoteSpeed * speedMultiplier * deltaTime;

            // Keep note stable in X and Y (no drifting or falling)
            Vector3 stablePosition = activeNote.currentPosition;
            stablePosition.y = 0f; // Always at ground level
            activeNote.currentPosition = stablePosition;

            // Apply position to GameObject
            activeNote.gameObject.transform.position = activeNote.currentPosition;

            // Remove notes that have passed the hit zone
            if (activeNote.currentPosition.z <= noteDestroyZ)
            {
                ReturnNoteToPool(activeNote.gameObject);
                activeNotes.RemoveAt(i);
                continue;
            }

            // Apply perspective effects while moving
            ApplyPerspectiveEffects(activeNote, activeNote.currentPosition.z);
        }
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
    /// Simplified perspective effects - keep notes as stable rectangular tiles
    /// </summary>
    void ApplyPerspectiveEffects(RenderingNote activeNote, float zPosition)
    {
        var transform = activeNote.gameObject.transform;

        // Subtle perspective scaling only (closer notes slightly larger)
        if (enablePerspectiveScaling)
        {
            float distanceFromPlayer = Mathf.Abs(zPosition - hitZoneZ);
            float scale = Mathf.Lerp(2.5f, 2.0f, distanceFromPlayer / worldDepth);
            transform.localScale = new Vector3(scale, 1.0f, scale);
        }

        // Keep rotation minimal - notes should stay as flat tiles
        if (enablePerspectiveRotation)
        {
            // Very slight rotation for visual depth (much less than original)
            float rotationX = Mathf.Lerp(0f, -10f, Mathf.Abs(zPosition) / worldDepth);
            transform.rotation = Quaternion.Euler(rotationX, 0, 0);
        }
        else
        {
            // Keep notes completely flat (no rotation)
            transform.rotation = Quaternion.identity;
        }

        // Keep Y position stable at ground level - no accumulation
        Vector3 position = transform.position;
        position.y = 0f;
        transform.position = position;
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
        // Processing notes (debug removed for performance)

        foreach (var note in notes)
        {
            SpawnNote(note);
        }
    }

    void SpawnNote(GameNoteInfo noteInfo)
    {
        // Spawning note (debug removed)

        GameObject noteObject = GetPooledNote();
        if (noteObject == null)
        {
            Debug.LogWarning("🎨 No pooled note available! Creating fallback cube...");
            // Create a fallback cube note if no prefab is available
            noteObject = CreateFallbackNote();
            if (noteObject == null)
            {
                Debug.LogError("🎨 Failed to create fallback note!");
                return;
            }
        }

        // Set up note renderer
        Renderer noteRenderer = noteObject.GetComponent<Renderer>();
        if (noteRenderer != null && noteMaterial != null)
        {
            noteRenderer.material = noteMaterial;
        }
        else if (noteRenderer != null)
        {
            // Fallback material
            noteRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            noteRenderer.material.color = Color.cyan;
        }

        // Calculate spawn position
        Vector3 spawnPosition;
        if (lanePositions != null && noteInfo.idx >= 0 && noteInfo.idx < lanePositions.Length)
        {
            spawnPosition = lanePositions[noteInfo.idx];
        }
        else
        {
            float laneWidth = 1.8f;
            float xOffset = (noteInfo.idx - 2.5f) * laneWidth;
            spawnPosition = new Vector3(xOffset, 0, 0);
        }

        // Position note at back of world (far from camera)
        spawnPosition.z = worldDepth;
        spawnPosition.y = 0;

        // Set note properties
        noteObject.transform.localScale = new Vector3(2.0f, 1.0f, 2.0f);
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

        // Note spawned successfully
    }

    GameObject CreateFallbackNote()
    {
        // Create a simple cube as fallback note
        GameObject fallbackNote = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // Set up parent if available
        if (noteParent != null)
        {
            fallbackNote.transform.SetParent(noteParent);
        }

        // Remove collider to avoid physics issues
        var collider = fallbackNote.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }

        fallbackNote.name = "FallbackNote";
        return fallbackNote;
    }

    GameObject GetPooledNote()
    {
        if (enableObjectPooling && notePool.Count > 0)
        {
            return notePool.Dequeue();
        }
        else if (notePrefab != null && noteParent != null)
        {
            return Instantiate(notePrefab, noteParent);
        }

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
        // Removed debug spam
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