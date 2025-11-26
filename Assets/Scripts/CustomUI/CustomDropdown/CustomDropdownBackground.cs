using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

public class CustomDropdownBackground : MonoBehaviour
{
    #region Változók

    private UIVars uiVars;
    [SerializeField] private Debugger debugger;

    private CustomPainter customPainter;
    [SerializeField] private float duration;

    #endregion

    #region Getterek és Setterek
    
    public float Duration { get => duration; set => duration = value; }

    #endregion

    #region Unity metódusok

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

        EventTrigger eventTrigger = GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();

        // PointerEnter
        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((eventData) =>
        {
            if (uiVars.IsPointerDown) return;
            //debugger.CustomDebugLog($"Selecting {gameObject.name} dropdown because PointerEnter event was triggered.");
            EventSystem.current.SetSelectedGameObject(gameObject);
        });
        eventTrigger.triggers.Add(entryEnter);

        // PointerDown
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((eventData) =>
        {
            uiVars.IsPointerDown = true;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(GetComponent<Image>().DOColor(new Color(0.5f, 1, 0.5f), duration).SetEase(Ease.OutBack));
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