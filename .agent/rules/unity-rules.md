---
trigger: always_on
---

# Unity C# Development Rules (Unity 6 / 2024+)

## API Compatibility (Unity 6 - October 2024)

dont forget this project for mobile every aspect should aligns with mobile ui etc

no emojies in the actual code
dont use emojies in code only u can use in debug or in md files

### Object Finding Methods - DEPRECATED vs NEW
| Deprecated (Pre-2022) | Unity 2022+ | Unity 6+ (Fastest) |
|----------------------|-------------|-------------------|
| `FindObjectOfType<T>()` | `FindFirstObjectByType<T>()` | `FindAnyObjectByType<T>()` |
| `FindObjectsOfType<T>()` | `FindObjectsByType<T>(FindObjectsSortMode.InstanceID)` | `FindObjectsByType<T>(FindObjectsSortMode.None)` |

### Correct Usage:
```csharp
// ❌ DEPRECATED (Unity 2021 and earlier only)
var obj = FindObjectOfType<MyType>();
var objs = FindObjectsOfType<MyType>();

// ✅ Unity 2022+ (sorted, slower)
var obj = FindFirstObjectByType<MyType>();
var objs = FindObjectsByType<MyType>(FindObjectsSortMode.InstanceID);

// ✅✅ Unity 6+ BEST (unsorted, fastest)
var obj = FindAnyObjectByType<MyType>();  // No sorting overhead
var objs = FindObjectsByType<MyType>(FindObjectsSortMode.None);  // 2x faster

// Include inactive objects:
var obj = FindAnyObjectByType<MyType>(FindObjectsInactive.Include);
```

- Prefer `Object.Instantiate()` over `GameObject.Instantiate()`
- Use `TryGetComponent<T>()` instead of `GetComponent<T>() != null`

## Async/Await - Use Awaitable (Unity 2023.1+ / Unity 6)

### Replace Coroutines with Awaitable:
```csharp
// ❌ OLD - Coroutine style
IEnumerator WaitAndDoSomething()
{
    yield return new WaitForSeconds(1f);
    DoSomething();
}

// ✅ NEW - Awaitable style (Unity 6)
async Awaitable WaitAndDoSomethingAsync()
{
    await Awaitable.WaitForSecondsAsync(1f);
    DoSomething();
}
```

### Awaitable Methods:
- `Awaitable.NextFrameAsync()` - Wait for next frame
- `Awaitable.EndOfFrameAsync()` - Wait until end of frame
- `Awaitable.FixedUpdateAsync()` - Wait for next FixedUpdate
- `Awaitable.WaitForSecondsAsync(float)` - Wait for seconds
- `Awaitable.FromAsyncOperation(asyncOp)` - Wrap AsyncOperation
- `Awaitable.BackgroundThreadAsync()` - Switch to background thread
- `Awaitable.MainThreadAsync()` - Switch back to main thread

### Cancellation:
```csharp
async Awaitable LoadDataAsync(CancellationToken token)
{
    await Awaitable.WaitForSecondsAsync(1f, token);
    // Auto-cancels when token is triggered
}
```

## Null Safety
- Always null-check before accessing Unity objects: `if (obj != null) obj.DoSomething()`
- Use null-conditional operators for optional access: `component?.Method()`
- Never assume Inspector-assigned references are set - validate in Awake/Start
- Use `== null` for Unity objects (not `is null` - Unity overloads equality)

## Performance - Critical Rules

### Update Loop Optimization
- Cache `GetComponent<T>()` calls in Awake/Start, NEVER call in Update
- Distribute operations across frames - not everything needs to run every frame
- Use FixedUpdate only for physics, not for general logic

### Memory & GC Prevention
- Use `StringBuilder` for string concatenation in Update loops
- Avoid LINQ in performance-critical code (allocations)
- Use `struct` for small data containers (≤16 bytes), `class` for complex objects
- Clear event subscriptions in OnDestroy to prevent memory leaks
- Object pool frequently spawned/destroyed objects

