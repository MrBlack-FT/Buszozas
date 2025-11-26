using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// Custom Network Manager a Buszozas játékhoz.
/// Kezeli a szoba létrehozást, csatlakozást, és a játékosok spawn-olását.
public class BuszNetworkManager : NetworkManager
{
    [Header("Debug & Scene Management")]
    [SerializeField] private Debugger debugger;
    [SerializeField] private CustomSceneManager customSceneManager;

    [Header("Busz Room Settings")]
    [SerializeField] private GameObject roomDataPrefab;             // NetworkRoomData prefab
    [SerializeField] private GameObject networkGameManagerPrefab;   // NetworkGameManager prefab (Game scene-ben spawn)
    private NetworkRoomData roomData;                               // Runtime instance

    // Player tracking: connectionId -> playerName
    private Dictionary<int, string> connectedPlayers = new Dictionary<int, string>();

    public NetworkRoomData GetRoomData() => roomData;

    public static new BuszNetworkManager singleton { get; private set; }

    public override void Awake()
    {
        if (debugger == null)
        {
            debugger = Resources.FindObjectsOfTypeAll<Debugger>()[0];
        }

        if (customSceneManager == null)
        {
            customSceneManager = FindAnyObjectByType<CustomSceneManager>();
            if (customSceneManager == null)
            {
                Debug.LogWarning("CustomSceneManager not found in the scene.");
            }
        }

        base.Awake();
        singleton = this;
        
        // Automatikus player spawn engedélyezése
        autoCreatePlayer = true;
    }

    #region Host Room

    /// Host létrehoz egy szobát a beállított paraméterekkel

    public void CreateRoom(string busName, int maxPlayers, bool reversedPyramid)
    {
        // Host indítása (Server + Client egyben)
        StartHost();

        // NetworkRoomData spawn-olása
        if (NetworkServer.active && roomDataPrefab != null)
        {
            GameObject obj = Instantiate(roomDataPrefab);
            NetworkServer.Spawn(obj);
            roomData = obj.GetComponent<NetworkRoomData>();
            
            if (roomData != null)
            {
                roomData.SetRoomSettings(busName, maxPlayers, reversedPyramid);
            }
        }

        // Discovery indítása (hogy mások lássák a szobát)
        var discovery = FindFirstObjectByType<BuszNetworkDiscovery>();
        if (discovery != null)
        {
            discovery.AdvertiseServer();
            Debug.Log("[Host] Discovery advertising started"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Host] Discovery advertising started");
        }

        Debug.Log($"[Host] Room created: {busName}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Host] Room created: {busName}");
    }

    #endregion

    #region Join Room

    
    /// Client csatlakozik egy szobához IP alapján

    public void JoinRoom(string ipAddress, string playerName)
    {
        networkAddress = ipAddress;
        
        // Játékos nevének mentése
        //PlayerPrefs.SetString("PlayerName", playerName);
        PlayerPrefs.SetString("playerName", playerName);
        
        // Client indítása
        StartClient();

        Debug.Log($"[Client] Joining room at {ipAddress} as {playerName}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client] Joining room at {ipAddress} as {playerName}");
    }

    #endregion

