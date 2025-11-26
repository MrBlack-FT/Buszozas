using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;
using DG.Tweening;

public class RoomPanelUI : MonoBehaviour
{
    #region Változók
    [SerializeField] private UIActionTriggers uiActionTriggers;
    [SerializeField] private TextMeshProUGUI busNameText;
    [SerializeField] private Transform playerListContainer;
    [SerializeField] private GameObject playerEntryPrefab;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText; // Ready gomb szövege
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;

    private NetworkRoomData roomData;
    private bool isReady = false;
    private bool isInitialized = false;

    [SerializeField] private Debugger debugger;

    #endregion

    #region Unity metódusok

    private void OnEnable()
    {
        // Panel megnyitásakor MINDIG frissíteni kell a roomData referenciát!
        ResetRoomData();
    }

    private void OnDestroy()
    {
        if (roomData != null)
        {
            roomData.playerNames.Callback -= OnPlayerListChanged;
            roomData.playerReadyStates.Callback -= OnPlayerReadyChanged;
        }
    }

    private void Start()
    {
        if (debugger == null)
        {
            debugger = Resources.FindObjectsOfTypeAll<Debugger>()[0];
        }

        if (readyButton != null)
        {
            readyButton.onClick.AddListener(OnReadyButtonClicked);
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.AddListener(OnStartGameClicked);
            // Start gomb csak host-nak látszik.
            startGameButton.gameObject.SetActive(NetworkServer.active);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.AddListener(OnLeaveButtonClicked);
        }
    }

    private void Update()
    {
        // Várjuk meg amíg a NetworkRoomData elérhető lesz (client oldalon később spawn-olódik)
        if (!isInitialized)
        {
            TryInitializeRoomData();
        }
    }

    #endregion

    #region Metódusok

