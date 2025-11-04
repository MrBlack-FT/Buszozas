using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    #region Változók

    private Player player;
    private int playerId;
    private GameEvents gameEvents;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI pointsText;
    
    [Header("Card Slots")]
    [SerializeField] private CardManager[] cardSlots;

    [Header("Pont Osztás UI")]
    [SerializeField] private GameObject cardsGroup;
    [SerializeField] private GameObject pointGiveGroup;
    [SerializeField] private Button decreaseButton;
    [SerializeField] private TextMeshProUGUI pointsToGiveText;
    [SerializeField] private Button increaseButton;

    private Transform originalTransform = null;

    #endregion

    #region Unity metódusok

    void Awake()
    {
        if (nameText == null) Debug.LogWarning("PlayerManager: nameText is not assigned!");
        if (pointsText == null) Debug.LogWarning("PlayerManager: pointsText is not assigned!");
        if (cardSlots == null || cardSlots.Length != 5) Debug.LogWarning("PlayerManager: cardSlots must have exactly 5 elements!");

        // CardManager-eknek beállítjuk a parent referenciát
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (cardSlots[i] != null)
            {
                cardSlots[i].SetParentPlayerManager(this, i);
            }
        }
    }

    void Start()
    {

    }

    void Update()
    {

    }

    #endregion

    #region Metódusok

    public void Initialize(int id)
    {
        playerId = id;
        player = new Player(id, GameVars.Instance.GetPlayerName(id));
        UpdateUI();

        originalTransform = transform;
        //Debug.Log($"PlayerManager for {player.GetPlayerName()} initialized. Original Transform saved: {originalTransform.position}");
    }

    public void SetPlayerData(Player newPlayer)
    {
        player = newPlayer;
        playerId = newPlayer.GetPlayerID();
        UpdateUI();
    }

    public void SetInteractive(bool isActive)
    {
        foreach (var card in cardSlots)
        {
            Selectable selectable = card.GetComponent<Selectable>();
            if (selectable != null && card.CardData != null && card.CardData.GetCardType() != CardType.NONE)
            {
                selectable.interactable = isActive;
            }
        }
    }

    public void AddPoints(int amount)
    {
        player.IncreasePlayerScore(amount);
        UpdateUI();
    }

    public void AddCard(Card card, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 5)
        {
            Debug.LogWarning($"PlayerManager: Invalid slot index {slotIndex}");
            return;
        }

        player.AddCardToPlayer(card);
        cardSlots[slotIndex].SetCard(card);
        cardSlots[slotIndex].ShowCardFront();
    }

    public void RemoveCard(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 5)
        {
            Debug.LogWarning($"PlayerManager: Invalid slot index {slotIndex}");
            return;
        }

        cardSlots[slotIndex].SetEmptyCard();
        //player.RemoveCardFromPlayerAtIndex(slotIndex);
    }

    public void ChangeCardToEmptyCard(int slotIndex, bool resetTransform)
    {
        if (slotIndex < 0 || slotIndex >= 5)
        {
            Debug.LogWarning($"PlayerManager: Invalid slot index {slotIndex}");
            return;
        }

        Debug.Log($"ChangeCardToEmptyCard called on slotIndex: \"{slotIndex}\"\n" +
                  $"Card is: {cardSlots[slotIndex].GetCardData().GetCardBackType()} | {cardSlots[slotIndex].GetCardData().GetCardType()} | {cardSlots[slotIndex].GetCardData().GetCardValue()}");
        cardSlots[slotIndex].SetEmptyCard();
        Debug.Log($"After SetEmptyCard() slotIndex: \"{slotIndex}\"\n" +
                  $"Card is: {cardSlots[slotIndex].GetCardData().GetCardBackType()} | {cardSlots[slotIndex].GetCardData().GetCardType()} | {cardSlots[slotIndex].GetCardData().GetCardValue()}");
        if (resetTransform)
        {
            cardSlots[slotIndex].ResetCardOriginalTransform();
            Debug.Log($"Card Transform after Reset: {cardSlots[slotIndex].transform.position}");
        }
    }



    public int GetScore()
    {
        return player.GetPlayerScore();
    }

    public string GetPlayerName()
    {
        return player.GetPlayerName();
    }
    public int GetPlayerId()
    {
        return playerId;
    }
    private void UpdateUI()
    {
        if (nameText != null) nameText.text = player.GetPlayerName();
        if (pointsText != null) pointsText.text = "Pontszám:  " + player.GetPlayerScore().ToString();

        var cards = player.GetPlayerCards();
        for (int I = 0; I < 5; I++)
        {
            if (I < cards.Count)
            {
                cardSlots[I].SetCard(cards[I]);
            }
            else
            {
                cardSlots[I].SetEmptyCard();
            }
            cardSlots[I].ShowCardFront();
        }
    }
    public void ClearAllCards()
    {
        player.ClearPlayerCards();
        // TODO: CardManager-ek resetelése
    }

    public Transform GetOriginalTransform()
    {
        return originalTransform;
    }

    public void ResetPosition()
    {
        if (originalTransform != null)
        {
            transform.position = originalTransform.position;
            //transform.rotation = originalTransform.rotation;
            //transform.localScale = originalTransform.localScale;
        }
    }

    #endregion

    #region Pont osztáshoz metódusok

    public void ShowPointGiving()
    {
        pointsText.gameObject.SetActive(false);
        cardsGroup.SetActive(false);
        pointGiveGroup.SetActive(true);
        
        SetPointsToGiveText(0);
    }

    public void HidePointGiving()
    {
        pointGiveGroup.SetActive(false);
        pointsText.gameObject.SetActive(true);
        cardsGroup.SetActive(true);
    }

    public void SetPointsToGiveText(int points)
    {
        pointsToGiveText.text = points.ToString();
    }

    public void UpdatePointGiveButtons(bool canDecrease)
    {
        decreaseButton.interactable = canDecrease;
    }

    public Button GetIncreaseButton() => increaseButton;
    public Button GetDecreaseButton() => decreaseButton;

    #endregion

    #region Drag & Drop

    public void SaveCardsOriginalTransforms()
    {
        foreach (var card in cardSlots)
        {
            card.SaveCardOriginalTransform();
        }
    }

    public void SetGameEvents(GameEvents events)
    {
        gameEvents = events;
    }

    public void SetCardsDraggable(bool enabled)
    {
        //Debug.Log($"PlayerManager {playerId}: SetCardsDraggable({enabled}) called");
        foreach (var card in cardSlots)
        {
            if (card != null && card.CardData != null && card.CardData.GetCardType() != CardType.NONE)
            {
                card.SetInteractable(enabled);
                //Debug.Log($"  Card {card.name}: SetInteractable({enabled})");
            }
            else
            {
                card.SetInteractable(false);
            }
        }
    }

    public void OnCardDroppedToPyramid(int cardSlotIndex, CardManager droppedOnThisPiramisCard, int PiramisRowIndex)
    {
        if (cardSlotIndex < 0 || cardSlotIndex >= cardSlots.Length)
        {
            Debug.LogWarning($"Invalid card slot index: {cardSlotIndex}");
            return;
        }

        Card cardData = cardSlots[cardSlotIndex].GetCardData();
        if (cardData == null)
        {
            Debug.LogWarning("No card in this slot!");
            return;
        }

        gameEvents?.TriggerCardDroppedToPiramis(playerId, cardData, cardSlotIndex, droppedOnThisPiramisCard, PiramisRowIndex);
    }

    public void OnCardReturnedToPlayer(int cardSlotIndex)
    {
        if (cardSlotIndex < 0 || cardSlotIndex >= cardSlots.Length)
        {
            Debug.LogWarning($"Invalid card slot index: {cardSlotIndex}");
            return;
        }

        Debug.Log($"RESETTING Card Slot at  {cardSlotIndex} [index] Original Transform");
        cardSlots[cardSlotIndex].ResetCardOriginalTransform();
    }

    #endregion
}
