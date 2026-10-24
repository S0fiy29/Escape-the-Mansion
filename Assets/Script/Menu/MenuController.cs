using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Linq;


public class MenuController : MonoBehaviour
{
    [Header("Volume Setting")]
    [SerializeField] private TMP_Text volumeTextValue = null;
    [SerializeField] private Slider volumeSlider = null;
    [SerializeField] private float defaultVolume = 1.0f;

    [Header("Toggle Settings")]
    [SerializeField] private Toggle invertYToggle = null;

    [Header("Confirmation")]
    [SerializeField] private GameObject confirmationPrompt = null;

    [Header("Graphics Settings")]
    [SerializeField] private Slider brightnessSlider = null;
    [SerializeField] private TMP_Text brightnessTextValue = null;
    [SerializeField] private float defaultBrightness = 1;

    [Space(10)]
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle fullScreenToggle;


    // The menu choice (Low, Medium, High, Ultra), not the Unity quality level. See DisplaySettings.
    private int _qualityLevel;
    private bool _isFullScreen;
    private float _brightnessLevel;

    [Header("Levels To Load")]
    public string _newGameLevel;
    private string levelToLoad;

    [SerializeField] private GameObject noSavedGameDialog = null;

    [Header("Resolution Dropdowns")]
    public TMP_Dropdown resolutionDropdown;
    private Resolution[] resolutions = new Resolution[0];

    // Set when a slot is loaded. The GameController of the next scene restores it.
    public static List<SaveData> LoadedSaveData;

    private void Start()
    {
        BuildResolutionList();
        SyncControlsWithSettings();
    }

    // One entry per screen size, with the window's current size selected.
    private void BuildResolutionList()
    {
        if (resolutionDropdown == null) return;

        resolutions = DisplaySettings.UniqueResolutions(Screen.resolutions);
        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(resolutions.Select(r => r.width + " x " + r.height).ToList());
        resolutionDropdown.SetValueWithoutNotify(DisplaySettings.ClosestResolution(resolutions, Screen.width, Screen.height));
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    // The controls start from what is really in effect. Without this, a first Apply would use the default values
    // of the private fields (quality 0, windowed) for anything the player did not touch.
    private void SyncControlsWithSettings()
    {
        _qualityLevel = DisplaySettings.ChoiceForLevel(QualitySettings.GetQualityLevel());
        _isFullScreen = Screen.fullScreenMode != FullScreenMode.Windowed;

        float minBrightness = brightnessSlider != null ? brightnessSlider.minValue : BrightnessOverlay.Min;
        float maxBrightness = brightnessSlider != null ? brightnessSlider.maxValue : BrightnessOverlay.Max;
        _brightnessLevel = Mathf.Clamp(PlayerPrefs.GetFloat(DisplaySettings.BrightnessKey, defaultBrightness), minBrightness, maxBrightness);

        if (qualityDropdown != null)
        {
            qualityDropdown.SetValueWithoutNotify(_qualityLevel);
            qualityDropdown.RefreshShownValue();
        }
        if (fullScreenToggle != null) fullScreenToggle.SetIsOnWithoutNotify(_isFullScreen);
        if (brightnessSlider != null) brightnessSlider.SetValueWithoutNotify(_brightnessLevel);
        if (brightnessTextValue != null) brightnessTextValue.text = _brightnessLevel.ToString("0.0");
        if (volumeSlider != null) volumeSlider.SetValueWithoutNotify(AudioListener.volume);
        if (volumeTextValue != null) volumeTextValue.text = AudioListener.volume.ToString("0.0");
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= resolutions.Length) return;

        Resolution resolution = resolutions[resolutionIndex];
        DisplaySettings.SetResolution(resolution.width, resolution.height);
    }

    // Asks which slot the new game will save to, then starts it.
    public void NewGameDialogYes()
    {
        SaveSlotsUI.Open(SaveSlotsUI.Mode.NewGame, slot =>
        {
            SaveSystem.ActiveSlot = slot;
            LoadedSaveData = null;
            HintManager.ResetProfile();
            Time.timeScale = 1f;
            SceneManager.LoadScene(_newGameLevel);
            return null;
        });
    }

