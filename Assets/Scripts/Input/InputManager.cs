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

    // This header was causing an error because it was not attached to a field.
    // [Header("🔧 Configuration")] 
    // [SerializeField] private int maxSimultaneousTouches = 10; // No longer used

    [Header("🐛 Debug")]
    [SerializeField] private bool showTouchDebug = true; // Enable for testing

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

    // Debug tracking
    private Vector2 lastTouchScreenPos;
    private Vector3 lastTouchWorldPos;
    private int lastDetectedLane = -1;

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

        // ================== ADIM 1: KONTROL LOGU ==================
        if (laneWorldPositions != null && laneWorldPositions.Length > 0)
        {
            string positions = "";
            for (int i = 0; i < laneWorldPositions.Length; i++)
            {
                positions += $"Lane {i}: {laneWorldPositions[i].x:F2} | ";
            }
        }
        else
        {
        }
        // =========================================================
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
            // Use centralized GameConstants for lane positions
            float xOffset = GameConstants.GetLaneXPosition(i);
            laneWorldPositions[i] = new Vector3(xOffset, 0, 0);
        }
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
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
                OnLaneTapped?.Invoke(lane, screenPosition);
            }
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

    void HandleMouseInput()
    {
        // Handle mouse input for PC testing
        if (UnityEngine.InputSystem.Mouse.current == null) return;
        
        var mouse = UnityEngine.InputSystem.Mouse.current;
        
        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = mouse.position.ReadValue();
            int lane = ScreenPositionToLane(mousePosition);
            
            if (lane >= 0 && lane < laneCount)
            {
                OnLaneTapped?.Invoke(lane, mousePosition);
                
                if (showTouchDebug)
                {
                    Debug.Log($"[Mouse] Click at ({mousePosition.x:F0}, {mousePosition.y:F0}) -> Lane {lane}");
                }
            }
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

        // Store for debug visualization
        lastTouchScreenPos = screenPosition;

        // Use camera raycast to convert screen position to world coordinates
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        // Calculate intersection with the HIT ZONE plane (Y = hitZoneY)
        // This matches the actual Y position of HitZoneTrigger objects
        if (Mathf.Approximately(ray.direction.y, 0f)) return 0; // Prevent division by zero
        
        float distanceToPlane = (hitZoneY - ray.origin.y) / ray.direction.y;
        
        // Ensure we're hitting the plane in front of camera
        if (distanceToPlane < 0) return 0;
        
        Vector3 worldPosition = ray.origin + ray.direction * distanceToPlane;
        
        // Store for debug visualization
        lastTouchWorldPos = worldPosition;

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

        // Store for debug visualization
        lastDetectedLane = closestLane;

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

    #region Debug Visualization
    void OnGUI()
    {
        if (!showTouchDebug) return;

        GUI.Box(new Rect(5, 5, 420, 120), "Touch Debug");
        GUI.Label(new Rect(10, 25, 400, 20), 
            $"Screen: ({lastTouchScreenPos.x:F0}, {lastTouchScreenPos.y:F0})");
        GUI.Label(new Rect(10, 45, 400, 20), 
            $"World: ({lastTouchWorldPos.x:F2}, {lastTouchWorldPos.y:F2}, {lastTouchWorldPos.z:F2})");
        GUI.Label(new Rect(10, 65, 400, 20), 
            $"Lane: {lastDetectedLane}");
        GUI.Label(new Rect(10, 85, 400, 20), 
            $"HitZone Y: {hitZoneY}");
        GUI.Label(new Rect(10, 105, 400, 20), 
            $"Camera: {(mainCamera != null ? mainCamera.transform.position.ToString() : "NULL")}");
    }

    void OnDrawGizmos()
    {
        if (!showTouchDebug || mainCamera == null) return;

        // Draw ray from camera to touch point
        Ray ray = mainCamera.ScreenPointToRay(lastTouchScreenPos);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(ray.origin, ray.origin + ray.direction * 20f);

        // Draw hit point on hit zone plane
        if (Mathf.Abs(ray.direction.y) > 0.001f)
        {
            float distanceToPlane = (hitZoneY - ray.origin.y) / ray.direction.y;
            if (distanceToPlane > 0)
            {
                Vector3 hitPoint = ray.origin + ray.direction * distanceToPlane;
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(hitPoint, 0.2f);

                // Draw lane boundaries
                Gizmos.color = Color.green;
                for (int i = 0; i < laneCount; i++)
                {
                    float laneX = laneWorldPositions[i].x;
                    Gizmos.DrawLine(
                        new Vector3(laneX - 0.9f, hitZoneY, hitPoint.z),
                        new Vector3(laneX + 0.9f, hitZoneY, hitPoint.z)
                    );
                }
            }
        }
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
    public int lane;
    public bool isActive;
}