    #region Server Callbacks

    
    /// Amikor egy játékos csatlakozik a serverhez

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);

        Debug.Log($"[Server] Client connected: {conn.connectionId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] Client connected: {conn.connectionId}");
    }

    
    /// Amikor egy játékos ready lesz (teljes connection)

    public override void OnServerReady(NetworkConnectionToClient conn)
    {
        base.OnServerReady(conn);

        Debug.Log($"[Server] Client ready: Connection {conn.connectionId}, has player object: {conn.identity != null}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] Client ready: Connection {conn.connectionId}, has player object: {conn.identity != null}");
        
        // NE adjuk hozzá itt a room listához - a NetworkPlayer.CmdRequestPlayerId() fogja
        // amikor megkapja a kliens nevét
    }

    
    /// Amikor egy játékos disconnect
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        // Játékos eltávolítása a room listából
        if (connectedPlayers.TryGetValue(conn.connectionId, out string playerName))
        {
            if (roomData != null)
            {
                roomData.RemovePlayer(playerName);
            }
            connectedPlayers.Remove(conn.connectionId);
            Debug.Log($"[Server] {playerName} disconnected"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] {playerName} disconnected");
        }
        
        base.OnServerDisconnect(conn);

        Debug.Log($"[Server] Client disconnected: {conn.connectionId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] Client disconnected: {conn.connectionId}");
    }

    #endregion

    #region Kliens Callbacks

    
    /// Amikor sikeresen csatlakoztunk a serverhez
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        
        Debug.Log("[Client] Connected to server!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Client] Connected to server!");
    }

    
    /// Amikor disconnect-oltunk a serverről
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();

        Debug.Log("[Client] Disconnected from server!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Client] Disconnected from server!");
        
        // Discovery leállítása (ha futott)
        var discovery = FindFirstObjectByType<BuszNetworkDiscovery>();
        if (discovery != null)
        {
            discovery.StopDiscovery();
            Debug.Log("[Client] Discovery stopped"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Client] Discovery stopped");
        }

        // NetworkRoomData törlése (DontDestroyOnLoad objektum tisztítása)
        // Client oldalon a NetworkRoomData a server despawn-olja, de Instance-t nullázni kell
        if (NetworkRoomData.Instance != null)
        {
            Destroy(NetworkRoomData.Instance.gameObject);
        }
        roomData = null;

        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            Debug.Log("[BuszNetworkManager] Loading MainMenu."); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[BuszNetworkManager] Loading MainMenu.");
            if (customSceneManager != null)
            {
                customSceneManager.LoadScene("MainMenu");
            }
            else
            {
                customSceneManager = FindAnyObjectByType<CustomSceneManager>();
                if (customSceneManager != null)
                {
                    customSceneManager.LoadScene("MainMenu");
                }
                else
                {
                    SceneManager.LoadScene("MainMenu");
                }
            }
        }
    }

    public override void OnStopHost()
    {
        // Discovery leállítása
        var discovery = FindFirstObjectByType<BuszNetworkDiscovery>();
        if (discovery != null)
        {
            discovery.StopDiscovery();
            Debug.Log("[Host] Discovery stopped"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Host] Discovery stopped");
        }

        // NetworkRoomData törlése amikor host leáll
        if (roomData != null)
        {
            // FONTOS: Singleton Instance nullázása ELŐBB
            if (NetworkRoomData.Instance == roomData)
            {
                // Trigger OnDestroy to clear singleton
                Destroy(roomData.gameObject);
            }
            roomData = null;
        }

        base.OnStopHost();
        Debug.Log("[Host] Stopped"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Host] Stopped");

        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            Debug.Log("[BuszNetworkManager] Loading MainMenu."); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[BuszNetworkManager] Loading MainMenu.");
            if (customSceneManager != null)
            {
                customSceneManager.LoadScene("MainMenu");
            }
            else
            {
                customSceneManager = FindAnyObjectByType<CustomSceneManager>();
                if (customSceneManager != null)
                {
                    customSceneManager.LoadScene("MainMenu");
                }
                else
                {
                    SceneManager.LoadScene("MainMenu");
                }
            }
        }
    }

    
    /// Amikor a server betölt egy új scene-t
    public override void OnServerChangeScene(string newSceneName)
    {
        base.OnServerChangeScene(newSceneName);

        Debug.Log($"[Server] Changing scene to: {newSceneName}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] Changing scene to: {newSceneName}");
        
        // Ha Game scene-re váltunk, spawn-oljuk a NetworkGameManager-t
        if (newSceneName == "Game" && networkGameManagerPrefab != null)
        {
            GameObject obj = Instantiate(networkGameManagerPrefab);
            NetworkServer.Spawn(obj);

            Debug.Log("[Server] NetworkGameManager spawned for Game scene"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Server] NetworkGameManager spawned for Game scene");
        }
    }

    
    /// Amikor a server befejezte a scene betöltést
    public override void OnServerSceneChanged(string sceneName)
    {
        base.OnServerSceneChanged(sceneName);
        
        Debug.Log($"[Server] Scene loaded: {sceneName}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] Scene loaded: {sceneName}");
        
        // NetworkPlayer objektumok automatikusan perzisztensek (DontDestroyOnLoad a Mirror-ben)
        // autoCreatePlayer = true marad, így a host is kap player objektumot minden scene-ben
    }
    
    /// Amikor a client betöltött egy új scene-t
    public override void OnClientChangeScene(string newSceneName, SceneOperation sceneOperation, bool customHandling)
    {
        base.OnClientChangeScene(newSceneName, sceneOperation, customHandling);

        Debug.Log($"[Client] Scene changed to: {newSceneName}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client] Scene changed to: {newSceneName}");
    }

    #endregion

    #region Exit / Disconnect

    /// Exit gomb hívja: host leállítja a szervert, client disconnectel
    public void LeaveGame()
    {
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            // Host (Server + Client egyben)
            Debug.Log("[Host] Stopping host and returning to MainMenu"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Host] Stopping host and returning to MainMenu");
            
            StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            // Client
            Debug.Log("[Client] Disconnecting from host and returning to MainMenu"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Client] Disconnecting from host and returning to MainMenu");
            
            StopClient();
        }

        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            Debug.Log("[BuszNetworkManager] Loading MainMenu."); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[BuszNetworkManager] Loading MainMenu.");
            if (customSceneManager != null)
            {
                customSceneManager.LoadScene("MainMenu");
            }
            else
            {
                customSceneManager = FindAnyObjectByType<CustomSceneManager>();
                if (customSceneManager != null)
                {
                    customSceneManager.LoadScene("MainMenu");
                }
                else
                {
                    SceneManager.LoadScene("MainMenu");
                }
            }
        }       
    }

    #endregion
}
