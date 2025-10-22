using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using DG.Tweening;
using UnityEngine.UI;

public class CardManager : MonoBehaviour
{
    #region Változók
    private Card cardData;

    private UIVars uiVars;
    [SerializeField] private Debugger debugger;
    private CustomPainter customPainter;


    [SerializeField] private SpriteRenderer cardFrontRenderer;
    [SerializeField] private SpriteRenderer cardBackRenderer;

    #endregion

    #region Unity metódusok

    void Awake()
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
            //Debug.Log("🟨 PointerEnter");
            if (uiVars.IsPointerDown || gameObject.GetComponent<Selectable>().interactable == false) return;
            EventSystem.current.SetSelectedGameObject(gameObject);
        });
        eventTrigger.triggers.Add(entryEnter);

        // PointerDown
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((eventData) =>
        {
            uiVars.IsPointerDown = true;
            DragAndDrop(Input.mousePosition);

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

    void Start()
    {

    }

    void Update()
    {

    }

    #endregion

    #region metódusok

    public void InitializeCard(Card data, Sprite frontSprite, Sprite backSprite)
    {
        cardData = data;
        cardFrontRenderer.sprite = frontSprite;
        cardBackRenderer.sprite = backSprite;
    }

    public Card GetCardData()
    {
        return cardData;
    }

    public void ShowCardFront()
    {
        cardFrontRenderer.gameObject.SetActive(true);
        cardBackRenderer.gameObject.SetActive(false);
    }

    public void ShowCardBack()
    {
        cardFrontRenderer.gameObject.SetActive(false);
        cardBackRenderer.gameObject.SetActive(true);
    }

    public void FlipCard()
    {
        bool isFrontActive = cardFrontRenderer.gameObject.activeSelf;
        cardFrontRenderer.gameObject.SetActive(!isFrontActive);
        cardBackRenderer.gameObject.SetActive(isFrontActive);
    }

    public void SetCardPosition(Vector3 position)
    {
        transform.position = position;
    }

    public void SetCardRotation(Quaternion rotation)
    {
        transform.rotation = rotation;
    }

    public void SetCardScale(Vector3 scale)
    {
        transform.localScale = scale;
    }

    public void MoveCardTo(Vector3 targetPosition, float duration)
    {
        transform.DOMove(targetPosition, duration).SetEase(Ease.OutCubic);
    }

    public void DragAndDrop(Vector3 newPosition)
    {
        transform.position = newPosition;
    }

    public void AnimateCardFlip(float duration)
    {
        Sequence flipSequence = DOTween.Sequence();
        flipSequence.Append(transform.DORotate(new Vector3(0, 90, 0), duration / 2).SetEase(Ease.InCubic));
        flipSequence.AppendCallback(() => FlipCard());
        flipSequence.Append(transform.DORotate(new Vector3(0, 0, 0), duration / 2).SetEase(Ease.OutCubic));
    }

    #endregion
}