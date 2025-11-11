using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

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
        Shuffle();
    }

    private void InitializeDeck()
    {
        // Csak PIROS és KÉK hátlapú kártyák
        CardBackType[] backTypes = { CardBackType.RED, CardBackType.BLUE };

        foreach (CardBackType backType in backTypes)
        {
            foreach (CardType type in System.Enum.GetValues(typeof(CardType)))
            {
                if (type == CardType.NONE) continue;

                for (int value = 2; value <= 14; value++)
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
        for (int I = n - 1; I > 0; I--)
        {
            int J = rand.Next(0, I + 1);
            Card temp = cards[I];
            cards[I] = cards[J];
            cards[J] = temp;
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

    public Card DrawSpecificCard(Card specificCard)
    {
        foreach (var card in cards)
        {
            if
            (
                card.GetCardBackType() == specificCard.GetCardBackType() &&
                card.GetCardType() == specificCard.GetCardType() &&
                card.GetCardValue() == specificCard.GetCardValue()
            )
            {
                cards.Remove(card);
                usedCards.Add(card);
                return card;
            }
        }
        Debug.LogWarning("Specific card not found in the deck!");
        return new Card(CardType.NONE, CardBackType.NONE, CardValue.ZERO);
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
            debugLogstring += $"\t❌Pakli hiba! Nincs 104 kártyából csak {totalCards} van.\n";
            string cardStr = "";
            foreach (var card in cards)
            {
                cardStr += card.GetCardType() + "\t " + card.GetCardValue() + "\t " + card.GetCardBackType() + "\n";
            }
            Debug.Log(cardStr);
        }
        else
        {
            debugLogstring += $"\t✅104 kártya van a pakliban. Megfelelő.\n";
        }

        debugLogstring += $"\tDuplikáció ellenőrzése:\n";
        // Duplikáció ellenőrzése a pakliban
        HashSet<string> cardSet = new HashSet<string>();
        foreach (var card in cards)
        {
            string cardIdentifier = $"{card.GetCardBackType()}-{card.GetCardType()}-{card.GetCardValue()}";
            if (cardSet.Contains(cardIdentifier))
            {
                debugLogstring += $"\t❌Pakli hiba! Duplikált kártya található a pakliban: {cardIdentifier}\n";
            }
            else
            {
                cardSet.Add(cardIdentifier);
            }
        }
        debugLogstring += $"\t✅Nincs duplikált kártya a pakliban. Megfelelő.\n";


        // Duplikáció ellenőrzése a használt kártyák között
        foreach (var card in usedCards)
        {
            string cardIdentifier = $"{card.GetCardBackType()}-{card.GetCardType()}-{card.GetCardValue()}";
            if (cardSet.Contains(cardIdentifier))
            {
                debugLogstring += $"\t❌Pakli hiba! Duplikált kártya található a használt kártyák között: {cardIdentifier}\n";
            }
            else
            {
                cardSet.Add(cardIdentifier);
            }
        }
        debugLogstring += $"\t✅Nincs duplikált kártya a használt kártyák között. Megfelelő.\n";

        debugLogstring += $"\tHiányzó kártyák ellenőrzése:\n";
        // Hiányzó kártyák ellenőrzése (csak PIROS és KÉK)
        CardBackType[] backTypes = { CardBackType.RED, CardBackType.BLUE };
        foreach (CardBackType backType in backTypes)
        {
            foreach (CardType type in System.Enum.GetValues(typeof(CardType)))
            {
                if (type == CardType.NONE) continue;

                for (int value = 2; value <= 13; value++)
                {
                    string cardIdentifier = $"{backType}-{type}-{(CardValue)value}";
                    if (!cardSet.Contains(cardIdentifier))
                    {
                        debugLogstring += $"\t❌Pakli hiba! Hiányzó kártya: {cardIdentifier}\n";
                    }
                }
            }
        }
        debugLogstring += $"\t✅Nincsenek hiányzó kártyák a pakliban és a használt kártyák között. Megfelelő.\n";

        // Kártyák típusának ellenőrzése [NONE típusú kártya nem lehet a pakliban]
        debugLogstring += $"\tKártya típus ellenőrzése:\n";
        foreach (var card in cards)
        {
            if (card.GetCardType() == CardType.NONE || card.GetCardBackType() == CardBackType.NONE || card.GetCardValue() == CardValue.ZERO)
            {
                debugLogstring += $"\t❌Pakli hiba! NONE típusú kártya található a pakliban!\n";
            }
        }

        debugLogstring += $"\t✅Nincsen NONE típusú kártya a pakliban. Megfelelő.\n";
        debugLogstring += "Hibakeresés vége!\n";
        Debug.Log(debugLogstring);
    }
}
