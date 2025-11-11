using System.Collections.Generic;
using UnityEngine;

public enum PlayerExitStatus { PLAYING, COMPLETED, FAILED, GAVE_UP, DISCONNECTED }

[System.Serializable]
public class Player
{
    private int _playerID;
    private string _playerName;
    private int _playerScore;
    private List<Card> _playerCards;

    private TippValue _currentTipp;
    private PlayerExitStatus _exitStatus;

    public Player(int id, string name)
    {
        _playerID = id;
        _playerName = name;
        _playerScore = 0;
        _playerCards = new List<Card>(); // MAX 5 kártya lehet nála!
        _currentTipp = TippValue.NONE;
        _exitStatus = PlayerExitStatus.PLAYING;
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

    public Card GetPlayerCardAtIndex(int index)
    {
        if (index >= 0 && index < _playerCards.Count)
        {
            return _playerCards[index];
        }
        else
        {
            Debug.LogWarning($"Index {index} is out of range for player {_playerName}'s cards.");
            return null;
        }
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
        return _playerCards;
    }

    private Card GetPlayersCardAtIndex(int index)
    {
        if (index >= 0 && index < _playerCards.Count)
        {
            return _playerCards[index];
        }
        else
        {
            Debug.LogWarning($"Index {index} is out of range for player {_playerName}'s cards.");
            return null;
        }
    }

    private void SetPlayerCard(int index, Card card)
    {
        if (index >= 0 && index < _playerCards.Count)
        {
            _playerCards[index] = card;
        }
        else
        {
            Debug.LogWarning($"Index {index} is out of range for player {_playerName}'s cards.");
        }
    }

    public TippValue GetTipp()
    {
        return _currentTipp;
    }

    public void SetTipp(TippValue tipp)
    {
        _currentTipp = tipp;
    }

    public PlayerExitStatus GetExitStatus()
    {
        return _exitStatus;
    }

    public void SetExitStatus(PlayerExitStatus status)
    {
        _exitStatus = status;
    }

    public void AddCardToPlayer(Card card)
    {
        if (_playerCards.Count < 5)
        {
            _playerCards.Add(card);
        }
        else
        {
            Debug.LogWarning($"Player \"{_playerName}\" already has 5 cards. Cannot add more.");
        }
    }

    public void RemoveCardFromPlayer(Card card)
    {
        if (_playerCards.Contains(card))
        {
            _playerCards.Remove(card);
        }
        else
        {
            Debug.LogWarning($"Player {_playerName} does not have the specified card.");
        }
    }

    public void RemoveCardFromPlayerAtIndex(int index)
    {
        if (index >= 0 && index < _playerCards.Count)
        {
            _playerCards.RemoveAt(index);
        }
        else
        {
            Debug.LogWarning($"Index {index} is out of range for player {_playerName}'s cards.");
        }
    }

    public void ChangeCardToEmptyCardAtIndex(int index)
    {
        if (index >= 0 && index < _playerCards.Count)
        {
            _playerCards[index] = new Card(CardType.NONE, CardBackType.NONE, CardValue.ZERO);
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
        _playerCards.Clear();
    }

    public string GetPlayerStatus()
    {
        string statusString = "Player ID: " + GetPlayerID() + ", Name: " + GetPlayerName() + ", Score: " + GetPlayerScore() + "\n";
        statusString += "\tCards: ";
        foreach (var card in _playerCards)
        {
            statusString += "[" + card.GetCardBackType() + "_" + card.GetCardType() + "_" + card.GetCardValue() + "] ";
        }
        return statusString;
    }
}