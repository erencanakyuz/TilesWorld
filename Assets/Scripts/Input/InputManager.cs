using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("🎮 Input Configuration")]
    [SerializeField] private int maxSimultaneousTouches = 6; // 6 lanes support

    [Header("📱 Mobile Settings")]
    [SerializeField] private float touchSensitivity = 1.0f;
    [SerializeField] private float holdTimeThreshold = 0.1f; // For hold notes

    [Header("🎯 Lane Configuration")]
    [SerializeField] private int laneCount = 6;
    private float laneWidth = 2.4f;        // Updated to match NoteRenderer

    // Input Events
    public static event Action<int, Vector2> OnLaneTapped;     // lane, position
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

    void Start()
    {
        // Setup lane positions after SerializeField values are loaded
        SetupLanePositions();
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

    void Update()
    {
        HandleTouchInput();
        UpdateActiveTouches();
    }

    void HandleTouchInput()
    {
        // Handle keyboard input for Q,W,E,R,T,Y keys mapped to lanes
        HandleKeyboardInput();

        // Handle new Input System touch input only on mobile
#if UNITY_ANDROID || UNITY_IOS
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
#endif
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
        // Keyboard input for Q,W,E,R,T,Y keys mapped to lanes 0-5
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;

            // Q = Lane 0
            if (keyboard.qKey.wasPressedThisFrame)
            {
                HandleLaneKeyPress(0);
            }
            // W = Lane 1  
            if (keyboard.wKey.wasPressedThisFrame)
            {
                HandleLaneKeyPress(1);
            }
            // E = Lane 2
            if (keyboard.eKey.wasPressedThisFrame)
            {
                HandleLaneKeyPress(2);
            }
            // R = Lane 3
            if (keyboard.rKey.wasPressedThisFrame)
            {
                HandleLaneKeyPress(3);
            }
            // T = Lane 4
            if (keyboard.tKey.wasPressedThisFrame)
            {
                HandleLaneKeyPress(4);
            }
            // Y = Lane 5
            if (keyboard.yKey.wasPressedThisFrame)
            {
                HandleLaneKeyPress(5);
            }
        }
    }

    void HandleLaneKeyPress(int lane)
    {
        if (lane >= 0 && lane < laneCount)
        {
            // Calculate screen position for the lane center
            Vector2 lanePosition = LaneToScreenPosition(lane);

            Debug.Log($"🎮 KEY PRESSED: Lane {lane} (Key: {GetKeyForLane(lane)}), Screen Pos: {lanePosition}");

            // Fire lane tapped event
            OnLaneTapped?.Invoke(lane, lanePosition);
        }
    }

    string GetKeyForLane(int lane)
    {
        return lane switch
        {
            0 => "Q",
            1 => "W",
            2 => "E",
            3 => "R",
            4 => "T",
            5 => "Y",
            _ => "Unknown"
        };
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

        // Simple screen division approach for reliability
        float normalizedX = screenPosition.x / Screen.width; // 0 to 1
        int lane = Mathf.FloorToInt(normalizedX * laneCount);

        // Clamp to valid range
        lane = Mathf.Clamp(lane, 0, laneCount - 1);

        return lane;
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