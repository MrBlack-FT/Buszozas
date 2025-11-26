using UnityEngine;
using Mirror;
using System.Collections.Generic;
using DG.Tweening;

/// Piramis kártya adat struct (Mirror serialization)
public struct PyramidCardData
{
    public int cardType;
    public int cardValue;
    public int cardBackType;

    public PyramidCardData(Card card)
    {
        cardType = (int)card.GetCardType();
        cardValue = (int)card.GetCardValue();
        cardBackType = (int)card.GetCardBackType();
    }
}

/// Server authority - minden döntés a serveren történik.
public class NetworkGameManager : NetworkBehaviour
{
    public static NetworkGameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Network State")]

    [SyncVar] private int pendingClients = 0;


    [SyncVar] private int randomSeed; // Szinkronizált random seed minden clientnek
    [SyncVar] private int currentPhaseInt; // GamePhase enum → int
    [SyncVar] private int currentPlayerIndex;
    [SyncVar] private int currentRound;
    [SyncVar] private float currentTimer; // Visszaszámláló (másodperc)
    [SyncVar] private float pointGiveTimer; // Pont osztás timer
    [SyncVar] private bool waitingForTipp; // Vár-e tippre
    
    // Aktuális kártya adatok (szerver húzza, kliensek megjelenítik)
    [SyncVar] private int currentCardType; // Card.Type → int
    [SyncVar] private int currentCardValue; // Card.Value → int
    [SyncVar] private int currentCardBackType; // Card.BackType → int
    //[SyncVar] private bool currentCardRevealed; // Fel van-e fordítva a kártya

    // Piramis kártyák adatok (szerver húzza, kliensek megjelenítik) - összesen 15 kártya
    private List<PyramidCardData> pyramidCards = new List<PyramidCardData>();

    // Piramis állapot szinkronizálása
    [SyncVar] private int currentPiramisRow = 1;           // 1-5 (melyik sorban vagyunk)
    [SyncVar] private int currentPiramisCardIndex = 0;     // 0-4 (hányadik kártya a sorban)
    [SyncVar] private int placedCardsNumber = 0;           // Letett kártyák száma
    [SyncVar] private int totalPointsToGive = 0;           // Osztandó pontok
    [SyncVar] private float cardDropTimer = 30f;           // Kártya letevés timer
    [SyncVar] private bool waitingForCardDrop = false;     // Vár-e kártya letevésre

    // Pont osztás állapot szinkronizálása
    [SyncVar] private bool isGivingPoints = false;

    // Busz állapot szinkronizálása
    [SyncVar] private int currentBuszCardIndex = 0;        // 0-5 (melyik busz kártyánál tartunk)
    [SyncVar] private bool waitingForBuszTipp = false;     // Vár-e busz tippre
    public readonly SyncDictionary<int, int> playerBuszAttempts = new SyncDictionary<int, int>();  // playerId -> attempts

    //private bool canServerAcceptCmd = true;

    //private bool gameStarted = false;

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
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        
        // Server generál egy random seed-et.
        randomSeed = Random.Range(int.MinValue, int.MaxValue);

