using UnityEngine;

public enum CardType { NONE, SZIV, ROMBUSZ, LOHERE, PIKK }
public enum CardBackType { RED, BLUE }
public enum CardValue { ZERO = 0, TWO = 2, THREE, FOUR, FIVE, SIX, SEVEN, EIGHT, NINE, TEN, JACK = 11, QUEEN, KING, ACE = 13 }

[System.Serializable]
public class Card
{
    private CardType _cardType;
    private CardBackType _cardBackType;
    private CardValue _cardValue;

    public Card(CardType type, CardBackType backType, CardValue value)
    {
        _cardType = type;
        _cardBackType = backType;
        _cardValue = value;
    }
    public CardType GetCardType()
    {
        return _cardType;
    }

    public CardBackType GetCardBackType()
    {
        return _cardBackType;
    }

    public CardValue GetCardValue()
    {
        return _cardValue;
    }

    public void SetCardType(CardType type)
    {
        _cardType = type;
    }

    public void SetCardBackType(CardBackType backType)
    {
        _cardBackType = backType;
    }

    public void SetCardValue(CardValue value)
    {
        _cardValue = value;
    }
}