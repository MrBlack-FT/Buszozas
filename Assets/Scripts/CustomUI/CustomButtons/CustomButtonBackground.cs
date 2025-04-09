using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class CustomButtonBackground : CustomButtonBase
{
    #region Változók

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
        BackgroundColor = GetComponent<Image>().color;

        customPainter = GameObject.Find("CustomPainter").GetComponent<CustomPainter>();
        if (customPainter == null)
        {
            Debug.LogWarning("CustomPainter not found in the scene.");
        }


        EventTrigger eventTrigger = GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.PointerClick;
        entry.callback.AddListener((eventData) => 
        {
            Debug.Log("🟨 EventTrigger → PointerClick (CustomButtonBackground)");
        });
        eventTrigger.triggers.Add(entry);

        EventTrigger.Entry entry2 = new EventTrigger.Entry();
        entry2.eventID = EventTriggerType.PointerDown;
        entry2.callback.AddListener((eventData) => 
        {
            Debug.Log("🟧 EventTrigger → PointerDown (CustomButtonBackground)");
        });
        eventTrigger.triggers.Add(entry2);

        GetComponent<Button>().onClick.AddListener(() =>
        {
            Debug.Log("🟩 Button.onClick UnityEvent (CustomButtonBackground)");
        });

    }

    #endregion

    #region Metódusok

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (!GetComponent<Button>().interactable) return;

        //Debug.Log("Pointer Enter");
        EventSystem.current.SetSelectedGameObject(gameObject);
        //GetComponent<Image>().color = new Color(1, 0.5f, 0);

        //customPainter.ChangeColor(gameObject, new Color(1, 0.5f, 0));

        //GetComponent<Image>().DOFade(1, duration).SetEase(Ease.InOutSine);

        /*
        Kísérlet későbbre...
        GetComponent<Image>().DOColor(new Color(1, 0.5f, 0), Duration).SetEase(Ease.InBack).OnComplete(() =>
        {
            GetComponent<Image>().DOColor(BackgroundColor, Duration).SetEase(Ease.OutBack);
        });
        */
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        if (!GetComponent<Button>().interactable) return;

        //Debug.Log("Pointer Down");
        /*
        GetComponent<Image>().color = Color.green;
        GetComponent<Button>()?.onClick.Invoke();
        GetComponent<Image>().color = BackgroundColor;
        */

        //purple box debug log
        Debug.Log("🟪 OnPointerDown (IPointerDownHandler)");

        Sequence sequence = DOTween.Sequence();
        sequence.Append(GetComponent<Image>().DOColor(new Color(0.5f, 1, 0.5f), Duration).SetEase(Ease.OutBack).OnComplete(() =>
        {
             //GetComponent<Button>()?.onClick.Invoke();
        }));
        sequence.AppendCallback(() => customPainter.ResetColor(gameObject));
        /*
        customPainter.ChangeColor(gameObject, new Color(0.5f, 1, 0.5f));
        GetComponent<Button>()?.onClick.Invoke();
        customPainter.ResetColor(gameObject);
        */
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (!GetComponent<Button>().interactable) return;

        //Debug.Log("Pointer Click");
        /*
        GetComponent<Image>().color = Color.green;
        GetComponent<Button>()?.onClick.Invoke();
        GetComponent<Image>().color = BackgroundColor;
        */

        //blue box debug log
        Debug.Log("🟦 OnPointerClick (IPointerClickHandler)");

        customPainter.ChangeColor(gameObject, new Color(0.5f, 1, 0.5f));
        /*
        Debug.Log("CBB - Button clicked: " + gameObject.name);
        Debug.Log("eventData: " + eventData);
        Debug.Log("eventData.pointerPress: " + eventData.pointerPress);
        //GetComponent<Button>()?.onClick.Invoke();
        Debug.Log("CBB - Button clicked: " + gameObject.name + " - Invoke() called.");
        */
        customPainter.ResetColor(gameObject);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        //Debug.Log("Pointer Exit");
        //GetComponent<Image>().color = BackgroundColor;

        //GetComponent<Image>().DOFade(0, duration).SetEase(Ease.InOutSine);

        //customPainter.ResetColor(gameObject);

        /*
        //Kísérlet későbbre...
        GetComponent<Image>().DOFade(0, duration).SetEase(Ease.InOutSine).OnComplete(() =>
        {
            GetComponent<Image>().DOFade(1, duration).SetEase(Ease.InOutSine);
        });
        */
    }
    
    #endregion
}