    private void ResetRoomData()
    {
        // Leiratkozás régi callback-ekről.
        if (roomData != null)
        {
            roomData.playerNames.Callback -= OnPlayerListChanged;
            roomData.playerReadyStates.Callback -= OnPlayerReadyChanged;
        }

        // FRISS NetworkRoomData keresése.
        roomData = null;
        isInitialized = false;
        ClearPlayerList();

        Debug.Log("[RoomPanelUI -> ResetRoomData()] Reset room data, waiting for fresh instance"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[RoomPanelUI -> ResetRoomData()] Reset room data, waiting for fresh instance");
    }

    private void TryInitializeRoomData()
    {
        // NetworkRoomData megkeresése (FRISS instance).
        if (roomData == null)
        {
            roomData = NetworkRoomData.Instance; // Singleton használata.
            if (roomData == null)
            {
                roomData = FindFirstObjectByType<NetworkRoomData>(); // Fallback...
            }
        }

        if (roomData != null && !isInitialized)
        {
            isInitialized = true;

            // Feliratkozás SyncList változásokra.
            roomData.playerNames.Callback += OnPlayerListChanged;
            roomData.playerReadyStates.Callback += OnPlayerReadyChanged;

            // Kezdeti lista megjelenítése.
            RefreshPlayerList();
            UpdateUI();

            Debug.Log($"[RoomPanelUI -> TryInitializeRoomData()] Initialized with room data: {roomData.busName}, players: {roomData.playerNames.Count}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[RoomPanelUI -> TryInitializeRoomData()] Initialized with room data: {roomData.busName}, players: {roomData.playerNames.Count}");
        }
    }

    private void OnPlayerListChanged(SyncList<string>.Operation op, int index, string oldItem, string newItem)
    {
        Debug.Log($"[RoomPanelUI->OnPlayerListChanged()] Player list changed: {op}, index={index}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[RoomPanelUI->OnPlayerListChanged()] Player list changed: {op}, index={index}");

        RefreshPlayerList();
    }

    private void OnPlayerReadyChanged(SyncList<bool>.Operation op, int index, bool oldItem, bool newItem)
    {
        Debug.Log($"[RoomPanelUI->OnPlayerReadyChanged()] Ready state changed: index={index}, old={oldItem}, new={newItem}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[RoomPanelUI->OnPlayerReadyChanged()] Ready state changed: index={index}, old={oldItem}, new={newItem}");
            
        RefreshPlayerList();
        UpdateStartButton();
    }

    private void RefreshPlayerList()
    {
        if (roomData == null || playerListContainer == null || playerEntryPrefab == null)
            return;

        ClearPlayerList();

        // Játékosok hozzáadása.
        for (int I = 0; I < roomData.playerNames.Count; I++)
        {
            GameObject entry = Instantiate(playerEntryPrefab, playerListContainer);
            var playerEntry = entry.GetComponent<PlayerEntryUI>();
            
            if (playerEntry != null)
            {
                string playerName = roomData.playerNames[I];
                bool isReady = I < roomData.playerReadyStates.Count ? roomData.playerReadyStates[I] : false;
                playerEntry.SetData(playerName, isReady);
            }
        }
    }

    private void UpdateUI()
    {
        if (roomData != null && busNameText != null)
        {
            busNameText.text = roomData.busName;
        }
    }

    private void UpdateStartButton()
    {
        if (startGameButton == null || !NetworkServer.active)
            return;

        // Start gomb aktív ha mindenki ready (kivéve host)
        bool allReady = true;
        for (int I = 1; I < roomData.playerReadyStates.Count; I++) // i=1, mert host nem kell ready
        {
            if (!roomData.playerReadyStates[I])
            {
                allReady = false;
                break;
            }
        }

        startGameButton.GetComponentInChildren<CustomButtonForeground>().SetInteractiveState(allReady && roomData.playerNames.Count >= 2);
    }

    public void OnReadyButtonClicked()
    {
        isReady = !isReady;

        // NetworkPlayer megkeresése és ready state beállítása
        var networkPlayer = NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (networkPlayer != null)
        {
            networkPlayer.CmdSetReady(isReady);
            Debug.Log($"[RoomPanelUI->OnReadyButtonClicked()] Set ready state to: {isReady}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[RoomPanelUI->OnReadyButtonClicked()] Set ready state to: {isReady}");
        }

        // Gomb szöveg frissítése
        UpdateReadyButtonText();
    }

    private void UpdateReadyButtonText()
    {
        if (readyButtonText != null)
        {
            // Ha ready vagyok -> "Jegy érvényesítve" (már kész)
            // Ha nem vagyok ready -> "Jegy érvényesítése" (ready opció)
            readyButtonText.text = isReady ? "Jegy érvényesítve" : "Jegy érvényesítése";
            Debug.Log($"[RoomPanelUI->UpdateReadyButtonText()] isReady={isReady}, Button text set to: {readyButtonText.text}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[RoomPanelUI->UpdateReadyButtonText()] isReady={isReady}, Button text set to: {readyButtonText.text}");
            
            var customButton = readyButton.GetComponentInChildren<CustomButtonBackground>();
            if (customButton != null)
            {
                customButton.ClearButtonColor();
            }
        }
    }

    public void OnStartGameClicked()
    {
        if (NetworkServer.active && roomData != null)
        {
            roomData.StartGame();
        }
    }

    public void OnLeaveButtonClicked()
    {
        // UI felület visszaállítása.
        ClearPlayerList();
        isInitialized = false;
        roomData = null;

        if (NetworkServer.active && NetworkClient.isConnected)
        {
            // HOST
            NetworkManager.singleton.StopHost();
            Debug.Log($"[Host] OnLeaveButtonClicked() -> Stopped \"{busNameText.text}\" hosting, all players disconnected."); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Host] OnLeaveButtonClicked() -> Stopped \"{busNameText.text}\" hosting, all players disconnected.");
            //ShowMultiplayerPanel();
        }
        else if (NetworkServer.active)
        {
            // DEDICATED SERVER
            NetworkManager.singleton.StopServer();
            Debug.Log($"[Server] OnLeaveButtonClicked() -> Stopped server. [\"{busNameText.text}\"]"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] OnLeaveButtonClicked() -> Stopped server. [\"{busNameText.text}\"]");
        }
        else if (NetworkClient.isConnected)
        {
            // CLIENT
            NetworkManager.singleton.StopClient();
            Debug.Log($"[Client] OnLeaveButtonClicked() -> Disconnected from server. [\"{busNameText.text}\"]"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client] OnLeaveButtonClicked() -> Disconnected from server. [\"{busNameText.text}\"]");
            ShowLobbyPanel();
        }

        DOVirtual.DelayedCall(1f, () =>
        {
            Debug.Log("END OF OnLeaveButtonClicked()"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("END OF OnLeaveButtonClicked()");
        });
    }

    private void ClearPlayerList()
    {
        if (playerListContainer == null)
            return;

        // Lista törlése.
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void ShowLobbyPanel()
    {
        DOVirtual.DelayedCall(0.25f, () =>
        {
            uiActionTriggers.AddCloseMultiplayerRoom();
            uiActionTriggers.AddOpenMultiplayerLobby();
            uiActionTriggers.RunSequence("CloseMultiplayerRoom - OpenMultiplayerLobby");
        });
    }

    private void ShowMultiplayerPanel()
    {
        DOVirtual.DelayedCall(0.25f, () =>
        {
            uiActionTriggers.AddCloseMultiplayerRoom();
            uiActionTriggers.AddOpenMultiplayer();
            uiActionTriggers.RunSequence("CloseMultiplayerRoom - OpenMultiplayer");
        });
    }

    #endregion
}
