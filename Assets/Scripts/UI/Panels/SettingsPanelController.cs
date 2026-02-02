using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SettingsPanelController : MonoBehaviour
{
    private const string DifficultyPrefKey = "PreferredDifficulty";

    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private TMP_Dropdown instrumentDropdown;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button cancelButton;

    private GameManager gameManager;
    private AudioManager audioManager;
    private InputManager inputManager;

    private float initialVolume;
    private DifficultyLevel initialDifficulty;
    private InstrumentType initialInstrument;

    public void Initialize(GameManager gameManager, AudioManager audioManager, InputManager inputManager)
    {
        this.gameManager = gameManager;
        this.audioManager = audioManager;
        this.inputManager = inputManager;

        CacheReferences();
        SetupDropdowns();
        LoadInitialValues();
        WireButtons();
    }

    private void CacheReferences()
    {
        if (volumeSlider == null)
        {
            var volume = transform.Find("VolumeSlider");
            if (volume != null)
            {
                volumeSlider = volume.GetComponent<Slider>();
            }
        }

        if (difficultyDropdown == null)
        {
            var difficulty = transform.Find("DifficultyDropdown");
            if (difficulty != null)
            {
                difficultyDropdown = difficulty.GetComponent<TMP_Dropdown>();
            }
        }

        if (instrumentDropdown == null)
        {
            var instrument = transform.Find("InstrumentDropdown");
            if (instrument != null)
            {
                instrumentDropdown = instrument.GetComponent<TMP_Dropdown>();
            }
        }

        if (saveButton == null)
        {
            var save = transform.Find("SaveButton");
            if (save != null)
            {
                saveButton = save.GetComponent<Button>();
            }
        }

        if (cancelButton == null)
        {
            var cancel = transform.Find("CancelButton");
            if (cancel != null)
            {
                cancelButton = cancel.GetComponent<Button>();
            }
        }
    }

    private void SetupDropdowns()
    {
        if (difficultyDropdown != null)
        {
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new System.Collections.Generic.List<string>(Enum.GetNames(typeof(DifficultyLevel))));
        }

        if (instrumentDropdown != null)
        {
            instrumentDropdown.ClearOptions();
            instrumentDropdown.AddOptions(new System.Collections.Generic.List<string>(Enum.GetNames(typeof(InstrumentType))));
        }
    }

    private void LoadInitialValues()
    {
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            initialVolume = audioManager != null ? audioManager.GetMasterVolume() : PlayerPrefs.GetFloat("MasterVolume", 1.0f);
            volumeSlider.SetValueWithoutNotify(initialVolume);
        }

        int difficultyValue = PlayerPrefs.GetInt(DifficultyPrefKey, 0);
        initialDifficulty = (DifficultyLevel)Mathf.Clamp(difficultyValue, 0, Enum.GetValues(typeof(DifficultyLevel)).Length - 1);
        if (difficultyDropdown != null)
        {
            difficultyDropdown.SetValueWithoutNotify((int)initialDifficulty);
        }

        initialInstrument = gameManager != null ? gameManager.GetSelectedInstrument() : InstrumentType.Piano;
        if (instrumentDropdown != null)
        {
            instrumentDropdown.SetValueWithoutNotify((int)initialInstrument);
        }
    }

    private void WireButtons()
    {
        if (saveButton != null)
        {
            saveButton.onClick.RemoveAllListeners();
            saveButton.onClick.AddListener(ApplyAndClose);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(CancelAndClose);
        }
    }

    private void ApplyAndClose()
    {
        if (audioManager != null && volumeSlider != null)
        {
            audioManager.SetMasterVolume(volumeSlider.value);
        }

        if (gameManager != null && instrumentDropdown != null)
        {
            gameManager.SetSelectedInstrument((InstrumentType)instrumentDropdown.value);
            gameManager.UpdatePlayerDataInMemory();
        }

        if (difficultyDropdown != null)
        {
            PlayerPrefs.SetInt(DifficultyPrefKey, difficultyDropdown.value);
        }

        PlayerPrefs.Save();
        gameManager?.CloseSettings();
    }

    private void CancelAndClose()
    {
        if (audioManager != null)
        {
            audioManager.SetMasterVolume(initialVolume);
        }

        if (gameManager != null)
        {
            gameManager.SetSelectedInstrument(initialInstrument);
            gameManager.UpdatePlayerDataInMemory();
        }

        PlayerPrefs.SetInt(DifficultyPrefKey, (int)initialDifficulty);
        PlayerPrefs.Save();
        gameManager?.CloseSettings();
    }
}
