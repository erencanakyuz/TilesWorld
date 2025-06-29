using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    [SerializeField] private float laneWidth = 2.4f;       // Genişletildi: 1.8f → 2.4f

    [Header("🚀 Perspective Movement (Original Algorithm)")]
    [SerializeField] private float worldDepth = 25f;           // Original: 25.0F depth
    [SerializeField] private float speedMultiplier = 35.0f;    // *** ORİJİNAL JAVA: 35.0F ***
    [SerializeField] private bool enablePerspectiveScaling = true;
    [SerializeField] private bool enablePerspectiveRotation = true;

    [Header("🎯 Hit Zone Configuration")]
    [SerializeField] private float hitZoneZ = 0.0f;            // Hit line at Z=0 for easier calculation
    [SerializeField] private float hitZoneWidth = 2f;          // Will be set to laneWidth dynamically
    [SerializeField] private float noteDestroyZ = -20f;        // Far behind: Notes destroyed at -20f (much later)

    [Header("📊 Performance & Debug")]
    [SerializeField] private bool enableObjectPooling = true;
    [SerializeField] private int poolSize = 50;
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

        // *** FORCE FIX: Unity-adapted speed (Java 35.0f was too fast) ***
        speedMultiplier = 8.0f;
        Debug.Log($"🚀 SPEED FORCED: speedMultiplier set to {speedMultiplier} (Unity-adapted)");

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

        // Final fallback to Unity's default material
        if (noteMaterial.shader.name.Contains("Hidden"))
        {
            noteMaterial = new Material(Shader.Find("Diffuse"));
            noteMaterial.color = Color.cyan;
        }

        // Ultimate fallback - create from scratch
        if (noteMaterial.shader.name.Contains("Hidden"))
        {
            noteMaterial = new Material(Shader.Find("Standard"));
            noteMaterial.color = Color.cyan;
            noteMaterial.SetFloat("_Mode", 1); // Set to fade mode
            noteMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            noteMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        Debug.Log($"🎨 Created base material with shader: {noteMaterial.shader.name}");
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

        // Set hit zone width to match lane width exactly
        hitZoneWidth = laneWidth;

        for (int i = 0; i < laneCount; i++)
        {
            // Center lanes around world origin
            float xOffset = (i - (laneCount - 1) * 0.5f) * laneWidth;
            lanePositions[i] = new Vector3(xOffset, 0, 0);
        }

        if (showHitZone)
            Debug.Log($"🎯 Lanes setup: {laneCount} lanes, {laneWidth} width each, hitZone: {hitZoneWidth}, total world width: {worldWidth}");
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
    }

    void SubscribeToEvents()
    {
        // Using direct call from GameplayManager instead of events
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

            // --- ORİJİNAL İVMELENME MANTIĞI (Unity koordinat uyarlaması) ---
            float currentSpeed;
            if (activeNote.currentPosition.z > hitZoneZ)
            {
                // Unity: +25 (uzak) → 0 (hit zone), Java: -25 (uzak) → 0 (hit zone)
                // Java formülü: (z - 3.0F) / -25.0F * speedMultiplier
                // Unity uyarlaması: (25.0F - z - 3.0F) / 25.0F * speedMultiplier
                float javaEquivalentZ = -activeNote.currentPosition.z; // Unity +25 = Java -25
                currentSpeed = (javaEquivalentZ - 3.0f) / -25.0f * speedMultiplier;
            }
            else
            {
                // Hit zone'u geçtikten sonra sabit hız
                currentSpeed = speedMultiplier * 0.2f;
            }

            // *** UNITY COORDINATE: Move notes TOWARD player (Z decreases) ***
            float oldZ = activeNote.currentPosition.z;
            activeNote.currentPosition.z -= currentSpeed * deltaTime;

            // Minimal debug only for first note to verify system working
            if (i == 0 && totalNotesRendered <= 5)
            {
                Debug.Log($"🚀 NOTE MOVE: Z {oldZ:F1} → {activeNote.currentPosition.z:F1}, speed={currentSpeed:F1}");
            }

            // Update Unity transform directly (no coordinate conversion needed)
            activeNote.gameObject.transform.position = activeNote.currentPosition;

            // *** OTOMATIK DESTROY KAPATILDI! (TEST AMAÇLI) ***
            /*
            if (activeNote.currentPosition.z < noteDestroyZ)
            {
                if (totalNotesRendered <= 15)
                {
                    Debug.Log($"💥 NOTE DESTROYED: Z={activeNote.currentPosition.z:F1}, destroyZ={noteDestroyZ} (notes pass -20f)");
                }
                HandleNoteMissed(activeNote);
                ReturnNoteToPool(activeNote.gameObject);
                activeNotes.RemoveAt(i);
                continue;
            }
            */

            // Apply perspective effects
            ApplyPerspectiveEffects(activeNote, activeNote.currentPosition.z);
        }
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
            // Simple highlight effect - enhance existing color
            Color baseColor = renderer.material.color;
            renderer.material.color = highlight ? Color.Lerp(baseColor, Color.white, 0.3f) : baseColor;
        }
    }

    /// <summary>
    /// Get material for specific pitch and instrument
    /// </summary>
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
        indicatorRenderer.material = GetMaterialForPitch(pitch, InstrumentType.Piano);

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

        // *** CRITICAL: Ensure note has material ***
        Renderer noteRenderer = noteObject.GetComponent<Renderer>();
        if (noteRenderer != null)
        {
            // Always assign material to ensure visibility
            Material pitchMaterial = GetMaterialForPitch(noteInfo.pitch, noteInfo.instrumentType);
            noteRenderer.material = pitchMaterial;

            // Double-check material is applied
            if (noteRenderer.material == null)
            {
                Debug.LogError("🚨 Material assignment failed! Using fallback...");
                noteRenderer.material = noteMaterial; // Use base material as fallback
            }
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
        spawnPosition.z = worldDepth; // Start far from player (+25.0f)
        spawnPosition.y = 0;

        // Set note properties - fit exactly to lane width
        float noteScale = laneWidth * 0.7f; // %70 of lane width for nice fit
        noteObject.transform.localScale = new Vector3(noteScale, 1.0f, noteScale);
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

        // Debug only for first few spawns to check for duplicates
        if (totalNotesRendered <= 10)
        {
            Debug.Log($"🎵 SPAWN #{totalNotesRendered}: Lane {noteInfo.idx}, Pitch {noteInfo.pitch}, Pos {spawnPosition}");
        }
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
        Debug.Log($"🎯 LANE TAPPED: Lane {lane}, Finding notes in hit zone... ACTIVE NOTES: {activeNotes.Count}");

        // Find notes in hit zone for this lane
        var candidateNotes = FindNotesInHitZone(lane);

        Debug.Log($"🎯 Found {candidateNotes.Count} candidate notes in lane {lane}");

        if (candidateNotes.Count == 0)
        {
            Debug.Log($"🎯 NO NOTES IN HIT ZONE for lane {lane} - but firing empty hit anyway");
            // Even if no notes, show some feedback to player
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowHitEffect(HitAccuracy.Miss, screenPosition);
            }
            return;
        }

        // Get closest note (like original Java onTap logic)
        var bestNote = candidateNotes.OrderBy(n => Mathf.Abs(n.currentPosition.z - hitZoneZ)).FirstOrDefault();
        if (bestNote == null) return;

        float hitPos = bestNote.currentPosition.z;

        Debug.Log($"🎯 BEST NOTE: Lane {lane}, Z-pos {hitPos:F2}, Hit Zone: {hitZoneZ}");

        // 🎯 ORIGINAL JAVA HIT DETECTION - LAYERED TIMING WINDOWS
        // Hit windows are relative to hitZoneZ (0.0f = perfect hit line)
        // Calculate distance from hit zone center
        float distanceFromHitZone = Mathf.Abs(hitPos - hitZoneZ);

        HitAccuracy hitAccuracy;
        int score;

        // Layered hit windows - closer to hit zone = better accuracy
        if (distanceFromHitZone <= 0.8f)
        {
            // PERFECT hit zone (very close to hit line)
            hitAccuracy = HitAccuracy.Perfect;
            score = 300;
        }
        else if (distanceFromHitZone <= 1.5f)
        {
            // GOOD hit zone 
            hitAccuracy = HitAccuracy.Good;
            score = 200;
        }
        else if (distanceFromHitZone <= 3.0f)
        {
            // OKAY hit zone (using Miss as placeholder)
            hitAccuracy = HitAccuracy.Miss;
            score = 100;
        }
        else
        {
            // Outside hit window - no action taken (like original Java)
            Debug.Log($"🎯 Note outside hit window: Z={hitPos:F2}, distance from hit zone={distanceFromHitZone:F2}, ignoring hit");
            return;
        }

        Debug.Log($"✅ HIT SUCCESS: Lane {lane}, Accuracy {hitAccuracy}, Score {score}, Z-pos {hitPos:F2}");

        // Process the hit with original Java timing precision
        ProcessNoteHit(bestNote, score, hitAccuracy, screenPosition);

        // Trigger audio (interactive music creation)
        TriggerNoteAudio(bestNote.noteInfo);

        // Remove note from game
        ReturnNoteToPool(bestNote.gameObject);
        activeNotes.Remove(bestNote);
    }

    /// <summary>
    /// Process note hit with original Java accuracy calculation
    /// </summary>
    void ProcessNoteHit(RenderingNote activeNote, int score, HitAccuracy accuracy, Vector2 screenPosition)
    {
        // Update game stats like original Java
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateScore(score);

            // Update combo based on accuracy
            if (accuracy == HitAccuracy.Perfect || accuracy == HitAccuracy.Good)
            {
                GameManager.Instance.UpdateCombo(1); // Increase combo
            }
            else
            {
                GameManager.Instance.UpdateCombo(0); // Reset combo for poor hits
            }
        }

        // Trigger visual effect 
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowHitEffect(accuracy, screenPosition);
        }
    }

    List<RenderingNote> FindNotesInHitZone(int lane)
    {
        var hitNotes = new List<RenderingNote>();

        Debug.Log($"🔍 Finding notes in hit zone for lane {lane}:");
        Debug.Log($"🔍 Total active notes: {activeNotes.Count}");
        Debug.Log($"🔍 Hit zone Z: {hitZoneZ}, Detection range: ±4.0f");

        foreach (var activeNote in activeNotes)
        {
            Debug.Log($"🔍 Note in lane {activeNote.noteInfo.idx}, Z: {activeNote.currentPosition.z:F2}");

            if (activeNote.noteInfo.idx == lane)
            {
                // Hit zone detection - allow notes within reasonable hitting distance
                float distanceFromHitZone = Mathf.Abs(activeNote.currentPosition.z - hitZoneZ);

                Debug.Log($"🔍 LANE MATCH! Distance from hit zone: {distanceFromHitZone:F2}");

                // Allow notes within expanded detection zone for better user experience
                if (distanceFromHitZone <= 4.0f) // Generous detection zone (was 2.0f)
                {
                    Debug.Log($"✅ NOTE IN HIT ZONE! Adding to candidates");
                    hitNotes.Add(activeNote);
                }
                else
                {
                    Debug.Log($"❌ Note too far from hit zone: {distanceFromHitZone:F2} > 4.0");
                }
            }
        }

        return hitNotes;
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
            InteractiveMusicSystem.Instance.TriggerNoteAudio(noteInfo);
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
        // DEPRECATED: baseNoteSpeed removed - using speedMultiplier directly
        Debug.LogWarning("⚠️ SetNoteSpeed is deprecated - use SetSpeedMultiplier instead");
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Max(0.1f, multiplier);
    }
    #endregion

    #region Debug Visualization

    void OnDrawGizmos()
    {
        if (!showHitZone) return;

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
        if (activeNotes != null)
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