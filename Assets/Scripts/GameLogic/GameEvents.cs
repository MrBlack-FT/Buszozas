using UnityEngine;
using System;

public class GameEvents : MonoBehaviour
{
    public event Action<int, Card, int> OnCardDroppedToPiramis;
    
    public void TriggerCardDroppedToPiramis(int playerId, Card card, int cardSlotIndex)
    {
        OnCardDroppedToPiramis?.Invoke(playerId, card, cardSlotIndex);
    }
}
