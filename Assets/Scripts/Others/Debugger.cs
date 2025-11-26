using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Debugger : MonoBehaviour
{
    #region Változók

    private TextMeshProUGUI outputText;
    private TextMeshProUGUI outputText2;
    private GameObject currentSelectedGameObject;

    private string debugText = "!DEBUG!\n";

    private string transientLogs = "";  // Csak a frame során hozzáadott logok
    private Dictionary<string, string> persistentLogs = new Dictionary<string, string>(); // Kulcs: komponens neve, Érték: státusz üzenet

    private GameObject[] interactivePanels;

    private float clearLogsTimer = 0f;

    private string debugTextForFile = "";
    
    // Debugger UI frissítési rate limiting
    private float debuggerUpdateTimer = 0f;
    private float debuggerUpdateInterval = 0.1f; // 10x per second instead of 60x

    #endregion

    #region Awake, Start, Update, LateUpdate

    private void Awake()
    {
        //outputText = GetComponentInChildren<TextMeshProUGUI>();
        outputText = transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
        outputText2 = transform.Find("BG_Text2/Text2 (TMP)")?.GetComponent<TextMeshProUGUI>();
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
        // Billentyűzet shortcut a debuglog fájlba mentéséhez
        if (Input.GetKeyDown(KeyCode.F12))
        {
            SaveDebugFile("DebuggerLog_ManualSave.txt");
        }

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
        
        // Ha outputText2 sorainak száma eléri a 25-öt, akkor töröljük a legrégebbi sorokat
    }

    // A LateUpdate-ben összeállítjuk az outputot
    void LateUpdate()
    {
        // Rate limiting: csak debuggerUpdateInterval másodpercenként frissítsd a UI-t
        debuggerUpdateTimer += Time.deltaTime;
        if (debuggerUpdateTimer < debuggerUpdateInterval)
        {
            return;
        }
        debuggerUpdateTimer = 0f;
        
        debugText = "";

        // Persistent logok hozzáadása
        foreach (var kvp in persistentLogs)
        {
            if (kvp.Key == "") // Üres kulcs esetén csak az értéket írjuk ki
            {
                debugText += kvp.Value + "\n";
                continue;
            }
            debugText += ColoredString(kvp.Key + ": ", Color.white) + kvp.Value + "\n";
        }

        // Transient logok hozzáadása (frame-es logok)
        debugText += transientLogs;

        // Összes Interactive Panel Panel és annak elemeinek státuszának kiírása
        foreach (var panel in interactivePanels)
        {
            //debugText += panel.gameObject.name + ": " + (panel.activeInHierarchy ? ColoredString("Active", Color.green) : ColoredString("Inactive", Color.red)) + "\n";
            foreach (var selectable in panel.GetComponentsInChildren<Selectable>())
            {
                //debugText += "  " + selectable.gameObject.name + ": " + (EventSystem.current.currentSelectedGameObject == selectable.gameObject ? ColoredString("Selected", Color.green) : ColoredString("Not Selected", Color.red)) + "\n";
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

        if (outputText2 != null)
        {
            string[] lines = outputText2.text.Split('\n');
            if (lines.Length > 9)
            {
                int excessLineCount = lines.Length - 9;
                outputText2.text = string.Join("\n", lines, excessLineCount, lines.Length - excessLineCount);
            }
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

    public void AddTextToDebugFile(string text)
    {
        outputText2.text += $"[{System.DateTime.Now:HH:mm:ss:fff}] {text}\n";
        debugTextForFile += $"[{System.DateTime.Now:HH:mm:ss:fff}] {text}\n";
    }

    public void SaveDebugFile(string filename)
    {
        if (string.IsNullOrEmpty(filename))
            filename = "DebuggerLog.txt";

        string name = System.IO.Path.GetFileNameWithoutExtension(filename);
        string ext = System.IO.Path.GetExtension(filename);
        string finalFilename = filename;

        if (true)
        {
            string pid = $"PID{System.Diagnostics.Process.GetCurrentProcess().Id}";
            string ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string guid = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            finalFilename = $"{name}_{pid}_{ts}_{guid}{ext}";
        }

        Debug.Log($"Debugger: Attempting to save debug file as {finalFilename}...");
        // Save to: "C:\111\BUSZOZAS_DEBUGOUTPUTS\"
        string outputDir = @"C:\111\BUSZOZAS_DEBUGOUTPUTS\";
        try
        {
            System.IO.Directory.CreateDirectory(outputDir);
            string fullPath = System.IO.Path.Combine(outputDir, finalFilename);
            System.IO.File.WriteAllText(fullPath, debugTextForFile);
            Debug.Log($"Debugger: Saved debug file to {fullPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Debugger: Failed to save debug file to {outputDir}: {ex.Message}");
            // Fallback to persistentDataPath
            try
            {
                string fallbackDir = Application.persistentDataPath;
                System.IO.Directory.CreateDirectory(fallbackDir);
                string fallbackPath = System.IO.Path.Combine(fallbackDir, finalFilename);
                System.IO.File.WriteAllText(fallbackPath, debugTextForFile);
                Debug.Log($"Debugger: Saved debug file to fallback path {fallbackPath}");
            }
            catch (System.Exception ex2)
            {
                Debug.LogError($"Debugger: Failed fallback save: {ex2.Message}");
            }
        }

        debugTextForFile = ""; // Kiürítjük a tartalmat mentés után
    }

    private void OnApplicationQuit()
    {
        
        if (!string.IsNullOrEmpty(debugTextForFile))
        {
            SaveDebugFile("DebuggerLog_OnQuit.txt");
        }
        
    }

    public void ClearDebugFileBuffer()
    {
        debugTextForFile = "";
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
