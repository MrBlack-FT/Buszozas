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

    private Image cardBackgroundColor;

    [SerializeField] private SpriteRenderer cardFrontRenderer;
    [SerializeField] private SpriteRenderer cardBackRenderer;

    private bool isDragging = false;
    private Vector3 dragOffset;
    
    private PlayerManager parentPlayerManager; // Szülő PlayerManager referencia
    private int cardSlotIndex = -1; // Hányadik slot ebben a PlayerManager-ben
    
    // Layout Group-ból való kilépéshez
    private Transform originalParent;
    private int originalSiblingIndex;
    private Transform playerTransform;

    #endregion

    #region Getterek és Setterek

    public Card CardData { get => cardData; set => cardData = value; }
    public bool IsDragging { get => isDragging; set => isDragging = value; }
    public Vector3 DragOffset { get => dragOffset; set => dragOffset = value; }

    public SpriteRenderer CardFrontRenderer { get => cardFrontRenderer; set => cardFrontRenderer = value; }
    public SpriteRenderer CardBackRenderer { get => cardBackRenderer; set => cardBackRenderer = value; }
    
    // Prefab-ben nem lehet beállítani.
    public void SetParentPlayerManager(PlayerManager manager, int slotIndex)
    {
        parentPlayerManager = manager;
        cardSlotIndex = slotIndex;
        
        if (manager != null)
        {
            playerTransform = manager.transform;
        }
    }

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

        // ====================

        cardBackgroundColor = GetComponent<Image>();

        EventTrigger eventTrigger = GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();


        // PointerEnter
        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((eventData) =>
        {
            //Debug.Log("🟨 PointerEnter in CardManager");
            if (uiVars.IsPointerDown || gameObject.GetComponent<Selectable>().interactable == false) return;

            cardBackgroundColor.DOColor(new Color(1f, 0.5f, 0f), 0.5f).SetEase(Ease.OutCubic);
            EventSystem.current.SetSelectedGameObject(gameObject);
        });
        eventTrigger.triggers.Add(entryEnter);

        // PointerDown
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((eventData) =>
        {
            //if (!gameObject.GetComponent<Selectable>().interactable) return;

            var selectable = gameObject.GetComponent<Selectable>();
            bool isInteractable = selectable != null && selectable.interactable;

            if (!isInteractable) return;

            uiVars.IsPointerDown = true;
            IsDragging = true;
            
            // KILÉPÉS A LAYOUT-BÓL: Parent átállítás Player-re
            SaveCardOriginalTransform();
            
            if (playerTransform != null)
            {
                transform.SetParent(playerTransform);
                transform.SetAsLastSibling(); // Legfelül rajzolódik (más kártyák fölött)
            }

            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPosition.z = transform.position.z;
            DragOffset = transform.position - mouseWorldPosition;

            DragAndDrop(Input.mousePosition);

        });
        eventTrigger.triggers.Add(entryDown);

        // PointerUp
        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener((eventData) =>
        {
            uiVars.IsPointerDown = false;
            
            bool successfulDrop = false;
            
            // Ellenőrizzük hogy piramis kártya fölött ejtettük-e el
            if (IsDragging && parentPlayerManager != null)
            {
                successfulDrop = CheckDropOnPiramisCard();
            }

            IsDragging = false;
            
            // Sikertelen drop esetén visszaállítjuk az eredeti helyre
            if (!successfulDrop && originalParent != null)
            {
                ResetCardOriginalTransform();
            }
        });
        eventTrigger.triggers.Add(entryUp);

        // PointerExit
        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((eventData) =>
        {
            if (uiVars.IsPointerDown) return;
            cardBackgroundColor.DOColor(new Color(0f, 0f, 0f, 0f), 0.5f).SetEase(Ease.OutCubic);
        });
        eventTrigger.triggers.Add(entryExit);
    }

    void Start()
    {
        
    }

    void Update()
    {
        if (debugger != null && debugger.gameObject.activeSelf)
        {
            string IsDraggingStatus = debugger.ColoredString(IsDragging ? "TRUE" : "FALSE", IsDragging ? Color.green : Color.red);
            debugger.UpdatePersistentLog("isDragging", IsDraggingStatus);
        }

        // Csak akkor húzza, ha EZEN a kártyán nyomták le az egérgombot
        if (IsDragging)
        {
            DragAndDrop(Input.mousePosition);
        }
    }

    #endregion

    #region metódusok

    public void SetInteractable(bool interactable)
    {
        var selectable = gameObject.GetComponent<Selectable>();
        if (selectable != null)
        {
            selectable.interactable = interactable;
        }
    }

    public bool GetInteractable()
    {
        var selectable = gameObject.GetComponent<Selectable>();
        if (selectable != null)
        {
            return selectable.interactable;
        }
        return false;
    }

    public void SetCard(Card card)
    {
        cardData = card;
        
        Sprite frontSprite = LoadCardFrontSprite(card);
        Sprite backSprite = LoadCardBackSprite(card);
        
        if (frontSprite != null) CardFrontRenderer.sprite = frontSprite;
        if (backSprite != null) CardBackRenderer.sprite = backSprite;
    }

    private Sprite LoadCardFrontSprite(Card card)
    {
        string suit = card.GetCardType() switch
        {
            CardType.ROMBUSZ => "ROMB",
            CardType.PIKK => "PIKK",
            CardType.LOHERE => "LOHE",
            CardType.SZIV => "SZIV",
            CardType.NONE => "back",
            _ => "NONE"
        };

        string value = card.GetCardValue() switch
        {
            CardValue.ACE => "A",
            CardValue.JACK => "J",
            CardValue.QUEEN => "Q",
            CardValue.TEN => "10",
            CardValue.NINE => "09",
            CardValue.EIGHT => "08",
            CardValue.SEVEN => "07",
            CardValue.SIX => "06",
            CardValue.FIVE => "05",
            CardValue.FOUR => "04",
            CardValue.THREE => "03",
            CardValue.TWO => "02",
            CardValue.ZERO => "",
            _ => ""
        };

        string spritePath = $"Cards_42x60/card_{suit}_{value}";
        if (suit == "back" || value == "")
        {
            spritePath = $"Cards_42x60/card_back";
        }

        Sprite sprite = Resources.Load<Sprite>(spritePath);

        if (sprite == null)
        {
            Debug.LogError($"Card front sprite not found: {spritePath}");
            return Resources.Load<Sprite>("Cards_42x60/card_back");
        }
        
        return sprite;
    }

    private Sprite LoadCardBackSprite(Card card)
    {
        string backType = card.GetCardBackType() switch
        {
            CardBackType.RED => "card_back_red",
            CardBackType.BLUE => "card_back_blue",
            _ => "card_back"
        };

        string spritePath = $"Cards_42x60/{backType}";
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        
        if (sprite == null)
        {
            Debug.LogError($"Card back sprite not found: {spritePath}");
            return Resources.Load<Sprite>("Cards_42x60/card_back");
        }
        
        return sprite;
    }

    public void InitializeCard(Card data, Sprite frontSprite, Sprite backSprite)
    {
        cardData = data;
        cardFrontRenderer.sprite = frontSprite;
        cardBackRenderer.sprite = backSprite;
    }

    public void SetEmptyCard()
    {
        cardData = new Card(CardType.NONE, CardBackType.RED, CardValue.ZERO);
        cardFrontRenderer.sprite = Resources.Load<Sprite>("Cards_42x60/card_empty_grey");
        cardBackRenderer.sprite = Resources.Load<Sprite>("Cards_42x60/card_back");
    }

    public Card GetCardData()
    {
        return cardData;
    }

    public void ShowCardFront()
    {
        cardFrontRenderer.gameObject.SetActive(true);
        cardBackRenderer.gameObject.SetActive(false);
        //cardFrontRenderer.DOFade(1f, 0.5f).SetEase(Ease.OutCubic);
    }

    public void ShowCardBack()
    {
        cardFrontRenderer.gameObject.SetActive(false);
        cardBackRenderer.gameObject.SetActive(true);
        //cardBackRenderer.DOFade(1f, 0.5f).SetEase(Ease.OutCubic);
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

    public void DragAndDrop(Vector3 mouseScreenPosition)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
        worldPos.z = transform.position.z;
        transform.position = worldPos + DragOffset;
    }

    public void AnimateCardFlip(float duration, System.Action onComplete = null)
    {
        Sequence flipSequence = DOTween.Sequence();
        flipSequence.Append(transform.DORotate(new Vector3(0, 90, 0), duration / 2).SetEase(Ease.InCubic));
        flipSequence.AppendCallback(() => FlipCard());
        flipSequence.Append(transform.DORotate(new Vector3(0, 0, 0), duration / 2).SetEase(Ease.OutCubic));
        flipSequence.OnComplete(() => onComplete?.Invoke());
    }

    private bool CheckDropOnPiramisCard()
    {
        // Raycast az egér pozíciójából
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit2D[] hits = Physics2D.RaycastAll(ray.origin, ray.direction);
        string debugText = "";
        foreach (var hit in hits)
        {
            debugText += $"Hit: \"{hit.collider.gameObject.name}\" \n";
        }
        Debug.Log(debugText);
        debugger.CustomDebugLog($"Checking drop on piramis card for \"{gameObject.name}\". Raycast hits:\n{debugText}");

        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                CardManager droppedOnThisPiramisCard = hit.collider.GetComponent<CardManager>();
                // Piramis kártyát keresünk (hierarchy: piramisGroup/Row_X/Card_Y)
                Transform parent = hit.collider.transform.parent;
                if (parent != null && parent.name.StartsWith("Row_"))
                {
                    Debug.Log($"CardManager -> CheckDropOnPiramisCard\t✅ Dropped on piramis card: \"{hit.collider.gameObject.name}\"");

                    // PlayerManager értesítése
                    if (parentPlayerManager != null && cardSlotIndex >= 0)
                    {
                        int rowIndex = parent.name[4] - '0'; // Char → Int konverzió ('1' - '0' = 1)
                        parentPlayerManager.OnCardDroppedToPyramid(cardSlotIndex, droppedOnThisPiramisCard, rowIndex);
                    }

                    return true;
                }
            }
        }

        Debug.Log("CardManager -> CheckDropOnPiramisCard\t❌ No piramis card detected on drop.");
        return false;
    }

    public void SaveCardOriginalTransform()
    {
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
    }
    public void ResetCardOriginalTransform()
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
        }
    }

    #endregion
}