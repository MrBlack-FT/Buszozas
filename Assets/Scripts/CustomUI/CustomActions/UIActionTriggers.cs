using UnityEngine;
using System.Collections.Generic;

public class UIActionTriggers : MonoBehaviour
{
    #region Változók

    private UIVars uiVars;
    private UIActionSequence uiActionSequence;
    private List<UIActionConfig> TheSequenceList = new List<UIActionConfig>();

    #endregion

    // Inspector-ban megjelenő listák, elemeik ott vannak meghatározva.
    [Header("UI Action Configurations")]
    [Header("Itt kell beállítani az animációkat és eseményeket!")]
    [Header("Singleplayer")]
    public List<UIActionConfig> OpenSingleplayerActions;
    public List<UIActionConfig> CloseSingleplayerActions;
    [Header("Multiplayer")]
    public List<UIActionConfig> OpenMultiplayerActions;
    public List<UIActionConfig> CloseMultiplayerActions;
    [Header("Main Menu")]
    public List<UIActionConfig> OpenMainMenuActions;
    public List<UIActionConfig> CloseMainMenuActions;
    [Header("Settings")]
    public List<UIActionConfig> OpenSettingsActions;
    public List<UIActionConfig> CloseSettingsActions;
    [Header("Rules")]
    public List<UIActionConfig> OpenRulesActions;
    public List<UIActionConfig> CloseRulesActions;
    [Header("Credits")]
    public List<UIActionConfig> OpenCreditsActions;
    public List<UIActionConfig> CloseCreditsActions;
    [Header("Exit")]
    public List<UIActionConfig> OpenExitActions;
    public List<UIActionConfig> CloseExitActions;

    #region Awake
    private void Awake()
    {
        uiVars = GameObject.Find("UIVars").GetComponent<UIVars>();

        if (uiVars == null) Debug.LogWarning("UIVars not found in the scene.");

        uiActionSequence = GameObject.Find("UIActionSequence").GetComponent<UIActionSequence>();

        if (uiActionSequence == null) Debug.LogWarning("UIActionSequence not found in the scene.");

    }

    #endregion

    #region Wrapper Metódusok

    // A Wrappereket a gombok fogják hívni.
    public void AddOpenSinglePlayer() => TheSequenceList.AddRange(OpenSingleplayerActions);
    public void AddCloseSinglePlayer() => TheSequenceList.AddRange(CloseSingleplayerActions);

    public void AddOpenMultiplayer() => TheSequenceList.AddRange(OpenMultiplayerActions);
    public void AddCloseMultiplayer() => TheSequenceList.AddRange(CloseMultiplayerActions);

    public void AddOpenMainMenu() => TheSequenceList.AddRange(OpenMainMenuActions);
    public void AddCloseMainMenu() => TheSequenceList.AddRange(CloseMainMenuActions);

    public void AddOpenSettings() => TheSequenceList.AddRange(OpenSettingsActions);
    public void AddCloseSettings() => TheSequenceList.AddRange(CloseSettingsActions);

    public void AddOpenRules() => TheSequenceList.AddRange(OpenRulesActions);
    public void AddCloseRules() => TheSequenceList.AddRange(CloseRulesActions);

    public void AddOpenCredits() => TheSequenceList.AddRange(OpenCreditsActions);
    public void AddCloseCredits() => TheSequenceList.AddRange(CloseCreditsActions);

    public void AddOpenExit() => TheSequenceList.AddRange(OpenExitActions);
    public void AddCloseExit() => TheSequenceList.AddRange(CloseExitActions);

    #endregion


    #region Metódusok

    // A TheSequenceList tartalmát lefuttatja.
    public void RunSequence(string actionName)
    {
        //Debug.Log($"{actionName} RunSequence!");
        if (uiVars.IsMenuTransitioning || TheSequenceList.Count == 0)
        {
            Debug.LogWarning($"TheSequenceList is empty or menu is already transitioning, skipping {actionName} actions!");
            return;
        }

        //Debug
        /*
        int index = 0;
        foreach (var a in TheSequenceList)
        {
            string targetInfo = "";
            if (a.target == null)
            {
                targetInfo = "NULL";
            }
            else if (a.target)
            {
                targetInfo = $"{a.target.name} (Active: {a.target.activeInHierarchy})";
            }
            else
            {
                targetInfo = "DESTROYED (Unity fake null)";
            }
            
            Debug.Log($"Action {index++}: {a.actionType} | Timing: {a.timing} | Target: {targetInfo}");
        }
        */

        uiActionSequence.BuildAndRunSequence(TheSequenceList);

        TheSequenceList.Clear(); // fontos, hogy ne duplázódjon a következő futáskor!
    }

    #endregion

}