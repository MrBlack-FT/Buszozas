using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

public class CustomSliderBackground : MonoBehaviour
{
    #region Változók

    private UIVars uiVars;
    [SerializeField] private Debugger debugger;

    private CustomPainter customPainter;
    private Image handleImage;
    [SerializeField] private float duration;

    #endregion

    #region Getterek és Setterek
    
    public float Duration           {get => duration; set => duration = value;}
    public Image HandleImage        {get => handleImage; set => handleImage = value;}

    #endregion

    #region Awake

    private void Awake()
    {
        uiVars = GameObject.Find("UIVars").GetComponent<UIVars>();
        if (uiVars == null)
        {
            Debug.LogWarning("UIVars not found in the scene.");
        }

        if (debugger == null)
        {
            debugger = Resources.FindObjectsOfTypeAll<Debugger>().FirstOrDefault();
        }

        customPainter = GameObject.Find("CustomPainter").GetComponent<CustomPainter>();
        if (customPainter == null)
        {
            Debug.LogWarning("CustomPainter not found in the scene.");
        }

        // Handle megkeresése
        Slider slider = GetComponent<Slider>();
        if (slider != null && slider.handleRect != null)
        {
            HandleImage = slider.handleRect.GetComponent<Image>();
            if (HandleImage == null)
            {
                Debug.LogWarning("No Image component found on Handle of " + gameObject.name);
            }
        }
        else
        {
            Debug.LogWarning("No Slider component or Handle found on " + gameObject.name);
        }

        // Slider regisztrálása a CustomPainter-ben
        if (customPainter != null)
        {
            //customPainter.SaveGOColorToDictionary(gameObject);
            customPainter.SaveGOColorToDictionary(HandleImage.gameObject);
        }

        EventTrigger eventTrigger = GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();

        // EventTrigger a Handle-re
        //EventTrigger eventTrigger = HandleImage.gameObject.GetComponent<EventTrigger>() ?? HandleImage.gameObject.AddComponent<EventTrigger>();

        // PointerEnter
        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((eventData) =>
        {
            //Debug.Log("🟨 PointerEnter");
            if (uiVars.IsPointerDown) return;
            //debugger.CustomDebugLog($"Selecting \"{HandleImage.name}\" because PointerEnter event was triggered.");
            EventSystem.current.SetSelectedGameObject(HandleImage.gameObject);
        });
        eventTrigger.triggers.Add(entryEnter);

        // PointerDown
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((eventData) =>
        {
            uiVars.IsPointerDown = true;
            if (HandleImage != null)
            {
                Sequence sequence = DOTween.Sequence();
                sequence.Append(HandleImage.DOColor(new Color(0.5f, 1, 0.5f), duration).SetEase(Ease.OutBack));
            }
            else
            {
                Debug.LogWarning("HandleImage is null on \"" + HandleImage.name + "\"");
            }
        });
        eventTrigger.triggers.Add(entryDown);

        // PointerUp
        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener((eventData) =>
        {
            uiVars.IsPointerDown = false;
        });
        eventTrigger.triggers.Add(entryUp);
    }

    private void Update()
    {
        if (debugger != null && debugger.gameObject.activeSelf)
        {
            debugger.UpdatePersistentLog("isPointerDown", debugger.ColoredString(uiVars.IsPointerDown ? "TRUE" : "FALSE", uiVars.IsPointerDown ? Color.green : Color.red));
        }
    }

    #endregion

    #region Metódusok

    #endregion
}