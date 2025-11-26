using UnityEngine;
using Mirror;
using System.Collections.Generic;


/// Szoba adatait tárolja és szinkronizálja a hálózaton.
/// Ez a NetworkManager-en lesz komponensként.
public class NetworkRoomData : NetworkBehaviour
{
    public static NetworkRoomData Instance { get; private set; }

    [Header("Room Settings (Host sets these)")]
    [SyncVar] public string busName = "Busz";
    [SyncVar] public int maxPlayers = 4;
    [SyncVar] public bool reversedPyramid = false;

    [Header("Players in Room")]
    // SyncList - automatikusan szinkronizált lista
    public readonly SyncList<string> playerNames = new SyncList<string>();
    public readonly SyncList<bool> playerReadyStates = new SyncList<bool>();

    [Header("Game State")]
    private bool gameStarting = false; // Flag hogy ne induljon kétszer

    [SerializeField] private Debugger debugger;

    void Awake()
    {
        if (debugger == null)
        {
            debugger = Resources.FindObjectsOfTypeAll<Debugger>()[0];
        }

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Callback inicializálás azonnal (ne várjuk meg a Start()-ot)
        playerNames.Callback += OnPlayerListChanged;
        playerReadyStates.Callback += OnReadyStateChanged;
    }

    void OnDestroy()
    {
        // Singleton tisztítása amikor ez az instance törlődik
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #region Host Methods

    
    /// Host beállítja a szoba paramétereit

    [Server]
    public void SetRoomSettings(string busName, int maxPlayers, bool reversedPyramid)
    {
        this.busName = busName;
        this.maxPlayers = maxPlayers;
        this.reversedPyramid = reversedPyramid;

        Debug.Log($"[Server] Room settings: {busName}, {maxPlayers} players, Reversed: {reversedPyramid}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] Room settings: {busName}, {maxPlayers} players, Reversed: {reversedPyramid}");
    }

    
    /// Host indítja a játékot

    [Server]
    public void StartGame()
    {
        // Ellenőrzés: már fut a játék indítása?
        if (gameStarting)
        {
            Debug.LogWarning("[Server] Game start already in progress!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("<WARNING!> [Server] Game start already in progress!");
            return;
        }
        gameStarting = true;

        // Ellenőrzés: Mindenki ready?
        bool allReady = true;
        for (int i = 0; i < playerReadyStates.Count; i++)
        {
            if (!playerReadyStates[i])
            {
                allReady = false;
                break;
            }
        }

        if (!allReady)
        {
            Debug.LogWarning("[Server] Not all players are ready!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("<WARNING!> [Server] Not all players are ready!");
            gameStarting = false;
            return;
        }

        // Mindenki ready → Scene váltás
        Debug.Log("[Server] All players ready! Starting game..."); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Server] All players ready! Starting game...");
        
        // GameVars beállítása a host által megadott értékekkel
        GameVars.Instance.BusName = busName;
        GameVars.Instance.NumberOfPlayersInGame = playerNames.Count;
        GameVars.Instance.ReversedPyramidMode = reversedPyramid;

        // Játékosnevek átadása GameVars-ba.
        for (int I = 0; I < playerNames.Count; I++)
        {
            GameVars.Instance.SetPlayerName(I, playerNames[I]);
        }

        /*
        if (debugger != null && debugger.gameObject.activeInHierarchy)
            debugger.AddTextToDebugFile($"[Server] GameVars set: BusName={busName}, NumberOfPlayers={playerNames.Count}, ReversedPyramid={reversedPyramid}");
            debugger.AddTextToDebugFile($"[Server] Player Names: {string.Join(", ", playerNames)}");
            debugger.AddTextToDebugFile("[Server] Loading Game scene for all clients...SAVING LOG TO FILE!");
            debugger.SaveDebugFile("DebuggerLog_SwitchingToGameScene.txt");
        */

        // Scene betöltés minden clientnek
        NetworkManager.singleton.ServerChangeScene("Game");
    }

    #endregion

    #region Player Management

    
    /// Játékos csatlakozik a szobához

    [Server]
    public void AddPlayer(string playerName)
    {
        if (playerNames.Count >= maxPlayers)
        {
            Debug.LogWarning($"[Server] Room is full! Cannot add {playerName}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"<WARNING!> [Server] Room is full! Cannot add {playerName}");
            return;
        }

        playerNames.Add(playerName);
        playerReadyStates.Add(false); // Alapból nem ready

        Debug.Log($"[Server] {playerName} joined the room ({playerNames.Count}/{maxPlayers})"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] {playerName} joined the room ({playerNames.Count}/{maxPlayers})");
    }

    
    /// Játékos kilép a szobából

    [Server]
    public void RemovePlayer(string playerName)
    {
        int index = playerNames.IndexOf(playerName);
        if (index >= 0)
        {
            playerNames.RemoveAt(index);
            playerReadyStates.RemoveAt(index);

            Debug.Log($"[Server] {playerName} left the room"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] {playerName} left the room");
        }
    }

    
    /// Játékos ready státuszának változtatása

    [Server]
    public void SetPlayerReady(string playerName, bool isReady)
    {
        Debug.Log($"[NetworkRoomData] Looking for player: '{playerName}' in list: [{string.Join(", ", playerNames)}]"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[NetworkRoomData] Looking for player: '{playerName}' in list: [{string.Join(", ", playerNames)}]");

        int index = playerNames.IndexOf(playerName);
        if (index >= 0)
        {
            playerReadyStates[index] = isReady;

            Debug.Log($"[Server] {playerName} is now {(isReady ? "READY" : "NOT READY")}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] {playerName} is now {(isReady ? "READY" : "NOT READY")}");
        }
        else
        {
            Debug.LogWarning($"[NetworkRoomData] Player '{playerName}' not found in room!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"<WARNING!> [NetworkRoomData] Player '{playerName}' not found in room!");
        }
    }

    #endregion

    #region Client Callbacks (UI frissítéshez)

    void OnPlayerListChanged(SyncList<string>.Operation op, int index, string oldItem, string newItem)
    {
        // UI frissítés amikor változik a lista
        Debug.Log($"[Client] Player list changed: {op}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client] Player list changed: {op}");
        
        RefreshRoomUI();
    }

    void OnReadyStateChanged(SyncList<bool>.Operation op, int index, bool oldValue, bool newValue)
    {
        // UI frissítés amikor valaki ready-t nyom
        Debug.Log($"[Client] Ready state changed for player {index}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client] Ready state changed for player {index}");
        RefreshRoomUI();
    }

    void RefreshRoomUI()
    {
        // TODO: UI Manager-t hívni a frissítéshez
        // RoomUIManager.Instance?.RefreshPlayerList(playerNames, playerReadyStates);
    }

    #endregion
}
