using UnityEngine;
using System.Collections.Generic;

public class UIActionTriggers : MonoBehaviour
{
    private UIVars uiVars;
    private UIActionSequence uiActionSequence;

    [Header("UI Action Configurations")]
    [Header("Main Menu")]
    public List<UIActionConfig> OpenMainMenuActions;
    public List<UIActionConfig> CloseMainMenuActions;
    [Header("Settings")]
    public List<UIActionConfig> OpenSettingsActions;
    public List<UIActionConfig> CloseSettingsActions;
    [Header("Exit")]
    public List<UIActionConfig> OpenExitActions;
    public List<UIActionConfig> CloseExitActions;

    private void Awake()
    {
        uiVars = GameObject.Find("UIVars").GetComponent<UIVars>();

        if (uiVars == null) Debug.LogWarning("UIVars not found in the scene.");

        uiActionSequence = GameObject.Find("UIActionSequence").GetComponent<UIActionSequence>();

        if (uiActionSequence == null) Debug.LogWarning("UIActionSequence not found in the scene.");
    }

    public void RunActionSequence(List<UIActionConfig> actions, string actionName)
    {
        if (uiVars.IsMenuTransitioning)
        {
            Debug.LogWarning($"Menu is already transitioning, skipping {actionName} action!");
            return;
        }
        uiActionSequence.BuildAndRunSequence(actions);
    }


    // TEST
    public List<UIActionConfig> TheTestList = new List<UIActionConfig>();

    public void AddCloseMenu() => TheTestList.AddRange(CloseMainMenuActions);
    public void AddOpenMenu() => TheTestList.AddRange(OpenMainMenuActions);
    public void AddOpenSettings() => TheTestList.AddRange(OpenSettingsActions);
    public void AddCloseSettings() => TheTestList.AddRange(CloseSettingsActions);

    public void RunSequence()
    {
        if (uiVars.IsMenuTransitioning || TheTestList.Count == 0) return;

        int index = 0;
        foreach (var a in TheTestList)
        Debug.Log($"{index++} | {a.actionType} | {a.timing} | {a.target?.name}");

        RunActionSequence(TheTestList, "TEST SEQUENCE!");
        TheTestList.Clear(); // fontos, hogy ne duplázódjon a következő futáskor!
    }
    // TEST


    #region Wrapper Metódusok

    public void OpenMenu() => RunActionSequence(OpenMainMenuActions, "OPENING MENU");
    public void CloseMenu() => RunActionSequence(CloseMainMenuActions, "CLOSING MENU");

    public void OpenSettings() => RunActionSequence(OpenSettingsActions, "OPENING SETTINGS");
    public void CloseSettings() => RunActionSequence(CloseSettingsActions, "CLOSING SETTINGS");


    #endregion
}
