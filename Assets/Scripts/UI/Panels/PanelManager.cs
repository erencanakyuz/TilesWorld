using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PanelManager : MonoBehaviour
{
    public static PanelManager Instance { get; private set; }

    private UIConfig config;
    private CanvasLocator canvasLocator;
    private Dictionary<GameState, GameObject> statePanelPrefabs;
    private GameObject currentPanelInstance;

    public System.Action OnPausePressed;
    public System.Action OnResumePressed;
    public System.Action OnRestartPressed;
    public System.Action OnMainMenuPressed;

    public void Initialize(UIConfig config, CanvasLocator canvasLocator)
    {
        Instance = this;
        this.config = config;
        this.canvasLocator = canvasLocator;
        
        // Clear stale panel reference (panel was destroyed with scene but ref lingers)
        if (currentPanelInstance != null && currentPanelInstance.Equals(null))
        {
            currentPanelInstance = null;
        }
        
        // NOTE: Do NOT call HideCurrentPanel() here!
        // GameManager.Start() may have already created a valid panel (e.g. MainMenu).
        // Destroying it here causes the user to see an empty screen with no buttons.
        
        SetupPanelDictionary();
        Debug.Log("[PanelManager] Initialized (keeping active panel if any)");
    }

    private void SetupPanelDictionary()
    {
        statePanelPrefabs = new Dictionary<GameState, GameObject>
        {
            { GameState.MainMenu, config != null ? config.mainMenuPanelPrefab : null },
            { GameState.SongSelection, config != null ? config.songSelectionPanelPrefab : null },
            { GameState.Playing, config != null ? config.gameplayPanelPrefab : null },
            { GameState.Paused, config != null ? config.pausePanelPrefab : null },
            { GameState.GameOver, config != null ? config.gameOverPanelPrefab : null },
            { GameState.Settings, config != null ? config.settingsPanelPrefab : null },
            { GameState.WorldTour, config != null ? config.worldTourPanelPrefab : null },
            { GameState.ArtistBattle, config != null ? config.artistBattlePanelPrefab : null },
            { GameState.DailyChallenge, config != null ? config.dailyChallengePanelPrefab : null },
            { GameState.Profile, config != null ? config.profilePanelPrefab : null },
            { GameState.SongResult, config != null ? config.songResultPanelPrefab : null }
        };
    }

public void ShowPanelForState(GameState state)
    {
        HideCurrentPanel();

        Transform parentCanvas = GetParentCanvasForState(state);
        GameObject prefab = null;
        bool hasPrefab = statePanelPrefabs != null && statePanelPrefabs.TryGetValue(state, out prefab) && prefab != null;
        
        // NOTE: Use hasPrefab guard before accessing prefab.name — Unity "fake-null"
        // serialized fields pass C#'s ?. null-check but throw on member access.
        string prefabName = hasPrefab ? prefab.name : "NULL";
        Debug.Log($"[PanelManager] ShowPanelForState({state}): canvas={parentCanvas?.name ?? "NULL"}, prefab={prefabName}, hasPrefab={hasPrefab}");
        
        if (parentCanvas != null && hasPrefab)
        {
            currentPanelInstance = Object.Instantiate(prefab, parentCanvas);
            Debug.Log($"[PanelManager] Created panel: {currentPanelInstance.name}");
        }
        else if (!hasPrefab && parentCanvas != null)
        {
            // Try RuntimePanelFactory for gamification panels
            currentPanelInstance = RuntimePanelFactory.CreatePanel(state, parentCanvas);
            if (currentPanelInstance != null)
            {
                Debug.Log($"[PanelManager] Created runtime panel: {currentPanelInstance.name}");
            }
            else if (state != GameState.SongResult && state != GameState.Playing && state != GameState.Loading)
            {
                Debug.LogWarning($"[PanelManager] No prefab or runtime factory for state: {state}");
            }
        }

        // Ensure raycaster exists on parent canvas for click handling
        if (parentCanvas != null) EnsureGraphicRaycaster(parentCanvas);

        switch (state)
        {
            case GameState.MainMenu:
                if (currentPanelInstance != null)
                {
                    PanelButtonWirer.WireMainMenuPanel(currentPanelInstance, 
                        () => GameManager.Instance?.ChangeGameState(GameState.SongSelection),
                        () => GameManager.Instance?.OpenSettings(),
                        () => Application.Quit());

                    // Add gamification navigation buttons to MainMenu
                    PanelButtonWirer.AddGamificationButtons(currentPanelInstance,
                        () => GameManager.Instance?.ChangeGameState(GameState.Profile),
                        () => GameManager.Instance?.ChangeGameState(GameState.WorldTour),
                        () => GameManager.Instance?.ChangeGameState(GameState.ArtistBattle),
                        () => GameManager.Instance?.ChangeGameState(GameState.DailyChallenge));
                }
                break;
            case GameState.Paused:
                if (currentPanelInstance != null)
                {
                    PanelButtonWirer.WirePausePanel(currentPanelInstance, OnResumePressed, OnRestartPressed, OnMainMenuPressed);
                }
                break;
            case GameState.Settings:
                if (currentPanelInstance != null)
                {
                    var controller = currentPanelInstance.GetComponent<SettingsPanelController>();
                    if (controller == null)
                    {
                        controller = currentPanelInstance.AddComponent<SettingsPanelController>();
                    }
                    controller.Initialize(GameManager.Instance, AudioManager.Instance, InputManager.Instance);
                }
                break;
            case GameState.GameOver:
                if (currentPanelInstance != null)
                {
                    PanelButtonWirer.WireGameOverPanel(currentPanelInstance, OnRestartPressed, OnMainMenuPressed);
                }
                break;
            case GameState.Profile:
                if (currentPanelInstance != null)
                {
                    var profileCtrl = currentPanelInstance.AddComponent<ProfilePanelController>();
                    WireBackButton(currentPanelInstance);
                }
                break;
            case GameState.WorldTour:
                if (currentPanelInstance != null)
                {
                    var tourCtrl = currentPanelInstance.AddComponent<WorldTourPanelController>();
                    WireBackButton(currentPanelInstance);
                }
                break;
            case GameState.ArtistBattle:
                if (currentPanelInstance != null)
                {
                    var battleCtrl = currentPanelInstance.AddComponent<ArtistBattlePanelController>();
                    WireBackButton(currentPanelInstance);
                }
                break;
            case GameState.DailyChallenge:
                if (currentPanelInstance != null)
                {
                    var dailyCtrl = currentPanelInstance.AddComponent<DailyChallengePanelController>();
                    WireBackButton(currentPanelInstance);
                }
                break;
        }
    }

    /// <summary>
    /// Finds and wires any BackButton in a panel to go back to MainMenu.
    /// </summary>
    private void WireBackButton(GameObject panel)
    {
        var buttons = panel.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.name.ToLower().Contains("back") || btn.name.ToLower().Contains("return"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => GameManager.Instance?.ChangeGameState(GameState.MainMenu));
                Debug.Log($"[PanelManager] Back button wired: {btn.name}");
            }
        }
    }

    public void HideCurrentPanel()
    {
        if (currentPanelInstance != null)
        {
            Object.Destroy(currentPanelInstance);
            currentPanelInstance = null;
        }
    }

    public GameObject CurrentPanel => currentPanelInstance;

    private Transform GetParentCanvasForState(GameState state)
    {
        switch (state)
        {
            case GameState.Paused:
            case GameState.Settings:
                return canvasLocator?.OverlayCanvas != null ? canvasLocator.OverlayCanvas.transform : canvasLocator?.MainCanvas?.transform;
            case GameState.MainMenu:
            case GameState.SongSelection:
            case GameState.GameOver:
            case GameState.Profile:
            case GameState.WorldTour:
            case GameState.ArtistBattle:
            case GameState.DailyChallenge:
            default:
                return canvasLocator?.MainCanvas != null ? canvasLocator.MainCanvas.transform : null;
        }
    }

    private void EnsureGraphicRaycaster(Transform parentCanvas)
    {
        if (parentCanvas == null) return;

        Canvas canvas = parentCanvas.GetComponent<Canvas>();
        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }
    }
}
