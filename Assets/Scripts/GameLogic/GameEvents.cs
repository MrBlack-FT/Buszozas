using UnityEngine;
using System;

public class GameEvents : MonoBehaviour
{
    public event Action<int, Card, int, CardManager, int> OnCardDroppedToPiramis;

    public void TriggerCardDroppedToPiramis(int playerId, Card droppedCard, int cardSlotIndex, CardManager droppedOnThisPiramisCard, int PiramisRowIndex)
    {
        OnCardDroppedToPiramis?.Invoke(playerId, droppedCard, cardSlotIndex, droppedOnThisPiramisCard, PiramisRowIndex);
    }
}
