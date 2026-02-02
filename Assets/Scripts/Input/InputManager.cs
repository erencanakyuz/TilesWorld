using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Collections.Generic;
using System;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// InputManager - EnhancedTouch based input handling for rhythm game.
/// Uses Unity's recommended EnhancedTouch API that protects against losing short taps.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("Lane Configuration")]
    [SerializeField] private int laneCount = 6;
    [Tooltip("Hit zone Y position - must match HitZoneTrigger objects in scene")]
    [SerializeField] private float hitZoneY = 0.45f;

    [Header("Hold Note Settings")]
    [SerializeField] private float holdTimeThreshold = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool measureLatency = false;
    [SerializeField] private bool logTouchEvents = false;

    // Input Events - fired for gameplay
    public delegate void LaneTapHandler(int lane, Vector2 screenPos);
    public static event LaneTapHandler OnLaneTapped;
    public static event Action<int, float> OnLaneHeld;
    public static event Action<int> OnLaneReleased;

    // Touch tracking - maps fingerId to lane
    private Dictionary<int, int> fingerToLane = new Dictionary<int, int>();
    private HashSet<int> activeLanes = new HashSet<int>();

    // Mouse tracking (simulates touch)
    private const int MOUSE_FINGER_ID = -999;
    private bool isMouseDown = false;
    private int lastMouseLane = -1;

    // Camera for screen-to-world conversion
    private Camera mainCamera;
    private Vector3[] laneWorldPositions;

    // Latency measurement
    private float[] latencySamples = new float[30];
    private int latencySampleIndex = 0;
    private int latencySampleCount = 0;

    #region Unity Lifecycle

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Get camera early
            mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        // Enable EnhancedTouch - CRITICAL for reliable touch detection
        EnhancedTouchSupport.Enable();
        
        // Set low-latency mode
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        
        if (Application.isMobilePlatform)
        {
            InputSystem.pollingFrequency = 120f;
        }
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Start()
    {
        SetupLanePositions();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        fingerToLane?.Clear();
        activeLanes?.Clear();
    }

    void Update()
    {
        // Ensure camera is available
        if (mainCamera == null)
        {
            mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();
            if (mainCamera == null) return;
        }

        // Process all input sources
        ProcessEnhancedTouches();
        ProcessKeyboardInput();
        ProcessMouseInput();
        ProcessHoldNotes();
    }

    #endregion

    #region Lane Setup

    void SetupLanePositions()
    {
        laneWorldPositions = new Vector3[laneCount];
        for (int i = 0; i < laneCount; i++)
        {
            float xOffset = GameConstants.GetLaneXPosition(i);
            laneWorldPositions[i] = new Vector3(xOffset, 0, 0);
        }
    }

    #endregion

    #region EnhancedTouch Processing

    /// <summary>
    /// Process touches using EnhancedTouch API.
    /// This API protects against losing short taps that begin and end in the same frame.
    /// </summary>
    void ProcessEnhancedTouches()
    {
        // Use Touch.activeTouches - the recommended way to read touches
        foreach (var touch in Touch.activeTouches)
        {
            int fingerId = touch.finger.index;
            Vector2 screenPos = touch.screenPosition;
            int lane = ScreenPositionToLane(screenPos);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnFingerDown(fingerId, screenPos, lane);
                    break;

                case TouchPhase.Moved:
                    OnFingerMoved(fingerId, screenPos, lane);
                    break;

                case TouchPhase.Stationary:
                    // Hold is processed separately in ProcessHoldNotes
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    OnFingerUp(fingerId, lane);
                    break;
            }
        }
    }

    #endregion

    #region Core Input Handlers

    /// <summary>
    /// Called when a finger touches the screen or mouse clicks.
    /// ALWAYS fires OnLaneTapped - no blocking conditions.
    /// </summary>
    void OnFingerDown(int fingerId, Vector2 screenPos, int lane)
    {
        if (lane < 0 || lane >= laneCount) return;

        float inputTime = Time.realtimeSinceStartup;

        // Track this finger's lane
        fingerToLane[fingerId] = lane;
        activeLanes.Add(lane);

        // ALWAYS fire tap event - this is critical for rhythm games
        OnLaneTapped?.Invoke(lane, screenPos);

        // Log if enabled
        if (logTouchEvents)
        {
            Debug.Log($"[INPUT] Finger {fingerId} DOWN on Lane {lane}");
        }

        // Measure latency if enabled
        if (measureLatency)
        {
            float latency = (Time.realtimeSinceStartup - inputTime) * 1000f;
            RecordLatency(latency);
            Debug.Log($"[LATENCY] Lane {lane} - {latency:F3}ms");
        }
    }

    /// <summary>
    /// Called when finger moves. Triggers lanes during swipe.
    /// Simple logic: if lane changed, fire tap for the new lane.
    /// </summary>
    void OnFingerMoved(int fingerId, Vector2 screenPos, int currentLane)
    {
        if (currentLane < 0 || currentLane >= laneCount) return;

        // Get previous lane for this finger
        if (!fingerToLane.TryGetValue(fingerId, out int previousLane))
        {
            // First time seeing this finger in Moved state - treat as new touch
            OnFingerDown(fingerId, screenPos, currentLane);
            return;
        }

        // If lane changed, trigger all lanes between old and new
        if (currentLane != previousLane)
        {
            int step = (currentLane > previousLane) ? 1 : -1;
            
            // Fire OnLaneTapped for each lane we swipe through
            for (int lane = previousLane + step; lane != currentLane + step; lane += step)
            {
                if (lane >= 0 && lane < laneCount)
                {
                    OnLaneTapped?.Invoke(lane, screenPos);
                    
                    if (logTouchEvents)
                    {
                        Debug.Log($"[INPUT] Swipe through Lane {lane}");
                    }
                }
            }

            // Update finger's current lane
            fingerToLane[fingerId] = currentLane;
            activeLanes.Add(currentLane);
        }
    }

    /// <summary>
    /// Called when finger lifts from screen.
    /// </summary>
    void OnFingerUp(int fingerId, int lane)
    {
        if (fingerToLane.TryGetValue(fingerId, out int trackedLane))
        {
            fingerToLane.Remove(fingerId);
            
            // Check if any other finger is still on this lane
            bool laneStillActive = false;
            foreach (var kvp in fingerToLane)
            {
                if (kvp.Value == trackedLane)
                {
                    laneStillActive = true;
                    break;
                }
            }

            if (!laneStillActive)
            {
                activeLanes.Remove(trackedLane);
                OnLaneReleased?.Invoke(trackedLane);
            }

            if (logTouchEvents)
            {
                Debug.Log($"[INPUT] Finger {fingerId} UP from Lane {trackedLane}");
            }
        }
    }

    #endregion

    #region Hold Note Processing

    void ProcessHoldNotes()
    {
        foreach (var kvp in fingerToLane)
        {
            int fingerId = kvp.Key;
            int lane = kvp.Value;

            // Find the touch to get hold duration
            foreach (var touch in Touch.activeTouches)
            {
                if (touch.finger.index == fingerId)
                {
                    float holdTime = (float)(touch.time - touch.startTime);
                    if (holdTime > holdTimeThreshold)
                    {
                        OnLaneHeld?.Invoke(lane, holdTime);
                    }
                    break;
                }
            }
        }

        // Also check mouse hold
        if (isMouseDown && lastMouseLane >= 0)
        {
            // Mouse hold time would need separate tracking - simplified for now
            OnLaneHeld?.Invoke(lastMouseLane, Time.deltaTime);
        }
    }

    #endregion

    #region Mouse Input (PC Testing)

    /// <summary>
    /// Mouse input uses same handlers as touch for identical behavior.
    /// </summary>
    void ProcessMouseInput()
    {
        if (Mouse.current == null) return;

        var mouse = Mouse.current;
        Vector2 mousePos = mouse.position.ReadValue();
        int lane = ScreenPositionToLane(mousePos);

        // Mouse down - same as finger down
        if (mouse.leftButton.wasPressedThisFrame)
        {
            isMouseDown = true;
            lastMouseLane = lane;
            OnFingerDown(MOUSE_FINGER_ID, mousePos, lane);
        }
        // Mouse held and moved - same as finger moved
        else if (mouse.leftButton.isPressed && isMouseDown)
        {
            OnFingerMoved(MOUSE_FINGER_ID, mousePos, lane);
        }
        // Mouse up - same as finger up
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            isMouseDown = false;
            OnFingerUp(MOUSE_FINGER_ID, lane);
            lastMouseLane = -1;
        }
    }

    #endregion

    #region Keyboard Input

    void ProcessKeyboardInput()
    {
        if (Keyboard.current == null) return;

        var keyboard = Keyboard.current;

        // Q W E R T Y for lanes 0-5
        if (keyboard.qKey.wasPressedThisFrame) FireLaneTap(0);
        if (keyboard.wKey.wasPressedThisFrame) FireLaneTap(1);
        if (keyboard.eKey.wasPressedThisFrame) FireLaneTap(2);
        if (keyboard.rKey.wasPressedThisFrame) FireLaneTap(3);
        if (keyboard.tKey.wasPressedThisFrame) FireLaneTap(4);
        if (keyboard.yKey.wasPressedThisFrame) FireLaneTap(5);
    }

    void FireLaneTap(int lane)
    {
        if (lane >= 0 && lane < laneCount)
        {
            Vector2 screenPos = LaneToScreenPosition(lane);
            OnLaneTapped?.Invoke(lane, screenPos);
        }
    }

    #endregion

    #region Screen-to-Lane Conversion

    int ScreenPositionToLane(Vector2 screenPosition)
    {
        if (mainCamera == null || laneWorldPositions == null || laneWorldPositions.Length == 0)
        {
            return 0;
        }

        // Ray from screen position
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        // Intersect with hit zone plane (Y = hitZoneY)
        if (Mathf.Approximately(ray.direction.y, 0f)) return 0;
        
        float distanceToPlane = (hitZoneY - ray.origin.y) / ray.direction.y;
        if (distanceToPlane < 0) return 0;
        
        Vector3 worldPosition = ray.origin + ray.direction * distanceToPlane;

        // Find closest lane
        int closestLane = 0;
        float closestDistance = Mathf.Abs(worldPosition.x - laneWorldPositions[0].x);

        for (int i = 1; i < laneWorldPositions.Length; i++)
        {
            float distance = Mathf.Abs(worldPosition.x - laneWorldPositions[i].x);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestLane = i;
            }
        }

        return closestLane;
    }

    Vector2 LaneToScreenPosition(int lane)
    {
        float normalizedX = (lane + 0.5f) / laneCount;
        return new Vector2(normalizedX * Screen.width, Screen.height * 0.5f);
    }

    #endregion

    #region Public API

    public bool IsLaneActive(int lane) => activeLanes.Contains(lane);
    public int GetActiveLaneCount() => activeLanes.Count;
    public List<int> GetActiveLanes() => new List<int>(activeLanes);
    public void SetLaneCount(int count)
    {
        laneCount = Mathf.Clamp(count, 1, 10);
        SetupLanePositions();
    }

    #endregion

    #region Latency Measurement

    void RecordLatency(float latency)
    {
        latencySamples[latencySampleIndex] = latency;
        latencySampleIndex = (latencySampleIndex + 1) % latencySamples.Length;
        if (latencySampleCount < latencySamples.Length)
        {
            latencySampleCount++;
        }
    }

    public float GetAverageLatency()
    {
        if (latencySampleCount == 0) return 0f;
        float sum = 0f;
        for (int i = 0; i < latencySampleCount; i++)
        {
            sum += latencySamples[i];
        }
        return sum / latencySampleCount;
    }

    void OnGUI()
    {
        if (!measureLatency) return;

        float avgLatency = GetAverageLatency();
        GUI.Box(new Rect(10, 10, 300, 80), "Input Stats");
        GUI.Label(new Rect(20, 35, 280, 20), $"Average Latency: {avgLatency:F2}ms");
        GUI.Label(new Rect(20, 55, 280, 20), $"Active Touches: {Touch.activeTouches.Count}");
        GUI.Label(new Rect(20, 75, 280, 20), $"Active Lanes: {activeLanes.Count}");
    }

    #endregion

    #region Settings Persistence

    public void SaveInputSettings()
    {
        PlayerPrefs.Save();
    }

    #endregion
}

[System.Serializable]
public class TouchData
{
    public int touchId;
    public Vector2 startPosition;
    public Vector2 currentPosition;
    public float startTime;
    public float inputTime;
    public int lane;
    public bool isActive;
}
