using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("🎮 Input Configuration")]
    [SerializeField] private int maxSimultaneousTouches = 6; // 6 lanes support
    [SerializeField] private bool enableTouchVisualization = true; // Enable for debugging

    [Header("📱 Mobile Settings")]
    [SerializeField] private float touchSensitivity = 1.0f;
    [SerializeField] private float holdTimeThreshold = 0.1f; // For hold notes

    [Header("🎯 Lane Configuration")]
    [SerializeField] private int laneCount = 6;
    private float laneWidth = 1.8f; // Match NoteRenderer laneWidth - don't serialize, get from NoteRenderer

    [Header("📊 Debug Info")]
    [SerializeField] private bool showDebugInfo = false; // Disable debug spam
    [SerializeField] private int activeTouchCount = 0;

    // Input Events
    public static event Action<int, Vector2> OnLaneTapped;     // lane, position
    public static event Action<int, float> OnLaneHeld;        // lane, holdTime
    public static event Action<int> OnLaneReleased;           // lane

    // Touch tracking
    private Dictionary<int, TouchData> activeTouches = new Dictionary<int, TouchData>();
    private List<int> currentlyActiveLanes = new List<int>();

    // Screen to lane conversion
    private Camera mainCamera;
    private Vector2 screenBounds;
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

    void Start()
    {
        // Setup lane positions after SerializeField values are loaded
        SetupLanePositions();
        SetupScreenConfiguration();
    }

    void InitializeInputSystem()
    {
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
            // EXACT same algorithm as NoteRenderer
            float xOffset = (i - (laneCount - 1) * 0.5f) * laneWidth;
            laneWorldPositions[i] = new Vector3(xOffset, 0, 0);
        }
    }

    void SetupScreenConfiguration()
    {
        if (mainCamera != null)
        {
            // Get screen bounds for UI positioning
            Vector3 screenBounds = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.nearClipPlane));
        }
    }

    void Update()
    {
        HandleTouchInput();
        UpdateActiveTouches();
        UpdateDebugInfo();
    }

    void HandleTouchInput()
    {
        // Handle new Input System touch input
        if (UnityEngine.InputSystem.Touchscreen.current != null)
        {
            var touchscreen = UnityEngine.InputSystem.Touchscreen.current;
            for (int i = 0; i < touchscreen.touches.Count && i < maxSimultaneousTouches; i++)
            {
                var touch = touchscreen.touches[i];
                if (touch.isInProgress)
                {
                    ProcessNewTouch(touch);
                }
            }
        }

        // Handle mouse input for testing on PC
        HandleMouseInput();
    }

    void HandleTouchBegan(int touchId, Vector2 screenPosition, int lane)
    {
        if (lane >= 0 && lane < laneCount)
        {
            TouchData touchData = new TouchData
            {
                touchId = touchId,
                startPosition = screenPosition,
                currentPosition = screenPosition,
                startTime = Time.time,
                lane = lane,
                isActive = true
            };

            activeTouches[touchId] = touchData;

            if (!currentlyActiveLanes.Contains(lane))
            {
                currentlyActiveLanes.Add(lane);
            }

            // Fire tap event
            OnLaneTapped?.Invoke(lane, screenPosition);

            // Visual feedback for input
            CreateInputVisualization(lane, screenPosition);

            if (showDebugInfo)
                Debug.Log($"Touch began: Lane {lane}, Position {screenPosition}");
        }
    }

    void HandleTouchMoved(int touchId, Vector2 screenPosition, int lane)
    {
        if (activeTouches.ContainsKey(touchId))
        {
            TouchData touchData = activeTouches[touchId];
            touchData.currentPosition = screenPosition;

            // Check if moved to different lane
            if (lane != touchData.lane && lane >= 0 && lane < laneCount)
            {
                // Remove from old lane
                currentlyActiveLanes.Remove(touchData.lane);

                // Add to new lane
                touchData.lane = lane;
                if (!currentlyActiveLanes.Contains(lane))
                {
                    currentlyActiveLanes.Add(lane);
                    OnLaneTapped?.Invoke(lane, screenPosition);
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
            currentlyActiveLanes.Remove(touchData.lane);
            activeTouches.Remove(touchId);

            OnLaneReleased?.Invoke(touchData.lane);

            if (showDebugInfo)
                Debug.Log($"Touch ended: Lane {touchData.lane}");
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

    void HandleMouseInput()
    {
        // Mouse input for PC testing using new Input System
        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            int lane = ScreenPositionToLane(mousePosition);

            if (lane >= 0 && lane < laneCount)
            {
                OnLaneTapped?.Invoke(lane, mousePosition);

                // Visual feedback for input
                if (enableTouchVisualization)
                    CreateInputVisualization(lane, mousePosition);
            }
        }
    }

    void ProcessNewTouch(UnityEngine.InputSystem.Controls.TouchControl touch)
    {
        int touchId = touch.touchId.ReadValue();
        Vector2 screenPosition = touch.position.ReadValue();
        int lane = ScreenPositionToLane(screenPosition);

        var phase = touch.phase.ReadValue();

        switch (phase)
        {
            case UnityEngine.InputSystem.TouchPhase.Began:
                HandleTouchBegan(touchId, screenPosition, lane);
                break;

            case UnityEngine.InputSystem.TouchPhase.Moved:
                HandleTouchMoved(touchId, screenPosition, lane);
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

    int ScreenPositionToLane(Vector2 screenPosition)
    {
        if (mainCamera == null || laneWorldPositions == null) return 0;

        // Convert screen position to world space (at Z=0 where lanes are)
        Vector3 worldPoint = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane));

        // Find closest lane based on world X position
        int closestLane = 0;
        float closestDistance = Mathf.Abs(worldPoint.x - laneWorldPositions[0].x);

        for (int i = 1; i < laneCount; i++)
        {
            float distance = Mathf.Abs(worldPoint.x - laneWorldPositions[i].x);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestLane = i;
            }
        }

        // Debug the mapping
        if (showDebugInfo)
        {
            Debug.Log($"🎯 Input mapping: Screen {screenPosition.x:F0}px → World {worldPoint.x:F2} → Lane {closestLane} (distance: {closestDistance:F2})");
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
        activeTouchCount = activeTouches.Count;

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

    void UpdateDebugInfo()
    {
        // Debug removed for performance
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
        Debug.Log($"Lane count updated: {laneCount} lanes");
    }

    public void EnableDebugVisualization(bool enable)
    {
        enableTouchVisualization = enable;
        showDebugInfo = enable;
    }
    #endregion

    #region Settings Integration
    void LoadInputSettings()
    {
        touchSensitivity = PlayerPrefs.GetFloat("TouchSensitivity", 1.0f);
        enableTouchVisualization = PlayerPrefs.GetInt("TouchVisualization", 0) == 1;
        showDebugInfo = PlayerPrefs.GetInt("InputDebug", 0) == 1;
    }

    public void SaveInputSettings()
    {
        PlayerPrefs.SetFloat("TouchSensitivity", touchSensitivity);
        PlayerPrefs.SetInt("TouchVisualization", enableTouchVisualization ? 1 : 0);
        PlayerPrefs.SetInt("InputDebug", showDebugInfo ? 1 : 0);
        PlayerPrefs.Save();
    }
    #endregion

    void OnDrawGizmos()
    {
        if (enableTouchVisualization)
        {
            // Draw lane boundaries
            Gizmos.color = Color.yellow;
            for (int i = 0; i <= laneCount; i++)
            {
                float x = (float)i / laneCount * Screen.width;
                Vector3 screenPos = new Vector3(x, 0, 0);
                Vector3 worldPos = mainCamera != null ? mainCamera.ScreenToWorldPoint(screenPos) : Vector3.zero;

                Gizmos.DrawLine(
                    new Vector3(worldPos.x, -10, 0),
                    new Vector3(worldPos.x, 10, 0)
                );
            }

            // Draw active touches
            Gizmos.color = Color.red;
            foreach (var touch in activeTouches.Values)
            {
                Vector3 worldPos = mainCamera != null ?
                    mainCamera.ScreenToWorldPoint(new Vector3(touch.currentPosition.x, touch.currentPosition.y, 5)) :
                    Vector3.zero;
                Gizmos.DrawSphere(worldPos, 0.5f);
            }
        }
    }

    void CreateInputVisualization(int lane, Vector2 screenPosition)
    {
        if (!enableTouchVisualization || lane < 0 || lane >= laneCount) return;

        // Create visual feedback for input (temporary sphere)
        GameObject feedback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        feedback.name = "InputFeedback";

        // Position at exact lane center in world space
        Vector3 worldPosition = laneWorldPositions[lane];
        worldPosition.y = 1f; // Above ground level
        worldPosition.z = 3f; // At hit zone

        feedback.transform.position = worldPosition;
        feedback.transform.localScale = Vector3.one * 0.5f;

        // Color coding by lane
        Renderer renderer = feedback.GetComponent<Renderer>();
        Color laneColor = Color.HSVToRGB((float)lane / laneCount, 0.8f, 1.0f);
        renderer.material.color = laneColor;

        // Auto cleanup after 1 second
        Destroy(feedback, 1.0f);
    }
}

[System.Serializable]
public class TouchData
{
    public int touchId;
    public Vector2 startPosition;
    public Vector2 currentPosition;
    public float startTime;
    public int lane;
    public bool isActive;
}