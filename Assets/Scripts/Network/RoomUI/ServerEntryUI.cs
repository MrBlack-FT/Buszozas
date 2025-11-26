using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Net;
using Mirror;

public class ServerEntryUI : MonoBehaviour
{
    #region Változók

    [SerializeField] private TextMeshProUGUI busNameText;
    [SerializeField] private TextMeshProUGUI playersCountText;
    [SerializeField] private Button joinButton;

    private ServerResponse serverData;
    private IPEndPoint endpoint;
    public System.Action OnJoinClicked;

    #endregion

    #region Unity metódusok

    private void Start()
    {
        if (joinButton != null)
        {
            joinButton.onClick.AddListener(() => OnJoinClicked?.Invoke());
        }
    }

    #endregion

    #region Metódusok
    public void SetData(ServerResponse response, IPEndPoint serverEndpoint)
    {
        serverData = response;
        endpoint = serverEndpoint;

        if (busNameText != null)
            busNameText.text = response.busName;

        if (playersCountText != null)
            playersCountText.text = $"{response.currentPlayers}/{response.maxPlayers}";

        // Join gomb letiltása ha tele van a szoba.
        if (joinButton != null)
            joinButton.GetComponentInChildren<CustomButtonForeground>().SetInteractiveState(response.currentPlayers < response.maxPlayers);
    }

    #endregion
}
