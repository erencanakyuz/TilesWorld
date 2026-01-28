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
        SetupPanelDictionary();
    }

    private void SetupPanelDictionary()
    {
        statePanelPrefabs = new Dictionary<GameState, GameObject>
        {
            { GameState.MainMenu, config != null ? config.mainMenuPanelPrefab : null },
            { GameState.SongSelection, config != null ? config.songSelectionPanelPrefab : null },
            { GameState.Playing, config != null ? config.gameplayPanelPrefab : null },
            { GameState.Paused, config != null ? config.pausePanelPrefab : null },
            { GameState.GameOver, config != null ? config.gameOverPanelPrefab : null }
        };
    }

    public void ShowPanelForState(GameState state)
    {
        HideCurrentPanel();

        Transform parentCanvas = GetParentCanvasForState(state);
        GameObject prefab = null;
        bool hasPrefab = statePanelPrefabs != null && statePanelPrefabs.TryGetValue(state, out prefab) && prefab != null;
        
        if (parentCanvas != null && hasPrefab)
        {
            currentPanelInstance = Object.Instantiate(prefab, parentCanvas);
        }

        switch (state)
        {
            case GameState.Paused:
                if (currentPanelInstance != null)
                {
                    PanelButtonWirer.WirePausePanel(currentPanelInstance, OnResumePressed, OnRestartPressed);
                }
                break;
            case GameState.GameOver:
                EnsureGraphicRaycaster(parentCanvas);
                if (currentPanelInstance != null)
                {
                    PanelButtonWirer.WireGameOverPanel(currentPanelInstance, OnRestartPressed, OnMainMenuPressed);
                }
                break;
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
                return canvasLocator?.OverlayCanvas != null ? canvasLocator.OverlayCanvas.transform : canvasLocator?.MainCanvas?.transform;
            case GameState.MainMenu:
            case GameState.SongSelection:
            case GameState.GameOver:
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