    // Opens the slot list. It is opened even when there is no save, so a different save folder can be chosen.
    public void LoadGameDialogYes()
    {
        if (noSavedGameDialog != null)
        {
            noSavedGameDialog.SetActive(false);
        }

        SaveSlotsUI.Open(SaveSlotsUI.Mode.Load, slot => SaveSystem.LoadSlotIntoGame(slot, out string error) ? null : error);
    }

    public void ExitButton()
    {
        Application.Quit();
    }

    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        volumeTextValue.text = volume.ToString("0.0");
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.GetComponent<AudioSource>().volume = volume;
        }
    }

    public void VolumeApply()
    {
        PlayerPrefs.SetFloat(DisplaySettings.VolumeKey, AudioListener.volume);
        PlayerPrefs.Save();
        StartCoroutine(ConfirmationBox());
    }

    // The screen changes as the slider moves, so the player can see the effect. Apply saves it.
    public void SetBrightness(float brightness)
    {
        _brightnessLevel = brightness;
        if (brightnessTextValue != null) brightnessTextValue.text = brightness.ToString("0.0");
        BrightnessOverlay.Set(brightness);
    }

    public void SetFullScreen(bool isFullscreen)
    {
        _isFullScreen = isFullscreen;
    }

    public void SetQuality(int qualityIndex)
    {
        _qualityLevel = qualityIndex;
    }

    public void GraphicApply()
    {
        PlayerPrefs.SetFloat(DisplaySettings.BrightnessKey, _brightnessLevel);
        BrightnessOverlay.Set(_brightnessLevel);

        PlayerPrefs.SetInt(DisplaySettings.QualityKey, _qualityLevel);
        DisplaySettings.ApplyQuality(DisplaySettings.LevelForChoice(_qualityLevel));

        PlayerPrefs.SetInt(DisplaySettings.FullscreenKey, _isFullScreen ? 1 : 0);
        DisplaySettings.ApplyFullscreen(_isFullScreen);

        PlayerPrefs.Save();
        StartCoroutine(ConfirmationBox());
    }

    public void ResetButton(string MenuType)
    {
        if(MenuType == "Audio")
        {
            AudioListener.volume = defaultVolume;
            volumeSlider.value = defaultVolume;
            volumeTextValue.text = defaultVolume.ToString("0.0");
            VolumeApply();
        }
        if(MenuType == "Graphics")
        {
            // Defaults are what the game starts with: a fullscreen window at the screen's own size, Medium quality.
            _brightnessLevel = defaultBrightness;
            _qualityLevel = DisplaySettings.DefaultQualityChoice;
            _isFullScreen = true;
            if (brightnessSlider != null) brightnessSlider.SetValueWithoutNotify(defaultBrightness);
            if (brightnessTextValue != null) brightnessTextValue.text = defaultBrightness.ToString("0.0");
            if (qualityDropdown != null)
            {
                qualityDropdown.SetValueWithoutNotify(_qualityLevel);
                qualityDropdown.RefreshShownValue();
            }
            if (fullScreenToggle != null) fullScreenToggle.SetIsOnWithoutNotify(true);

            DisplaySettings.ApplyFullscreen(true);
            Resolution native = Screen.currentResolution;
            Screen.SetResolution(native.width, native.height, FullScreenMode.FullScreenWindow);
            DisplaySettings.ClearSavedResolution();
            if (resolutionDropdown != null && resolutions.Length > 0)
            {
                resolutionDropdown.SetValueWithoutNotify(DisplaySettings.ClosestResolution(resolutions, native.width, native.height));
                resolutionDropdown.RefreshShownValue();
            }

            GraphicApply();
        }
    }

    public IEnumerator ConfirmationBox()
    {
        if (confirmationPrompt == null) yield break;

        confirmationPrompt.SetActive(true);
        yield return new WaitForSeconds(2);
        confirmationPrompt.SetActive(false);
    }
}
