#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif

using System;
using UnityEngine;
using TMPro;

public class BuildNumber : MonoBehaviour
{
    private void Start()
    {
        string buildVersion = PlayerPrefs.GetString("buildNumber", CalculateVersionNumber());

        #if UNITY_EDITOR
        buildVersion = $"(Editor - build {buildVersion})";
        #elif UNITY_ANDROID
        buildVersion = $"(Android - build {buildVersion})";
        #else
        buildVersion = $"(build {buildVersion})";
        #endif

        gameObject.GetComponent<TextMeshProUGUI>().text = buildVersion;
    }

    private static string CalculateVersionNumber()
    {
        // Alap dátum: 2025. március 16.
        DateTime startDate = new DateTime(2025, 3, 16);
        TimeSpan elapsedTime = DateTime.UtcNow - startDate;
        int elapsedDays = (int)elapsedTime.TotalDays;
        return $"{elapsedDays}";
    }

    #if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void SetBuildNumberInEditor()
    {
        string version = CalculateVersionNumber();
        PlayerSettings.bundleVersion = version;
        PlayerPrefs.SetString("buildNumber", version);
        PlayerPrefs.Save();
    }

    public class AutoIncrementBuildNumber : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            int elapsedDays = (int)(DateTime.UtcNow - new DateTime(2025, 3, 16)).TotalDays;

            string version = $"{elapsedDays}";
            PlayerSettings.bundleVersion = version;
            PlayerPrefs.SetString("buildNumber", version);
            PlayerPrefs.Save();

            Debug.Log($"Új verzió beállítva: {version}");
        }
    }
    #endif
}
