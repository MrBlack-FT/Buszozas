using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using System.Linq;

public class CustomDropdownItemBackground : MonoBehaviour
{
    #region Változók

    private UIVars uiVars;
    [SerializeField] private Debugger debugger;

    private CustomPainter customPainter;
    [SerializeField]private Image _backgroundImage;
    [SerializeField] private float duration;

    #endregion

    #region Getterek és Setterek
    
    public float Duration                   { get => duration; set => duration = value; }
    public Image BackgroundImage           { get => _backgroundImage; set => _backgroundImage = value; }

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

        if (BackgroundImage == null)
        {
            BackgroundImage = transform.Find("Item Background")?.GetComponent<Image>();
            if (BackgroundImage == null)
            {
                Debug.LogWarning($"No Image component found on 'Item Background' of {gameObject.name}");
            }
        }

        EventTrigger eventTrigger = GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();

        // PointerEnter
        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((eventData) =>
        {
            if (uiVars.IsPointerDown) return;
            //debugger.CustomDebugLog($"Selecting {gameObject.name} dropdown item because PointerEnter event was triggered.");
            EventSystem.current.SetSelectedGameObject(gameObject);
        });
        eventTrigger.triggers.Add(entryEnter);

        // PointerDown
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((eventData) =>
        {
            uiVars.IsPointerDown = true;
            //Sequence sequence = DOTween.Sequence();
            if (BackgroundImage != null)
            {
                //sequence.Append(BackgroundImage.DOColor(new Color(0.5f, 1, 0.5f), duration).SetEase(Ease.OutBack));
                BackgroundImage.color = new Color(0.5f, 1, 0.5f);
            }
            else
            {
                Debug.LogWarning($"BackgroundImage is null on \"{gameObject.name}\". Cannot change color.");
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

        // Scroll esemény átadása a szülőnek (nem blokkolni)
        EventTrigger.Entry entryScroll = new EventTrigger.Entry();
        entryScroll.eventID = EventTriggerType.Scroll;
        entryScroll.callback.AddListener((eventData) =>
        {
            // Továbbítjuk a scroll eseményt a szülőnek
            ScrollRect parentScrollRect = GetComponentInParent<ScrollRect>();
            if (parentScrollRect != null)
            {
                parentScrollRect.OnScroll(eventData as PointerEventData);
            }
        });
        eventTrigger.triggers.Add(entryScroll);

        //Debug.Log($"CustomDropdownItemBackground initialized on \"{gameObject.name}\"");
    }

    private void Update()
    {
        if (debugger != null && debugger.gameObject.activeSelf)
        {
            string PointerDownStatus = debugger.ColoredString(uiVars.IsPointerDown ? "TRUE" : "FALSE", uiVars.IsPointerDown ? Color.green : Color.red);
            debugger.UpdatePersistentLog("isPointerDown", PointerDownStatus);
        }
    }

    #endregion

    #region Metódusok

    #endregion
}