using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;
using TouchPhase = UnityEngine.TouchPhase; // Use legacy TouchPhase for Input.GetTouch()

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("🎮 Input Configuration")]
    [SerializeField] private int maxSimultaneousTouches = 6; // 6 lanes support
    [SerializeField] private bool enableTouchVisualization = false;

    [Header("📱 Mobile Settings")]
    [SerializeField] private float touchSensitivity = 1.0f;
    [SerializeField] private float holdTimeThreshold = 0.1f; // For hold notes

    [Header("🎯 Lane Configuration")]
    [SerializeField] private float screenWidth = 1080f; // Reference width
    [SerializeField] private int laneCount = 6;
    [SerializeField] private float laneWidth;

    [Header("📊 Debug Info")]
    [SerializeField] private bool showDebugInfo = false;
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
        SetupScreenConfiguration();
    }

    void InitializeInputSystem()
    {
        // Get main camera
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindFirstObjectByType<Camera>();

        // Calculate lane configuration
        laneWidth = screenWidth / laneCount;

        Debug.Log($"🎮 InputManager initialized - {laneCount} lanes, {laneWidth:F0}px wide each");
    }

    void SetupScreenConfiguration()
    {
        // Calculate screen bounds in world space
        if (mainCamera != null)
        {
            Vector3 screenBottomLeft = mainCamera.ScreenToWorldPoint(new Vector3(0, 0, mainCamera.nearClipPlane));
            Vector3 screenTopRight = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, mainCamera.nearClipPlane));
            screenBounds = new Vector2(screenTopRight.x - screenBottomLeft.x, screenTopRight.y - screenBottomLeft.y);
        }

        Debug.Log($"📱 Screen configuration: {Screen.width}x{Screen.height}, bounds: {screenBounds}");
    }

    void Update()
    {
        HandleTouchInput();
        UpdateActiveTouches();
        UpdateDebugInfo();
    }

    void HandleTouchInput()
    {
        // Handle touch input
        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount && i < maxSimultaneousTouches; i++)
            {
                Touch touch = Input.GetTouch(i);
                ProcessTouch(touch);
            }
        }

        // Handle mouse input for testing on PC
        HandleMouseInput();
    }

    void ProcessTouch(Touch touch)
    {
        int touchId = touch.fingerId;
        Vector2 screenPosition = touch.position;
        int lane = ScreenPositionToLane(screenPosition);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                HandleTouchBegan(touchId, screenPosition, lane);
                break;

            case TouchPhase.Moved:
                HandleTouchMoved(touchId, screenPosition, lane);
                break;

            case TouchPhase.Stationary:
                HandleTouchHeld(touchId);
                break;

            case TouchPhase.Ended:
                HandleTouchEnded(touchId, lane);
                break;

            case TouchPhase.Canceled:
                HandleTouchCanceled(touchId);
                break;
        }
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

            if (showDebugInfo)
                Debug.Log($"🎯 Touch began: Lane {lane}, Position {screenPosition}");
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
                Debug.Log($"🎯 Touch ended: Lane {touchData.lane}");
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
        // Mouse input for PC testing
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Input.mousePosition;
            int lane = ScreenPositionToLane(mousePosition);

            if (lane >= 0 && lane < laneCount)
            {
                OnLaneTapped?.Invoke(lane, mousePosition);

                if (showDebugInfo)
                    Debug.Log($"🖱️ Mouse click: Lane {lane}, Position {mousePosition}");
            }
        }
    }

    int ScreenPositionToLane(Vector2 screenPosition)
    {
        // Convert screen position to lane index
        float normalizedX = screenPosition.x / Screen.width;
        int lane = Mathf.FloorToInt(normalizedX * laneCount);

        // Clamp to valid range
        return Mathf.Clamp(lane, 0, laneCount - 1);
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
        if (showDebugInfo && Time.frameCount % 60 == 0) // Every second
        {
            Debug.Log($"🎮 Input Status - Active touches: {activeTouchCount}, Active lanes: [{string.Join(", ", currentlyActiveLanes)}]");
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
        laneWidth = screenWidth / laneCount;
        Debug.Log($"🎮 Lane count updated: {laneCount} lanes");
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