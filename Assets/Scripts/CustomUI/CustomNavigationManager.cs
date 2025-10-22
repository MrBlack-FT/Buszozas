using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

public class CustomNavigationManager : MonoBehaviour
{
    #region Változók

    [SerializeField] private UIVars uiVars;
    [SerializeField] private CustomPainter customPainter;
    [SerializeField] private Debugger debugger;
    
    private GameObject _currentActivePanel;
    private GameObject _currentSelected;
    private GameObject _lastSelected;


    #endregion


    #region Getterek és Setterek
    
    public GameObject CurrentActivePanel { get => _currentActivePanel; set => _currentActivePanel = value; }
    public GameObject CurrentSelected    { get => _currentSelected; set => _currentSelected = value; }
    public GameObject LastSelected       { get => _lastSelected; set => _lastSelected = value; }
    
    #endregion


    #region Awake, Start, Update

    private void Awake()
    {
        if (debugger == null)
        {
            debugger = Resources.FindObjectsOfTypeAll<Debugger>().FirstOrDefault();
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
        /*
        foreach (GameObject panel in uiVars.InteractivePanels)
        {
            // Keresd meg az összes Selectable komponenst a panel gyermekeiben
            Selectable[] selectables = panel.GetComponentsInChildren<Selectable>();

            foreach (Selectable selectable in selectables)
            {
                customPainter.SaveGOColorToDictionary(selectable.gameObject);
            }
        }
        */
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            debugger.gameObject.SetActive(!debugger.gameObject.activeSelf);
        }

        // Ellenőrizzük, hogy változott-e az aktív panel
        GameObject newActivePanel = GetActivePanel();
        if (newActivePanel != CurrentActivePanel)
        {
            // Ha változott, frissítjük az aktív panelt
            CurrentActivePanel = newActivePanel;

            // Kijelöljük az első Selectable elemet az új aktív panelen
            if (CurrentActivePanel != null)
            {
                if (debugger != null && debugger.gameObject.activeSelf)
                {
                    debugger.CustomDebugLog($"Selecting first selectable in {CurrentActivePanel.name} panel");
                }
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
                debugger.CustomDebugLog($"Re-selecting last selected: {LastSelected.name}");
                EventSystem.current.SetSelectedGameObject(LastSelected);
            }
            else if (CurrentActivePanel != null)
            {
                // Ha nincs utoljára kijelölt elem, jelöljük ki az aktuális panel első elemét
                debugger.CustomDebugLog($"Selecting first selectable in {CurrentActivePanel.name} panel \n because there is no last selected element");
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
                if (LastSelected.name == "Blocker") return;

                // Ha a LastSelected slider, akkor a handle-t állítjuk vissza
                if (LastSelected.CompareTag("Slider"))
                {
                    /*
                    Slider slider = LastSelected.GetComponent<Slider>();
                    if (slider != null && slider.handleRect != null)
                    {
                        customPainter.ResetColor(slider.handleRect.gameObject);
                    }
                    */
                    customPainter.ResetColor(LastSelected.GetComponent<Slider>().handleRect.gameObject);
                }
                else if (LastSelected.CompareTag("Toggle"))     // Ha toggle, akkor a Background-ot
                {
                    customPainter.ResetColor(LastSelected.transform.Find("Background").gameObject);
                }
                else if (LastSelected.CompareTag("DropdownItem"))
                {
                    customPainter.ResetColor(LastSelected.transform.Find("Item Background").gameObject);
                }
                else
                {
                    customPainter.ResetColor(LastSelected);
                }
            }

            // Az aktuálisan kiválasztott elem háttérszínének beállítása narancsra
            if (CurrentSelected != null)
            {
                if (CurrentSelected.name == "Blocker") return;

                // Ha a CurrentSelected slider, akkor a handle-t állítjuk be
                Slider slider = CurrentSelected.GetComponent<Slider>();
                if (slider != null && slider.handleRect != null)
                {
                    customPainter.ChangeColor(slider.handleRect.gameObject, new Color(1f, 0.5f, 0f)); // Narancssárga
                }
                else if (CurrentSelected.CompareTag("Toggle"))  // Ha toggle, akkor a Background-ot
                {
                    customPainter.ChangeColor(CurrentSelected.transform.Find("Background").gameObject, new Color(1f, 0.5f, 0f));
                }
                else if (CurrentSelected.CompareTag("DropdownItem"))
                {
                    customPainter.ChangeColor(CurrentSelected.transform.Find("Item Background").gameObject, new Color(1f, 0.5f, 0f));
                }
                else
                {
                    customPainter.ChangeColor(CurrentSelected, new Color(1f, 0.5f, 0f));
                }
            }

            // Persistent státusz frissítése a Debugger-ben
            if (debugger != null && debugger.gameObject.activeSelf)
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