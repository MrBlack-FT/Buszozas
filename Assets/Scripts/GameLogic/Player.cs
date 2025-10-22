using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Player
{
    private int _playerID;
    private string _playerName;
    private int _playerScore;
    private List<Card> playerCards;

    public Player(int id, string name)
    {
        _playerID = id;
        _playerName = name;
        _playerScore = 0;
        playerCards = new List<Card>(); // MAX 5 kártya lehet nála!
    }

    public int GetPlayerID()
    {
        return _playerID;
    }

    public string GetPlayerName()
    {
        return _playerName;
    }

    public int GetPlayerScore()
    {
        return _playerScore;
    }

    public void SetPlayerName(string name)
    {
        _playerName = name;
    }

    public void SetPlayerScore(int score)
    {
        _playerScore = score;
    }

    public List<Card> GetPlayerCards()
    {
        return playerCards;
    }

    public void AddCardToPlayer(Card card)
    {
        if (playerCards.Count < 5)
        {
            playerCards.Add(card);
        }
        else
        {
            Debug.LogWarning($"Player {_playerName} already has 5 cards. Cannot add more.");
        }
    }

    public void RemoveCardFromPlayer(Card card)
    {
        if (playerCards.Contains(card))
        {
            playerCards.Remove(card);
        }
        else
        {
            Debug.LogWarning($"Player {_playerName} does not have the specified card.");
        }
    }

    public void RemoveCardFromPlayerAtIndex(int index)
    {
        if (index >= 0 && index < playerCards.Count)
        {
            playerCards.RemoveAt(index);
        }
        else
        {
            Debug.LogWarning($"Index {index} is out of range for player {_playerName}'s cards.");
        }
    }

    public void IncreasePlayerScore(int amount)
    {
        _playerScore += amount;
    }

    public void ClearPlayerCards()
    {
        playerCards.Clear();
    }
}
