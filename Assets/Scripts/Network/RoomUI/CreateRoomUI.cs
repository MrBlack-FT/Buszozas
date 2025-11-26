using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class CreateRoomUI : MonoBehaviour
{
    #region Változók
    [SerializeField] private UIActionTriggers uiActionTriggers;
    [SerializeField] private GameObject warningText;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_InputField busNameInput;
    [SerializeField] private Slider maxPlayersSlider;
    [SerializeField] private Toggle reversedPyramidToggle;

    [SerializeField] private Debugger debugger;

    #endregion

    #region Unity metódusok

    private void Start()
    {
        if (uiActionTriggers != null)
        {
            uiActionTriggers = GameObject.Find("UIActionTriggers").GetComponent<UIActionTriggers>();
        }
        else
        {
            Debug.LogWarning("UIActionTriggers reference is missing in CreateRoomUI.");
        }

        if (debugger == null)
        {
            debugger = Resources.FindObjectsOfTypeAll<Debugger>()[0];
        }
    }

    #endregion

    #region Metódusok

    public void OnCreateButtonClicked()
    {
        if (string.IsNullOrWhiteSpace(busNameInput.text) || busNameInput.text.Length == 0 || string.IsNullOrWhiteSpace(playerNameInput.text) || playerNameInput.text.Length == 0)
        {
            CanvasGroup canvasGroup = warningText.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                warningText.SetActive(true);
                
                if (string.IsNullOrWhiteSpace(busNameInput.text) || busNameInput.text.Length == 0)
                    warningText.GetComponent<TMP_Text>().text = "Járat neve nem lehet üres!";
                else if (string.IsNullOrWhiteSpace(playerNameInput.text) || playerNameInput.text.Length == 0)
                    warningText.GetComponent<TMP_Text>().text = "Játékos neve nem lehet üres!";

                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(2f, 0.5f).OnComplete(() =>
                {
                    DOVirtual.DelayedCall(2f, () =>
                    {
                        canvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
                        {
                            warningText.SetActive(false);
                            return;
                        });
                    });
                });
            }
            else
            {
                Debug.LogWarning("WarningText GameObject is missing a CanvasGroup component.");
                return;
            }
        }

        string busName = busNameInput.text;
        int maxPlayers = (int)maxPlayersSlider.value;
        bool reversedPyramid = reversedPyramidToggle.isOn;

        // Játékos név mentése PlayerPrefs-be
        if (!string.IsNullOrWhiteSpace(playerNameInput.text))
        {
            PlayerPrefs.SetString("playerName", playerNameInput.text);
            PlayerPrefs.Save();
        }

    
        if (debugger != null && debugger.gameObject.activeInHierarchy)
        {
            debugger.AddTextToDebugFile($"<OnCreateButtonClicked> Creating room with Bus Name: {busName}, Max Players: {maxPlayers}, Reversed Pyramid: {reversedPyramid}");
        }
        
        // NetworkManager hívása
        BuszNetworkManager.singleton.CreateRoom(busName, maxPlayers, reversedPyramid);

        // UI váltás RoomPanel-re
        ShowRoomPanel();
    }

    void ShowRoomPanel()
    {
        DOVirtual.DelayedCall(0.25f, () =>
        {
        uiActionTriggers.AddCloseMultiplayerCreate();
        uiActionTriggers.AddOpenMultiplayerRoom();
        uiActionTriggers.RunSequence("CloseMultiplayerCreate - OpenMultiplayerRoom");
        });
    }

    #endregion
}