using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Debugger : MonoBehaviour
{
    #region Változók

    private TextMeshProUGUI outputText;
    private GameObject currentSelectedGameObject;

    private string debugText = "!DEBUG!\n";

    private string transientLogs = "";  // Csak a frame során hozzáadott logok
    private Dictionary<string, string> persistentLogs = new Dictionary<string, string>(); // Kulcs: komponens neve, Érték: státusz üzenet

    private GameObject[] interactivePanels;

    private float clearLogsTimer = 0f;

    #endregion

    #region Awake, Start, Update, LateUpdate

    private void Awake()
    {
        outputText = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Start()
    {
        GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        List<GameObject> panels = new List<GameObject>();
        foreach (GameObject obj in allGameObjects)
        {
            // Ellenőrizzük, hogy a GameObject rendelkezik-e a megfelelő tag-gel
            if (obj.CompareTag("Interactive Panel"))
            {
                panels.Add(obj);
            }
        }
        interactivePanels = panels.ToArray();
    }

    private void Update()
    {
        if(!gameObject.activeSelf)
        {
            return;
        }

        currentSelectedGameObject = EventSystem.current ? EventSystem.current.currentSelectedGameObject : null;
        
        //debugText = "";
        debugText += "currentSelectedGameObject: "
                  + (currentSelectedGameObject == null ? ColoredString("NULL", Color.red) : ColoredString(currentSelectedGameObject.name, Color.green))
                  + "\n";

        //outputText.text = debugText;
    }

    // A LateUpdate-ben összeállítjuk az outputot
    void LateUpdate()
    {
        debugText = "";

        // Persistent logok hozzáadása (például mindig friss állapot)
        foreach (var kvp in persistentLogs)
        {
            debugText += ColoredString(kvp.Key + ": ", Color.white) + kvp.Value + "\n";
        }

        // Transient logok hozzáadása (frame-es logok)
        debugText += transientLogs;

        // Összes Interactive Panel Panel és annak elemeinek státuszának kiírása
        foreach (var panel in interactivePanels)
        {
            debugText += panel.gameObject.name + ": " + (panel.activeInHierarchy ? ColoredString("Active", Color.green) : ColoredString("Inactive", Color.red)) + "\n";
            foreach (var selectable in panel.GetComponentsInChildren<Selectable>())
            {
                debugText += "  " + selectable.gameObject.name + ": " + (EventSystem.current.currentSelectedGameObject == selectable.gameObject ? ColoredString("Selected", Color.green) : ColoredString("Not Selected", Color.red)) + "\n";
            }
        }

        outputText.text = debugText;
        //Debug.Log(debugText);


        clearLogsTimer += Time.deltaTime;
        if (clearLogsTimer >= 2f)
        {
            transientLogs = ""; // Töröljük a transient logokat
            clearLogsTimer = 0f; // Timer visszaállítása
        }
    }

    #endregion

    #region Metódusok

    // Hívható a többi komponensből, hogy frissítsék a persistent státuszukat
    public void UpdatePersistentLog(string key, string message)
    {
        persistentLogs[key] = message;
    }

    public void CustomDebugLog(string message)
    {
        transientLogs += "DL  - " + message + "\n";
    }

    public void CustomDebugLog2(params (string text, Color color)[] messages)
    {
        transientLogs += "DL2 - ";
        foreach (var message in messages)
        {
            transientLogs += ColoredString(message.text, message.color) + "\t";
        }
        transientLogs += "\n";
    }

    public string ColoredString(string text, Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }

    private string ColorToString(Color color)
    {
        string colorCode = ColorUtility.ToHtmlStringRGB(color);

        switch (colorCode)
        {
            case "FFFF00": return "<color=yellow>Citrom</color>";
            case "FFFFFF": return "<color=white>Fehér</color>";
            case "800080": return "<color=#800080>Lila</color>"; // Lila hex kód
            default: return $"<color=#{colorCode}>{colorCode}</color>";
        }
    }
    
    #endregion
}
