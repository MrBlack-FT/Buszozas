using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MyClickHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public Button button;

    private GameObject TesztButton;

    void Awake()
    {
        //Összes gameobject tárolása
        GameObject[] allGameObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject obj in allGameObjects)
        {
            if (obj.name == "TesztButton")
            {
                TesztButton = obj;
                break;
            }
        }

        EventTrigger eventTrigger = TesztButton.GetComponent<EventTrigger>() ?? TesztButton.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerClick
        };
        entry.callback.AddListener((eventData) => 
        {
            Debug.Log("🟨 EventTrigger → PointerClick (TesztButton)");
        });
        eventTrigger.triggers.Add(entry);
    }

    void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(() =>
            {
                Debug.Log("🟩 Button.onClick UnityEvent");
            });
        }
    }

    // Ez akkor fut le, ha az objektumra kattintanak (még a Button.onClick előtt)
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("🟦 OnPointerClick (IPointerClickHandler)");
        //Debug.Log("button.onClick.Invoke()");
        //button.onClick.Invoke();
        //Debug.Log("button.onClick.Invoke() END");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("🟪 OnPointerDown (IPointerDownHandler)");
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("🟫 OnPointerUp (IPointerUpHandler)");
    }

    public void TriggerEvent()
    {
        Debug.Log("🟥 EventTrigger → PointerClick");
    }
}
