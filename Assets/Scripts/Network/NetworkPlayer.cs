using UnityEngine;
using Mirror;


/// Minden csatlakozott játékost reprezentál a hálózaton.
/// Minden játékosnak van egy ilyen a scene-ben.
public class NetworkPlayer : NetworkBehaviour
{
    [Header("Player Info")]
    [SyncVar] // Automatikusan szinkronizálódik minden clienttel
    public int playerId;
    
    [SyncVar]
    public string playerName = "";
    
    [SyncVar]
    public int playerScore = 0;
    
    [SyncVar]
    public bool isReady = false;

    [SerializeField] private Debugger debugger;

    void Start()
    {
        if (debugger == null)
        {
            debugger = Resources.FindObjectsOfTypeAll<Debugger>()[0];
        }

        // Ha ez a local player (saját magunk)
        if (isLocalPlayer)
        {
            // CSAK akkor kérjünk új ID-t, ha még nincs beállítva (új scene-ben is megőrizzük)
            if (playerId == 0)
            {
                // Név lekérése helyi PlayerPrefs-ből
                string myName = PlayerPrefs.GetString("playerName", "Unknown Player");
                
                Debug.Log($"[Client] This is my player! I am: \"{myName}\" (from PlayerPrefs)"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client] This is my player! I am: \"{myName}\" (from PlayerPrefs)");
                
                // Kérjünk játékos ID-t a servertől ÉS küldjük el a nevünket
                CmdRequestPlayerId(myName);
            }
            else
            {
                Debug.Log($"[Client] Player already has ID: {playerId} ({playerName})"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client] Player already has ID: {playerId} ({playerName})");
            }
        }
        else
        {
            Debug.Log($"[NetworkPlayer] Remote player spawned, waiting for name sync..."); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[NetworkPlayer] Remote player spawned, waiting for name sync...");
        }
    }
    
    /// Client kér egy játékos ID-t a servertől

    [Command]
    void CmdRequestPlayerId(string clientPlayerName)
    {
        // Server osztja ki az ID-t
        playerId = (int)netId; // Egyedi ID minden játékosnak
        
        // Név beállítása a client által küldött névvel
        playerName = string.IsNullOrWhiteSpace(clientPlayerName) ? $"Player {playerId}" : clientPlayerName;
        
        Debug.Log($"[Server] Assigned Player ID: {playerId} to {playerName}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] Assigned Player ID: {playerId} to {playerName}");

        // Játékos hozzáadása a NetworkRoomData-hoz
        var networkManager = Mirror.NetworkManager.singleton as BuszNetworkManager;
        if (networkManager != null)
        {
            var roomData = networkManager.GetRoomData();
            if (roomData != null)
            {
                // Ellenőrizzük hogy nincs-e már benne (pl. reconnect)
                if (!roomData.playerNames.Contains(playerName))
                {
                    roomData.AddPlayer(playerName);
                    Debug.Log($"[Server] Added {playerName} to room data"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] Added {playerName} to room data");
                }
            }
        }
    }

    
    /// Client megjelöli magát késznek

    public void SetReady()
    {
        if (!isLocalPlayer) return;
        
        CmdSetReady(true);
    }

    [Command]
    public void CmdSetReady(bool ready)
    {
        isReady = ready;
        Debug.Log($"[Server] {playerName} is {(ready ? "ready" : "not ready")}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] {playerName} is {(ready ? "ready" : "not ready")}");
        
        // Frissítjük a NetworkRoomData-t is
        var roomData = FindFirstObjectByType<NetworkRoomData>();
        if (roomData != null)
        {
            Debug.Log($"[NetworkPlayer] Calling SetPlayerReady for: {playerName}, ready={ready}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[NetworkPlayer] Calling SetPlayerReady for: {playerName}, ready={ready}");

            roomData.SetPlayerReady(playerName, ready);
        }
        else
        {
            Debug.LogWarning("[NetworkPlayer] RoomData not found!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("<WARNING!> [NetworkPlayer] RoomData not found!");
        }
    }

    
    /// Client beküldi a tippjét

    public void SubmitTipp(TippValue tipp)
    {
        if (!isLocalPlayer) return;
        
        CmdSubmitTipp((int)tipp);
    }

    [Command]
    void CmdSubmitTipp(int tippValue)
    {
        //Debug.Log($"[Server]\t{playerName} submitted tipp: {(TippValue)tippValue}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\t{playerName} submitted tipp: {(TippValue)tippValue}");
        
        // Továbbítjuk a NetworkGameManager-nek
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.CmdSubmitTipp(playerId, tippValue);
        }
    }

    /// Client beküldi a Busz tippjét
    public void SubmitBuszTipp(TippValue tipp)
    {
        if (!isLocalPlayer) return;
        
        CmdSubmitBuszTipp((int)tipp);
    }

    [Command]
    void CmdSubmitBuszTipp(int tippValue)
    {
        Debug.Log($"[Server]\t{playerName} submitted Busz tipp: {(TippValue)tippValue}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\t{playerName} submitted Busz tipp: {(TippValue)tippValue}");
        
        // Továbbítjuk a NetworkGameManager-nek
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.CmdSubmitBuszTipp(playerId, tippValue);
        }
    }

    /// Client feladja a Buszt
    public void GiveUpBusz()
    {
        if (!isLocalPlayer) return;
        
        CmdGiveUpBusz();
    }

    [Command]
    void CmdGiveUpBusz()
    {
        Debug.Log($"[Server]\t{playerName} gave up Busz"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\t{playerName} gave up Busz");
        
        // Továbbítjuk a NetworkGameManager-nek
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.CmdGiveUpBusz(playerId);
        }
    }

    /// Pont osztás megerősítése (kliens küldi a szervernek)
    public void CmdConfirmPointGive(int[] pointsToGive, bool isPiramis)
    {
        if (!isLocalPlayer) return;
        
        CmdConfirmPointGiveServer(pointsToGive, isPiramis);
    }

    [Command]
    void CmdConfirmPointGiveServer(int[] pointsToGive, bool isPiramis)
    {
        Debug.Log($"[Server]\t{playerName} confirmed point giving [isPiramis {isPiramis}]"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\t{playerName} confirmed point giving [isPiramis {isPiramis}]");
        
        // Továbbítjuk a NetworkGameManager-nek
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.CmdConfirmPointGive(playerId, pointsToGive, isPiramis);
        }
    }

    /// Skip pyramid card (kliens küldi a szervernek)
    public void CmdSkipPyramidCard()
    {
        if (!isLocalPlayer) return;
        
        CmdSkipPyramidCardServer();
    }

    [Command]
    void CmdSkipPyramidCardServer()
    {
        Debug.Log($"[Server]\t{playerName} skipped pyramid card"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\t{playerName} skipped pyramid card");
        
        // Továbbítjuk a NetworkGameManager-nek
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.CmdSkipPyramidCard(playerId);
        }
    }

    /// Confirm pyramid card placement and start point giving (kliens küldi a szervernek)
    public void CmdConfirmPyramidCard(string magicWord)
    {
        if (!isLocalPlayer) return;
        
        CmdConfirmPyramidCardServer(magicWord);
    }

    [Command]
    void CmdConfirmPyramidCardServer(string magicWord)
    {
        Debug.Log($"[Server]\t{playerName} confirmed pyramid card placement"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\t{playerName} confirmed pyramid card placement");
        
        // Továbbítjuk a NetworkGameManager-nek
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.CmdConfirmPyramidCard(playerId, magicWord);
        }
    }

    [Command]
    public void CmdRegisterClientReady()
    {
        Debug.Log($"[Server] Registering client ready for player: {playerName}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] Registering client ready for player: {playerName}");
        
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.CmdClientReady();
        }
    }

    
    /// Debug info kiírása

    public override void OnStopClient()
    {
        base.OnStopClient();
        
        // Ha ez a local player volt, akkor disconnectelt
        if (isLocalPlayer)
        {
            Debug.Log($"[Client] Local player disconnected: {playerName} (ID: {playerId})"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client] Local player disconnected: {playerName} (ID: {playerId})");
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        
        // Server oldalon kezeljük a disconnect-et
        Debug.Log($"[Server] Player disconnected: {playerName} (ID: {playerId})"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] Player disconnected: {playerName} (ID: {playerId})");
        
        // Értesítjük a GameManager-t
        if (NetworkGameManager.Instance != null && playerId > 0)
        {
            NetworkGameManager.Instance.HandlePlayerDisconnect(playerId, playerName);
        }
    }

    
    void OnGUI()
    {
        if (!isLocalPlayer) return;

        GUI.Box(new Rect(10, 10, 200, 90), "My Player Info");
        GUI.Label(new Rect(20, 35, 180, 20), $"ID: {playerId}");
        GUI.Label(new Rect(20, 55, 180, 20), $"Name: {playerName}");
        //GUI.Label(new Rect(20, 75, 180, 20), $"Score: {playerScore}");
    }
    
}