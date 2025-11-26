using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
//using System.Collections.Generic;

public class LoadPrefs : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool canUse = false;
    [SerializeField] private HandleSettings settings;

    [SerializeField] private TMP_Text volumeText = null;
    [SerializeField] private Slider volumeSlider = null;

    [SerializeField] private TMP_Dropdown resolutionDropdown = null;

    [SerializeField] private Toggle fullscreenToggle = null;

    [SerializeField] private Toggle vsyncToggle = null;

    [SerializeField] private TMP_Text playerNameText_MJoin = null;
    [SerializeField] private TMP_Text playerNameText_MCreate = null;

    private void Start()
    {
        if ( volumeText == null || volumeSlider == null || resolutionDropdown == null || fullscreenToggle == null || vsyncToggle == null )
        {
            Debug.LogWarning("LoadPrefs: One or more UI components are not assigned in the inspector.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Game" && (playerNameText_MJoin == null || playerNameText_MCreate == null))
            {
                Debug.LogWarning("LoadPrefs: playerNameText_MJoin or playerNameText_MCreate is not assigned in the inspector.");
            }
            return;
        }
        
        if (canUse)
        {
            StartCoroutine(LoadSettingsWithDelay());
        }
    }

    private IEnumerator LoadSettingsWithDelay()
    {
        yield return new WaitUntil(() => resolutionDropdown.options.Count > 0);
        LoadSettings();
    }


    private void LoadSettings()
    {
        if (PlayerPrefs.HasKey("masterVolume"))
        {
            float localVolume = PlayerPrefs.GetFloat("masterVolume");

            volumeText.text = Mathf.RoundToInt(localVolume * 100).ToString();
            volumeSlider.value = localVolume * 100;
            AudioListener.volume = localVolume;
        }
        else
        {
            settings.setVolume(50);
        }

        if (PlayerPrefs.HasKey("fullscreen"))
        {
            bool localFullscreen = PlayerPrefs.GetInt("fullscreen") == 1;

            fullscreenToggle.isOn = localFullscreen;
            Screen.fullScreen = localFullscreen;
        }
        else
        {
            settings.setFullscreen(true);
        }

        if (PlayerPrefs.HasKey("vsync"))
        {
            bool localVsync = PlayerPrefs.GetInt("vsync") == 1;

            QualitySettings.vSyncCount = localVsync ? 1 : 0;
            vsyncToggle.isOn = localVsync;
        }
        else
        {
            settings.setVsync(true);
        }

        if (PlayerPrefs.HasKey("resolution"))
        {
            int localResolution = PlayerPrefs.GetInt("resolution");
            //resolutionDropdown.value = localResolution < resolutionDropdown.options.Count ? localResolution : 0;
            if (localResolution >= 0 && localResolution < resolutionDropdown.options.Count)
            {
                resolutionDropdown.value = localResolution;
            }
            else
            {
                resolutionDropdown.value = 0;
            }

            //resolutionDropdown.captionText.GetComponent<TMP_Text>().text = resolutionDropdown.options[resolutionDropdown.value].text;
            resolutionDropdown.RefreshShownValue();
        }
        else
        {
            settings.setResolution(0);
        }

        resolutionDropdown.RefreshShownValue();

        if (playerNameText_MJoin == null || playerNameText_MCreate == null) return;
        
        if (PlayerPrefs.HasKey("playerName"))
        {
            string localPlayerName = PlayerPrefs.GetString("playerName");
            playerNameText_MJoin.text = localPlayerName;
            playerNameText_MCreate.text = localPlayerName;
        }
        else
        {
            playerNameText_MJoin.text = "Névtelen Utas";
            playerNameText_MCreate.text = "Névtelen Utas";
        }
    }
}