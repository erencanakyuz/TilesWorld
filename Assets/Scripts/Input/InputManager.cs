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
    [Tooltip("Minimum time between OnLaneHeld ticks (seconds).")]
    [SerializeField] private float holdTickInterval = 0.05f;

    [Header("Swipe / Lane Change (DPI-aware)")]
    [Tooltip("Minimum movement in mm to register a lane change (DPI-aware).")]
    [SerializeField] private float deadzoneMm = 2.0f;
    [SerializeField] private float fallbackDpi = 300f;

    [Header("Debug")]
    [SerializeField] private bool measureLatency = false;
    [SerializeField] private bool logTouchEvents = false;

    // Input Events - fired for gameplay (backward compatible)
    public delegate void LaneTapHandler(int lane, Vector2 screenPos);
    public static event LaneTapHandler OnLaneTapped;
    public static event Action<int, float> OnLaneHeld;
    public static event Action<int> OnLaneReleased;
    
    // Timestamped events for DSP-accurate timing (use with RhythmTimingSystem)
    // inputTime is from touch.time or Time.realtimeSinceStartupAsDouble
    public delegate void TimestampedTapHandler(int lane, Vector2 screenPos, double inputTime);
    public static event TimestampedTapHandler OnLaneTappedTimestamped;
    public static event Action<int, float, double> OnLaneHeldTimestamped;  // lane, holdDuration, inputTime
    public static event Action<int, double> OnLaneReleasedTimestamped;     // lane, inputTime

    // Per-finger state array (zero GC, fast access by finger.index)
    private const int MaxFingers = 16;
    
    private struct FingerState
    {
        public bool down;
        public int lane;
        public Vector2 lastPos;
        public double nextHoldTickTime;  // input timestamp based
        public double lastSeenTime;      // for hold processing
    }
    
    private FingerState[] fingerStates = new FingerState[MaxFingers];
    private HashSet<int> activeLanes = new HashSet<int>();

    // Mouse tracking (simulates finger index 15)
    private const int MOUSE_FINGER_INDEX = 15;
    private bool isMouseDown = false;
    private double mouseDownTime = 0;

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
        // Reset all finger states
        for (int i = 0; i < MaxFingers; i++)
        {
            fingerStates[i] = default;
        }
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

        // Process all input sources in one pass
        ProcessEnhancedTouches();
        ProcessKeyboardInput();
        ProcessMouseInput();
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
    /// Uses input timestamps for frame-rate independent judgment.
    /// </summary>
    void ProcessEnhancedTouches()
    {
        // Use Touch.activeTouches - the recommended way to read touches
        foreach (var touch in Touch.activeTouches)
        {
            int fingerIndex = touch.finger.index;
            Vector2 screenPos = touch.screenPosition;
            int lane = ScreenPositionToLane(screenPos);
            double inputTime = touch.time;

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleFingerDown(fingerIndex, screenPos, lane, inputTime);
                    break;

                case TouchPhase.Moved:
                    HandleFingerMove(fingerIndex, screenPos, lane, inputTime);
                    ProcessHoldTick(fingerIndex, inputTime);
                    break;

                case TouchPhase.Stationary:
                    // Update lastSeenTime for hold processing
                    if ((uint)fingerIndex < MaxFingers && fingerStates[fingerIndex].down)
                    {
                        fingerStates[fingerIndex].lastSeenTime = inputTime;
                    }
                    ProcessHoldTick(fingerIndex, inputTime);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    HandleFingerUp(fingerIndex, inputTime);
                    break;
            }
        }
    }

    #endregion

    #region Core Input Handlers

    /// <summary>
    /// Called when a finger touches the screen.
    /// Uses finger index for zero-GC array access.
    /// ALWAYS fires OnLaneTapped - critical for rhythm games.
    /// </summary>
    void HandleFingerDown(int fingerIndex, Vector2 screenPos, int lane, double inputTime)
    {
        if ((uint)fingerIndex >= MaxFingers) return;
        if (lane < 0 || lane >= laneCount) return;

        ref FingerState state = ref fingerStates[fingerIndex];
        state.down = true;
        state.lane = lane;
        state.lastPos = screenPos;
        state.nextHoldTickTime = inputTime + holdTickInterval;
        state.lastSeenTime = inputTime;
        
        activeLanes.Add(lane);

        // ALWAYS fire tap event - critical for rhythm games
        OnLaneTapped?.Invoke(lane, screenPos);
        OnLaneTappedTimestamped?.Invoke(lane, screenPos, inputTime);

        if (logTouchEvents)
        {
            Debug.Log($"[INPUT] Finger {fingerIndex} DOWN on Lane {lane}");
        }

        if (measureLatency)
        {
            float latency = (float)((Time.realtimeSinceStartupAsDouble - inputTime) * 1000.0);
            RecordLatency(latency);
            Debug.Log($"[LATENCY] Lane {lane} - {latency:F3}ms");
        }
    }

    /// <summary>
    /// Called when finger moves. Triggers intermediate lanes during swipe.
    /// Uses mm-based deadzone for DPI-aware behavior.
    /// </summary>
    void HandleFingerMove(int fingerIndex, Vector2 screenPos, int currentLane, double inputTime)
    {
        if ((uint)fingerIndex >= MaxFingers) return;
        if (currentLane < 0 || currentLane >= laneCount) return;

        ref FingerState state = ref fingerStates[fingerIndex];
        
        // If not tracked, treat as new touch
        if (!state.down)
        {
            HandleFingerDown(fingerIndex, screenPos, currentLane, inputTime);
            return;
        }

        state.lastSeenTime = inputTime;
        int previousLane = state.lane;

        // Apply mm-based deadzone
        float dz = DeadzonePixels();
        if ((screenPos - state.lastPos).sqrMagnitude < dz * dz)
        {
            return;
        }

        // If lane changed, emit intermediate lanes
        if (currentLane != previousLane)
        {
            EmitIntermediateLanes(previousLane, currentLane, screenPos, inputTime);
            
            // Update state
            state.lane = currentLane;
            state.lastPos = screenPos;
            activeLanes.Add(currentLane);

            // Check if previous lane should be released
            if (!IsLaneStillActive(previousLane, fingerIndex))
            {
                activeLanes.Remove(previousLane);
                OnLaneReleased?.Invoke(previousLane);
                OnLaneReleasedTimestamped?.Invoke(previousLane, inputTime);
            }
        }
        else
        {
            state.lastPos = screenPos;
        }
    }

    /// <summary>
    /// Emit tap events for all lanes between from and to (exclusive of from, inclusive of to).
    /// </summary>
    void EmitIntermediateLanes(int from, int to, Vector2 screenPos, double inputTime = 0)
    {
        if (inputTime == 0) inputTime = Time.realtimeSinceStartupAsDouble;
        
        int dir = System.Math.Sign(to - from);
        int lane = from;
        while (lane != to)
        {
            lane += dir;
            if (lane >= 0 && lane < laneCount)
            {
                OnLaneTapped?.Invoke(lane, screenPos);
                OnLaneTappedTimestamped?.Invoke(lane, screenPos, inputTime);
                
                if (logTouchEvents)
                {
                    Debug.Log($"[INPUT] Swipe through Lane {lane}");
                }
            }
        }
    }

    /// <summary>
    /// Check if any other finger is still on the given lane.
    /// </summary>
    bool IsLaneStillActive(int lane, int exceptFingerIndex)
    {
        for (int i = 0; i < MaxFingers; i++)
        {
            if (i == exceptFingerIndex) continue;
            if (fingerStates[i].down && fingerStates[i].lane == lane) return true;
        }
        return false;
    }

    /// <summary>
    /// Process hold tick using input timestamps for deterministic cadence.
    /// </summary>
    void ProcessHoldTick(int fingerIndex, double currentTime)
    {
        if ((uint)fingerIndex >= MaxFingers) return;
        
        ref FingerState state = ref fingerStates[fingerIndex];
        if (!state.down) return;

        float holdTime = (float)(currentTime - state.lastSeenTime + holdTickInterval);
        if (holdTime <= holdTimeThreshold) return;

        while (currentTime >= state.nextHoldTickTime)
        {
            float tickHoldTime = (float)(state.nextHoldTickTime - state.lastSeenTime);
            OnLaneHeld?.Invoke(state.lane, tickHoldTime);
            OnLaneHeldTimestamped?.Invoke(state.lane, tickHoldTime, state.nextHoldTickTime);
            state.nextHoldTickTime += holdTickInterval;
        }
    }

    /// <summary>
    /// Called when finger lifts from screen.
    /// </summary>
    void HandleFingerUp(int fingerIndex, double inputTime)
    {
        if ((uint)fingerIndex >= MaxFingers) return;

        ref FingerState state = ref fingerStates[fingerIndex];
        if (!state.down) return;

        int trackedLane = state.lane;
        state.down = false;

        // Check if any other finger is still on this lane
        if (!IsLaneStillActive(trackedLane, fingerIndex))
        {
            activeLanes.Remove(trackedLane);
            OnLaneReleased?.Invoke(trackedLane);
            OnLaneReleasedTimestamped?.Invoke(trackedLane, inputTime);
        }

        if (logTouchEvents)
        {
            Debug.Log($"[INPUT] Finger {fingerIndex} UP from Lane {trackedLane}");
        }
    }

    /// <summary>
    /// Calculate deadzone in pixels from mm value (DPI-aware).
    /// </summary>
    float DeadzonePixels()
    {
        float dpi = Screen.dpi > 0f ? Screen.dpi : fallbackDpi;
        float inches = deadzoneMm / 25.4f;
        return inches * dpi;
    }

    #endregion



    #region Mouse Input (PC Testing)

    /// <summary>
    /// Mouse input uses same handlers as touch for identical behavior.
    /// Uses MOUSE_FINGER_INDEX (15) to avoid conflicts with touch indices.
    /// </summary>
    void ProcessMouseInput()
    {
        if (Mouse.current == null) return;

        var mouse = Mouse.current;
        Vector2 mousePos = mouse.position.ReadValue();
        int lane = ScreenPositionToLane(mousePos);
        double inputTime = Time.realtimeSinceStartupAsDouble;

        // Mouse down - same as finger down
        if (mouse.leftButton.wasPressedThisFrame)
        {
            isMouseDown = true;
            mouseDownTime = inputTime;
            HandleFingerDown(MOUSE_FINGER_INDEX, mousePos, lane, inputTime);
        }
        // Mouse held and moved - same as finger moved
        else if (mouse.leftButton.isPressed && isMouseDown)
        {
            HandleFingerMove(MOUSE_FINGER_INDEX, mousePos, lane, inputTime);
            ProcessHoldTick(MOUSE_FINGER_INDEX, inputTime);
        }
        // Mouse up - same as finger up
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            isMouseDown = false;
            HandleFingerUp(MOUSE_FINGER_INDEX, inputTime);
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
            double inputTime = Time.realtimeSinceStartupAsDouble;
            OnLaneTapped?.Invoke(lane, screenPos);
            OnLaneTappedTimestamped?.Invoke(lane, screenPos, inputTime);
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
