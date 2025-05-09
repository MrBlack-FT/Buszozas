using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class CustomButtonBackground : MonoBehaviour
{
    #region Változók

    private UIVars uiVars;
    private Debugger debugger;

    private CustomPainter customPainter;
    private Color _backgroundColor;
    [SerializeField] private float duration;

    #endregion

    #region Getterek és Setterek
    
    public Color BackgroundColor    {get => _backgroundColor; set => _backgroundColor = value;}
    public float Duration           {get => duration; set => duration = value;}

    #endregion

    #region Awake

    private void Awake()
    {
        uiVars = GameObject.Find("UIVars").GetComponent<UIVars>();
        if (uiVars == null)
        {
            Debug.LogWarning("UIVars not found in the scene.");
        }

        GameObject debugPanel = GameObject.Find("!DEBUGGER!");
        if (debugPanel != null)
        {
            debugger = debugPanel.GetComponent<Debugger>();
        }
        else
        {
            Debug.LogWarning("!DEBUGGER! GameObject not found in the scene.");
        }

        BackgroundColor = GetComponent<Image>().color;

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
            //Debug.Log("🟨 PointerEnter");
            if (uiVars.IsPointerDown) return;
            debugger.CustomDebugLog($"Selecting {gameObject.name} button because PointerEnter event was triggered.");
            EventSystem.current.SetSelectedGameObject(gameObject);
        });
        eventTrigger.triggers.Add(entryEnter);

        // PointerDown
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((eventData) =>
        {
            //Debug.Log("🟧 PointerDown");
            uiVars.IsPointerDown = true;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(GetComponent<Image>().DOColor(new Color(0.5f, 1, 0.5f), duration).SetEase(Ease.OutBack));
            //sequence.OnComplete(() => Debug.Log("Sequence: DOColor completed!"));
            GetComponent<Button>().onClick.Invoke();
        });
        eventTrigger.triggers.Add(entryDown);

        
        // PointerUp
        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener((eventData) =>
        {
            //Debug.Log("🟩 PointerUp");
            uiVars.IsPointerDown = false;
            //customPainter.ResetColor(gameObject);
        });
        eventTrigger.triggers.Add(entryUp);
    }

    private void Update()
    {
        if (debugger != null)
        {
            string PointerDownStatus = debugger.ColoredString(uiVars.IsPointerDown ? "TRUE" : "FALSE", uiVars.IsPointerDown ? Color.green : Color.red);
            debugger.UpdatePersistentLog("isPointerDown", PointerDownStatus);
        }
    }

    #endregion

    #region Metódusok

    #endregion
}
