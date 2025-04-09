using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomNavigationManager : MonoBehaviour
{
    #region Változók

    [SerializeField] private UIVars uiVars;
    [SerializeField] private CustomPainter customPainter;
    
    
    private GameObject _currentActivePanel;
    private GameObject _currentSelected;
    private GameObject _lastSelected;

    private Debugger debugger;

    #endregion


    #region Getterek és Setterek
    
    public GameObject CurrentActivePanel { get => _currentActivePanel; set => _currentActivePanel = value; }
    public GameObject CurrentSelected    { get => _currentSelected; set => _currentSelected = value; }
    public GameObject LastSelected       { get => _lastSelected; set => _lastSelected = value; }
    
    #endregion


    #region Awake, Start, Update

    private void Awake()
    {
        GameObject debugPanel = GameObject.Find("!DEBUGGER!");
        if (debugPanel != null)
        {
            debugger = debugPanel.GetComponent<Debugger>();
        }
        else
        {
            Debug.LogWarning("!DEBUGGER! GameObject not found in the scene.");
        }

        if (uiVars == null)
        {
            Debug.LogWarning("UIVars is not set in the Inspector!");
        }

        if (customPainter == null)
        {
            Debug.LogWarning("CustomPainter is not set in the Inspector!");
        }
    }

    private void Start()
    {
        foreach (GameObject panel in uiVars.InteractivePanels)
        {
            // Keresd meg az összes Selectable komponenst a panel gyermekeiben
            Selectable[] selectables = panel.GetComponentsInChildren<Selectable>();

            foreach (Selectable selectable in selectables)
            {
                /*
                // Ellenőrizd, hogy van-e Image komponens
                Image image = selectable.GetComponent<Image>();
                if (image != null)
                {
                    customPainter.AddColorToDictionary(selectable.gameObject, image.color);
                }
                else
                {
                    Debug.LogWarning($"Found Selectable GameObject without Image component: {selectable.gameObject.name}");
                }
                */
                customPainter.SaveGOColorToDictionary(selectable.gameObject);
            }
        }
    }

    private void Update()
    {
        // Ellenőrizzük, hogy változott-e az aktív panel
        GameObject newActivePanel = GetActivePanel();
        if (newActivePanel != CurrentActivePanel)
        {
            // Ha változott, frissítjük az aktív panelt
            CurrentActivePanel = newActivePanel;

            // Kijelöljük az első Selectable elemet az új aktív panelen
            if (CurrentActivePanel != null)
            {
                SelectFirstSelectable(CurrentActivePanel);
            }
        }

        // Az aktuálisan kiválasztott UI elem lekérése
        CurrentSelected = EventSystem.current.currentSelectedGameObject;

        // Ha nincs kijelölt elem, próbáljunk kijelölni egyet
        if (CurrentSelected == null)
        {
            if (LastSelected != null)
            {
                // Jelöljük ki az utoljára kijelölt elemet
                EventSystem.current.SetSelectedGameObject(LastSelected);
            }
            else if (CurrentActivePanel != null)
            {
                // Ha nincs utoljára kijelölt elem, jelöljük ki az aktuális panel első elemét
                SelectFirstSelectable(CurrentActivePanel);
            }
            else    return; // Ha nincs aktív panel, nem csinálunk semmit
        }

        // Ha változott a kiválasztás
        if (CurrentSelected != LastSelected)
        {
            // Az előzőleg kiválasztott elem visszaállítása az eredeti színre
            if (LastSelected != null)
            {
                /*
                Image lastImage = LastSelected.GetComponent<Image>();
                if (lastImage != null && customPainter.OriginalColors.TryGetValue(LastSelected, out Color p_originalColor))
                {
                    lastImage.color = p_originalColor;
                }
                */
                customPainter.ResetColor(LastSelected);
            }

            // Az aktuálisan kiválasztott elem háttérszínének beállítása narancsra
            if (CurrentSelected != null)
            {
                /*
                Image currentImage = CurrentSelected.GetComponent<Image>();
                if (currentImage != null)
                {
                    // Ha még nincs eltárolva az eredeti szín, akkor elmentjük
                    if (!customPainter.OriginalColors.ContainsKey(CurrentSelected))
                    {
                        customPainter.OriginalColors[CurrentSelected] = currentImage.color;
                    }
                    currentImage.color = new Color(1f, 0.5f, 0f); // Narancssárga
                }
                */
                customPainter.ChangeColor(CurrentSelected, new Color(1f, 0.5f, 0f)); // Narancssárga
            }

            // Persistent státusz frissítése a Debugger-ben
            if (debugger != null)
            {
                string currentStatus = debugger.ColoredString(CurrentSelected != null ? CurrentSelected.name : "NULL", 
                                                     CurrentSelected != null ? Color.green : Color.red);
                string lastStatus = debugger.ColoredString(LastSelected != null ? LastSelected.name : "NULL", 
                                                  LastSelected != null ? Color.green : Color.red);

                debugger.UpdatePersistentLog("Current Selected", currentStatus);
                debugger.UpdatePersistentLog("Last Selected", lastStatus);
            }

            LastSelected = CurrentSelected;
        }
    }

    #endregion


    #region Metódusok

    // Segédfüggvény az aktív panel lekérésére
    private GameObject GetActivePanel()
    {
        foreach (GameObject panel in uiVars.InteractivePanels)
        {
            if (panel.activeSelf) // Ellenőrizzük, hogy a panel aktív-e
            {
                return panel;
            }
        }
        return null;
    }

    // Segédfüggvény az első Selectable kijelölésére
    private void SelectFirstSelectable(GameObject panel)
    {
        Selectable[] selectables = panel.GetComponentsInChildren<Selectable>();
        if (selectables.Length > 0)
        {
            EventSystem.current.SetSelectedGameObject(selectables[0].gameObject);
        }
    }
    
    #endregion
}