        Debug.Log($"[Server]\tRandom seed: {randomSeed}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tRandom seed: {randomSeed}");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        
        if (!isServer)
        {
            // Client beállítja a Random.seed-et a server által adott értékre.
            Random.InitState(randomSeed);

            Debug.Log($"[Client]\tRandom seed synchronized: {randomSeed}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client]\tRandom seed synchronized: {randomSeed}");
        }
    }

    /*
    public override void OnStopClient()
    {
        base.OnStopClient();

        string offlineScene = NetworkManager.singleton.offlineScene;

        if (SceneManager.GetActiveScene().name == offlineScene)
            return;

        if (!string.IsNullOrEmpty(offlineScene))
            SceneManager.LoadScene(offlineScene);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        string offlineScene = NetworkManager.singleton.offlineScene;

        if (SceneManager.GetActiveScene().name == offlineScene)
            return;

        if (!string.IsNullOrEmpty(offlineScene))
            SceneManager.LoadScene(offlineScene);
    }
    */



    void Start()
    {
        // Server késleltetett inicializálás (várjuk meg a GameManager scene betöltését)
        if (isServer)
        {
            Invoke(nameof(InitializeMultiplayerGame), 0.5f);
        }
    }

    #region Timer Management

    [Server]
    public void UpdateCurrentPhase(GamePhase newPhase)
    {
        currentPhaseInt = (int)newPhase;
    }

    public GamePhase GetCurrentPhase()
    {
        return (GamePhase)currentPhaseInt;
    }

    /// Server frissíti a timer SyncVar-t
    [Server]
    public void UpdateTimer(float newTimerValue)
    {
        currentTimer = newTimerValue;
    }

    /// Kliens lekéri az aktuális timer értéket
    public float GetCurrentTimer()
    {
        return currentTimer;
    }

    /// Server frissíti a pont osztás timer SyncVar-t
    [Server]
    public void UpdatePointGiveTimer(float newTimerValue)
    {
        pointGiveTimer = newTimerValue;
    }

    /// Kliens lekéri az aktuális pont osztás timer értéket
    public float GetPointGiveTimer()
    {
        return pointGiveTimer;
    }

    #region Piramis State Sync

    /// Server frissíti a piramis sor indexét
    [Server]
    public void UpdatePiramisRow(int row)
    {
        currentPiramisRow = row;
    }

    public int GetPiramisRow()
    {
        return currentPiramisRow;
    }

    [Server]
    public void UpdateCurrentRound(int round)
    {
        currentRound = round;
    }

    public int GetCurrentRound()
    {
        return currentRound;
    }

    [Server]
    public void UpdateWaitingForTipp(bool waiting)
    {
        waitingForTipp = waiting;
    }

    public bool GetWaitingForTipp()
    {
        return waitingForTipp;
    }

    /// Server frissíti a játékos indexét
    [Server]
    public void UpdateCurrentPlayerIndex(int index)
    {
        currentPlayerIndex = index;
    }

    public int GetCurrentPlayerIndex()
    {
        return currentPlayerIndex;
    }

    /// Server frissíti a piramis kártya indexét
    [Server]
    public void UpdatePiramisCardIndex(int index)
    {
        currentPiramisCardIndex = index;
    }

    public int GetPiramisCardIndex()
    {
        return currentPiramisCardIndex;
    }

    /// Server frissíti a letett kártyák számát
    [Server]
    public void UpdatePlacedCardsNumber(int count)
    {
        placedCardsNumber = count;
    }

    public int GetPlacedCardsNumber()
    {
        return placedCardsNumber;
    }

    /// Server frissíti az osztandó pontokat
    [Server]
    public void UpdateTotalPointsToGive(int points)
    {
        totalPointsToGive = points;
    }

    public int GetTotalPointsToGive()
    {
        return totalPointsToGive;
    }

    /// Server frissíti a kártya letevés timert
    [Server]
    public void UpdateCardDropTimer(float time)
    {
        cardDropTimer = time;
    }

    public float GetCardDropTimer()
    {
        return cardDropTimer;
    }

    /// Server frissíti a kártya letevésre várakozás állapotát
    [Server]
    public void UpdateWaitingForCardDrop(bool waiting)
    {
        waitingForCardDrop = waiting;
    }

    public bool GetWaitingForCardDrop()
    {
        return waitingForCardDrop;
    }

    [Server]
    public void UpdateIsGivingPoints(bool isGiving)
    {
        isGivingPoints = isGiving;
    }

    public bool GetIsGivingPoints()
    {
        return isGivingPoints;
    }

    #region Busz State Sync

    /// Server frissíti a busz kártya indexét
    [Server]
    public void UpdateBuszCardIndex(int index)
    {
        currentBuszCardIndex = index;
    }

    public int GetBuszCardIndex()
    {
        return currentBuszCardIndex;
    }

    /// Server frissíti a busz tippre várakozás állapotát
    [Server]
    public void UpdateWaitingForBuszTipp(bool waiting)
    {
        waitingForBuszTipp = waiting;
    }

    public bool GetWaitingForBuszTipp()
    {
        return waitingForBuszTipp;
    }

    // PlayerBuszAttempts szinkronizálás
    [Server]
    public void UpdatePlayerBuszAttempts(int playerId, int attempts)
    {
        playerBuszAttempts[playerId] = attempts;
    }

    public int GetPlayerBuszAttempts(int playerId)
    {
        return playerBuszAttempts.ContainsKey(playerId) ? playerBuszAttempts[playerId] : 0;
    }

    public bool ContainsPlayerBuszAttempts(int playerId)
    {
        return playerBuszAttempts.ContainsKey(playerId);
    }

    #endregion

    #endregion

    #endregion

    #region Server Methods

    /// Server inicializálja a multiplayer játékot.
    [Server]
    public void InitializeMultiplayerGame()
    {
        Debug.Log("[Server]\tInitializing multiplayer game..."); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Server]\tInitializing multiplayer game...");
        
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }
        
        if (gameManager == null)
        {
            Debug.LogError("[Server]\tGameManager not found! Retrying..."); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("<ERROR!> [Server]\tGameManager not found! Retrying...");
            Invoke(nameof(InitializeMultiplayerGame), 0.2f);
            return;
        }

        // NetworkRoomData-ból játékosnevek kiolvasása
        NetworkRoomData roomData = NetworkRoomData.Instance;
        if (roomData == null)
        {
            Debug.LogError("[Server]\tNetworkRoomData not found!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("<ERROR!> [Server]\tNetworkRoomData not found!");
            return;
        }

        // Játékosnevek átalakítása List<string>-gé
        List<string> playerNamesFromNetwork = new List<string>();
        for (int i = 0; i < roomData.playerNames.Count; i++)
        {
            playerNamesFromNetwork.Add(roomData.playerNames[i]);
        }

        Debug.Log($"[Server]\tInitializing game with {playerNamesFromNetwork.Count} players"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tInitializing game with {playerNamesFromNetwork.Count} players");

        // GameManager inicializálása a hálózatról kapott játékosokkal
        gameManager.InitializeMultiplayerGame(playerNamesFromNetwork);
        
        //gameStarted = true;
        
        // Clienteknek is elküldjük a játékosneveket
        RpcInitializeClients(playerNamesFromNetwork.ToArray());
    }

    /// Host indítja a játékot (Start gomb)
    [Server]
    public void ServerStartGame()
    {
        if (gameManager == null)
        {
            Debug.LogError("[Server]\tGameManager not found!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("<ERROR!>\t[Server]\tGameManager not found!");

            return;
        }

        Debug.Log("[Server]\tHost starting game for all clients"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Server]\tHost starting game for all clients");
        
        // Server elindítja a játékot (UI animációk)
        RpcStartGameForAllClients();
    }

    /*
    /// Server inicializálja a játékot amikor minden játékos csatlakozott
    [Server]
    public void StartGame()
    {
        Debug.Log("[Server] Starting game with all players...NOT YET IMPLEMENTED"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Server] Starting game with all players...NOT YET IMPLEMENTED");
        // TODO: GameManager.StartGame() hívása
    }
    */

    #endregion

    #region Client → Server (Commands)

    /// Client beküldi a tippjét a servernek
    [Command(requiresAuthority = false)]
    public void CmdSubmitTipp(int playerId, int tippValue, NetworkConnectionToClient sender = null)
    {
        //Debug.Log($"[Server]\tPlayer {playerId} submitted tipp: {tippValue}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tPlayer {playerId} submitted tipp: {tippValue}");
        
        // GameManager feldolgozza a tippet
        if (gameManager != null)
        {
            gameManager.GM_Server_ProcessPlayerTipp(playerId, (TippValue)tippValue);
        }
        else
        {
            Debug.LogError("[Server]\tGameManager not available for tipp processing!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("<ERROR!> [Server]\tGameManager not available for tipp processing!");
        }
    }

    /// Client jelzi hogy készen áll (ready)
    [Command(requiresAuthority = false)]
    public void CmdPlayerReady(int playerId, NetworkConnectionToClient sender = null)
    {
        Debug.Log($"[Server]\tPlayer {playerId} is ready"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tPlayer {playerId} is ready");
        
        // TODO: Ready state tracking
    }

    /// Client megerősíti a pont osztást
    [Command(requiresAuthority = false)]
    public void CmdConfirmPointGive(int playerId, int[] pointsToGive, bool isPiramis, NetworkConnectionToClient sender = null)
    {
        Debug.Log($"[Server]\tPlayer {playerId} confirmed point giving"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tPlayer {playerId} confirmed point giving");
        
        // SZERVER: Pont osztás állapot leállítása
        UpdateIsGivingPoints(false);
        UpdatePointGiveTimer(20f);
        
        // Broadcast minden kliensnek: állítsd le a timert!
        RpcStopPointGiveTimer();
        
        // Pont osztás feldolgozása SERVER OLDALON
        if (gameManager != null)
        {
            gameManager.GM_Server_ProcessPointGiveConfirm(playerId, pointsToGive, isPiramis);
        }
        else
        {
            Debug.LogError("[Server]\tGameManager not available for point give processing!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("<ERROR!> [Server]\tGameManager not available for point give processing!");
        }
    }

    /// Client skip pyramid card
    [Command(requiresAuthority = false)]
    public void CmdSkipPyramidCard(int playerId, NetworkConnectionToClient sender = null)
    {
        Debug.Log($"[Server]\tPlayer {playerId} skipped pyramid card");  if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tPlayer {playerId} skipped pyramid card");
        
        // Skip feldolgozása SERVER OLDALON
        if (gameManager != null)
        {
            UpdateWaitingForCardDrop(false);
            UpdateCardDropTimer(30f);
            // Broadcast minden kliensnek: állítsd le a card drop timert!
            RpcStopCardDropTimer();
            
            // Server broadcastolja a skip-et és a következő játékost
            RpcHideTimer();
            RpcHidePiramisButtons();
            RpcNextPiramisPlayer(2f);
        }
        else
        {
            Debug.LogError("[Server]\tGameManager not available for skip processing!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("<ERROR!> [Server]\tGameManager not available for skip processing!");
        }
    }

    /// Client confirms pyramid card placement and starts point giving
    [Command(requiresAuthority = false)]
    public void CmdConfirmPyramidCard(int playerId, string magicWord, NetworkConnectionToClient sender = null)
    {
        Debug.Log($"[Server]\tPlayer {playerId} confirmed pyramid card placement");  if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tPlayer {playerId} confirmed pyramid card placement");
        
        // Confirm feldolgozása SERVER OLDALON
        if (gameManager != null)
        {
            UpdateWaitingForCardDrop(false);
            UpdateCardDropTimer(30f);
            // Broadcast minden kliensnek: állítsd le a card drop timert!
            RpcStopCardDropTimer();
            
            // SZERVER: Pont osztás állapot beállítása
            UpdateIsGivingPoints(true);
            UpdatePointGiveTimer(15f);
            
            // Broadcast minden kliensnek: indítsd el a pont kiosztást!
            RpcStartPyramidPointGiving(magicWord == "piramis");
        }
        else
        {
            Debug.LogError("[Server]\tGameManager not available for confirm processing!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("<ERROR!> [Server]\tGameManager not available for confirm processing!");
        }
    }

    /// Server húz egy kártyát és broadcastolja minden kliensnek
    [Server]
    public void ServerDrawAndBroadcastCurrentCard()
    {
        if (gameManager == null)
        {
            Debug.LogError("[Server]\tGameManager not available for card drawing!");
            return;
        }

        // Deck referencia a GameManager-ből (reflection vagy public metódus)
        var deckField = gameManager.GetType().GetField("deck", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (deckField != null)
        {
            Deck deck = deckField.GetValue(gameManager) as Deck;
            if (deck != null)
            {
                Card drawnCard = deck.DrawCard();
                
                // Kártya adatok mentése SyncVar-okba
                currentCardType = (int)drawnCard.GetCardType();
                currentCardValue = (int)drawnCard.GetCardValue();
                currentCardBackType = (int)drawnCard.GetCardBackType();
                //currentCardRevealed = false;

                //Debug.Log($"[Server]\tDrew card: {drawnCard.GetCardType()} {drawnCard.GetCardValue()}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tDrew card: {drawnCard.GetCardType()} {drawnCard.GetCardValue()}");

                // Broadcast minden kliensnek
                RpcShowCard(currentCardType, currentCardValue, currentCardBackType);
            }
        }
    }

    /// Server húz piramis kártyákat és broadcastolja minden kliensnek
    [Server]
    public void ServerBroadcastFillPyramidCards()
    {
        if (gameManager == null)
        {
            Debug.LogError("[Server]\tGameManager not available for pyramid card drawing!");
            return;
        }

        // Deck referencia a GameManager-ből
        var deckField = gameManager.GetType().GetField("deck", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (deckField != null)
        {
            Deck deck = deckField.GetValue(gameManager) as Deck;
            if (deck != null)
            {
                pyramidCards.Clear();
                
                for (int i = 0; i < 15; i++)
                {
                    Card drawnCard = deck.DrawCard();
                    pyramidCards.Add(new PyramidCardData(drawnCard));
                }

                //Debug.Log($"[Server]\tDrew {pyramidCards.Count} pyramid cards"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tDrew {pyramidCards.Count} pyramid cards");

                // Broadcast minden kliensnek
                RpcFillPyramidWithCards(pyramidCards.ToArray());
            }
        }
    }

    #endregion

    #region Server → Clients (ClientRpc)

    /// Server elküldi a húzott kártyát minden kliensnek
    [ClientRpc]
    public void RpcShowCard(int cardType, int cardValue, int cardBackType)
    {
        //Debug.Log($"[Client]\tReceived card: Suit={cardType}, Rank={cardValue}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client]\tReceived card: Suit={cardType}, Rank={cardValue}");

        // GameManager ShowCard hívása
        if (gameManager != null)
        {
            gameManager.ShowDrawnCard((CardType)cardType, (CardValue)cardValue, (CardBackType)cardBackType);
        }
    }

    /// Server elküldi a piramis kártyákat minden kliensnek
    [ClientRpc]
    public void RpcFillPyramidWithCards(PyramidCardData[] cards)
    {
        //Debug.Log($"[Client]\tReceived {cards.Length} pyramid cards"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client]\tReceived {cards.Length} pyramid cards");

        // GameManager FillPyramidWithCards hívása
        if (gameManager != null)
        {
            gameManager.FillPyramidWithBroadcastedCards(cards);
        }
    }

    #endregion

    #region Server → Clients (ClientRpc) - Continued

    /// Server elküldi minden kliensnek hogy induljon a játék
    [ClientRpc]
    public void RpcStartGameForAllClients()
    {
        Debug.Log("[ClientRpc]   Starting game on client"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[ClientRpc]   Starting game on client");

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null)
        {
            // Közvetlenül hívjuk a GameManager metódusait
            StartCoroutine(StartGameSequence());
        }
        else
        {
            Debug.LogError("[NetworkGameManager] GameManager reference is null!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("<ERROR!>\t[NetworkGameManager] GameManager reference is null!");
        }
    }

    private System.Collections.IEnumerator StartGameSequence()
    {
        if (gameManager == null)
        {
            Debug.LogError("[NetworkGameManager] GameManager reference is null!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("<ERROR!>\t[NetworkGameManager] GameManager reference is null!");
            yield break;
        }

        // Start gombok keresése a GameManager-en keresztül
        // A GameManager Inspector-ban van referencia a startButtonsGroup-ra
        var startButtonsField = gameManager.GetType().GetField("startButtonsGroup", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (startButtonsField != null)
        {
            GameObject startButtonsGroup = startButtonsField.GetValue(gameManager) as GameObject;
            var gameDisplayField = gameManager.GetType().GetField("gameDisplay", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (gameDisplayField != null && startButtonsGroup != null)
            {
                GameDisplay gameDisplay = gameDisplayField.GetValue(gameManager) as GameDisplay;
                if (gameDisplay != null)
                {
                    gameDisplay.HideStartButtons(startButtonsGroup, 0.5f, null);
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }

        // InitializeGame hívása
        gameManager.InitializeGame();
    }

    /// Server elküldi a játékosneveket minden kliensnek
    [ClientRpc]
    public void RpcInitializeClients(string[] playerNamesArray)
    {
        Debug.Log($"[ClientRpc]   Received {playerNamesArray.Length} player names from server"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   Received {playerNamesArray.Length} player names from server");
        
        // FONTOS: Host már inicializálva lett a Server oldalon, ne duplikáljuk!
        if (isServer)
        {
            Debug.Log("[ClientRpc]   Skipping RpcInitializeClients on host (already initialized on server)"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[ClientRpc]   Skipping RpcInitializeClients on host (already initialized on server)");
            return;
        }
        
        // GameVars szinkronizálás (ha a client nem host)
        if (!isServer)
        {
            NetworkRoomData roomData = NetworkRoomData.Instance;
            if (roomData != null)
            {
                GameVars.Instance.BusName = roomData.busName;
                GameVars.Instance.NumberOfPlayersInGame = playerNamesArray.Length;
                GameVars.Instance.ReversedPyramidMode = roomData.reversedPyramid;

                // Játékosnevek beállítása
                for (int i = 0; i < playerNamesArray.Length; i++)
                {
                    GameVars.Instance.SetPlayerName(i, playerNamesArray[i]);
                }
            }
        }
        
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null)
        {
            List<string> playerNamesList = new List<string>(playerNamesArray);
            gameManager.InitializeMultiplayerGame(playerNamesList);
        }
        
        //gameStarted = true;
    }

    /*
    /// Server értesíti a clienteket hogy elindult a játék
    [ClientRpc]
    public void RpcNotifyGameStarted()
    {
        Debug.Log("[Client] Game has started!");
        if (debugger != null && debugger.gameObject.activeInHierarchy)
            debugger.AddTextToDebugFile("[Client] Game has started!");
        
        gameStarted = true;
    }
    */

    [ClientRpc]
    public void RpcRotatePlayers()
    {
        Debug.Log("[ClientRpc]   RotatePlayers called"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[ClientRpc]   RotatePlayers called");
        
        if (gameManager != null)
        {
            gameManager.RotatePlayers();
        }
    }

    /// Server broadcastolja a tipp eredményét minden clientnek
    [ClientRpc]
    public void RpcShowTippResult(int playerId, bool isCorrect, int cardType, int cardValue, int cardBackType, int penaltyPoints)
    {
        Debug.Log($"[ClientRpc]   Player {playerId} tipp was {(isCorrect ? "correct" : "wrong")}, currentRound: {penaltyPoints}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   Player {playerId} tipp was {(isCorrect ? "correct" : "wrong")}, currentRound: {penaltyPoints}");
        
        // GameManager ShowTippResult hívása
        if (gameManager != null)
        {
            gameManager.GM_Client_ShowTippResult(playerId, isCorrect, cardType, cardValue, cardBackType, penaltyPoints);
        }
    }

    [ClientRpc]
    public void RpcWaitForPlayerToDropCard()
    {
        Debug.Log("[ClientRpc]   WaitForPlayerToDropCard"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[ClientRpc]   WaitForPlayerToDropCard");
        
        if (gameManager != null)
        {
            gameManager.WaitForMultiPlayerToDropCard();
        }
    }

    /// Server broadcastolja a Piramis játékos folytatást minden kliensnek
    [ClientRpc]
    public void RpcNextPiramisPlayer(float delay)
    {
        Debug.Log($"[ClientRpc]   ServerNextPiramisPlayer called with delay {delay}. Calling GM_Client_NextPiramisPlayer"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   ServerNextPiramisPlayer with delay {delay} called. Calling GM_Client_NextPiramisPlayer");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_NextPiramisPlayer(delay);
        }
        else
        {
            Debug.LogError("[ClientRpc]   GameManager reference is null in ServerNextPiramisPlayer!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("<ERROR!>\t[ClientRpc]\tGameManager reference is null in ServerNextPiramisPlayer!");
        }
    }

    /// Server broadcastolja a piramis kártya flip animációt
    /// MAJD 1s delay után ellenőrzi, hogy a játékos tud-e kártyát letenni
    [ClientRpc]
    public void RpcFlipPyramidCard()
    {
        //Debug.Log("[ClientRpc]   RpcFlipPyramidCard()"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[ClientRpc]   RpcFlipPyramidCard");
        //SendLogToServer("[ClientRpc]   RpcFlipPyramidCard called.     [Message sent from client to server for logging]");
        
        if (gameManager != null)
        {
            gameManager.FlipPyramidCard();
            
            /*
            // 1s delay után ellenőrizzük, hogy tud-e kártyát letenni
            // CSAK A SZERVER dönt!
            DOVirtual.DelayedCall(1f, () =>
            {
                if (isServer && gameManager != null)
                {
                    bool hasMatchingCard = gameManager.CheckIfPlayerCanDropCard();
                    
                    // Ha NINCS kártya, akkor 1 másodperc után lépünk a következő játékosra
                    if (!hasMatchingCard)
                    {
                        DOVirtual.DelayedCall(1f, () =>
                        {
                            RpcNextPiramisPlayer(2f);
                        });
                    }
                }
            });
            */
        }
    }

    [ClientRpc]
    public void RpcNextPiramisCard(float delay)
    {
        if (isServer)
        {
            Debug.Log("[In ClientRpc SERVER Call]   RpcNextPiramisCard() called. Calling GM_Client_NextPiramisCard()."); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[In ClientRpc SERVER Call]   RpcNextPiramisCard() called. Calling GM_Client_NextPiramisCard().");
            UpdatePiramisCardIndex(currentPiramisCardIndex + 1);
            //currentPiramisCardIndex++;
        }
        Debug.Log("[ClientRpc]   RpcNextPiramisCard() called. Calling GM_Client_NextPiramisCard()."); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[ClientRpc]   RpcNextPiramisCard() called. Calling GM_Client_NextPiramisCard().");
        
        if (gameManager != null)
        {
            DOVirtual.DelayedCall(delay, () => 
            {
                gameManager.GM_Client_NextPiramisCard();
            });
        }
    }

    /// Server broadcastolja a RefreshPlayerUI hívást
    [ClientRpc]
    public void RpcRefreshPlayerUI()
    {
        //Debug.Log("[ClientRpc]   RefreshPlayerUI"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[ClientRpc]   RefreshPlayerUI");
        
        if (gameManager != null)
        {
            gameManager.RefreshPlayerUI();
        }
    }

    /// Server broadcastolja a játékos pontszám változását
    [ClientRpc]
    public void RpcUpdatePlayerScore(int playerId, int newScore)
    {
        //Debug.Log($"[ClientRpc]   Player {playerId} score updated to {newScore}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   Player {playerId} score updated to {newScore}");

        // GameManager UpdatePlayerScore hívása
        if (gameManager != null)
        {
            gameManager.GM_Client_UpdatePlayerScore(playerId, newScore);
        }
    }

    /// Server broadcastolja hogy lejárt a timer
    [ClientRpc]
    public void RpcShowTimerExpired(int playerId)
    {
        Debug.Log($"[ClientRpc]   Timer expired for player {playerId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   Timer expired for player {playerId}");

        // GameManager ShowTimerExpiredToast hívása
        if (gameManager != null)
        {
            gameManager.ShowTimerExpired();
        }
    }

    /// Server broadcastolja hogy lejárt a Busz timer
    [ClientRpc]
    public void RpcShowBuszTimerExpired(int playerId)
    {
        Debug.Log($"[ClientRpc]   Busz timer expired for player {playerId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   Busz timer expired for player {playerId}");

        // GameManager ShowBuszTimerExpired hívása
        if (gameManager != null)
        {
            gameManager.ShowBuszTimerExpired();
        }
    }

    /*
    /// Server broadcastolja hogy melyik játékos jön
    [ClientRpc]
    public void RpcCurrentPlayerChanged(int playerId)
    {
        Debug.Log($"[ClientRpc]   Current player is now: {playerId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   Current player is now: {playerId}");
        
        
        // TODO: UI frissítés (timer, gombok enable/disable)
    }

    /// Server broadcastolja a játék végét
    [ClientRpc]
    public void RpcGameEnded(string resultsMessage)
    {
        Debug.Log($"[ClientRpc]   Game ended: {resultsMessage}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   Game ended: {resultsMessage}");
        
        // TODO: EndGame UI megjelenítés
    }*/
    
    /// Server broadcastolja az EndGame UI-t
    [ClientRpc]
    public void RpcShowEndGame(string resultsMessage)
    {
        Debug.Log($"[ClientRpc]   RpcShowEndGame"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcShowEndGame");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_ShowEndGame(resultsMessage);
        }
    }

    [ClientRpc]
    public void RpcHideTimer()
    {
        if (gameManager != null)
        {
            gameManager.GM_Client_HideTimer();
        }
    }

    /// Server kezeli a játékos disconnectet
    [Server]
    public void HandlePlayerDisconnect(int playerId, string playerName)
    {
        Debug.Log($"[Server] HandlePlayerDisconnect - Player {playerId} ({playerName}) disconnected"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] HandlePlayerDisconnect - Player {playerId} ({playerName}) disconnected");
        
        if (gameManager != null)
        {
            gameManager.GM_Server_HandlePlayerDisconnect(playerId, playerName);
        }
    }

    /// Server broadcastolja a játékos disconnect-et
    [ClientRpc]
    public void RpcPlayerDisconnected(int playerId, string playerName)
    {
        Debug.Log($"[ClientRpc] RpcPlayerDisconnected - Player {playerId} ({playerName})"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc] RpcPlayerDisconnected - Player {playerId} ({playerName})");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_PlayerDisconnected(playerId, playerName);
        }
    }

    [ClientRpc]
    public void RpcHidePiramisButtons()
    {
        if (gameManager != null)
        {
            gameManager.GM_Client_HidePiramisButtons();
        }
    }

    [ClientRpc]
    public void RpcNextRound()
    {
        Debug.Log($"[ClientRpc]   RpcNextRound called  =>  Calling NextRound()"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcNextRound called  =>  Calling NextRound()");
        
        if (gameManager != null)
        {
            /*
            // Csak növeljük a round-ot (NE NextRound mert az broadcastolna!)
            gameManager.IncrementRound();
            
            // Közvetlenül hívjuk a StartTippKor-t
            gameManager.StartTippKor();
            */
            gameManager.NextRound();
        }
    }

    /// Server broadcastolja a következő kör indítását (tipp folytatás)
    [ClientRpc]
    public void RpcTippContinue(float delay)
    {
        //Debug.Log($"[ClientRpc]   RpcTippContinue called"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcTippContinue called");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_TippContinue(delay);
        }
    }

    /// Server broadcastolja a következő round-ot (új kártya kör)
    [ClientRpc]
    public void RpcStartTippKor()
    {
        //Debug.Log($"[ClientRpc]   RpcStartTippKor() called  =>  Now calling StartTippKor()"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcStartTippKor() called  =>  Now calling StartTippKor()");
        if (gameManager != null)
        {
            gameManager.StartTippKor();
        }
    }

    /// Server broadcastolja a piramis kezdését
    [ClientRpc]
    public void RpcStartPiramis()
    {
        Debug.Log($"[ClientRpc]   RpcStartPiramis() called  =>  Now calling StartPiramis()"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcStartPiramis() called  =>  Now calling StartPiramis()");
        if (gameManager != null)
        {
            gameManager.GM_Client_StartPiramis();
        }
    }

    /// Server broadcastolja a kártya hozzáadást a játékoshoz
    [ClientRpc]
    public void RpcAddCardToPlayer(int playerId, int cardType, int cardValue, int cardBackType)
    {
        //Debug.Log($"[ClientRpc]   RpcAddCardToPlayer - Player {playerId}, {(CardType)cardType} {(CardValue)cardValue} {(CardBackType)cardBackType}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcAddCardToPlayer - Player {playerId}, {(CardType)cardType} {(CardValue)cardValue} {(CardBackType)cardBackType}");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_AddCardToPlayer(playerId, (CardType)cardType, (CardValue)cardValue, (CardBackType)cardBackType);
        }
    }

    /// Server broadcastolja a kártya elvételt a játékostól
    [ClientRpc]
    public void RpcRemoveCardFromPlayer(int playerId, int cardSlotIndex)
    {
        //Debug.Log($"[ClientRpc]   RpcRemoveCardFromPlayer - Player {playerId}, slot {cardSlotIndex}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcRemoveCardFromPlayer - Player {playerId}, slot {cardSlotIndex}");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_RemoveCardFromPlayer(playerId, cardSlotIndex);
        }
    }

    /// Server broadcastolja a letett kártyák számát és a pontokat (Piramis UI frissítés)
    [ClientRpc]
    public void RpcUpdatePiramisCardCount(int placedCards, int totalPoints)
    {
        Debug.Log($"[ClientRpc]   RpcUpdatePiramisCardCount - Placed: {placedCards}, Total Points: {totalPoints}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcUpdatePiramisCardCount - Placed: {placedCards}, Total Points: {totalPoints}");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_UpdatePiramisCardCount(placedCards, totalPoints);
        }
    }

    [ClientRpc]
    public void RpcShowPointGiving(int playerId)
    {
        Debug.Log($"[ClientRpc]   RpcShowPointGiving - Player {playerId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcShowPointGiving - Player {playerId}");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_ShowPointGiving(playerId);
        }
    }

    [ClientRpc]
    public void RpcStartPyramidPointGiving(bool isPiramis)
    {
        Debug.Log($"[ClientRpc]   RpcStartPyramidPointGiving - isPiramis: {isPiramis}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcStartPyramidPointGiving - isPiramis: {isPiramis}");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_StartPyramidPointGiving(isPiramis);
        }
    }

    [ClientRpc]
    public void RpcHidePointGiveUI()
    {
        Debug.Log($"[ClientRpc]   RpcHidePointGiveUI"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcHidePointGiveUI");
        
        if (gameManager != null)
        {
            gameManager.HidePointGiveMultiplayer();
        }
    }

    /// Server broadcastolja: állítsd le a pont osztás timert minden kliensen
    [ClientRpc]
    public void RpcStopPointGiveTimer()
    {
        Debug.Log($"[ClientRpc]   RpcStopPointGiveTimer"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcStopPointGiveTimer");
        
        //UpdateIsGivingPoints(false);
        //UpdatePointGiveTimer(15f);

        // A server-oldali állapot frissítés már megtörtént a hívó oldalon (CmdConfirmPyramidCard-ban)
        // Itt csak a kliens-oldali UI frissítést végezzük
        
        if (gameManager != null)
        {
            gameManager.GM_Client_StopPointGiveTimerMultiplayer();
        }
    }

    /// Server broadcastolja: állítsd le a kártya lerakás timert minden kliensen
    [ClientRpc]
    public void RpcStopCardDropTimer()
    {
        Debug.Log($"[ClientRpc]   RpcStopCardDropTimer"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcStopCardDropTimer");
        
        // TODO DEBUG - EREDETI! EZEKET AMÚGY SZERVERREL KÉNE HÍVNI!
        //UpdateWaitingForCardDrop(false);
        //UpdateCardDropTimer(30f);

        
        if (gameManager != null)
        {
            gameManager.GM_Client_StopCardDropTimerMultiplayer();
        }
    }

    [ClientRpc]
    public void RpcShowToast(string message, bool isLongMessage, float duration, GamePhase phase)
    {
        Debug.Log($"[ClientRpc]   RpcShowToast - {message}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcShowToast - {message}");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_ShowToastMultiplayer(message, isLongMessage, duration, phase);
        }
    }

    [ClientRpc]
    public void RpcSendLogToClients(string logMessage)
    {
        if (debugger != null && debugger.gameObject.activeInHierarchy)
        {
            debugger.AddTextToDebugFile(logMessage);
        }
    }

    [Server]
    public void SendLogToServer(string logMessage)
    {
        if (debugger != null && debugger.gameObject.activeInHierarchy)
        {
            debugger.AddTextToDebugFile(logMessage);
        }
    }

    /// Kliens küldi a szervernek: kártya lerakás piramison
    [Command(requiresAuthority = false)]
    public void CmdDropCardOnPiramis(int playerId, int cardSlotIndex)
    {
        Debug.Log($"[Command]     CmdDropCardOnPiramis - Player {playerId}, slot {cardSlotIndex}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Command]     CmdDropCardOnPiramis - Player {playerId}, slot {cardSlotIndex}");
        
        if (gameManager != null)
        {
            // Szerver lekéri a kártya adatokat és feldolgozza
            gameManager.GM_Server_ProcessCardDropOnPiramis(playerId, cardSlotIndex);
        }
    }

    /// Server broadcastolja: kártya visszaadása játékosnak (hibás lerakás)
    [ClientRpc]
    public void RpcReturnCardToPlayer(int playerId, int cardSlotIndex)
    {
        Debug.Log($"[ClientRpc]   RpcReturnCardToPlayer - Player {playerId}, slot {cardSlotIndex}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcReturnCardToPlayer - Player {playerId}, slot {cardSlotIndex}");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_ReturnCardToPlayer(playerId, cardSlotIndex);
        }
    }

    /// Server broadcastolja: Confirm Piramis gomb megjelenítése (kártya sikeresen lerakva)
    [ClientRpc]
    public void RpcShowConfirmPiramisButton()
    {
        Debug.Log($"[ClientRpc]   RpcShowConfirmPiramisButton"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcShowConfirmPiramisButton");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_ShowConfirmPiramisButton();
        }
    }

    [Server]
    public void WaitForAllClients()
    {
        pendingClients = NetworkServer.connections.Count - 1; // Exclude host
        Debug.Log($"[Server] Waiting for \"{pendingClients}\" clients to be ready..."); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] Waiting for \"{pendingClients}\" clients to be ready...");
    }

    [Command(requiresAuthority = false)]
    public void CmdClientReady()
    {
        Debug.Log($"[Command] CmdClientReady received from client."); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Command] CmdClientReady received from client.");
        pendingClients--;

        // Ellenőrzés minden kliens után
        CheckAllReady();
    }

    [Server]
    void CheckAllReady()
    {
        Debug.Log($"[Server] Checking ready clients. Pending: \"{pendingClients}\""); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] Checking ready clients. Pending: \"{pendingClients}\"");
        if (pendingClients <= 0)
        {
            ContinueGameFlow();
        }
    }

    [Server]
    void ContinueGameFlow()
    {
        Debug.Log($"[Server] All clients ready. Continuing game flow."); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] All clients ready. Continuing game flow.");
       gameManager.AllClientsReadyToNextRound();
    }

    #region Busz RPCs

    /// Server broadcastolja a Busz indítását
    [ClientRpc]
    public void RpcStartBusz()
    {
        Debug.Log("[ClientRpc]   RpcStartBusz"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[ClientRpc]   RpcStartBusz");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_StartBusz();
        }
    }

    /// Server húz 6 busz kártyát és broadcastolja minden kliensnek
    [Server]
    public void ServerBroadcastFillBuszCards()
    {
        if (gameManager == null)
        {
            Debug.LogError("[Server]\tGameManager not available for busz card drawing!");
            return;
        }

        var deckField = gameManager.GetType().GetField("deck", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (deckField != null)
        {
            Deck deck = deckField.GetValue(gameManager) as Deck;
            if (deck != null)
            {
                PyramidCardData[] buszCardsData = new PyramidCardData[6];
                
                for (int i = 0; i < 6; i++)
                {
                    Card card = deck.DrawCard();
                    buszCardsData[i] = new PyramidCardData(card);
                }

                Debug.Log($"[Server]\tDrew {buszCardsData.Length} busz cards"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tDrew {buszCardsData.Length} busz cards");

                // Broadcast minden kliensnek
                RpcFillBuszWithCards(buszCardsData);
            }
        }
    }

    /// Client-side: Busz kártyák feltöltése (RPC-ből hívva)
    [ClientRpc]
    public void RpcFillBuszWithCards(PyramidCardData[] cards)
    {
        Debug.Log($"[ClientRpc]   Received {cards.Length} busz cards"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   Received {cards.Length} busz cards");

        if (gameManager != null)
        {
            gameManager.GM_Client_FillBuszWithCards(cards);
        }
    }

    /// Server húz egy konkrét busz kártyát és broadcastolja minden kliensnek
    [Server]
    public void ServerDrawAndBroadcastBuszCard(int cardIndex)
    {
        if (gameManager == null)
        {
            Debug.LogError("[Server]\tGameManager not available for busz card drawing!");
            return;
        }

        // Ellenőrizzük, hogy van-e elég lap a pakliban
        var checkDeckMethod = gameManager.GetType().GetMethod("CheckDeckForBusz", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (checkDeckMethod != null)
        {
            checkDeckMethod.Invoke(gameManager, null);
        }

        var deckField = gameManager.GetType().GetField("deck", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (deckField != null)
        {
            Deck deck = deckField.GetValue(gameManager) as Deck;
            if (deck != null)
            {
                Card card = deck.DrawCard();
                PyramidCardData cardData = new PyramidCardData(card);

                Debug.Log($"[Server]\tDrew busz card for index {cardIndex}: {card.GetCardType()} {card.GetCardValue()}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tDrew busz card for index {cardIndex}: {card.GetCardType()} {card.GetCardValue()}");

                // Broadcast minden kliensnek
                RpcUpdateBuszCard(cardIndex, (int)card.GetCardType(), (int)card.GetCardValue(), (int)card.GetCardBackType());
            }
        }
    }

    /// Client-side: Egy konkrét busz kártya frissítése (RPC-ből hívva)
    [ClientRpc]
    public void RpcUpdateBuszCard(int cardIndex, int cardType, int cardValue, int cardBackType)
    {
        Debug.Log($"[ClientRpc]   RpcUpdateBuszCard - index: {cardIndex}, card: {(CardType)cardType} {(CardValue)cardValue}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcUpdateBuszCard - index: {cardIndex}, card: {(CardType)cardType} {(CardValue)cardValue}");

        if (gameManager != null)
        {
            gameManager.GM_Client_UpdateBuszCard(cardIndex, cardType, cardValue, cardBackType);
        }
    }

    /// Client küldi a szervernek: Busz tipp
    [Command(requiresAuthority = false)]
    public void CmdSubmitBuszTipp(int playerId, int tippValue, NetworkConnectionToClient sender = null)
    {
        Debug.Log($"[Command]     CmdSubmitBuszTipp - Player {playerId}, tipp: {tippValue}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Command]     CmdSubmitBuszTipp - Player {playerId}, tipp: {tippValue}");
        
        if (gameManager != null)
        {
            gameManager.GM_Server_ProcessBuszTipp(playerId, (TippValue)tippValue);
        }
    }

    /// Server broadcastolja a Busz tipp eredményét
    [ClientRpc]
    public void RpcShowBuszTippResult(int playerId, bool isCorrect, int currentCardType, int currentCardValue, int currentCardBackType)
    {
        Debug.Log($"[ClientRpc]   RpcShowBuszTippResult - Player {playerId}, correct: {isCorrect}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcShowBuszTippResult - Player {playerId}, correct: {isCorrect}");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_ShowBuszTippResult(playerId, isCorrect, currentCardType, currentCardValue, currentCardBackType);
        }
    }

    /// Server broadcastolja a következő busz játékost
    [ClientRpc]
    public void RpcNextBuszPlayer(bool shouldRotate)
    {
        Debug.Log($"[ClientRpc]   RpcNextBuszPlayer - shouldRotate: {shouldRotate}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcNextBuszPlayer - shouldRotate: {shouldRotate}");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_NextBuszPlayer(shouldRotate);
        }
    }

    /// Client küldi a szervernek: Give Up Busz
    [Command(requiresAuthority = false)]
    public void CmdGiveUpBusz(int playerId, NetworkConnectionToClient sender = null)
    {
        Debug.Log($"[Command]     CmdGiveUpBusz - Player {playerId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Command]     CmdGiveUpBusz - Player {playerId}");
        
        if (gameManager != null)
        {
            gameManager.GM_Server_ProcessGiveUpBusz(playerId);
        }
    }

    /// Server broadcastolja: játékos feladta a buszt
    [ClientRpc]
    public void RpcPlayerGaveUpBusz(int playerId, string playerName)
    {
        Debug.Log($"[ClientRpc]   RpcPlayerGaveUpBusz - Player {playerId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcPlayerGaveUpBusz - Player {playerId}");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_PlayerGaveUpBusz(playerId, playerName);
        }
    }

    /// Server broadcastolja: játékos sikeresen teljesítette a buszt
    [ClientRpc]
    public void RpcPlayerCompletedBusz(int playerId, string playerName)
    {
        Debug.Log($"[ClientRpc]   RpcPlayerCompletedBusz - Player {playerId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcPlayerCompletedBusz - Player {playerId}");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_PlayerCompletedBusz(playerId, playerName);
        }
    }

    /// Server broadcastolja: játékos kiesett (túl sok próbálkozás)
    [ClientRpc]
    public void RpcPlayerFailedBusz(int playerId, string playerName)
    {
        Debug.Log($"[ClientRpc]   RpcPlayerFailedBusz - Player {playerId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcPlayerFailedBusz - Player {playerId}");
        
        if (gameManager != null)
        {
            gameManager.GM_Client_PlayerFailedBusz(playerId, playerName);
        }
    }

    /// Server broadcastolja: állítsd le a Busz timert minden kliensen
    [ClientRpc]
    public void RpcStopBuszTimer()
    {
        Debug.Log($"[ClientRpc]   RpcStopBuszTimer"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[ClientRpc]   RpcStopBuszTimer");
        
        UpdateWaitingForBuszTipp(false);
        UpdateTimer(15f);
        
        if (gameManager != null)
        {
            gameManager.StopBuszTimerMultiplayer();
        }
    }

    #endregion

    #endregion
}