### Caching Best Practices
```csharp
// ❌ BAD - Allocation every frame
void Update()
{
    string text = $"Score: {score}";  // String allocation
    var comp = GetComponent<Rigidbody>();  // Lookup every frame
}

// ✅ GOOD - Cached
private StringBuilder sb = new StringBuilder(32);
private Rigidbody rb;

void Awake() => rb = GetComponent<Rigidbody>();

void Update()
{
    sb.Clear();
    sb.Append("Score: ").Append(score);
}
```

### Tag Comparison
```csharp
// ❌ BAD - String allocation
if (gameObject.tag == "Player") { }

// ✅ GOOD - No allocation
if (gameObject.CompareTag("Player")) { }
```

## Coroutines (If still using instead of Awaitable)
- Cache WaitForSeconds: `private readonly WaitForSeconds wait = new WaitForSeconds(0.5f);`
- Never use `new WaitForSeconds()` inside coroutine loops
- Use `StopAllCoroutines()` or explicitly stop in OnDisable
- Consider migrating to Awaitable for Unity 6+

## Events & Delegates - CRITICAL
```csharp
// ❌ WRONG - Lambda creates new delegate, won't unsubscribe
void OnEnable() => SomeEvent += () => DoSomething();
void OnDisable() => SomeEvent -= () => DoSomething();  // DOES NOT WORK!

// ✅ RIGHT - Store delegate reference as field
private System.Action handler;

void Awake() => handler = DoSomething;
void OnEnable() => SomeEvent += handler;
void OnDisable() => SomeEvent -= handler;  // Works correctly
```

## UI Performance (Unity UI / TextMeshPro)
- Split up Canvases - separate static and dynamic elements
- Limit GraphicRaycasters - only on canvases that need input
- Disable Raycast Target on non-interactive elements
- Use `SetActive(false)` before heavy UI modifications
- Cache `RectTransform` references
- Pool UI objects for dynamic lists
- TextMeshPro: use `SetText(StringBuilder)` for dynamic text

## Editor Scripts
- Place Editor scripts in `Assets/Editor/` folder
- Use `#if UNITY_EDITOR` for editor-only code in runtime scripts
- Always check `Application.isPlaying` in editor utilities
- Use SerializedProperty for custom inspectors (undo support)

## Scene Management
- Subscribe to `SceneManager.sceneLoaded` in OnEnable, unsubscribe in OnDisable
- Use `DontDestroyOnLoad()` for persistent managers
- Implement singleton pattern with duplicate destruction check:
```csharp
void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
}
```

## Serialization
- Use `[SerializeField]` for private fields that need Inspector access
- Use `[HideInInspector]` to hide public fields from Inspector
- Prefer ScriptableObject for shared configuration data
- Use `[field: SerializeField]` for auto-properties

## Physics Optimization
- Use primitive colliders (Box, Sphere, Capsule) over MeshCollider
- Adjust Fixed Timestep if physics precision isn't critical
- Use layers and collision matrix to reduce collision checks
- Use `Physics.OverlapSphereNonAlloc()` instead of `Physics.OverlapSphere()`

## Naming Conventions
- **PascalCase**: Classes, Methods, Properties, Public Fields, Events
- **camelCase**: Private fields, Local variables, Parameters
- **Prefix**: `_` or `m_` for private member fields (optional but consistent)
- **Suffix**: `...Manager`, `...Controller`, `...Handler` for manager classes
- **Interfaces**: Prefix with `I` (e.g., `IInteractable`)

## Project Structure
```
Assets/
├── Editor/           # Editor-only scripts
├── Resources/        # Runtime-loaded assets (use sparingly)
├── Scenes/
├── Scripts/
│   ├── Core/         # Managers, Bootstrap
│   ├── UI/           # UI controllers
│   ├── Gameplay/     # Game logic
│   └── Utils/        # Utilities, Extensions
├── Prefabs/
├── Materials/
├── Textures/
└── Audio/
```