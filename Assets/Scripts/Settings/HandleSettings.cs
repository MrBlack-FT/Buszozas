using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
//using System.Globalization;

public class HandleSettings : MonoBehaviour
{
    #region Változók
    [Header("Settings UI Elements")]
    [Header("Resolution Dropdowns")]
    [SerializeField] private TextMeshProUGUI    resolutionTextTitle;
    [SerializeField] private TMP_Dropdown       resolutionDropdown;
    [Header("Fullscreen")]
    [SerializeField] private TextMeshProUGUI    fullScreenTitle;
    [SerializeField] private Toggle             fullscreenToggle;
    [Header("Vsync")]
    [SerializeField] private TextMeshProUGUI    vsyncTitle;
    [SerializeField] private Toggle             vsyncToggle;
    [Header("Volume Settings")]
    [SerializeField] private TextMeshProUGUI    volumeTextTitle;
    [SerializeField] private Slider             volumeSlider;
    [SerializeField] private TextMeshProUGUI    volumeText;
    [Header("Apply Buttons")]
    [SerializeField] private Button             applyButton;

    private Resolution[] resolutions;

    // Temporary settings storage
    private float tempVolume;
    private int tempResolutionIndex;
    private bool tempIsFullscreen;
    private int tempVsyncCount;

    #endregion

    #region Awake és Start
    void Awake()
    {
        #if UNITY_ANDROID
        Application.targetFrameRate = 120;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
		#endif

        if (resolutionDropdown == null || fullscreenToggle == null || vsyncToggle == null || volumeSlider == null || applyButton == null)
        {
            Debug.LogWarning("HandleSettings: One or more UI components are not assigned in the inspector.");
            return;
        }

        #if UNITY_ANDROID// || UNITY_EDITOR
            resolutionTextTitle.gameObject.SetActive(false);
            resolutionDropdown.gameObject.SetActive(false);
            vsyncTitle.gameObject.SetActive(false);
            vsyncToggle.gameObject.SetActive(false);

            fullScreenTitle.rectTransform.anchoredPosition = new Vector2(fullScreenTitle.rectTransform.anchoredPosition.x, 200);
            fullscreenToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(fullscreenToggle.GetComponent<RectTransform>().anchoredPosition.x, 200);

            volumeTextTitle.rectTransform.anchoredPosition = new Vector2(volumeTextTitle.rectTransform.anchoredPosition.x, -50);
            volumeSlider.GetComponent<RectTransform>().anchoredPosition = new Vector2(volumeSlider.GetComponent<RectTransform>().anchoredPosition.x, -50);
            // A volumeText-et nem kell átrendezni, mert az a volumeSlider gyereke, így azzal együtt mozog.            
        #endif
    }

    void Start()
    {
        if (resolutionDropdown == null || fullscreenToggle == null || vsyncToggle == null || volumeSlider == null || applyButton == null)
        {
            Debug.LogWarning("HandleSettings: One or more UI components are not assigned in the inspector.");
            return;
        }

        applyButton.interactable = false;
        
        // Load initial settings - if they don't exist, set default values in second parameter
        tempVolume = PlayerPrefs.GetFloat("masterVolume", 1f) * 100;
        tempIsFullscreen = PlayerPrefs.GetInt("fullscreen", 1) == 1;
        tempVsyncCount = PlayerPrefs.GetInt("vsync", 0);

        // Initialize volume
        volumeSlider.value = tempVolume;
        setVolume(tempVolume);

        HashSet<Resolution> uniqueResolutions = new HashSet<Resolution>(Screen.resolutions.Where(i => i.refreshRateRatio.value >= 59 && i.refreshRateRatio.value < 70).ToHashSet());
        resolutions = uniqueResolutions.ToArray<Resolution>();

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
           string option = string.Format("{0} x {1}", resolutions[i].width, resolutions[i].height);
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width && resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        tempResolutionIndex = currentResolutionIndex;
        resolutionDropdown.value = tempResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // Update fullscreen and vsync settings
        Screen.fullScreen = tempIsFullscreen;
        QualitySettings.vSyncCount = tempVsyncCount;
    }
    #endregion

    #region Setterek
    public void setResolution(int resolutionIndex)
    {
        tempResolutionIndex = resolutionIndex;
        EnableApplyButton();
    }

    public void setVolume(float volume)
    {
        tempVolume = volume;
        AudioListener.volume = tempVolume / 100; // Scale volume to 0-1
        volumeText.text = Mathf.RoundToInt(tempVolume).ToString();
        EnableApplyButton();
    }

    public void setFullscreen(bool isFullscreen)
    {
        tempIsFullscreen = isFullscreen;
        EnableApplyButton();
    }

    public void setVsync(bool isVsync)
    {
        tempVsyncCount = isVsync ? 1 : 0;
        EnableApplyButton();
    }

    private void EnableApplyButton()
    {
        applyButton.interactable = true;
    }
    #endregion

    #region apply és reset
    public void applySettings()
    {
        AudioListener.volume = tempVolume / 100;
        PlayerPrefs.SetFloat("masterVolume", tempVolume / 100);

        #if !UNITY_ANDROID
        Resolution resolution = resolutions[tempResolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, tempIsFullscreen);
        PlayerPrefs.SetInt("resolution", tempResolutionIndex);
        #endif

        PlayerPrefs.SetInt("fullscreen", tempIsFullscreen ? 1 : 0);

        QualitySettings.vSyncCount = tempVsyncCount;
        PlayerPrefs.SetInt("vsync", tempVsyncCount);

        PlayerPrefs.Save();

        applyButton.interactable = false;
    }
	
    public void resetSettings()
    {
        tempVolume = 50f;
        tempResolutionIndex = resolutions.Length - 1; // (1920x1080 felbontás)
        tempIsFullscreen = true;
        tempVsyncCount = 0;

        volumeSlider.value = tempVolume;
        volumeText.text = Mathf.RoundToInt(tempVolume).ToString();

        resolutionDropdown.value = tempResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        fullscreenToggle.isOn = tempIsFullscreen;
        vsyncToggle.isOn = tempVsyncCount == 1;

        AudioListener.volume = tempVolume / 100;

        #if !UNITY_ANDROID
        Screen.SetResolution(resolutions[tempResolutionIndex].width, resolutions[tempResolutionIndex].height, tempIsFullscreen);
        #endif

        QualitySettings.vSyncCount = tempVsyncCount;

        PlayerPrefs.SetFloat("masterVolume", tempVolume / 100);
        PlayerPrefs.SetInt("resolution", tempResolutionIndex);
        PlayerPrefs.SetInt("fullscreen", tempIsFullscreen ? 1 : 0);
        PlayerPrefs.SetInt("vsync", tempVsyncCount);

        PlayerPrefs.Save();

        applyButton.interactable = false;
    }
    #endregion
}