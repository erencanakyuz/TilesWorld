using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.InputSystem.EnhancedTouch;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("🎮 Input Configuration")]
    // [SerializeField] private int maxSimultaneousTouches = 6; // 6 lanes support

    [Header("📱 Mobile Settings")]
    [SerializeField] private float touchSensitivity = 1.0f;
    [SerializeField] private float holdTimeThreshold = 0.1f; // For hold notes

    [Header("🎯 Lane Configuration")]
    [SerializeField] private int laneCount = 6;
    [Tooltip("Hit zone Y position - must match HitZoneTrigger objects in scene")]
    [SerializeField] private float hitZoneY = 0.45f;

    [Header("Swipe Settings")]
    [Tooltip("Minimum swipe velocity in pixels/second to register lane changes")]
    [SerializeField] private float minSwipeVelocity = 0f;   // No velocity requirement - any movement works
    [Tooltip("Maximum time in seconds for a swipe gesture")]
    [SerializeField] private float maxSwipeTime = 5.0f;     // Long hold allowed
    [Tooltip("Maximum vertical deviation in pixels allowed during horizontal swipe")]
    [SerializeField] private float maxVerticalDeviation = 500f; // Very forgiving

    [Header("Latency Debug")]
    [Tooltip("Enable to measure and log input latency")]
    [SerializeField] private bool measureLatency = false;
    
    // Latency tracking
    private float[] latencySamples = new float[30];
    private int latencySampleIndex = 0;
    private int latencySampleCount = 0;

    // This header was causing an error because it was not attached to a field.
    // [Header("Configuration")] 
    // [SerializeField] private int maxSimultaneousTouches = 10; // No longer used

    // Input Events
    public delegate void LaneTapHandler(int lane, Vector2 screenPos);
    public static event LaneTapHandler OnLaneTapped;     // lane, position
    public static event Action<int, float> OnLaneHeld;        // lane, holdTime
    public static event Action<int> OnLaneReleased;           // lane

    // Touch tracking
    private Dictionary<int, TouchData> activeTouches = new Dictionary<int, TouchData>();
    private List<int> currentlyActiveLanes = new List<int>();

    // Screen to lane conversion
    private Camera mainCamera;
    private Vector3[] laneWorldPositions; // Match NoteRenderer lanes


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInputSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();

        if (Application.isMobilePlatform)
        {
            // Reduce input latency on mobile devices
            InputSystem.pollingFrequency = 120f;
        }
    }

    void Start()
    {
        // Setup lane positions after SerializeField values are loaded
        SetupLanePositions();
    }

    void InitializeInputSystem()
    {
        // Set low-latency mode for mobile touch response
        // Process input events every frame instead of fixed update
        InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
        
        // Get main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindFirstObjectByType<Camera>();
    }

    void SetupLanePositions()
    {
        laneWorldPositions = new Vector3[laneCount];

        for (int i = 0; i < laneCount; i++)
        {
            // Use centralized GameConstants for lane positions
            float xOffset = GameConstants.GetLaneXPosition(i);
            laneWorldPositions[i] = new Vector3(xOffset, 0, 0);
        }
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void OnDestroy()
    {
        // NOTE: Do NOT clear static events here!
        // Static events persist across scene loads and are subscribed to by HitZoneManager etc.
        // Clearing them breaks input handling until next domain reload.
        
        // Only clear instance reference
        if (Instance == this)
        {
            Instance = null;
        }
        
        // Clear tracking data
        activeTouches?.Clear();
        currentlyActiveLanes?.Clear();
    }

    void Update()
    {
        // Optimize: Only search for camera every 30 frames instead of every frame
        if (mainCamera == null && Time.frameCount % 30 == 0)
        {
            mainCamera = Camera.main;
            // If still null, exit early to prevent errors this frame.
            if (mainCamera == null) return;
        }
        
        // Skip input processing if no camera available
        if (mainCamera == null) return;

        HandleTouchInput();
        UpdateActiveTouches();
    }

    void HandleTouchInput()
    {
        // Handle keyboard input first
        HandleKeyboardInput();
        
        // Handle mouse input for PC testing
        HandleMouseInput();

        // Exit if there is no touchscreen device
        if (UnityEngine.InputSystem.Touchscreen.current == null) return;

        // Process all active touches each frame
        foreach (var touch in UnityEngine.InputSystem.Touchscreen.current.touches)
        {
            int touchId = touch.touchId.ReadValue();
            Vector2 position = touch.position.ReadValue();
            int lane = ScreenPositionToLane(position);

            // Use the phase to drive the state machine
            switch (touch.phase.ReadValue())
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    HandleTouchBegan(touchId, position, lane);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                    HandleTouchMoved(touchId, position, lane);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    HandleTouchHeld(touchId);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                    HandleTouchEnded(touchId, lane);
                    break;

                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    HandleTouchCanceled(touchId);
                    break;
            }
        }
    }

    void HandleTouchBegan(int touchId, Vector2 screenPosition, int lane)
    {
        if (lane >= 0 && lane < laneCount)
        {
            float inputReceivedTime = Time.realtimeSinceStartup;
            
            TouchData touchData = new TouchData
            {
                touchId = touchId,
                startPosition = screenPosition,
                currentPosition = screenPosition,
                startTime = Time.time,
                inputTime = inputReceivedTime,
                lane = lane,
                isActive = true
            };

            activeTouches[touchId] = touchData;

            // Track active lanes for hold detection
            if (!currentlyActiveLanes.Contains(lane))
            {
                currentlyActiveLanes.Add(lane);
            }
            
            // CRITICAL: ALWAYS fire OnLaneTapped for every new touch!
            // Don't skip based on lane active state - this was blocking rapid taps!
            if (measureLatency)
            {
                float processingTime = Time.realtimeSinceStartup;
                float latency = (processingTime - inputReceivedTime) * 1000f;
                MeasureInputLatency(latency);
                Debug.Log($"[LATENCY] Lane {lane} clicked - Latency: {latency:F3}ms");
            }
            
            OnLaneTapped?.Invoke(lane, screenPosition);
        }
    }

    void HandleTouchMoved(int touchId, Vector2 screenPosition, int lane)
    {
        if (activeTouches.ContainsKey(touchId))
        {
            TouchData touchData = activeTouches[touchId];
            touchData.currentPosition = screenPosition;

            // Check if moved to different lane - NO CONSTRAINTS, just trigger!
            if (lane != touchData.lane && lane >= 0 && lane < laneCount)
            {
                int oldLane = touchData.lane;
                int newLane = lane;
                
                // Trigger all lanes between old and new position
                int step = (newLane > oldLane) ? 1 : -1;
                
                for (int swipeLane = oldLane + step; swipeLane != newLane + step; swipeLane += step)
                {
                    if (swipeLane >= 0 && swipeLane < laneCount)
                    {
                        OnLaneTapped?.Invoke(swipeLane, screenPosition);
                    }
                }
                
                // Update touch data to new lane
                currentlyActiveLanes.Remove(oldLane);
                touchData.lane = newLane;
                
                if (!currentlyActiveLanes.Contains(newLane))
                {
                    currentlyActiveLanes.Add(newLane);
                }
            }

            activeTouches[touchId] = touchData;
        }
    }

    void HandleTouchHeld(int touchId)
    {
        if (activeTouches.ContainsKey(touchId))
        {
            TouchData touchData = activeTouches[touchId];
            float holdTime = Time.time - touchData.startTime;

            if (holdTime > holdTimeThreshold)
            {
                OnLaneHeld?.Invoke(touchData.lane, holdTime);
            }
        }
    }

    void HandleTouchEnded(int touchId, int lane)
    {
        if (activeTouches.ContainsKey(touchId))
        {
            TouchData touchData = activeTouches[touchId];
            // Remove this touch from tracking
            activeTouches.Remove(touchId);

            // If NO other active touch is still using this lane, mark lane as released
            bool laneStillActive = false;
            foreach (var kvp in activeTouches)
            {
                if (kvp.Value.lane == touchData.lane)
                {
                    laneStillActive = true;
                    break;
                }
            }

            if (!laneStillActive)
            {
                currentlyActiveLanes.Remove(touchData.lane);
                OnLaneReleased?.Invoke(touchData.lane);
            }
            // If the lane is still active via another finger, we keep its active state.
        }
    }

    void HandleTouchCanceled(int touchId)
    {
        if (activeTouches.ContainsKey(touchId))
        {
            TouchData touchData = activeTouches[touchId];
            currentlyActiveLanes.Remove(touchData.lane);
            activeTouches.Remove(touchId);

            OnLaneReleased?.Invoke(touchData.lane);
        }
    }

    void HandleKeyboardInput()
    {
        // Use the new Input System for keyboard checks
        if (UnityEngine.InputSystem.Keyboard.current == null) return;

        var keyboard = UnityEngine.InputSystem.Keyboard.current;

        if (keyboard.qKey.wasPressedThisFrame) HandleLaneKeyPress(0);
        if (keyboard.wKey.wasPressedThisFrame) HandleLaneKeyPress(1);
        if (keyboard.eKey.wasPressedThisFrame) HandleLaneKeyPress(2);
        if (keyboard.rKey.wasPressedThisFrame) HandleLaneKeyPress(3);
        if (keyboard.tKey.wasPressedThisFrame) HandleLaneKeyPress(4);
        if (keyboard.yKey.wasPressedThisFrame) HandleLaneKeyPress(5);
    }

    // Mouse drag tracking for PC swipe testing
    private int lastMouseLane = -1;
    private bool isMouseDragging = false;

    void HandleMouseInput()
    {
        // Handle mouse input for PC testing - behaves exactly like touch
        if (UnityEngine.InputSystem.Mouse.current == null) return;
        
        var mouse = UnityEngine.InputSystem.Mouse.current;
        Vector2 mousePosition = mouse.position.ReadValue();
        int lane = ScreenPositionToLane(mousePosition);
        
        // Use a fake touchId for mouse (negative to avoid collision with real touches)
        const int MOUSE_TOUCH_ID = -999;
        
        // Mouse button pressed - same as TouchBegan
        if (mouse.leftButton.wasPressedThisFrame)
        {
            HandleTouchBegan(MOUSE_TOUCH_ID, mousePosition, lane);
        }
        // Mouse button held and moving - same as TouchMoved
        else if (mouse.leftButton.isPressed)
        {
            HandleTouchMoved(MOUSE_TOUCH_ID, mousePosition, lane);
        }
        // Mouse button released - same as TouchEnded
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            HandleTouchEnded(MOUSE_TOUCH_ID, lane);
        }
    }



    void HandleLaneKeyPress(int lane)
    {
        if (lane >= 0 && lane < laneCount)
        {
            // Calculate screen position for the lane center
            Vector2 lanePosition = LaneToScreenPosition(lane);

            // Fire lane tapped event
            OnLaneTapped?.Invoke(lane, lanePosition);
        }
    }


    int ScreenPositionToLane(Vector2 screenPosition)
    {
        if (mainCamera == null || laneWorldPositions == null || laneWorldPositions.Length == 0)
        {
            return 0;
        }

        // Use camera raycast to convert screen position to world coordinates
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        // Calculate intersection with the HIT ZONE plane (Y = hitZoneY)
        // This matches the actual Y position of HitZoneTrigger objects
        if (Mathf.Approximately(ray.direction.y, 0f)) return 0; // Prevent division by zero
        
        float distanceToPlane = (hitZoneY - ray.origin.y) / ray.direction.y;
        
        // Ensure we're hitting the plane in front of camera
        if (distanceToPlane < 0) return 0;
        
        Vector3 worldPosition = ray.origin + ray.direction * distanceToPlane;

        // Find the closest lane to this world position
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
        // Convert lane index to screen position (center of lane)
        float normalizedX = (lane + 0.5f) / laneCount;
        return new Vector2(normalizedX * Screen.width, Screen.height * 0.5f);
    }

    void UpdateActiveTouches()
    {
        // Clean up any invalid touches
        List<int> touchesToRemove = new List<int>();
        foreach (var kvp in activeTouches)
        {
            if (!kvp.Value.isActive)
            {
                touchesToRemove.Add(kvp.Key);
            }
        }

        foreach (int touchId in touchesToRemove)
        {
            activeTouches.Remove(touchId);
        }
    }

    #region Public Interface
    public bool IsLaneActive(int lane)
    {
        return currentlyActiveLanes.Contains(lane);
    }

    public int GetActiveLaneCount()
    {
        return currentlyActiveLanes.Count;
    }

    public List<int> GetActiveLanes()
    {
        return new List<int>(currentlyActiveLanes);
    }

    public void SetTouchSensitivity(float sensitivity)
    {
        touchSensitivity = Mathf.Clamp01(sensitivity);
    }

    public void SetLaneCount(int count)
    {
        laneCount = Mathf.Clamp(count, 1, 10);
        SetupLanePositions(); // Recalculate lane positions
    }
    #endregion

    #region Latency Measurement
    void MeasureInputLatency(float latency)
    {
        // Store measured latency in circular buffer
        latencySamples[latencySampleIndex] = latency;
        latencySampleIndex = (latencySampleIndex + 1) % latencySamples.Length;
        
        if (latencySampleCount < latencySamples.Length)
        {
            latencySampleCount++;
        }
    }
    
    float GetAverageLatency()
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
        
        GUI.Box(new Rect(10, 10, 300, 80), "Input Latency Monitor");
        GUI.Label(new Rect(20, 35, 280, 20), $"Average Latency: {avgLatency:F2}ms");
        GUI.Label(new Rect(20, 55, 280, 20), $"Current FPS: {(1f / Time.deltaTime):F1}");
        GUI.Label(new Rect(20, 75, 280, 20), $"Update Mode: Dynamic Update");
    }
    #endregion

    #region Settings Integration
    void LoadInputSettings()
    {
        touchSensitivity = PlayerPrefs.GetFloat("TouchSensitivity", 1.0f);
    }

    public void SaveInputSettings()
    {
        PlayerPrefs.SetFloat("TouchSensitivity", touchSensitivity);
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
    public float inputTime; // Time when input was received
    public int lane;
    public bool isActive;
}
