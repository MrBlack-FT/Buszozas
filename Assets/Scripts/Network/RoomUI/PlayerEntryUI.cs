using UnityEngine;
using TMPro;
using UnityEngine.UI;

// Egy játékos a szobában.
public class PlayerEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private Image readyIndicator;

    [SerializeField] private Color readyColor = Color.green;
    [SerializeField] private Color notReadyColor = Color.red;

    public void SetData(string playerName, bool isReady)
    {
        if (playerNameText != null)
        {
            playerNameText.text = playerName;
        }

        if (readyIndicator != null)
        {
            readyIndicator.color = isReady ? readyColor : notReadyColor;
            //Debug.Log($"[PlayerEntryUI] {playerName} ready: {isReady}, color: {readyIndicator.color}");
        }
    }
}
