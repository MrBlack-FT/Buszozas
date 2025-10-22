using UnityEngine;
using System.Collections.Generic;

//Egy pakli 104 kártyát tartalmaz. [2 pakli egyben]
[System.Serializable]
public class Deck
{
    private List<Card> cards;
    private List<Card> usedCards;

    public Deck()
    {
        cards = new List<Card>();
        usedCards = new List<Card>();
        InitializeDeck();
    }

    private void InitializeDeck()
    {
        foreach (CardBackType backType in System.Enum.GetValues(typeof(CardBackType)))
        {

            foreach (CardType type in System.Enum.GetValues(typeof(CardType)))
            {
                if (type == CardType.NONE) continue;

                for (int value = 2; value <= 13; value++)
                {
                    cards.Add(new Card(type, backType, (CardValue)value));
                }
            }
        }
    }
    public List<Card> GetCards()
    {
        return cards;
    }

    //Fisher–Yates keverés algoritmus
    public void Shuffle()
    {
        System.Random rand = new System.Random();
        int n = cards.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = rand.Next(0, i + 1);
            Card temp = cards[i];
            cards[i] = cards[j];
            cards[j] = temp;
        }
    }

    public Card DrawCard()
    {
        if (cards.Count == 0)
        {
            Debug.LogWarning("No cards left in the deck!");
            return null;
        }

        Card drawnCard = cards[0];
        cards.RemoveAt(0);
        usedCards.Add(drawnCard);

        return drawnCard;
    }

    public void ResetDeck()
    {
        cards.AddRange(usedCards);
        usedCards.Clear();
        Shuffle();
    }

    public bool IsEmpty()
    {
        return cards.Count == 0;
    }

    public int CardsRemaining()
    {
        return cards.Count;
    }

    public int UsedCardsCount()
    {
        return usedCards.Count;
    }

    public void CheckForError()
    {
        string debugLogstring = "Hibakeresés a pakliban!\n";

        debugLogstring += $"\tPakli állapot:\n";
        // Összesen hány kártya van
        int totalCards = cards.Count + usedCards.Count;
        if (totalCards != 104)
        {
            debugLogstring += $"\tPakli hiba! Nincs 104 kártyából csak {totalCards} van.\n";
        }
        else
        {
            debugLogstring += $"\t104 kártya van a pakliban. Megfelelő.\n";
        }

        debugLogstring += $"\tDuplikáció ellenőrzése:\n";
        // Duplikáció ellenőrzése a pakliban
        HashSet<string> cardSet = new HashSet<string>();
        foreach (var card in cards)
        {
            string cardIdentifier = $"{card.GetCardBackType()}-{card.GetCardType()}-{card.GetCardValue()}";
            if (cardSet.Contains(cardIdentifier))
            {
                debugLogstring += $"\tPakli hiba! Duplikált kártya található a pakliban: {cardIdentifier}\n";
            }
            else
            {
                cardSet.Add(cardIdentifier);
            }
        }
        debugLogstring += $"\tNincs duplikált kártya a pakliban. Megfelelő.\n";


        // Duplikáció ellenőrzése a használt kártyák között
        foreach (var card in usedCards)
        {
            string cardIdentifier = $"{card.GetCardBackType()}-{card.GetCardType()}-{card.GetCardValue()}";
            if (cardSet.Contains(cardIdentifier))
            {
                debugLogstring += $"\tPakli hiba! Duplikált kártya található a használt kártyák között: {cardIdentifier}\n";
            }
            else
            {
                cardSet.Add(cardIdentifier);
            }
        }
        debugLogstring += $"\tNincs duplikált kártya a használt kártyák között. Megfelelő.\n";

        debugLogstring += $"\tHiányzó kártyák ellenőrzése:\n";
        // Hiányzó kártyák ellenőrzése
        foreach (CardBackType backType in System.Enum.GetValues(typeof(CardBackType)))
        {
            foreach (CardType type in System.Enum.GetValues(typeof(CardType)))
            {
                if (type == CardType.NONE) continue;

                for (int value = 2; value <= 13; value++)
                {
                    string cardIdentifier = $"{backType}-{type}-{(CardValue)value}";
                    if (!cardSet.Contains(cardIdentifier))
                    {
                        debugLogstring += $"\tPakli hiba! Hiányzó kártya: {cardIdentifier}\n";
                    }
                }
            }
        }
        debugLogstring += $"\tNincsenek hiányzó kártyák a pakliban és a használt kártyák között. Megfelelő.\n";

        // Kártyák típusának ellenőrzése [NONE típusú kártya nem lehet a pakliban]
        debugLogstring += $"\tKártya típus ellenőrzése:\n";
        foreach (var card in cards)
        {
            if (card.GetCardType() == CardType.NONE)
            {
                debugLogstring += $"\tPakli hiba! NONE típusú kártya található a pakliban!\n";
            }
        }

        debugLogstring += $"\tNincsen NONE típusú kártya a pakliban. Megfelelő.\n";
        debugLogstring += "Hibakeresés vége!\n";
        Debug.Log(debugLogstring);
    }
}
