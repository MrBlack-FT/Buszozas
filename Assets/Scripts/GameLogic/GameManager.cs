using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Nobi.UiRoundedCorners;
using System.Threading;

public class GameManager : MonoBehaviour
{
    #region Változók

    [Header("Debugger")]
    [SerializeField] private Debugger debugger;

    [Header("Custom Scene Manager")]
    [SerializeField] private CustomSceneManager customSceneManager;

    [Header("Megjelenítés")]
    [SerializeField] private GameDisplay gameDisplay;

    [Header("Canvas és Panelek")]
    [SerializeField] private Canvas PauseMenuCanvas;
    [SerializeField] private GameObject PauseMenuPanel;
    [SerializeField] private GameObject PauseOptionsPanel;
    [SerializeField] private GameObject PauseExitPanel;

    [Header("Játékosok")]
    [SerializeField] private GameObject playersGroup;
    [SerializeField] private PlayerManager[] playerManagers = new PlayerManager[10];
    

    [Space(10)]
    [Header("Játék események")]
    [SerializeField] private GameEvents gameEventsGameObject;
    private GameEvents gameEvents;

    [Header("UI referenciák")]
    [SerializeField] private GameObject startButtonsGroup;
    [SerializeField] private GameObject timerGroup;
    [SerializeField] private GameObject toast_FeedbackMessage;
    [SerializeField] private GameObject endButtonsGroup;

    [Header("Tipp gombok csoportjai")]
    [SerializeField] private GameObject tippGroupsGroup;
    [SerializeField] private GameObject redOrBlackGroup;
    [SerializeField] private GameObject belowOrAboveGroup;
    [SerializeField] private GameObject betweenOrApartGroup;
    [SerializeField] private GameObject exactColorGroup;
    [SerializeField] private GameObject exactNumberGroup;

    [Header("Piramis")]
    [SerializeField] private GameObject piramisGroup;

    [Header("Busz")]
    [SerializeField] private GameObject buszGroup;
    [SerializeField] private CardManager[] buszCards = new CardManager[6];

    [Header("Jelenleg húzott kártya")]
    [SerializeField] private GameObject tippCardTitle;
    [SerializeField] private GameObject tippCardGroup;

    [Header("Pont osztás UI")]
    [SerializeField] private GameObject pointGiveGroup;
    [SerializeField] private Button confirmPointGiveButton;

    [Header("Piramis UI")]
    [SerializeField] private GameObject pyramidButtonsGroup;
    [SerializeField] private Button skipPyramidCardButton;
    [SerializeField] private Button confirmPyramidCardButton;

    [Header("Busz UI")]
    [SerializeField] private GameObject buszButtonsGroup;
    [SerializeField] private Button giveUpBuszButton;

    [Header("Játék állapot")]
    bool IsMPServer =>  Mirror.NetworkServer.active;
    bool IsHost =>      Mirror.NetworkClient.isConnected && Mirror.NetworkServer.active;
    bool IsMPClient =>  Mirror.NetworkClient.isConnected && !Mirror.NetworkServer.active;
    bool IsHostOrClients => Mirror.NetworkClient.isConnected;
    bool IsSingle =>    !Mirror.NetworkClient.isConnected;
    private bool isGamePaused = false;
    private List<Player> activePlayers = new List<Player>();
    private List<Player> allPlayers = new List<Player>();
    private Deck deck;
    private float _currentTimer;
    private TextMeshProUGUI timerText;
    private GamePhase _currentPhase;
    private TippType currentTippType;
    private CardManager currentCard;
    private bool _waitingForTipp = false;
    private int _currentPlayerIndex = 0;
    private int _currentRound = 0;

    private GamePhase currentPhase
    {
        get 
        { 
            if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
                return NetworkGameManager.Instance.GetCurrentPhase();
            return _currentPhase;
        }
        set 
        { 
            if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.UpdateCurrentPhase(value);
            _currentPhase = value;
        }
    }

    private float currentTimer
    {
        get 
        {
            if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
                return NetworkGameManager.Instance.GetCurrentTimer();
            return _currentTimer;
        }
        set 
        { 
            if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.UpdateTimer(value);
            _currentTimer = value;
        }
    }

    private bool waitingForTipp
    {
        get 
        { 
            if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
                return NetworkGameManager.Instance.GetWaitingForTipp();
            return _waitingForTipp;
        }
        set 
        { 
            if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.UpdateWaitingForTipp(value);
            _waitingForTipp = value;
        }
    }
    private int currentPlayerIndex
    {
        get 
        { 
            if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
                return NetworkGameManager.Instance.GetCurrentPlayerIndex();
            return _currentPlayerIndex;
        }
        set 
        { 
            if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.UpdateCurrentPlayerIndex(value);
            _currentPlayerIndex = value;
        }
    }
    private int currentRound
    {
        get 
        { 
            if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
                return NetworkGameManager.Instance.GetCurrentRound();
            return _currentRound;
        }
        set 
        { 
            if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.UpdateCurrentRound(value);
            _currentRound = value;
        }
    }

    [Header("Pont osztás állapot")]
    private int[] pointsToGive = new int[10];
    // totalPointsToGive most már a Piramis szekcióban property-ként van definiálva (SyncVar)
    private bool _isGivingPoints = false;

    public bool isGivingPoints
    {
        get 
        { 
            if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
                return NetworkGameManager.Instance.GetIsGivingPoints();
            return _isGivingPoints;
        }
        set 
        { 
            if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.UpdateIsGivingPoints(value);
            _isGivingPoints = value;
        }
    }

    private float _pointGiveTimer = 15f;

    private float pointGiveTimer
    {
        get 
        { 
            if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
                return NetworkGameManager.Instance.GetPointGiveTimer();
            return _pointGiveTimer;
        }
        set 
        { 
            if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.UpdatePointGiveTimer(value);
            _pointGiveTimer = value;
        }
    }

    [Header("Piramis állapot")]
    // MULTIPLAYER: Ezek property-k, amelyek automatikusan szinkronizálódnak NetworkGameManager-en keresztül
    private int         _currentPiramisRow = 1;
    private int         _currentPiramisCardIndex = 0;
    private CardManager currentPiramisCard;     // Aktuálisan felfordított piramis kártya
    private int         _placedCardsNumber = 0;
    private int         _totalPointsToGive = 0;
    private float       _cardDropTimer = 30f;
    private bool        _waitingForCardDrop = false;
    private int currentPiramisRow          // 1-5
    {
        get 
        { 
            if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
                return NetworkGameManager.Instance.GetPiramisRow();
            return _currentPiramisRow;
        }
        set 
        { 
            if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.UpdatePiramisRow(value);
            _currentPiramisRow = value;
        }
    }
    private int currentPiramisCardIndex    // Hányadik kártya a sorban
    {
        get 
        { 
            if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
                return NetworkGameManager.Instance.GetPiramisCardIndex();
            return _currentPiramisCardIndex;
        }
        set 
        { 
            if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.UpdatePiramisCardIndex(value);
            _currentPiramisCardIndex = value;
        }
    }

    private int placedCardsNumber          // Játékos hány kártyát tett le
    {
        get 
        { 
            if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
                return NetworkGameManager.Instance.GetPlacedCardsNumber();
            return _placedCardsNumber;
        }
        set 
        { 
            if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.UpdatePlacedCardsNumber(value);
            _placedCardsNumber = value;
        }
    }

    private int totalPointsToGive
    {
        get 
        { 
            if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
                return NetworkGameManager.Instance.GetTotalPointsToGive();
            return _totalPointsToGive;
        }
        set 
        { 
            if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.UpdateTotalPointsToGive(value);
            _totalPointsToGive = value;
        }
    }
    private float cardDropTimer
    {
        get 
        { 
            if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
                return NetworkGameManager.Instance.GetCardDropTimer();
            return _cardDropTimer;
        }
        set 
        { 
            if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.UpdateCardDropTimer(value);
            _cardDropTimer = value;
        }
    }

    private bool waitingForCardDrop
    {
        get 
        { 
            if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
                return NetworkGameManager.Instance.GetWaitingForCardDrop();
            return _waitingForCardDrop;
        }
        set 
        { 
            if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.UpdateWaitingForCardDrop(value);
            _waitingForCardDrop = value;
        }
    }

    [Header("Busz állapot")]
    private Dictionary<int, int> _playerBuszAttempts = new Dictionary<int, int>();   // playerId -> próbálkozások száma (local)
    
    // Helper metódusok a playerBuszAttempts kezeléséhez
    private void SetPlayerBuszAttempts(int playerId, int attempts)
    {
        if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.UpdatePlayerBuszAttempts(playerId, attempts);
        }
        _playerBuszAttempts[playerId] = attempts;
    }
    
    private int GetPlayerBuszAttempts(int playerId)
    {
        if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
        {
            return NetworkGameManager.Instance.GetPlayerBuszAttempts(playerId);
        }
        return _playerBuszAttempts.ContainsKey(playerId) ? _playerBuszAttempts[playerId] : 0;
    }
    
    private bool ContainsPlayerBuszAttempts(int playerId)
    {
        if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
        {
            return NetworkGameManager.Instance.ContainsPlayerBuszAttempts(playerId);
        }
        return _playerBuszAttempts.ContainsKey(playerId);
    }
    
    private List<int> playersOnBusz = new List<int>();                              // Még buszon lévő játékosok ID-i
    private int _currentBuszCardIndex = 0;                                          // Jelenleg melyik busz kártyánál tartunk (0-5)
    private int maxBuszAttempts = 10;
    private bool _waitingForBuszTipp = false;

    private int currentBuszCardIndex
    {
        get 
        { 
            if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
                return NetworkGameManager.Instance.GetBuszCardIndex();
            return _currentBuszCardIndex;
        }
        set 
        { 
            if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.UpdateBuszCardIndex(value);
            _currentBuszCardIndex = value;
        }
    }

    private bool waitingForBuszTipp
    {
        get 
        { 
            if (Mirror.NetworkClient.isConnected && NetworkGameManager.Instance != null)
                return NetworkGameManager.Instance.GetWaitingForBuszTipp();
            return _waitingForBuszTipp;
        }
        set 
        { 
            if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
                NetworkGameManager.Instance.UpdateWaitingForBuszTipp(value);
            _waitingForBuszTipp = value;
        }
    }

    #endregion


    #region Unity metódusok

    void OnApplicationQuit()
    {


        DOTween.KillAll();

        if (Mirror.NetworkManager.singleton != null)
        {
            Mirror.NetworkManager.singleton.StopAllCoroutines();
            Mirror.NetworkManager.singleton.StopHost();
            Mirror.NetworkManager.singleton.StopClient();
        }
    }
    void Awake()
    {
        // EZ CSAK TESZT CÉLJÁBÓL VAN! EZT KÉSŐBB KOMMENTELNI KELL!
        
        /*
        // Létezik-e a GameVars instance és ha nem, akkor töltsük be a prefab-jét.
        if (GameVars.Instance == null)
        {
            //Debug.Log("GameManager: GameVars instance not found, loading prefab...");
            GameObject gameVarsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameVars.prefab");
            if (gameVarsPrefab != null)
            {
                Instantiate(gameVarsPrefab);
            }
            else
            {
                Debug.LogError("GameManager: GameVars prefab not found in Resources/Prefabs/");
            }
        }
        // DEMO ADATOK
        if (GameVars.Instance != null)
        {
            GameVars.Instance.BusName = "TEST BUS";
            GameVars.Instance.NumberOfPlayersInGame = 4;
            for (int I = 0; I < GameVars.Instance.NumberOfPlayersInGame; I++)
            {
                GameVars.Instance.SetPlayerName(I, $"Player {I + 1}");
            }
            GameVars.Instance.ReversedPyramidMode = true;
        }
        */

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        // GameEvents lekérése a GameObject-ről
        if (gameEventsGameObject != null)
        {
            gameEvents = gameEventsGameObject.GetComponent<GameEvents>();
            if (gameEvents == null)
            {
                Debug.LogError("GameManager: GameEvents component not found on assigned GameObject!");
            }
        }
        else
        {
            Debug.LogError("GameManager: GameEvents GameObject not assigned in Inspector!");
        }
    }

    void OnEnable()
    {
        // Event feliratkozás
        if (gameEvents != null)
        {
            gameEvents.OnCardDroppedToPiramis += HandleCardDroppedToPiramis;
        }
    }

    void OnDisable()
    {
        // Event leiratkozás
        if (gameEvents != null)
        {
            gameEvents.OnCardDroppedToPiramis -= HandleCardDroppedToPiramis;
        }
    }

    void Start()
    {
        // Ha multiplayer játék, akkor NE inicializáljunk még semmit
        // A NetworkGameManager fogja irányítani a játékot
        if (IsHostOrClients)
        {
            Debug.Log("[GameManager] Multiplayer mode - waiting for NetworkGameManager"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] Multiplayer mode - waiting for NetworkGameManager");

            return;
        }

        InitializeSingleplayer();
    }

    void Update()
    {
        // Szünet menü - MULTIPLAYER: csak singleplayer-ben álljon meg a játék
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsSingle)
            {
                ChangeIsGamePaused();
            }
        }

        if (isGamePaused && IsSingle) return;


        // MULTIPLAYER: Csak a server számolja a timert, kliensek a SyncVar-ból olvassák

        // Tipp timer kezelés
        if (waitingForTipp && currentTimer > 0)
        {
            // Server számítja a timert
            if (IsSingle || IsMPServer)
            {
                currentTimer -= Time.deltaTime;
                
                NetworkGameManager.Instance?.UpdateTimer(currentTimer);
            }
            // Kliens a NetworkGameManager-ből olvassa ki a timer értékét
            else if (IsMPClient)
            {
                //debugger.CustomDebugLog("[LOCAL CLIENT CALL - GETTING CURRENT TIMER!]");
                currentTimer = NetworkGameManager.Instance.GetCurrentTimer();
            }

            // UI frissítés minden kliensnél
            timerText.text = "Választási idő:\n" + currentTimer.ToString("F1");

            if (currentTimer <= 5f)
            {
                timerGroup.GetComponent<Image>().color = Color.red;
            }

            if (currentTimer <= 0 && !IsMPClient)
            {
                OnTimerExpired();
            }
        }
        
        // Pont osztás timer kezelés
        if (isGivingPoints && pointGiveTimer > 0)
        {
            // MULTIPLAYER: Server számítja, kliensek is látják
            if (IsSingle || IsMPServer)
            {
                pointGiveTimer -= Time.deltaTime;
                
                NetworkGameManager.Instance?.UpdatePointGiveTimer(pointGiveTimer);
            }
            else if (IsMPClient)
            {
                //debugger.CustomDebugLog("[LOCAL CLIENT CALL - GETTING POINT GIVE TIMER!]");
                // Kliens olvassa a SyncVar értékét
                pointGiveTimer = NetworkGameManager.Instance.GetPointGiveTimer();

            }
            
            timerText.text = "Pont kiosztása:\n" + pointGiveTimer.ToString("F1");

            if (pointGiveTimer <= 5f)
            {
                timerGroup.GetComponent<Image>().color = Color.red;
            }

            if (pointGiveTimer <= 0 && !IsMPClient)
            {
                OnPointGiveTimeout();
            }
        }

        // Kártya letevés timer kezelés (Piramis)
        if (waitingForCardDrop && cardDropTimer > 0)
        {
            // Server számítja a timert
            if (IsSingle || IsMPServer)
            {
                cardDropTimer -= Time.deltaTime;
                
                NetworkGameManager.Instance?.UpdateCardDropTimer(cardDropTimer);
            }
            // Kliens a NetworkGameManager-ből olvassa ki a timer értékét
            else if (IsMPClient)
            {
                //debugger.CustomDebugLog("[LOCAL CLIENT CALL - GETTING CARD DROP TIMER!]");
                cardDropTimer = NetworkGameManager.Instance.GetCardDropTimer();
            }
            
            // UI frissítés minden kliensnél
            timerText.text = "Kártya lerakása:\n" + cardDropTimer.ToString("F1");

            if (cardDropTimer <= 5f)
            {
                timerGroup.GetComponent<Image>().color = Color.red;
            }

            if (cardDropTimer <= 0 && !IsMPClient)
            {
                OnCardDropTimeout();
            }
        }

        // Busz tipp timer kezelés
        if (waitingForBuszTipp && currentTimer > 0)
        {
            currentTimer -= Time.deltaTime;
            timerText.text = "Választási idő:\n" + currentTimer.ToString("F1");

            if (currentTimer <= 5f)
            {
                timerGroup.GetComponent<Image>().color = Color.red;
            }

            if (currentTimer <= 0)
            {
                OnBuszTimerExpired();
            }
        }

        
        // DEBUG BEMENET
        /*
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // Véletlenszerű játékos "Kapcsolat megszakítás" szimulációja
            int randomIndex = Random.Range(1, activePlayers.Count);
            RemovePlayer(randomIndex);

            RefreshPlayerUI();
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            // Véletlenszerű kártya adása a jelenlegi játékosnak
            activePlayers[0].AddCardToPlayer(deck.DrawCard());

            //ITT VALAMI NEM JÓ A KÁRTYA ADÁSNÁL!

            RefreshPlayerUI();
        }
        if (Input.GetKeyDown(KeyCode.I))
        {
            activePlayers[0].IncreasePlayerScore(10);

            RefreshPlayerUI();
        }
        */
        /*
        if (Input.GetKeyDown(KeyCode.L))
        {
            string status = "";

            foreach (var player in activePlayers)
            {
                status += player.GetPlayerStatus() + "\n";
            }
            status += "--------------------\n";
            foreach (var player in activePlayers)
            {
                int attempts = GetPlayerBuszAttempts(player.GetPlayerID());
                status += $"Player {player.GetPlayerID()} Attempts: {attempts}\n";
            }
            Debug.Log(status);
            if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile(status);
        }*/
        /*

        if (Input.GetKeyDown(KeyCode.K))
        {
            foreach (var player in activePlayers)
            {
                playerBuszAttempts[player.GetPlayerID()] = 1;
            }
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            foreach (var player in activePlayers)
            {
                playerBuszAttempts[player.GetPlayerID()] = Random.Range(7, maxBuszAttempts);
            }
        }*/

        //timerDO += Time.deltaTime;
        //timerDOText.text = "Timer: " + timerDO.ToString("F5");
        /*
        debugger.UpdatePersistentLog("===================================", " ");
        debugger.UpdatePersistentLog("currentPhase", currentPhase.ToString());
        switch (currentPhase)
        {
            case GamePhase.Tipp:
                debugger.UpdatePersistentLog("currentPhase", "Tipp");
                break;
            case GamePhase.Piramis:
                debugger.UpdatePersistentLog("currentPhase", "Piramis");
                break;
            case GamePhase.Busz:
                debugger.UpdatePersistentLog("currentPhase", "Busz");
                break;
            case GamePhase.JatekVege:
                debugger.UpdatePersistentLog("currentPhase", "Játék vége");
                break;
        }
        debugger.UpdatePersistentLog("currentRound", currentRound.ToString());
        debugger.UpdatePersistentLog("==================================", " ");
        debugger.UpdatePersistentLog("currentPlayer", activePlayers.Count > 0 ? activePlayers[0].GetPlayerName() : "N/A");
        debugger.UpdatePersistentLog("currentPlayerIndex", currentPlayerIndex.ToString());
        debugger.UpdatePersistentLog("=================================", " ");
        debugger.UpdatePersistentLog("currentPhase", currentPhase.ToString());
        debugger.UpdatePersistentLog("currentRound", currentRound.ToString());
        debugger.UpdatePersistentLog("LOCAL  currentPiramisRow", _currentPiramisRow.ToString());
        debugger.UpdatePersistentLog("MIRROR currentPiramisRow", currentPiramisRow.ToString());
        debugger.UpdatePersistentLog("LOCAL  currentPiramisCardIndex", _currentPiramisCardIndex.ToString());
        debugger.UpdatePersistentLog("MIRROR currentPiramisCardIndex", currentPiramisCardIndex.ToString());
        debugger.UpdatePersistentLog("currentPiramisCard", currentPiramisCard != null && currentPiramisCard.GetCardData() != null ? "[" + currentPiramisCard.GetCardData().GetCardType().ToString() + " " + currentPiramisCard.GetCardData().GetCardValue().ToString() + " " + currentPiramisCard.GetCardData().GetCardBackType().ToString() + "]" : "N/A");
        debugger.UpdatePersistentLog("LOCAL placedCardsNumber", _placedCardsNumber.ToString());
        debugger.UpdatePersistentLog("MIRROR placedCardsNumber", placedCardsNumber.ToString());
        debugger.UpdatePersistentLog("LOCAL totalPointsToGive", _totalPointsToGive.ToString());
        debugger.UpdatePersistentLog("MIRROR totalPointsToGive", totalPointsToGive.ToString());
        */
        if (currentCard != null)
        {
            if (currentCard.GetCardData() != null)
            {
                debugger.UpdatePersistentLog("currentCard", "[" + currentCard.GetCardData().GetCardType().ToString() + " " + currentCard.GetCardData().GetCardValue().ToString() + " " + currentCard.GetCardData().GetCardBackType().ToString() + "]");
            }
        }
        debugger.UpdatePersistentLog("5. buszCard", buszCards[4] != null && buszCards[4].GetCardData() != null ? "[" + buszCards[4].GetCardData().GetCardType().ToString() + " " + buszCards[4].GetCardData().GetCardValue().ToString() + " " + buszCards[4].GetCardData().GetCardBackType().ToString() + "]" : "N/A");
        debugger.UpdatePersistentLog("LOCAL currentBuszCardIndex", _currentBuszCardIndex.ToString());
        debugger.UpdatePersistentLog("MIRROR currentBuszCardIndex", currentBuszCardIndex.ToString());
        /*

        debugger.UpdatePersistentLog("LOCAL waitingForBuszTipp", _waitingForBuszTipp ? debugger.ColoredString("TRUE", Color.green) : debugger.ColoredString("FALSE", Color.red));
        debugger.UpdatePersistentLog("MIRROR waitingForBuszTipp", waitingForBuszTipp ? debugger.ColoredString("TRUE", Color.green) : debugger.ColoredString("FALSE", Color.red));

        
        debugger.UpdatePersistentLog("================================", " ");
        debugger.UpdatePersistentLog("LOCAL waitingForTipp", _waitingForTipp ? debugger.ColoredString("TRUE", Color.green) : debugger.ColoredString("FALSE", Color.red));
        debugger.UpdatePersistentLog("MIRROR waitingForTipp", waitingForTipp ? debugger.ColoredString("TRUE", Color.green) : debugger.ColoredString("FALSE", Color.red));
        debugger.UpdatePersistentLog("LOCAL isGivingPoints", _isGivingPoints ? debugger.ColoredString("TRUE", Color.green) : debugger.ColoredString("FALSE", Color.red));
        debugger.UpdatePersistentLog("MIRROR isGivingPoints", isGivingPoints ? debugger.ColoredString("TRUE", Color.green) : debugger.ColoredString("FALSE", Color.red));
        debugger.UpdatePersistentLog("LOCAL waitingForCardDrop", _waitingForCardDrop ? debugger.ColoredString("TRUE", Color.green) : debugger.ColoredString("FALSE", Color.red));
        debugger.UpdatePersistentLog("MIRROR waitingForCardDrop", waitingForCardDrop ? debugger.ColoredString("TRUE", Color.green) : debugger.ColoredString("FALSE", Color.red));
        

        debugger.UpdatePersistentLog("====================================", " ");
        debugger.UpdatePersistentLog("LOCAL _timer", _currentTimer.ToString());
        debugger.UpdatePersistentLog("MIRROR timer", currentTimer.ToString());
        if (timerText != null)
            debugger.UpdatePersistentLog("timerText", timerText.text.ToString());
        debugger.UpdatePersistentLog("=====================================", " ");
        */
    }

    #endregion

    #region Game State Machine

    public void InitializeGame()
    {
        tippCardGroup.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 100);

        
        currentPhase = GamePhase.Tipp;
        currentRound = 1;
        currentPlayerIndex = 0;

        gameDisplay.ShowToast(toast_FeedbackMessage, "Kezdődjön a tipp kör!", false, 2f, GamePhase.Tipp);

        DOVirtual.DelayedCall(3f, () =>
        {
            StartTippKor();
        });
        
        
        
        // DEBUG!
        /*
        currentPhase = GamePhase.Tipp;
        currentRound = 6;
        currentPlayerIndex = 0;
        FillPlayersWithCards();
        RefreshPlayerUI();
        NextRound();
        */
        
        /*
        currentPhase = GamePhase.Piramis;
        currentPiramisRow = 6;
        currentPlayerIndex = 0;
        RefreshPlayerUI();
        NextRound();
        */
    }

    // StartTippKor gyakorlatilag csak megjelenítés és timer indítás.
    public void StartTippKor()
    {
        //DebugDepth("StartTippKor");
        currentTippType = GetTippTypeForRound(currentRound);

        // MULTIPLAYER: Szerver húzza a kártyát és broadcastolja - [Szerver + Kliens]
        if (IsMPServer && NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.ServerDrawAndBroadcastCurrentCard();       // ShowDrawnCard() RPC-ből lesz meghívva minden kliensnél
        }
        else if (IsSingle)
        {
            // SINGLEPLAYER: Direkt kártyahúzás
            currentCard.SetCard(deck.DrawCard());
            currentCard.ShowCardBack();
        }

        // [Singleplayer + Szerver + Kliens => mindenki látja]
        gameDisplay.ShowCurrentCard(tippCardGroup, 1f, () =>
        {
            // MULTIPLAYER: Speciális megjelenítés
            // MULTIPLAYER: Csak a soron lévő játékosnak jelenjenek meg a gombok
            if (IsHostOrClients)
            {
                var localPlayer = Mirror.NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();

                if (localPlayer != null && localPlayer.playerId == activePlayers[0].GetPlayerID())
                {
                    DisplayTippGroup(currentTippType);          // Ez a local player van soron
                }
                else
                {
                    DisplayTippGroup(TippType.NONE);            // Más játékos van soron - ne jelenjenek meg gombok
                    
                    string currentPlayerName = activePlayers[0].GetPlayerName();
                    gameDisplay.ShowToast(toast_FeedbackMessage, $"{currentPlayerName} tippel...", false, 2f, GamePhase.Tipp);
                    
                    //Debug.Log($"[GameManager] Waiting for player {activePlayers[0].GetPlayerName()} to make a tipp"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Waiting for player {activePlayers[0].GetPlayerName()} to make a tipp");
                }
            }
            // SINGLEPLAYER
            else
            {
                DisplayTippGroup(currentTippType);
            }

            // Timer indítása [Singleplayer + Szerver + Kliens => mindenki látja]
            StartTimer();
            //if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] Timer started for tipp phase");
        });
    }

    private void StartPiramis()
    {
        DebugDepth("StartPiramis()");
        
        //Debug.Log("[GameManager] StartPiramis() called"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] StartPiramis() called");
        // MULTIPLAYER: RPC broadcast flip, SINGLEPLAYER: direkt flip
        if (IsMPServer && NetworkGameManager.Instance != null)
        {
            Debug.Log($"[FromRpc][GameManager] StartPiramis() - calling RpcFlipPyramidCard() while currentPiramisCardIndex: \"{currentPiramisCardIndex}\""); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc][GameManager] StartPiramis() - calling RpcFlipPyramidCard() while currentPiramisCardIndex: \"{currentPiramisCardIndex}\"");
            NetworkGameManager.Instance.RpcFlipPyramidCard();
        }
        else if (IsSingle)
        {
            FlipPyramidCard();
        }

        if (IsMPServer || IsSingle)
        {
            DOVirtual.DelayedCall(1f, () =>
            {
                if (IsMPServer && NetworkGameManager.Instance != null)
                {
                    bool hasMatchingCard = CheckIfMultiPlayerCanDropCard();
                    
                    // Ha NINCS kártya, akkor 1 másodperc után lépünk a következő játékosra
                    if (!hasMatchingCard)
                    {
                        DOVirtual.DelayedCall(1f, () =>
                        {
                            NetworkGameManager.Instance.RpcNextPiramisPlayer(2f);
                        });
                    }
                }
                else if (IsSingle)
                {
                    CheckIfPlayerCanDropCard(); // void - már belsőleg kezeli a folytatást
                }
            });
        }
    }

    // MULTIPLAYER: Client-side StartPiramis RPC handler
    public void GM_Client_StartPiramis()
    {
        DebugDepth("GM_Client_StartPiramis()");
        Debug.Log($"[FromRpc] [GameManager] GM_Client_StartPiramis()"); if(debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc] [GameManager] GM_Client_StartPiramis()");
        StartPiramis();
    }

    private void StartBusz()
    {
        // Multiplayer: Server húzza a kártyákat és broadcastolja
        if (IsMPServer && NetworkGameManager.Instance != null)
        {
            Debug.Log("[Server]\tStartBusz - Broadcasting busz cards"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Server]\tStartBusz - Broadcasting busz cards");
            
            NetworkGameManager.Instance.ServerBroadcastFillBuszCards();
            NetworkGameManager.Instance.ServerDrawAndBroadcastCurrentCard();
            NetworkGameManager.Instance.RpcStartBusz();
        }
        else if (IsSingle)
        {
            // Singleplayer: Lokálisan húzzuk a kártyákat
            FillBuszWithCards();
            StartBuszUI();
        }
    }

    // MULTIPLAYER: Client-side StartBusz RPC handler
    public void GM_Client_StartBusz()
    {
        Debug.Log($"[FromRpc] [GameManager] GM_Client_StartBusz()"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc] [GameManager] GM_Client_StartBusz()");
        
        // Játékos tracking inicializálás
        if (!ContainsPlayerBuszAttempts(activePlayers[0].GetPlayerID()))
        {
            SetPlayerBuszAttempts(activePlayers[0].GetPlayerID(), 0);
        }
        
        StartBuszUI();
    }

    // MULTIPLAYER: Client-side busz kártyák feltöltése (RPC-ből hívva)
    public void GM_Client_FillBuszWithCards(PyramidCardData[] cards)
    {
        Debug.Log($"[FromRpc] [GameManager] GM_Client_FillBuszWithCards - {cards.Length} cards"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc] [GameManager] GM_Client_FillBuszWithCards - {cards.Length} cards");
        
        for (int i = 0; i < cards.Length && i < buszCards.Length; i++)
        {
            Card card = new Card((CardType)cards[i].cardType, (CardBackType)cards[i].cardBackType, (CardValue)cards[i].cardValue);
            buszCards[i].SetCard(card);
        }
    }

    // MULTIPLAYER: Client-side egy konkrét busz kártya frissítése (RPC-ből hívva)
    public void GM_Client_UpdateBuszCard(int cardIndex, int cardType, int cardValue, int cardBackType)
    {
        Debug.Log($"[FromRpc] [GameManager] GM_Client_UpdateBuszCard - index: {cardIndex}, card: {(CardType)cardType} {(CardValue)cardValue}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc] [GameManager] GM_Client_UpdateBuszCard - index: {cardIndex}, card: {(CardType)cardType} {(CardValue)cardValue}");
        
        if (cardIndex >= 0 && cardIndex < buszCards.Length)
        {
            Card card = new Card((CardType)cardType, (CardBackType)cardBackType, (CardValue)cardValue);
            buszCards[cardIndex].SetCard(card);
        }
        else
        {
            Debug.LogError($"[GameManager] GM_Client_UpdateBuszCard - Invalid card index: {cardIndex}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"<ERROR!> [GameManager] GM_Client_UpdateBuszCard - Invalid card index: {cardIndex}");
        }
    }

    private void StartBuszUI()
    {
        Debug.Log("[GameManager] StartBuszUI() called"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] StartBuszUI() called");
        // Játékos tracking inicializálás (ha még nincs benne a dictionary-ben)
        if (!ContainsPlayerBuszAttempts(activePlayers[0].GetPlayerID()))
        {
            SetPlayerBuszAttempts(activePlayers[0].GetPlayerID(), 0);
        }

        // SINGLEPLAYER: Lokálisan húzzuk a currentCard-ot
        // MULTIPLAYER: Már a StartBusz()-ban megtörtént a ServerDrawAndBroadcastCurrentCard()
        if (IsSingle)
        {
            CheckDeckForBusz();
            currentCard.SetCard(deck.DrawCard());
        }

        gameDisplay.ShowBusz(buszGroup, buszCards, 2f, () =>
        {
            gameDisplay.HighlightBuszCard(buszCards, currentBuszCardIndex, true);
            currentCard.ShowCardBack();
            gameDisplay.ShowCurrentCard(tippCardGroup, 1f, () =>
            {
                // MULTIPLAYER: Csak a soron lévő játékosnak jelenjenek meg a gombok
                if (Mirror.NetworkClient.isConnected)
                {
                    var localPlayer = Mirror.NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();

                    if (localPlayer != null && localPlayer.playerId == activePlayers[0].GetPlayerID())
                    {
                        // Ez a local player van soron
                        DisplayTippGroup(TippType.AlattaVagyFelette);

                        // Feladás gomb megjelenítése (ha van legalább 1 próbálkozás)
                        int attempts = GetPlayerBuszAttempts(activePlayers[0].GetPlayerID());

                        if (attempts >= 1)
                        {
                            Debug.Log($"!!!!! [GameManager] Enabling give up busz button for \"{activePlayers[0].GetPlayerName()}\" [localPlayer] \"{localPlayer.playerName}\""); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Enabling give up busz button for \"{activePlayers[0].GetPlayerName()}\" [localPlayer] \"{localPlayer.playerName}\"");
                            giveUpBuszButton.GetComponent<CustomButtonForeground>().SetInteractiveState(true);
                            gameDisplay.ShowBuszButtons(buszButtonsGroup, 0.5f);

                            TextMeshProUGUI buttonText = giveUpBuszButton.GetComponentInChildren<TextMeshProUGUI>();
                            if (buttonText != null)
                            {
                                buttonText.text = $"Feladás\n({attempts}/10 próba)";
                            }
                        }
                    }
                    else
                    {
                        // Más játékos van soron - ne jelenjenek meg gombok
                        DisplayTippGroup(TippType.NONE);
                        
                        string currentPlayerName = activePlayers[0].GetPlayerName();
                        gameDisplay.ShowToast(toast_FeedbackMessage, $"{currentPlayerName} tippel...", false, 2f, GamePhase.Busz);
                        
                        Debug.Log($"[GameManager] Waiting for player {activePlayers[0].GetPlayerName()} to make a busz tipp"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Waiting for player {activePlayers[0].GetPlayerName()} to make a busz tipp");
                    }
                }
                // SINGLEPLAYER
                else
                {
                    DisplayTippGroup(TippType.AlattaVagyFelette);
                    
                    int attempts = GetPlayerBuszAttempts(activePlayers[0].GetPlayerID());

                    if (attempts >= 1)
                    {
                        giveUpBuszButton.GetComponent<CustomButtonForeground>().SetInteractiveState(true);
                        gameDisplay.ShowBuszButtons(buszButtonsGroup, 0.5f);

                        TextMeshProUGUI buttonText = giveUpBuszButton.GetComponentInChildren<TextMeshProUGUI>();
                        if (buttonText != null)
                        {
                            buttonText.text = $"Feladás\n({attempts}/10 próba)";
                        }
                    }
                }
                
                // Timer indítása [Singleplayer + Szerver + Kliens => mindenki látja]
                StartTimer();
                gameDisplay.ShowTimer(timerGroup, 0.5f);
            });
        });
    }

    public void NextRound()
    {
        //DebugDepth("NextRound");
        switch (currentPhase)
        {
            case GamePhase.Tipp:
                if (currentRound < 5)
                {
                    currentRound++;
                    if (IsSingle)
                    {
                        StartTippKor();
                    }
                    else if (IsMPServer && NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.RpcStartTippKor();// Multiplayer: Csak a szerver broadcastol, kliensek várnak RPC-re
                    }
                }
                else
                {
                    gameDisplay.HideCurrentCard(tippCardGroup, 1f);
                    gameDisplay.HidePlayers(activePlayers.Count, playersGroup, 1f);

                    currentRound = 0;
                    currentPiramisRow = 1;
                    currentPiramisCardIndex = 0;
                    currentPlayerIndex = 0;

                    currentCard.SetEmptyCard();
                    currentPhase = GamePhase.Piramis;
                    if (IsSingle)
                    {
                        FillPiramisWithCards();
                    }
                    else if (IsMPServer && NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.ServerBroadcastFillPyramidCards();// MULTIPLAYER: Server húzza és broadcastolja a piramis kártyákat
                    }

                    gameDisplay.ShowToast(toast_FeedbackMessage, "Tipp kör vége!", false, 2f);
                    DOVirtual.DelayedCall(3f, () =>
                    {
                        gameDisplay.ShowToast(toast_FeedbackMessage, "Kezdődjön a piramis!", false, 2f);
                        DOVirtual.DelayedCall(3f, () =>
                        {
                            gameDisplay.ShowPlayers(activePlayers.Count, playersGroup, 1f);
                            gameDisplay.ShowPiramis(piramisGroup, 2f, () =>
                            {
                                if (IsSingle)
                                {
                                    // Singleplayer
                                    StartPiramis();
                                }
                                else if (IsMPServer && NetworkGameManager.Instance != null)
                                {
                                    NetworkGameManager.Instance.RpcStartPiramis(); // MULTIPLAYER: Csak a szerver broadcastolja a StartPiramis-t
                                }
                            });
                        });
                    });
                }
                break;

            case GamePhase.Piramis:
                if (currentPiramisRow < 5)
                {
                    currentPiramisRow++;
                    currentPiramisCardIndex = 0;

                    if (IsMPServer && NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.UpdatePiramisRow(currentPiramisRow);
                        NetworkGameManager.Instance.UpdatePiramisCardIndex(0);

                        DOVirtual.DelayedCall(0.25f, () =>
                        {
                            NetworkGameManager.Instance.RpcStartPiramis();
                        });
                        Debug.Log($"[GameManager] NextRound() - End of Piramis case's if block - Exiting NextRound()"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] NextRound() - End of Piramis case's if block - Exiting NextRound()");
                        DebugDepth("NextRound End of Piramis if");
                    }
                    else if (IsSingle)
                    {
                        StartPiramis();
                    }
                }
                else
                {
                    GivePlayersPointsAfterPyramid();
                    if (IsMPServer && NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.RpcShowToast("Büntetés pontok kiosztva a piramis után!", true, 2f, GamePhase.Piramis);
                    }
                    else if (IsSingle)
                    {
                        gameDisplay.ShowToast(toast_FeedbackMessage, "Büntetés pontok kiosztva a piramis után!", true, 2f, GamePhase.Piramis);
                    }
                    DOVirtual.DelayedCall(2f, () =>
                    {
                        currentRound = 0;
                        currentBuszCardIndex = 0;

                        gameDisplay.HidePiramis(piramisGroup, 1f);
                        gameDisplay.HidePlayers(activePlayers.Count, playersGroup, 1f);

                        FillPiramisWithEmptyCards();
                        FillPlayersWithEmptyCards();

                        foreach (var player in activePlayers)
                        {
                            player.SetTipp(TippValue.NONE);
                        }

                        currentPhase = GamePhase.Busz;

                        if(IsMPServer)
                        {
                            NetworkGameManager.Instance.UpdateCurrentRound(0);
                            NetworkGameManager.Instance.UpdateBuszCardIndex(0);
                            NetworkGameManager.Instance.UpdateCurrentPhase(GamePhase.Busz);
                        }

                        tippCardGroup.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 170);

                        playersOnBusz.Clear();
                        foreach (var player in activePlayers)
                        {
                            playersOnBusz.Add(player.GetPlayerID());
                        }

                        if (IsSingle)
                        {
                            FillBuszWithCards();
                        }
                        else if (IsMPServer && NetworkGameManager.Instance != null)
                        {
                            // Multiplayer: Server húzza a busz kártyákat
                            NetworkGameManager.Instance.ServerBroadcastFillBuszCards();
                        }

                        DOVirtual.DelayedCall(1f, () =>
                        {
                            for (int I = 0; I < activePlayers.Count; I++)
                            {
                                playerManagers[I].HideCardsGroup();
                            }

                            if (IsSingle)
                            {
                                gameDisplay.ShowToast(toast_FeedbackMessage, "Piramisnak vége!", false, 2f, GamePhase.Tipp);
                            }
                            else if (IsMPServer && NetworkGameManager.Instance != null)
                            {
                                NetworkGameManager.Instance.RpcShowToast("Piramisnak vége!", false, 2f, GamePhase.Tipp);
                            }

                            DOVirtual.DelayedCall(3f, () =>
                            {
                                string buszNev = GameVars.Instance.BusName;

                                if (IsSingle)
                                {
                                    gameDisplay.ShowToast(toast_FeedbackMessage, $"Felszállás a {buszNev} járatra!", false, 2f, GamePhase.Tipp);
                                }
                                else if (IsMPServer && NetworkGameManager.Instance != null)
                                {
                                    NetworkGameManager.Instance.RpcShowToast($"Felszállás a {buszNev} járatra!", false, 2f, GamePhase.Tipp);
                                }

                                DOVirtual.DelayedCall(3f, () =>
                                {
                                    if (IsMPServer && NetworkGameManager.Instance != null)
                                    {
                                        NetworkGameManager.Instance.RpcRefreshPlayerUI();
                                    }
                                    else if (IsSingle)
                                    {
                                        RefreshPlayerUI();
                                    }

                                    gameDisplay.ShowPlayers(activePlayers.Count, playersGroup, 1f);

                                    DOVirtual.DelayedCall(1f, () =>
                                    {
                                        if (IsMPServer && NetworkGameManager.Instance != null)
                                        {
                                            // Server húzza a currentCard-ot és broadcastolja
                                            NetworkGameManager.Instance.ServerDrawAndBroadcastCurrentCard();
                                            NetworkGameManager.Instance.RpcStartBusz();
                                        }
                                        else if (IsSingle)
                                        {
                                            StartBusz();
                                        }
                                    });
                                });
                            });
                        });
                    });
                }
                break;

            case GamePhase.Busz:        //Busznál előbb végigmegyünnk a buszon (ameddig tudunk) és csak utána Rotálunk.
                if (activePlayers.Count > 0 && playersOnBusz.Count > 0)
                {
                    if (currentBuszCardIndex < 5)
                    {
                        currentBuszCardIndex++;

                        if (IsMPServer && NetworkGameManager.Instance != null)
                        {
                            NetworkGameManager.Instance.UpdateBuszCardIndex(currentBuszCardIndex);
                        }

                        DOVirtual.DelayedCall(0.25f, () =>
                        {
                            StartBusz();
                        });
                    }
                    else
                    {
                        PlayerCompletedBusz();
                        currentBuszCardIndex = 0;

                        if (IsMPServer && NetworkGameManager.Instance != null)
                        {
                            NetworkGameManager.Instance.UpdateBuszCardIndex(currentBuszCardIndex);
                        }
                    }
                }
                else
                {
                    currentPhase = GamePhase.JatekVege;
                    DOVirtual.DelayedCall(2f, () =>
                    {
                        EndGame();
                    });
                }
                break;
        }
    }

    private void EndGame()
    {
        // Busz elrejtése
        gameDisplay.HideBusz(buszGroup, buszCards, 0.5f);
        gameDisplay.HideCurrentCard(tippCardGroup, 0.5f);
        buszGroup.SetActive(false);
        tippCardGroup.SetActive(false);
        
        // Végeredmények összegyűjtése - az allPlayers listából, ami MINDEN játékost tartalmaz
        List<(string name, int score, PlayerExitStatus status)> playerResults = new List<(string, int, PlayerExitStatus)>();

        foreach (Player player in allPlayers)
        {
            playerResults.Add((player.GetPlayerName(), player.GetPlayerScore(), player.GetExitStatus()));
        }
        
        string debugStr = "All Players Status:\n";
        foreach (var p in playerResults)
        {
            debugStr += $"{p.name} - Score: {p.score} - Status: {p.status}\n";
        }
        Debug.Log(debugStr);
        
        // Csoportosítás státusz szerint
        var completedOrFailed = playerResults.Where(p => p.status == PlayerExitStatus.COMPLETED ||
                                                    p.status == PlayerExitStatus.FAILED)
                                             .OrderBy(p => p.score)
                                             .ToList();
        var gaveUp = playerResults.Where(p => p.status == PlayerExitStatus.GAVE_UP)
                                  .OrderBy(p => p.score).ToList();
        var disconnected = playerResults.Where(p => p.status == PlayerExitStatus.DISCONNECTED)
                                        .OrderBy(p => p.score).ToList();
        
        // Eredmények megjelenítése
        string resultsMessage = "Játék vége!\n\nVégeredmények:\n\n";
        int rank = 1;
        
        // Teljesített/Kiesett játékosok (normál ranglista)
        foreach (var player in completedOrFailed)
        {
            string statusLabel = player.status == PlayerExitStatus.FAILED ? " [KIESETT]" : "";
            resultsMessage += $"{rank}. {player.name}: {player.score} pont{statusLabel}\n";
            rank++;
        }
        
        // Feladták a buszt
        if (gaveUp.Count > 0)
        {
            resultsMessage += "\n--- Feladták ---\n";
            foreach (var player in gaveUp)
            {
                resultsMessage += $"{rank}. {player.name}: {player.score} pont [FELADTA]\n";
                rank++;
            }
        }
        
        // Kapcsolat megszakadt
        if (disconnected.Count > 0)
        {
            resultsMessage += "\n--- Kapcsolat megszakadt ---\n";
            foreach (var player in disconnected)
            {
                resultsMessage += $"{rank}. {player.name}: {player.score} pont [Kapcsolat megszakadt]\n";
                rank++;
            }
        }
        
        // Győztes meghatározása (csak a teljesített/kiesett kategóriából)
        if (completedOrFailed.Count > 0)
        {
            resultsMessage += $"\nGyőztes: {completedOrFailed[0].name}!";
        }
        
        Debug.Log(resultsMessage);
        
        // MULTIPLAYER: Broadcast EndGame minden kliensnek
        if (IsMPServer && NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.RpcShowEndGame(resultsMessage);
        }
        else if (IsSingle)
        {
            gameDisplay.ShowToast(toast_FeedbackMessage, resultsMessage, false, -1f, GamePhase.JatekVege);
            
            DOVirtual.DelayedCall(5f, () =>
            {
                gameDisplay.ShowEndButtons(endButtonsGroup, 1f);
            });
        }
    }

    /// Multiplayer: Client-side EndGame megjelenítése
    public void GM_Client_ShowEndGame(string resultsMessage)
    {
        Debug.Log($"[FromRpc] [GameManager] GM_Client_ShowEndGame"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc] [GameManager] GM_Client_ShowEndGame");
        
        gameDisplay.ShowToast(toast_FeedbackMessage, resultsMessage, false, -1f, GamePhase.JatekVege);
        
        DOVirtual.DelayedCall(5f, () =>
        {
            gameDisplay.ShowEndButtons(endButtonsGroup, 1f);
        });
    }

    #endregion

    #region Metódusok

    public void ChangeIsGamePaused()
    {
        if (isGamePaused)
        {
            PauseMenuCanvas.gameObject.SetActive(false);
            
            // Ha Busz fázisban vagyunk és visszajövünk, állítsuk vissza a highlightot
            if (currentPhase == GamePhase.Busz && waitingForBuszTipp)
            {
                gameDisplay.HighlightBuszCard(buszCards, currentBuszCardIndex, true);
            }
        }
        else
        {
            PauseMenuCanvas.gameObject.SetActive(true);
        }

        PauseMenuPanel.SetActive(true);
        PauseOptionsPanel.SetActive(false);
        PauseExitPanel.SetActive(false);

        isGamePaused = !isGamePaused;
    }

    #region Setup

    public void StartGame()
    {
        // Ha multiplayer, akkor a NetworkGameManager-en keresztül indítunk
        if (IsHostOrClients)
        {
            if (Mirror.NetworkServer.active)
            {
                // Host indítja a játékot minden kliensnek
                //Debug.Log("[GameManager] Host starting game for all clients"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] Host starting game for all clients");
                Debug.Log("[GameManager] Host calling Server to start multiplayer game."); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] Host calling Server to start multiplayer game.");

                NetworkGameManager.Instance?.ServerStartGame();
            }
            else
            {
                // Client nem indíthat játékot (gomb rejtve van)
                Debug.LogWarning("[GameManager] Client cannot start game!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("<WARNING!>\t[GameManager]\tClient cannot start game!");
            }
        }
        else
        {
            // Singleplayer indítás
            gameDisplay.HideStartButtons(startButtonsGroup, 0.5f, InitializeGame);
        }
    }

    public void OnExitButtonClicked()
    {
        // Ha multiplayer, akkor hívjuk a BuszNetworkManager.LeaveGame()-t
        if (Mirror.NetworkClient.isConnected || Mirror.NetworkServer.active)
        {
            Debug.Log("[GameManager] Exit button clicked - leaving multiplayer game"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] Exit button clicked - leaving multiplayer game");

            BuszNetworkManager.singleton.LeaveGame();
        }
        else
        {
            // Singleplayer - csak visszamegyünk a MainMenu-be
            Debug.Log("[GameManager] Exit button clicked - returning to MainMenu (singleplayer)"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] Exit button clicked - returning to MainMenu (singleplayer)");

            customSceneManager.LoadScene("MainMenu");
        }
    }

    /// EndGame button megnyomásakor hívódik (játék végén)
    public void OnEndGameButtonClicked()
    {
        // Ha multiplayer, akkor hívjuk a BuszNetworkManager.LeaveGame()-t
        if (Mirror.NetworkClient.isConnected || Mirror.NetworkServer.active)
        {
            Debug.Log("[GameManager] EndGame button clicked - leaving multiplayer game"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] EndGame button clicked - leaving multiplayer game");

            BuszNetworkManager.singleton.LeaveGame();
        }
        else
        {
            // Singleplayer - MainMenu betöltése
            Debug.Log("[GameManager] EndGame button clicked - returning to MainMenu (singleplayer)"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] EndGame button clicked - returning to MainMenu (singleplayer)");

            customSceneManager.LoadScene("MainMenu");
        }
    }

    /// Singleplayer játék inicializálása (régi Start() logika)
    public void InitializeSingleplayer()
    {
        // Aktív játékosok inicializálása
        activePlayers.Clear();
        allPlayers.Clear();

        for (int I = 0; I < playerManagers.Length; I++)
        {
            if (I < GameVars.Instance.NumberOfPlayersInGame)
            {
                playerManagers[I].gameObject.SetActive(true);

                Player newPlayer = new Player(I, GameVars.Instance.GetPlayerName(I));
                activePlayers.Add(newPlayer);
                allPlayers.Add(newPlayer);
                playerManagers[I].Initialize(I);
                playerManagers[I].SetPlayerData(newPlayer);
                
                // GameEvents referencia átadása
                playerManagers[I].SetGameEvents(gameEvents);
            }
            else
            {
                playerManagers[I].gameObject.SetActive(false);
            }
        }

        SetupTippButtons();

        RefreshPlayerUI();

        deck = new Deck();

        currentCard = tippCardGroup.GetComponentInChildren<CardManager>();
        currentCard.IsDragging = false;

        if (currentCard == null)
        {
            Debug.LogError("GameManager: CardManager component not found on tippCardGroup or its children!");
        }

        timerText = timerGroup.GetComponentInChildren<TextMeshProUGUI>();

        if (timerText == null)
        {
            Debug.LogError("GameManager: Timer TextMeshProUGUI component not found in timerGroup!");
        }

        timerGroup.SetActive(false);

        if (GameVars.Instance.ReversedPyramidMode)
        {
            SetupReversedPyramid();
        }

        gameDisplay.ShowStartButtons(startButtonsGroup, 1f);
        gameDisplay.ShowPlayers(activePlayers.Count, playersGroup, 1f);
    }

    /// Multiplayer játék inicializálása (NetworkGameManager hívja)
    public void InitializeMultiplayerGame(List<string> playerNamesFromNetwork)
    {
        Debug.Log($"[GameManager] Initializing multiplayer game with {playerNamesFromNetwork.Count} players"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Initializing multiplayer game with {playerNamesFromNetwork.Count} players");

        // Aktív játékosok inicializálása
        activePlayers.Clear();
        allPlayers.Clear();

        // FONTOS: NetworkPlayer objektumok ID-ját használjuk (nem indexet!)
        var networkPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
        
        for (int I = 0; I < playerManagers.Length; I++)
        {
            if (I < playerNamesFromNetwork.Count)
            {
                playerManagers[I].gameObject.SetActive(true);
                
                // Megkeressük a megfelelő NetworkPlayer-t a név alapján
                int playerIdFromNetwork = I; // Default
                foreach (var np in networkPlayers)
                {
                    if (np.playerName == playerNamesFromNetwork[I])
                    {
                        playerIdFromNetwork = np.playerId;
                        Debug.Log($"[GameManager] Matched {playerNamesFromNetwork[I]} to NetworkPlayer ID {playerIdFromNetwork}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Matched {playerNamesFromNetwork[I]} to NetworkPlayer ID {playerIdFromNetwork}");
                        break;
                    }
                }
                
                Player newPlayer = new Player(playerIdFromNetwork, playerNamesFromNetwork[I]);
                activePlayers.Add(newPlayer);
                allPlayers.Add(newPlayer);
                playerManagers[I].Initialize(I);
                playerManagers[I].SetPlayerData(newPlayer);
                
                // GameEvents referencia átadása
                playerManagers[I].SetGameEvents(gameEvents);
                
                Debug.Log($"[GameManager] Player {I}: {playerNamesFromNetwork[I]} (NetworkPlayer ID: {playerIdFromNetwork})"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Player {I}: {playerNamesFromNetwork[I]} (NetworkPlayer ID: {playerIdFromNetwork})");
            }
            else
            {
                playerManagers[I].gameObject.SetActive(false);
            }
        }

        SetupTippButtons();

        RefreshPlayerUI();

        deck = new Deck();

        currentCard = tippCardGroup.GetComponentInChildren<CardManager>();
        currentCard.IsDragging = false;

        if (currentCard == null)
        {
            Debug.LogError("GameManager: CardManager component not found on tippCardGroup or its children!");
        }

        timerText = timerGroup.GetComponentInChildren<TextMeshProUGUI>();

        if (timerText == null)
        {
            Debug.LogError("GameManager: Timer TextMeshProUGUI component not found in timerGroup!");
        }

        timerGroup.SetActive(false);

        if (GameVars.Instance.ReversedPyramidMode)
        {
            SetupReversedPyramid();
        }

        SetupMultiplayerUI();

        gameDisplay.ShowStartButtons(startButtonsGroup, 1f);
        gameDisplay.ShowPlayers(activePlayers.Count, playersGroup, 1f);
    }

    private void SetupMultiplayerUI()
    {
        // Start gomb elrejtése csak clienteknél (nem host)
        if (!Mirror.NetworkServer.active)
        {
            Transform startButtonTransform = startButtonsGroup.transform.Find("Start - Button");
            if (startButtonTransform != null)
            {
                startButtonTransform.gameObject.SetActive(false);
                Debug.Log("[GameManager] Start button hidden for client"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] Start button hidden for client");
            }

            gameDisplay.ShowToast(toast_FeedbackMessage, "Várakozás a busz indulására!", false, 5f, GamePhase.Busz);
        }
    }

    private void SetupTippButtons()
    {
        Button[] redOrBlackButtons = redOrBlackGroup.GetComponentsInChildren<Button>();
        redOrBlackButtons[0].onClick.AddListener(() => OnTippButtonClicked(TippValue.PIROS));
        redOrBlackButtons[1].onClick.AddListener(() => OnTippButtonClicked(TippValue.FEKETE));

        Button[] belowOrAboveButtons = belowOrAboveGroup.GetComponentsInChildren<Button>();
        belowOrAboveButtons[0].onClick.AddListener(() => OnTippButtonClicked(TippValue.ALATTA));
        belowOrAboveButtons[1].onClick.AddListener(() => OnTippButtonClicked(TippValue.UGYANAZ));
        belowOrAboveButtons[2].onClick.AddListener(() => OnTippButtonClicked(TippValue.FELETTE));

        Button[] betweenOrApartButtons = betweenOrApartGroup.GetComponentsInChildren<Button>();
        betweenOrApartButtons[0].onClick.AddListener(() => OnTippButtonClicked(TippValue.UGYANAZ_ALSO));
        betweenOrApartButtons[1].onClick.AddListener(() => OnTippButtonClicked(TippValue.KOZTE));
        betweenOrApartButtons[2].onClick.AddListener(() => OnTippButtonClicked(TippValue.SZET));
        betweenOrApartButtons[3].onClick.AddListener(() => OnTippButtonClicked(TippValue.UGYANAZ_FELSO));

        Button[] exactColorButtons = exactColorGroup.GetComponentsInChildren<Button>();
        exactColorButtons[0].onClick.AddListener(() => OnTippButtonClicked(TippValue.LOHERE));
        exactColorButtons[1].onClick.AddListener(() => OnTippButtonClicked(TippValue.ROMBUSZ));
        exactColorButtons[2].onClick.AddListener(() => OnTippButtonClicked(TippValue.SZIV));
        exactColorButtons[3].onClick.AddListener(() => OnTippButtonClicked(TippValue.PIKK));

        Button[] exactNumberButtons = exactNumberGroup.GetComponentsInChildren<Button>();
        for (int I = 0; I < exactNumberButtons.Length; I++)
        {
            int index = I;
            exactNumberButtons[I].onClick.AddListener(() => OnTippButtonClicked((TippValue)(index + 2)));
        }
    }

    private void SetupReversedPyramid()
    {
        // Ha fordított piramis mód, akkor a piramis sorait fordított sorrendben kell beállítani a hierarchiában.
        // A sorok egyszerű GameObject-ek a piramisGroup alatt.
        // Eredeti:                                     Fordított:
        /*
        Row_5: [?] (1 kártya)                           Row_5: [?] [?] [?] [?] [?] (5 kártya)
        Row_4: [?] [?] (2 kártya)                       Row_4: [?] [?] [?] [?] (4 kártya)
        Row_3: [?] [?] [?] (3 kártya)                   Row_3: [?] [?] [?] (3 kártya)
        Row_2: [?] [?] [?] [?] (4 kártya)               Row_2: [?] [?] (2 kártya)
        Row_1: [?] [?] [?] [?] [?] (5 kártya)           Row_1: [?] (1 kártya)
        */
        Transform[] rows = new Transform[5];
        for (int I = 0; I < 5; I++)
        {
            rows[I] = piramisGroup.transform.GetChild(I);
        }

        for (int I = 0; I < 5; I++)
        {
            rows[I].SetSiblingIndex(4 - I);
            rows[I].name = $"Row_{I + 1}";
        }
    }

    #endregion

    #region Player Management

    public void RotatePlayers(float delay = 0f)
    {
        if (activePlayers.Count <= 1) return;
        gameDisplay.HidePlayers(activePlayers.Count, playersGroup, 1f, () =>
        {
            Player firstPlayer = activePlayers[0];
            activePlayers.RemoveAt(0);
            activePlayers.Add(firstPlayer);

            HidePointGive();

            RefreshPlayerUI();

            DOVirtual.DelayedCall(delay, () =>
            {
                gameDisplay.ShowPlayers(activePlayers.Count, playersGroup, 1f);
                if (currentPhase == GamePhase.Busz)
                {
                    for (int I = 0; I < activePlayers.Count; I++)
                    {
                        playerManagers[I].HideCardsGroup();
                    }
                }
            });
        });

        /*
        DOVirtual.DelayedCall(2f, () =>
        {
            gameDisplay.ShowPlayers(activePlayers.Count, playersGroup, 1f);
        });
        */
    }

    public void RefreshPlayerUI()
    {
        // Csak az aktív játékosokat frissítjük (akik még játszanak) - ÚJ KOMMENT
        for (int I = 0; I < activePlayers.Count; I++)
        {
            playerManagers[I].SetPlayerData(activePlayers[I]);
            //playerManagers[I].SetInteractive(I == 0);
        }
        
        // Az inaktív PlayerManager-eket elrejtjük - ÚJ KOMMENT ÉS KÓD [FOR CIKLUS]
        for (int I = activePlayers.Count; I < playerManagers.Length; I++)
        {
            if (playerManagers[I] != null)
            {
                playerManagers[I].gameObject.SetActive(false);
            }
        }
    }

    public void RemovePlayer(int playerId)
    {
        // playerId alapján megkeressük az indexet az activePlayers listában
        int playerIndex = activePlayers.FindIndex(p => p.GetPlayerID() == playerId);
        
        if (playerIndex < 0)
        {
            Debug.LogWarning($"Player with ID {playerId} not found in activePlayers!");
            return;
        }

        activePlayers[playerIndex].ClearPlayerCards();
        activePlayers.RemoveAt(playerIndex);
        
        // FONTOS: Az UTOLSÓ PlayerManager-t inaktiváljuk, nem az ID-hoz tartozót!
        // Mert Busz rotáció esetén PlayerManager[0] mindig aktív marad (új játékost mutat majd)
        int lastActiveIndex = activePlayers.Count; // Ez most már a régi utolsó index
        if (lastActiveIndex < playerManagers.Length && playerManagers[lastActiveIndex] != null)
        {
            playerManagers[lastActiveIndex].gameObject.SetActive(false);
        }

        // NEM töröljük az allPlayers listából - ott megmarad az eredeti Player objektum!
    }

    /// Multiplayer: Server kezeli a játékos disconnect-et
    public void GM_Server_HandlePlayerDisconnect(int playerId, string playerName)
    {
        Debug.Log($"[Server] GM_Server_HandlePlayerDisconnect - Player {playerId} ({playerName})"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] GM_Server_HandlePlayerDisconnect - Player {playerId} ({playerName})");
        
        // Keressük meg a játékost
        Player player = activePlayers.FirstOrDefault(p => p.GetPlayerID() == playerId);
        bool wasActivePlayer = (player != null && activePlayers.Count > 0 && activePlayers[0] == player);
        
        if (player == null)
        {
            // Lehet hogy már nem aktív (pl. már kiesett)
            player = allPlayers.FirstOrDefault(p => p.GetPlayerID() == playerId);
            if (player == null)
            {
                Debug.LogWarning($"[Server] Player {playerId} not found!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"<WARNING!> [Server] Player {playerId} not found!");
                return;
            }
        }

        // Ha épp ez a játékos volt soron, állítsuk le az időzítőt és a várakozást
        if (wasActivePlayer)
        {
            Debug.Log($"[Server] Active player disconnected, stopping timers and waiting states"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] Active player disconnected, stopping timers and waiting states");
            
            // Állítsuk le az időzítőket és várakozási állapotokat
            if (currentPhase == GamePhase.Tipp && waitingForTipp)
            {
                waitingForTipp = false;
                currentTimer = 0f;
                if (NetworkGameManager.Instance != null)
                {
                    NetworkGameManager.Instance.UpdateTimer(20f);
                    NetworkGameManager.Instance.RpcHideTimer();
                }
            }
            else if (currentPhase == GamePhase.Piramis && waitingForCardDrop)
            {
                waitingForCardDrop = false;
                cardDropTimer = 0f;
                if (NetworkGameManager.Instance != null)
                {
                    NetworkGameManager.Instance.UpdateCardDropTimer(20f);
                }
            }
            else if (currentPhase == GamePhase.Busz && waitingForBuszTipp)
            {
                waitingForBuszTipp = false;
                currentTimer = 0f;
                if (NetworkGameManager.Instance != null)
                {
                    NetworkGameManager.Instance.UpdateTimer(20f);
                    NetworkGameManager.Instance.RpcHideTimer();
                }
            }
            else if (isGivingPoints)
            {
                isGivingPoints = false;
                pointGiveTimer = 0f;
                if (NetworkGameManager.Instance != null)
                {
                    NetworkGameManager.Instance.UpdatePointGiveTimer(20f);
                }
            }
        }

        // Beállítjuk a disconnect státuszt
        player.SetExitStatus(PlayerExitStatus.DISCONNECTED);
        
        // Broadcast minden kliensnek
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.RpcPlayerDisconnected(playerId, playerName);
        }

        // Eltávolítjuk a játékost az activePlayers-ből (ha még benne van)
        if (activePlayers.Contains(player))
        {
            RemovePlayer(playerId);
        }

        // Eltávolítjuk a buszon lévő játékosok listájából is
        if (playersOnBusz.Contains(playerId))
        {
            playersOnBusz.Remove(playerId);
        }

        // Frissítjük a UI-t
        RefreshPlayerUI();

        // Ha nincs több játékos, akkor játék vége
        if (activePlayers.Count == 0 || (currentPhase == GamePhase.Busz && playersOnBusz.Count == 0))
        {
            DOVirtual.DelayedCall(2f, () =>
            {
                EndGame();
            });
        }
        // Ha ez a játékos volt soron, folytatjuk a következő játékossal (csak ha nem lett leállítva fentebb)
        else if (wasActivePlayer)
        {
            // Kis késleltetés hogy a kliensek láthassák a disconnect üzenetet
            DOVirtual.DelayedCall(1.5f, () =>
            {
                if (currentPhase == GamePhase.Tipp)
                {
                    // Következő tipp kör
                    if (NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.RpcRotatePlayers();
                        DOVirtual.DelayedCall(2.5f, () =>
                        {
                            NetworkGameManager.Instance.RpcStartTippKor();
                        });
                    }
                }
                else if (currentPhase == GamePhase.Piramis)
                {
                    // Következő piramis kártya
                    if (NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.RpcRotatePlayers();
                        DOVirtual.DelayedCall(2.5f, () =>
                        {
                            NetworkGameManager.Instance.RpcFlipPyramidCard();
                        });
                    }
                }
                else if (currentPhase == GamePhase.Busz)
                {
                    // Következő busz játékos
                    if (NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.RpcNextBuszPlayer(shouldRotate: false);
                    }
                }
            });
        }
    }

    /// Multiplayer: Client-side játékos disconnect megjelenítése
    public void GM_Client_PlayerDisconnected(int playerId, string playerName)
    {
        Debug.Log($"[FromRpc] [GameManager] GM_Client_PlayerDisconnected - Player {playerId} ({playerName})"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc] [GameManager] GM_Client_PlayerDisconnected - Player {playerId} ({playerName})");
        
        // Toast megjelenítése
        gameDisplay.ShowToast(toast_FeedbackMessage, $"{playerName} kilépett a játékból!", false, 3f, currentPhase);
        
        // Keressük meg a játékost
        Player player = activePlayers.FirstOrDefault(p => p.GetPlayerID() == playerId);
        if (player == null)
        {
            player = allPlayers.FirstOrDefault(p => p.GetPlayerID() == playerId);
        }

        if (player != null)
        {
            // Beállítjuk a disconnect státuszt
            player.SetExitStatus(PlayerExitStatus.DISCONNECTED);
            
            // Eltávolítjuk az activePlayers-ből (ha még benne van)
            if (activePlayers.Contains(player))
            {
                RemovePlayer(playerId);
            }

            // Eltávolítjuk a buszon lévő játékosok listájából is
            if (playersOnBusz.Contains(playerId))
            {
                playersOnBusz.Remove(playerId);
            }

            // Frissítjük a UI-t
            RefreshPlayerUI();
        }
    }

    #endregion

    #region Display Management

    private void DisplayTippGroup(TippType tippType)
    {
        if (tippType == TippType.NONE)
        {
            gameDisplay.HideTippGroups(tippGroupsGroup, 0.5f);
            
            tippCardTitle.GetComponentInChildren<TextMeshProUGUI>().text = "";
            tippCardTitle.SetActive(false);
        }

        redOrBlackGroup.SetActive(false);
        belowOrAboveGroup.SetActive(false);
        betweenOrApartGroup.SetActive(false);
        exactColorGroup.SetActive(false);
        exactNumberGroup.SetActive(false);

        switch (tippType)
        {
            case TippType.PirosVagyFekete:
                tippCardTitle.GetComponentInChildren<TextMeshProUGUI>().text = "Piros vagy Fekete?";
                tippCardTitle.SetActive(true);
                redOrBlackGroup.SetActive(true);
                break;
            case TippType.AlattaVagyFelette:
                tippCardTitle.GetComponentInChildren<TextMeshProUGUI>().text = "Alatta vagy Felette?";
                tippCardTitle.SetActive(true);
                belowOrAboveGroup.SetActive(true);
                break;
            case TippType.KozteVagySzet:
                tippCardTitle.GetComponentInChildren<TextMeshProUGUI>().text = "Közte vagy Szét?";
                tippCardTitle.SetActive(true);
                betweenOrApartGroup.SetActive(true);
                break;
            case TippType.PontosTipus:
                tippCardTitle.GetComponentInChildren<TextMeshProUGUI>().text = "Pontos Szín?";
                tippCardTitle.SetActive(true);
                exactColorGroup.SetActive(true);
                break;
            case TippType.PontosSzam:
                tippCardTitle.GetComponentInChildren<TextMeshProUGUI>().text = "Pontos Szám?";
                tippCardTitle.SetActive(true);
                exactNumberGroup.SetActive(true);
                break;
        }

        if (tippType != TippType.NONE)
        {
            gameDisplay.ShowTippGroups(tippGroupsGroup, 0.5f);
        }
    }

    #endregion

    #region Timer Management
    private void StartTimer()
    {
        if (IsSingle || (IsMPServer && NetworkGameManager.Instance != null))
        {
            currentTimer = GetTimerForPhase(currentPhase);

            // MULTIPLAYER: Server frissíti a SyncVar-t
            if (IsMPServer && NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.UpdateTimer(currentTimer);
            }

            if (currentPhase == GamePhase.Tipp)
            {
                waitingForTipp = true;
                waitingForBuszTipp = false;
            }
            else if (currentPhase == GamePhase.Busz)
            {
                waitingForTipp = false;
                waitingForBuszTipp = true;
            }
            else
            {
                Debug.LogError("StartTimer called in unsupported phase: " + currentPhase);
            }
        }

        timerText.text = "Választási idő:\n" + currentTimer.ToString("F1");
        gameDisplay.ShowTimer(timerGroup, 0.5f);
    }

    // Multiplayer esetén csak a szerver hívja de biztosra megyünk ezért az első dolog az ellenőrzés.
    private void OnTimerExpired()
    {
        // MULTIPLAYER: Csak a szerver generál random tippet
        if (IsMPServer)
        {
            // Szerver logika
            TippValue randomTipp = GetRandomTippForType(currentTippType);
            int currentPlayerId = activePlayers[0].GetPlayerID();
            
            Debug.Log($"[Server]\tTimer expired for player {currentPlayerId}, random tipp: {randomTipp}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tTimer expired for player {currentPlayerId}, random tipp: {randomTipp}");

            // Toast broadcast minden kliensnek
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.RpcShowTimerExpired(currentPlayerId);
            }

            /*
            Player player = activePlayers.Find(p => p.GetPlayerID() == currentPlayerId);
            if (player != null)
            {
                player.SetTipp(randomTipp);
            }
            else
            {
                Debug.LogError($"[Server] Player with ID {currentPlayerId} not found in activePlayers!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"<ERROR!> [Server] Player with ID {currentPlayerId} not found in activePlayers!");
            }
            */

            /*
            // Az RPC broadcast fogja hozzáadni MINDENKINEK (beleértve a host-ot is)
            // MULTIPLAYER: Kártya hozzáadás broadcast minden kliensnek (HOST IS!)
            if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
            {
                Debug.Log($"[Server] Broadcasting card addition to ALL clients (including host): Player {currentPlayerId}, {currentCard.GetCardData().GetCardType()} {currentCard.GetCardData().GetCardValue()}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server] Broadcasting card addition to ALL clients (including host): Player {currentPlayerId}, {currentCard.GetCardData().GetCardType()} {currentCard.GetCardData().GetCardValue()}");
                
                NetworkGameManager.Instance.RpcAddCardToPlayer
                (
                    currentPlayerId,
                    (int)currentCard.GetCardData().GetCardType(),
                    (int)currentCard.GetCardData().GetCardValue(),
                    (int)currentCard.GetCardData().GetCardBackType()
                );
            }
            */

            // Tipp feldolgozás (multiplayer flow)
            DOVirtual.DelayedCall(1f, () =>
            {
                GM_Server_ProcessPlayerTipp(currentPlayerId, randomTipp);
            });
        }
        else if (IsSingle)         // [Se nem Szerver, se nem Kliens => Singleplayer]
        {
            // SINGLEPLAYER: Eredeti logika
            gameDisplay.ShowToast(toast_FeedbackMessage, "Lejárt az idő! Véletlenszerű tippet kapsz!", false, 1f, currentPhase);
            TippValue randomTipp = GetRandomTippForType(currentTippType);

            activePlayers[0].SetTipp(randomTipp);
            waitingForTipp = false;
            gameDisplay.HideTimer(timerGroup, 0.25f);
            DisplayTippGroup(TippType.NONE);

            DOVirtual.DelayedCall(1f, () =>
            {
                ProcessTipp();
            });
        }
    }

    /// Client-side: Timer lejárt toast megjelenítése (RPC-ből hívva)
    public void ShowTimerExpired()
    {
        if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Client]\tShowTimerExpiredToast() called on clients.");

        gameDisplay.ShowToast(toast_FeedbackMessage, "Lejárt az idő! Véletlenszerű tippet kapsz!", false, 1f, currentPhase);
        waitingForTipp = false;
        gameDisplay.HideTimer(timerGroup, 0.25f);
        DisplayTippGroup(TippType.NONE);
    }

    private float GetTimerForPhase(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.Tipp => 15f,      // Eredeti: 10f
            GamePhase.Piramis => 20f,
            GamePhase.Busz => 15f,      // Eredeti: 10f
            _ => 10f
        };
    }

    #endregion

    #region Tipp Management

    // Multiplayer esetén az OnTippButtonClicked metódus csak a kliens oldalon fut le,
    // és elküldi a tippet a szervernek ami feldolgozza.
    private void OnTippButtonClicked(TippValue tippValue)
    {
        // Multiplayer esetén küldjük el a tippet a szervernek
        // [ Mirror.NetworkClient.isConnected => Host vagy Client ]
        if (IsHostOrClients)
        {
            var networkPlayer = Mirror.NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
            if (networkPlayer != null)
            {
                Debug.Log($"[GameManager] \"{networkPlayer.playerName}\" sending tipp to server: {tippValue}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] \"{networkPlayer.playerName}\" sending tipp to server: {tippValue}");

                if (currentPhase == GamePhase.Tipp)
                {
                    networkPlayer.SubmitTipp(tippValue);
                }
                else if (currentPhase == GamePhase.Busz)
                {
                    // Azonnal állítsuk le a várakozást, hogy ne lehessen többször kattintani
                    waitingForBuszTipp = false;
                    gameDisplay.HideTimer(timerGroup, 0.25f);
                    
                    if (buszButtonsGroup.activeInHierarchy)
                        gameDisplay.HideBuszButtons(buszButtonsGroup, 0.25f);

                    networkPlayer.SubmitBuszTipp(tippValue);
                }
                
                return; // Szerver fogja feldolgozni és visszaküldeni az eredményt
            }
        }

        // Singleplayer logika (eredeti)
        activePlayers[0].SetTipp(tippValue);

        waitingForTipp = false;
        waitingForBuszTipp = false;
        gameDisplay.HideTimer(timerGroup, 0.25f);

        DisplayTippGroup(TippType.NONE);

        if (currentPhase == GamePhase.Tipp)
        {
            ProcessTipp();
        }
        else if (currentPhase == GamePhase.Busz)
        {
            if (buszButtonsGroup.activeInHierarchy)
                gameDisplay.HideBuszButtons(buszButtonsGroup, 0.25f);
            ProcessBuszTipp();
        }
        else
        {
            Debug.LogError("Tipp button clicked in unsupported phase: " + currentPhase);
        }
    }

    private void ProcessTipp()
    {
        //currentCard.ShowCardFront();
        currentCard.AnimateCardFlip(1f);

        activePlayers[0].AddCardToPlayer(currentCard.GetCardData());

        bool isTippCorrect = CheckTipp(activePlayers[0], currentCard.GetCardData());

        if (isTippCorrect)
        {
            DOVirtual.DelayedCall(0.5f, () =>
            {
                gameDisplay.ShowToast(toast_FeedbackMessage, "Helyes tipp!", false, 1.5f, GamePhase.Tipp);
            });
            DOVirtual.DelayedCall(2f, () =>
            {
                gameDisplay.HideCurrentCard(tippCardGroup, 0.5f, () => ShowPointGiving());
            });
        }
        else
        {
            activePlayers[0].IncreasePlayerScore(currentRound);

            DOVirtual.DelayedCall(0.5f, () =>
            {
                gameDisplay.ShowToast(toast_FeedbackMessage, "Helytelen tipp!", false, 1.5f, GamePhase.Tipp);
            });
            DOVirtual.DelayedCall(2f, () =>
            {
                gameDisplay.HideCurrentCard(tippCardGroup, 0.5f, () => TippContinue(2f));
            });
        }
    }

    // Multiplayer: Szerver feldolgozza a játékos tippjét
    // Egyjátékos móddal ellentétben, a Process(Player)Tipp-ben állítjuk be a tippet és nem előtte.
    public void GM_Server_ProcessPlayerTipp(int playerId, TippValue tippValue)
    {
        
        // Megkeressük a játékost ID alapján
        Player player = activePlayers.FirstOrDefault(p => p.GetPlayerID() == playerId);
        if (player == null)
        {
            Debug.LogError($"<ERROR!>\t[Client]\t[GameManager]\tGM_Server_ProcessPlayerTipp - Player {playerId} not found!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"<ERROR!>\t[Client]\t[GameManager]\tGM_Server_ProcessPlayerTipp - Player {playerId} not found!");
            
            return;
        }

        // Tipp beállítása
        player.SetTipp(tippValue);

        // Tipp ellenőrzése (ehhez elég a currentCard adatai, nem kell hozzáadni)
        bool isTippCorrect = CheckTipp(player, currentCard.GetCardData());

        //Debug.Log($"[GameManager] GM_Server_ProcessPlayerTipp - Player {playerId}: {tippValue}  |  Current Card: [{currentCard.GetCardData().GetCardType()} {currentCard.GetCardData().GetCardValue()}]"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] GM_Server_ProcessPlayerTipp - Player {playerId}: {tippValue} |  Current Card: [{currentCard.GetCardData().GetCardType()} {currentCard.GetCardData().GetCardValue()}]");
        
        
        // Az RPC broadcast fogja hozzáadni MINDENKINEK (beleértve a host-ot is)
        // MULTIPLAYER: Kártya hozzáadás broadcast minden kliensnek (HOST IS!)
        if (IsMPServer && NetworkGameManager.Instance != null)
        {
            //Debug.Log($"[Server]\tBroadcasting card addition to ALL clients (including host): Player {playerId}, {currentCard.GetCardData().GetCardType()} {currentCard.GetCardData().GetCardValue()}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tBroadcasting card addition to ALL clients (including host): Player {playerId}, {currentCard.GetCardData().GetCardType()} {currentCard.GetCardData().GetCardValue()}");
            
            NetworkGameManager.Instance.RpcAddCardToPlayer
            (
                playerId,
                (int)currentCard.GetCardData().GetCardType(),
                (int)currentCard.GetCardData().GetCardValue(),
                (int)currentCard.GetCardData().GetCardBackType()
            );
        }
        

        // Ha helytelen, pont hozzáadása (BROADCAST mindenkinek, beleértve a host-ot is!)
        if (!isTippCorrect)
        {
            int newScore = player.GetPlayerScore() + currentRound;
            
            // Az RPC broadcast fogja beállítani MINDENKINEK (beleértve a host-ot is)
            // Score broadcast minden kliensnek (HOST IS!)
            if (IsMPServer && NetworkGameManager.Instance != null)
            {
                //Debug.Log($"[Server]\tBroadcasting score update to ALL clients (including host): Player {playerId}, new score: {newScore}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tBroadcasting score update to ALL clients (including host): Player {playerId}, new score: {newScore}");
                
                NetworkGameManager.Instance.RpcUpdatePlayerScore(playerId, newScore);
            }
        }

        // NetworkGameManager-en keresztül broadcast minden kliensnek
        if (IsMPServer && NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.RpcShowTippResult
            (
                playerId, 
                isTippCorrect, 
                (int)currentCard.GetCardData().GetCardType(), 
                (int)currentCard.GetCardData().GetCardValue(),
                (int)currentCard.GetCardData().GetCardBackType(),
                currentRound
            );
        }

        // Folytatás logika még mindig SERVER OLDALON
        DOVirtual.DelayedCall(2.5f, () =>
        {
            if (isTippCorrect)
            {
                // Helyes tipp - Broadcastoljuk a pont osztás UI megjelenítését
                if (IsMPServer && NetworkGameManager.Instance != null)
                {
                    // FONTOS: Szerver beállítja a totalPointsToGive-ot MÉG AZ RPC ELŐTT!
                    // Így a kliensek már látni fogják a helyes értéket a SyncVar-ból
                    totalPointsToGive = currentRound;
                    
                    //if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GM_Server_ProcessPlayerTipp()]\t[Server]\tBroadcasting ShowPointGiving to all clients");
                    
                    // Broadcast MINDEN kliensnek (beleértve a host-ot is!)
                    // => Majd az Rpc hívja meg a GM_Client_ShowPointGiving-et, ahol
                    //    eldől, hogy melyik kliens mit fog látni.
                    NetworkGameManager.Instance.RpcShowPointGiving(playerId);
                }
            }
            else
            {
                // Helytelen tipp - folytatjuk a következő játékossal
                if (IsMPServer && NetworkGameManager.Instance != null)
                {
                    //if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GM_Server_ProcessPlayerTipp()]\t[Server]\tBroadcasting TippContinue to all clients");
                    
                    // Broadcast RpcTippContinue minden kliensnek (beleértve a host-ot is)
                    NetworkGameManager.Instance.RpcTippContinue(2f);
                }
            }
        });
    }

    /// Multiplayer: Client-side pont osztás UI megjelenítése (RPC-ből hívva)
    public void  GM_Client_ShowPointGiving(int playerId)
    {
        Debug.Log($"[GameManager] GM_Client_ShowPointGiving - Player {playerId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] GM_Client_ShowPointGiving - Player {playerId}");
        
        var localPlayer = Mirror.NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (localPlayer != null && localPlayer.playerId == playerId)
        {
            // Ez a lokális játékos, megjelenítjük a pont osztás UI-t
            Debug.Log($"[GameManager] Showing point giving UI for local player {playerId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Showing point giving UI for local player {playerId}");
            
            ShowPointGiving();
        }
        else
        {
            // Más játékos, várakozási toast
            if (localPlayer != null)
            {
                string activePlayerName = activePlayers.FirstOrDefault(p => p.GetPlayerID() == playerId)?.GetPlayerName() ?? "Játékos";
                gameDisplay.ShowToast(toast_FeedbackMessage, $"{activePlayerName} pontot oszt...", false, 3f, GamePhase.Tipp);
                gameDisplay.ShowTimer(timerGroup, 0.5f);
                pointGiveTimer = 15f;
                isGivingPoints = true;
            }
        }
    }

    /// Multiplayer: Client-side piramis pont kiosztás indítása (RPC-ből hívva)
    public void GM_Client_StartPyramidPointGiving(bool isPiramis)
    {
        Debug.Log($"[FromRpc] [GameManager] GM_Client_StartPyramidPointGiving - isPiramis: {isPiramis}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc] [GameManager] GM_Client_StartPyramidPointGiving - isPiramis: {isPiramis}");
        
        var localPlayer = Mirror.NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (localPlayer != null && localPlayer.playerId == activePlayers[0].GetPlayerID())
        {
            // Ez a lokális játékos, elindítjuk a pont kiosztást
            Debug.Log($"[GameManager] Starting pyramid point giving for local player"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Starting pyramid point giving for local player");
            
            // UI elrejtése
            AllowPlayerToDragCard(0, false);
            skipPyramidCardButton.gameObject.SetActive(false);
            confirmPyramidCardButton.gameObject.SetActive(false);
            gameDisplay.HidePiramisButtons(pyramidButtonsGroup, 0.5f);
            confirmPyramidCardButton.GetComponent<CustomButtonForeground>().SetInteractiveState(false);
            gameDisplay.HideCurrentCard(tippCardGroup, 0.5f);
            
            DOVirtual.DelayedCall(0.5f, () =>
            {
                ShowPointGiving(isPiramis);
            });
        }
        else
        {
            // Más játékos, várakozási toast
            string activePlayerName = activePlayers[0].GetPlayerName();
            gameDisplay.ShowToast(toast_FeedbackMessage, $"{activePlayerName} pontot oszt...", false, 3f, GamePhase.Piramis);
        }
    }

    /// Multiplayer: Client-side pont osztás UI elrejtése (RPC-ből hívva)
    public void HidePointGiveMultiplayer()
    {
        Debug.Log("[GameManager] HidePointGiveMultiplayer"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] HidePointGiveMultiplayer");
        
        HidePointGive();
    }

    /// Multiplayer: Client-side pont osztás timer leállítása (RPC-ből hívva ConfirmPointGive után)
    public void GM_Client_StopPointGiveTimerMultiplayer()
    {
        Debug.Log("[GameManager] StopPointGiveTimerMultiplayer - stopping timer"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] StopPointGiveTimerMultiplayer - stopping timer");
        
        // Állítsd le a timert minden kliensen
        isGivingPoints = false;
        pointGiveTimer = 15f;
        
        // UI elrejtése
        gameDisplay.HideTimer(timerGroup, 0.25f);
    }

    /// Multiplayer: Client-side kártya lerakás timer leállítása (RPC-ből hívva Skip/Confirm után)
    public void GM_Client_StopCardDropTimerMultiplayer()
    {
        Debug.Log("[GameManager] StopCardDropTimerMultiplayer - stopping timer"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] StopCardDropTimerMultiplayer - stopping timer");
        
        // Állítsd le a timert minden kliensen
        waitingForCardDrop = false;
        cardDropTimer = 30f;
        
        // UI elrejtése
        gameDisplay.HideTimer(timerGroup, 0.25f);
    }

    /// Multiplayer: Client-side toast megjelenítése (RPC-ből hívva)
    public void GM_Client_ShowToastMultiplayer(string message, bool isLongMessage, float duration, GamePhase phase)
    {
        DebugDepth("GM_Client_ShowToastMultiplayer");
        Debug.Log($"[FromRpc] [GameManager] GM_Client_ShowToastMultiplayer - {message}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc] [GameManager] GM_Client_ShowToastMultiplayer - {message}");
        
        gameDisplay.ShowToast(toast_FeedbackMessage, message, isLongMessage, duration, phase);
    }

    /// Client-side: Tipp eredmény megjelenítése (RPC-ből hívva)
    public void GM_Client_ShowTippResult(int playerId, bool isCorrect, int cardType, int cardValue, int cardBackType, int penaltyPoints)
    {
        // UI MINDENKINEK!
        waitingForTipp = false;
        waitingForBuszTipp = false;
        gameDisplay.HideTimer(timerGroup, 0.25f);
        DisplayTippGroup(TippType.NONE);

        Debug.Log($"[GameManager] GM_Client_ShowTippResult - Player {playerId}, Correct: {isCorrect}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] GM_Client_ShowTippResult - Player {playerId}, Correct: {isCorrect}");
        
        // FONTOS: Kártya adatok beállítása MINDEN kliensnél MINDIG!
        // Ezzel biztosítjuk hogy a host is látja a helyes kártyát
        Card card = new Card((CardType)cardType, (CardBackType)cardBackType, (CardValue)cardValue);
        currentCard.SetCard(card);
        currentCard.ShowCardBack();

        Debug.Log($"[GameManager] Card data set in GM_Client_ShowTippResult: {(CardType)cardType} {(CardValue)cardValue}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Card data set in GM_Client_ShowTippResult: {(CardType)cardType} {(CardValue)cardValue}");
        
        // Kártya flip animáció (MINDEN kliensnél látható)
        currentCard.AnimateCardFlip(1f);

        // Toast megjelenítése
        string message = isCorrect ? "Helyes tipp!" : "Helytelen tipp!";
        DOVirtual.DelayedCall(0.5f, () =>
        {
            gameDisplay.ShowToast(toast_FeedbackMessage, message, false, 1.5f, GamePhase.Tipp);
        });

        // Ha helytelen, pont hozzáadása (csak a host számítja, de minden kliens látja)
        // A felső komment pontatlan, az alábbi if csak singleplayer esetén fut le.
        if (!isCorrect && !Mirror.NetworkClient.isConnected)
        {
            Debug.Log($"GM_Client_ShowTippResult - LEFUTOTT EZ AZ IF!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"GM_Client_ShowTippResult - LEFUTOTT EZ AZ IF!");
            // Singleplayer - direkt pont hozzáadás
            activePlayers[0].IncreasePlayerScore(penaltyPoints);
        }

        // Folytatás
        DOVirtual.DelayedCall(2f, () =>
        {
            // MULTIPLAYER: Mindig csak elrejtjük a kártyát, a szerver RPC fogja irányítani a folytatást
            // (ShowPointGiving vagy TippContinue)
            if (IsHostOrClients)
            {
                gameDisplay.HideCurrentCard(tippCardGroup, 0.5f);
            }
            else
            {
                // SINGLEPLAYER: Régi logika
                if (isCorrect)
                {
                    gameDisplay.HideCurrentCard(tippCardGroup, 0.5f, () => ShowPointGiving());
                }
                else
                {
                    gameDisplay.HideCurrentCard(tippCardGroup, 0.5f, () => TippContinue(2f));
                }
            }
        });
    }

    /// Client-side: Húzott kártya megjelenítése (RPC-ből hívva)
    public void ShowDrawnCard(CardType cardType, CardValue cardValue, CardBackType backType)
    {
        Debug.Log($"[GameManager] ShowDrawnCard - {cardType} {cardValue}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] ShowDrawnCard - {cardType} {cardValue}");
        
        // Card objektum létrehozása a kapott adatokból
        Card card = new Card(cardType, backType, cardValue);
        
        // currentCard beállítása
        currentCard.SetCard(card);
        currentCard.ShowCardBack();
    }

    /// Client-side: Kártya hozzáadása játékoshoz (RPC-ből hívva)
    public void GM_Client_AddCardToPlayer(int playerId, CardType cardType, CardValue cardValue, CardBackType backType)
    {
        Debug.Log($"[Client]\t[GameManager]\tGM_Client_AddCardToPlayer - Player {playerId}, {cardType} {cardValue} {backType}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client]\t[GameManager]\tGM_Client_AddCardToPlayer - Player {playerId}, {cardType} {cardValue} {backType}");
        
        // Játékos megkeresése
        Player player = activePlayers.FirstOrDefault(p => p.GetPlayerID() == playerId);
        if (player != null)
        {
            Card card = new Card(cardType, backType, cardValue);
            player.AddCardToPlayer(card);
            
            // UI frissítés
            RefreshPlayerUI();
        }
        else
        {
            Debug.LogError($"<ERROR!>\t[Client]\t[GameManager]\tGM_Client_AddCardToPlayer - Player {playerId} not found!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"<ERROR!>\t[Client]\t[GameManager]\tGM_Client_AddCardToPlayer - Player {playerId} not found!");
        }
    }

    /// Client-side: Kártya elvétele játékostól adott indexen (RPC-ből hívva)
    public void GM_Client_RemoveCardFromPlayer(int playerId, int cardSlotIndex)
    {
        //Debug.Log($"[Client]\t[GameManager]\tGM_Client_RemoveCardFromPlayer - Player {playerId}, slot {cardSlotIndex}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client]\t[GameManager]\tGM_Client_RemoveCardFromPlayer - Player {playerId}, slot {cardSlotIndex}");
        
        // Játékos megkeresése
        Player player = activePlayers.FirstOrDefault(p => p.GetPlayerID() == playerId);
        if (player != null)
        {
            // Kártya empty-re állítása
            player.ChangeCardToEmptyCardAtIndex(cardSlotIndex);
            
            // PlayerManager megkeresése és UI frissítés
            for (int i = 0; i < playerManagers.Length; i++)
            {
                if (playerManagers[i] != null && playerManagers[i].GetPlayerId() == playerId)
                {
                    playerManagers[i].ChangeCardToEmptyCard(cardSlotIndex, true);
                    break;
                }
            }
            
            // Teljes UI frissítés
            RefreshPlayerUI();
        }
        else
        {
            Debug.LogError($"<ERROR!>\t[Client]\t[GameManager]\tGM_Client_RemoveCardFromPlayer - Player {playerId} not found!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"<ERROR!>\t[Client]\t[GameManager]\tGM_Client_RemoveCardFromPlayer - Player {playerId} not found!");
        }
    }

    /// Client-side: Piramis letett kártya szám és pontok frissítése (RPC-ből hívva)
    public void GM_Client_UpdatePiramisCardCount(int placedCards, int totalPoints)
    {
        Debug.Log($"[Client]\t[GameManager]\tGM_Client_UpdatePiramisCardCount - Placed: {placedCards}, Total: {totalPoints}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client]\t[GameManager]\tGM_Client_UpdatePiramisCardCount - Placed: {placedCards}, Total: {totalPoints}");
        
        placedCardsNumber = placedCards;
        totalPointsToGive = totalPoints;
        
        // Confirm button text frissítése (ha létezik és aktív)
        if (confirmPyramidCardButton != null && confirmPyramidCardButton.gameObject.activeSelf)
        {
            TextMeshProUGUI buttonText = confirmPyramidCardButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"Kiosztás\n({totalPointsToGive} pont)";
            }
        }
        
        // Toast üzenet (csak aktuális játékosnak)
        if (IsHostOrClients)
        {
            var localPlayer = Mirror.NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
            if (localPlayer != null && localPlayer.playerId == activePlayers[0].GetPlayerID())
            {
                gameDisplay.ShowToast(toast_FeedbackMessage, $"Kártya letéve! ({placedCardsNumber} db)", false, 1.5f, GamePhase.Piramis);
            }
        }
    }

    /// Server-side: Feldolgozza a kártya lerakást (CMD RPC-ből hívva)
    public void GM_Server_ProcessCardDropOnPiramis(int playerId, int cardSlotIndex)
    {
        Debug.Log($"[Server]\t[GameManager]\tGM_Server_ProcessCardDropOnPiramis - Player {playerId}, slot {cardSlotIndex}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\t[GameManager]\tGM_Server_ProcessCardDropOnPiramis - Player {playerId}, slot {cardSlotIndex}");
        
        // Játékos és kártya adatok lekérése
        Player player = activePlayers.FirstOrDefault(p => p.GetPlayerID() == playerId);
        if (player == null)
        {
            Debug.LogError($"[Server] Player {playerId} not found!");
            return;
        }
        
        Card playerCard = player.GetPlayerCardAtIndex(cardSlotIndex);
        if (playerCard == null)
        {
            Debug.LogError($"[Server] Card at slot {cardSlotIndex} not found!");
            return;
        }
        
        // Feldolgozás
        ProcessCardDropOnPiramis(playerId, playerCard, cardSlotIndex, currentPiramisCard, currentPiramisRow);
    }

    /// Client-side: Kártya visszaadása játékosnak (RPC-ből hívva)
    public void GM_Client_ReturnCardToPlayer(int playerId, int cardSlotIndex)
    {
        Debug.Log($"[Client]\t[GameManager]\tGM_Client_ReturnCardToPlayer - Player {playerId}, slot {cardSlotIndex}"); 
        if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client]\t[GameManager]\tGM_Client_ReturnCardToPlayer - Player {playerId}, slot {cardSlotIndex}");
        
        // PlayerManager megkeresése
        for (int i = 0; i < playerManagers.Length; i++)
        {
            if (playerManagers[i] != null && playerManagers[i].GetPlayerId() == playerId)
            {
                playerManagers[i].OnCardReturnedToPlayer(cardSlotIndex);
                break;
            }
        }
    }

    /// Client-side: Játékos score frissítése (RPC-ből hívva)
    public void GM_Client_UpdatePlayerScore(int playerId, int newScore)
    {
        Debug.Log($"[GameManager] GM_Client_UpdatePlayerScore - Player {playerId} score: {newScore}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] GM_Client_UpdatePlayerScore - Player {playerId} score: {newScore}");        

        // Játékos megkeresése
        Player player = activePlayers.FirstOrDefault(p => p.GetPlayerID() == playerId);
        if (player != null)
        {
            player.SetPlayerScore(newScore);
            
            // PlayerManager automatikusan frissül a Player objektum alapján
            // (ha valós idejű binding van implementálva)
        }
    }


    // TODO -> EZEKET TÖRÖLNI!
    // DEBUG!
    //private float timerDO = 0f;
    [SerializeField] private TextMeshProUGUI timerDOText;
    // DEBUG!


    // Ez tiszta Singleplayer, Többjátékos módban soha nem fut le!
    private void TippContinue(float delay = 0f)
    {
        //if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] TippContinue() called");
        RotatePlayers();
        DOVirtual.DelayedCall(delay, () =>
        {
            currentPlayerIndex++;

            if (currentPlayerIndex >= activePlayers.Count)
            {
                currentPlayerIndex = 0;

                NextRound();
            }
            else
            {
                StartTippKor();
            }
        });
    }

    // Multiplayer: Szerver oldali TippContinue logika
    public void GM_Client_TippContinue(float delay = 0f)
    {
        //Debug.Log("[GameManager] GM_Client_TippContinue"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] GM_Client_TippContinue");
        
        // Kliensnél is ugyanaz történik mint szerveren
        RotatePlayers();
        DOVirtual.DelayedCall(delay, () =>
        {
            currentPlayerIndex++;
            // Innentől viszont már csak a szerver dönt
            if (IsMPServer && NetworkGameManager.Instance != null)
            {
                if (currentPlayerIndex >= activePlayers.Count)
                {
                    currentPlayerIndex = 0;
                    //Debug.Log("[GameManager] RpcNextRound"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] RpcNextRound");
                
                    NetworkGameManager.Instance.RpcNextRound();
                    //Debug.Log("[GameManager] In GM_Client_TippContinue() after RpcNextRound call"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] In GM_Client_TippContinue() after RpcNextRound call");
                    //DebugDepth("GM_Client_TippContinue()");
                }
                else
                {
                    //Debug.Log("[GameManager] RpcStartTippKor"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] RpcStartTippKor");

                    NetworkGameManager.Instance.RpcStartTippKor();
                }
            }
        });
    }

    #region Tipp Checks
    
    private bool CheckTipp(Player player, Card drawnCard)
    {
        TippValue tipp = player.GetTipp();

        switch (currentTippType)
        {
            case TippType.PirosVagyFekete:
                return CheckRedOrBlack(tipp, drawnCard);

            case TippType.AlattaVagyFelette:
                return CheckBelowOrAbove(tipp, drawnCard, player);

            case TippType.KozteVagySzet:
                return CheckBetweenOrApart(tipp, drawnCard, player);

            case TippType.PontosTipus:
                return CheckExactType(tipp, drawnCard);

            case TippType.PontosSzam:
                return CheckExactNumber(tipp, drawnCard);

            default:
                return false;
        }
    }

    private bool CheckRedOrBlack(TippValue tipp, Card card)
    {
        bool isRed = card.GetCardType() == CardType.SZIV || card.GetCardType() == CardType.ROMBUSZ;
        
        if (tipp == TippValue.PIROS) return isRed;
        if (tipp == TippValue.FEKETE) return !isRed;
        
        return false;
    }

    private bool CheckBelowOrAbove(TippValue tipp, Card drawnCard, Player player)
    {
        var cards = player.GetPlayerCards();
        if (cards.Count == 0) return false;

        Card firstCard = cards[0];
        int firstValue = (int)firstCard.GetCardValue();
        int drawnValue = (int)drawnCard.GetCardValue();

        if (tipp == TippValue.ALATTA) return drawnValue < firstValue;
        if (tipp == TippValue.UGYANAZ) return drawnValue == firstValue;
        if (tipp == TippValue.FELETTE) return drawnValue > firstValue;
        
        return false;
    }

    private bool CheckBetweenOrApart(TippValue tipp, Card drawnCard, Player player)
    {
        var cards = player.GetPlayerCards();
        if (cards.Count < 2) return false;
        
        int firstValue = (int)cards[0].GetCardValue();
        int secondValue = (int)cards[1].GetCardValue();
        int drawnValue = (int)drawnCard.GetCardValue();
        
        int min = Mathf.Min(firstValue, secondValue);
        int max = Mathf.Max(firstValue, secondValue);
        
        bool ketKartyaKozott = drawnValue > min && drawnValue < max;
        bool ketKartyaSzet = drawnValue < min || drawnValue > max;

        if (tipp == TippValue.KOZTE) return ketKartyaKozott;
        if (tipp == TippValue.SZET) return ketKartyaSzet;
        if (tipp == TippValue.UGYANAZ_ALSO) return drawnValue == min;
        if (tipp == TippValue.UGYANAZ_FELSO) return drawnValue == max;
        
        return false;
    }

    private bool CheckExactType(TippValue tipp, Card card)
    {
        CardType cardType = card.GetCardType();
        
        if (tipp == TippValue.LOHERE) return cardType == CardType.LOHERE;
        if (tipp == TippValue.ROMBUSZ) return cardType == CardType.ROMBUSZ;
        if (tipp == TippValue.SZIV) return cardType == CardType.SZIV;
        if (tipp == TippValue.PIKK) return cardType == CardType.PIKK;
        
        return false;
    }

    private bool CheckExactNumber(TippValue tipp, Card card)
    {
        return (int)tipp == (int)card.GetCardValue();
    }

    #endregion

    private TippType GetTippTypeForRound(int round)
    {
        return round switch
        {
            1 => TippType.PirosVagyFekete,
            2 => TippType.AlattaVagyFelette,
            3 => TippType.KozteVagySzet,
            4 => TippType.PontosTipus,
            5 => TippType.PontosSzam,
            _ => TippType.PirosVagyFekete
        };
    }
    private TippValue GetRandomTippForType(TippType tippType)
    {
        return tippType switch
        {
            TippType.PirosVagyFekete => Random.Range(0, 2) == 0 ? TippValue.PIROS : TippValue.FEKETE,
            
            TippType.AlattaVagyFelette => Random.Range(0, 3) switch
            {
                0 => TippValue.ALATTA,
                1 => TippValue.UGYANAZ,
                _ => TippValue.FELETTE
            },
            
            TippType.KozteVagySzet => Random.Range(0, 4) switch
            {
                0 => TippValue.UGYANAZ_ALSO,
                1 => TippValue.KOZTE,
                2 => TippValue.SZET,
                _ => TippValue.UGYANAZ_FELSO
            },
            
            TippType.PontosTipus => Random.Range(0, 4) switch
            {
                0 => TippValue.LOHERE,
                1 => TippValue.ROMBUSZ,
                2 => TippValue.SZIV,
                _ => TippValue.PIKK
            },
            
            TippType.PontosSzam => (TippValue)Random.Range(2, 15), // KETTO (2) - ASZ (14)
            
            _ => TippValue.PIROS
        };
    }

    #endregion

    #region Pont Osztás Management
    private void ShowPointGiving(bool isPiramis = false)
    {
        // MULTIPLAYER: Szerver már beállította a totalPointsToGive-ot az RPC előtt,
        // kliensek olvassák a SyncVar-ból. Csak singleplayer esetén állítjuk be itt.
        if (IsSingle)
        {
            if (isPiramis)
                totalPointsToGive = placedCardsNumber * currentPiramisRow;
            else
                totalPointsToGive = currentRound;
        }
        // Multiplayer: totalPointsToGive már szinkronizálva van a SyncVar-on keresztül
        
        //Debug.Log($"totalPointsToGive ({totalPointsToGive}) | isPiramis: {isPiramis} | placedCardsNumber: {placedCardsNumber} | currentPiramisRow: {currentPiramisRow} | currentRound: {currentRound}");

        System.Array.Clear(pointsToGive, 0, pointsToGive.Length);
        
        // Minden játékosnál megjelenítjük a pont osztó UI-t
        // [-] [Pont] [+]
        for (int I = 0; I < activePlayers.Count; I++)
        {
            int playerIndex = I; // FONTOS: closure miatt lokális változó!
            
            playerManagers[I].ShowPointGiving();
            
            // AddListener a [-] és [+] gombokhoz
            playerManagers[I].GetIncreaseButton().onClick.AddListener(() =>
            {
                IncreasePlayerPoints(playerIndex);
            });
            
            playerManagers[I].GetDecreaseButton().onClick.AddListener(() =>
            {
                DecreasePlayerPoints(playerIndex);
            });
        }
        
        pointGiveGroup.SetActive(true);
        gameDisplay.ShowTimer(timerGroup, 0.5f);
        pointGiveTimer = 15f;
        isGivingPoints = true;

        UpdatePointGiveUI(isPiramis);
    }

    private void HidePointGive()
    {
        isGivingPoints = false;
        pointGiveGroup.SetActive(false);
        gameDisplay.HideTimer(timerGroup, 0.25f);

        for (int I = 0; I < activePlayers.Count; I++)
        {
            // RemoveAllListeners a gomboktól
            playerManagers[I].GetIncreaseButton().onClick.RemoveAllListeners();
            playerManagers[I].GetDecreaseButton().onClick.RemoveAllListeners();
            
            playerManagers[I].HidePointGiving();
        }
    }

    public void IncreasePlayerPoints(int playerIndex)
    {
        int currentTotal = 0;
        for (int i = 0; i < activePlayers.Count; i++)
        {
            currentTotal += pointsToGive[i];
        }
        
        if (currentTotal < totalPointsToGive)
        {
            pointsToGive[playerIndex]++;
            playerManagers[playerIndex].SetPointsToGiveText(pointsToGive[playerIndex]);
            playerManagers[playerIndex].UpdatePointGiveButtons(pointsToGive[playerIndex] > 0);
            UpdatePointGiveUI();
        }
    }

    public void DecreasePlayerPoints(int playerIndex)
    {
        if (pointsToGive[playerIndex] > 0)
        {
            pointsToGive[playerIndex]--;
            playerManagers[playerIndex].SetPointsToGiveText(pointsToGive[playerIndex]);
            playerManagers[playerIndex].UpdatePointGiveButtons(pointsToGive[playerIndex] > 0);
            UpdatePointGiveUI();
        }
    }

    private void UpdatePointGiveUI(bool isPiramis = false)
    {
        if (isPiramis)
        {
            RectTransform rt = confirmPointGiveButton.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, 60f);
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 177.5f);

            ImageWithRoundedCorners roundedBG = confirmPointGiveButton.GetComponent<ImageWithRoundedCorners>();
            if (roundedBG != null)
            {
                roundedBG.radius = 30f;
                roundedBG.Refresh();
            }

            Transform foregroundTransform = confirmPointGiveButton.transform.Find("Foreground - Image");
            if (foregroundTransform != null)
            {
                ImageWithRoundedCorners roundedFG = foregroundTransform.GetComponent<ImageWithRoundedCorners>();
                if (roundedFG != null)
                {
                    roundedFG.radius = 25f;
                    roundedFG.Refresh();
                }
            }
        }

        int currentTotal = 0;
        for (int I = 0; I < activePlayers.Count; I++)
        {
            currentTotal += pointsToGive[I];
        }
        
        TextMeshProUGUI buttonText = confirmPointGiveButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = $"Pontok kiosztása ({currentTotal}/{totalPointsToGive})";
        }

        //confirmPointGiveButton.interactable = currentTotal == totalPointsToGive;
        confirmPointGiveButton.GetComponent<CustomButtonForeground>().SetInteractiveState(currentTotal == totalPointsToGive);
    }

    public void ConfirmPointGive()
    {
        Debug.Log("[GameManager] ConfirmPointGive() called"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] ConfirmPointGive() called");
        // MULTIPLAYER: Kliens küldi a pontokat a szervernek Cmd-vel
        if (IsHostOrClients)
        {
            var localPlayer = Mirror.NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
            if (localPlayer != null)
            {
                Debug.Log($"[GameManager] ConfirmPointGive() - Player \"{localPlayer.playerName}\" sending \"{totalPointsToGive}\" points to server"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] ConfirmPointGive() - Player \"{localPlayer.playerName}\" sending \"{totalPointsToGive}\" points to server");
                
                // Cmd hívás hogy szerver megkapja a pont osztást
                localPlayer.CmdConfirmPointGive(pointsToGive, currentPhase == GamePhase.Piramis);
                
                // EREDETI
                //NetworkGameManager.Instance.RpcHidePointGiveUI();
                //NetworkGameManager.Instance.RpcRefreshPlayerUI();

                if(IsMPServer)
                {
                    NetworkGameManager.Instance.RpcHidePointGiveUI();
                    NetworkGameManager.Instance.RpcRefreshPlayerUI();
                }

                //HidePointGive();
                
                return;
            }
            else
            {
                Debug.LogError("<ERROR!>\t[GameManager] ConfirmPointGive() - Local player not found!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("<ERROR!>\t[GameManager] ConfirmPointGive() - Local player not found!");
            }
        }
        else if (IsSingle)
        {
            
            // SINGLEPLAYER: Eredeti logika
            // Pontok kiosztása a játékosoknak
            for (int I = 0; I < activePlayers.Count; I++)
            {
                if (pointsToGive[I] > 0)
                {
                    activePlayers[I].IncreasePlayerScore(pointsToGive[I]);
                }
            }

            HidePointGive();
            RefreshPlayerUI();
            
            // Folytatás a fázis alapján
            if (currentPhase == GamePhase.Tipp)
            {
                gameDisplay.ShowToast(toast_FeedbackMessage, "Pontok kiosztva!", false, 2f, GamePhase.Tipp);
                DOVirtual.DelayedCall(2f, () =>
                {
                    // Singleplayer
                    TippContinue(1f);
                });
            }
            else if (currentPhase == GamePhase.Piramis)
            {
                DOVirtual.DelayedCall(1f, () =>
                {
                    NextPiramisPlayer(1f);
                });
            }
        }
    }

    /// Multiplayer: Server feldolgozza a pont osztás megerősítését
    public void GM_Server_ProcessPointGiveConfirm(int playerId, int[] pointsToGive, bool isPiramis)
    {
        Debug.Log($"[Server]\tGM_Server_ProcessPointGiveConfirm - Player {playerId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tGM_Server_ProcessPointGiveConfirm - Player {playerId}");
        
        // Pontok kiosztása MINDEN KLIENSNEK (broadcast RPC-vel)
        for (int I = 0; I < activePlayers.Count; I++)
        {
            if (pointsToGive[I] > 0)
            {
                int targetPlayerId = activePlayers[I].GetPlayerID();
                int newScore = activePlayers[I].GetPlayerScore() + pointsToGive[I];
                
                Debug.Log($"[Server]\tGiving {pointsToGive[I]} points to player {targetPlayerId}, new score: {newScore}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tGiving {pointsToGive[I]} points to player {targetPlayerId}, new score: {newScore}");
                
                // Broadcast minden kliensnek (beleértve a host-ot is!)
                if (Mirror.NetworkServer.active && NetworkGameManager.Instance != null)
                {
                    NetworkGameManager.Instance.RpcUpdatePlayerScore(targetPlayerId, newScore);
                }
            }
        }

        
        // Toast broadcast minden kliensnek
        if (IsMPServer && NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.RpcHidePointGiveUI();
            
            if (!isPiramis)
            {
                // Tipp fázis - folytatjuk a következő körrel
                NetworkGameManager.Instance.RpcShowToast("Pontok kiosztva!", false, 2f, GamePhase.Tipp);
                
                DOVirtual.DelayedCall(2f, () =>
                {
                    // Broadcast TippContinue minden kliensnek (UI/animáció)
                    NetworkGameManager.Instance.RpcTippContinue(1f);
                });
            }
            else
            {
                // Piramis fázis - következő játékos
                DOVirtual.DelayedCall(1f, () =>
                {
                    // RefreshPlayerUI broadcast minden kliensnek
                    NetworkGameManager.Instance.RpcRefreshPlayerUI();
                    
                    // Majd NextPiramisPlayer
                    NetworkGameManager.Instance.RpcNextPiramisPlayer(2f);
                });
            }
        }
    }

    private void OnPointGiveTimeout()
    {
        confirmPointGiveButton.GetComponent<CustomButtonForeground>().SetInteractiveState(false);

        if (currentPhase == GamePhase.Tipp)
        {
            // MULTIPLAYER: Server broadcastolja a toast-ot és a folytatást
            if (IsMPServer && NetworkGameManager.Instance != null)
            {
                Debug.Log("[Server]\tOnPointGiveTimeout - HidePointGiveUI|ShowToast & TippContinue"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Server]\tOnPointGiveTimeout - HidePointGiveUI|ShowToast & TippContinue");
                
                NetworkGameManager.Instance.RpcHidePointGiveUI();                                   // UI elrejtése broadcast minden kliensnek (beleértve a host-ot is!)
                NetworkGameManager.Instance.RpcShowToast("Nem osztottál ki időben pontokat!", false, 1f, GamePhase.Tipp);  // Toast broadcast minden kliensnek (beleértve a host-ot is!)

                DOVirtual.DelayedCall(1f, () =>
                {
                    NetworkGameManager.Instance.RpcTippContinue(2f);                                  // Broadcast TippContinue minden kliensnek (beleértve a host-ot is!)
                });
            }
            else if (IsSingle)
            {
                // SINGLEPLAYER: Eredeti logika
                HidePointGive();
                gameDisplay.ShowToast(toast_FeedbackMessage, "Nem osztottál ki időben pontokat!", false, 1f, GamePhase.Tipp);

                DOVirtual.DelayedCall(1f, () =>
                {
                    TippContinue(2f);
                });
            }
        }
        else if (currentPhase == GamePhase.Piramis)
        {
            // MULTIPLAYER: Server broadcastolja a toast-ot és a folytatást
            if (IsMPServer && NetworkGameManager.Instance != null)
            {
                Debug.Log("[Server]\tOnPointGiveTimeout - HidePointGiveUI|ShowToast & NextPiramisPlayer"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Server]\tOnPointGiveTimeout - HidePointGiveUI|ShowToast & NextPiramisPlayer");
                
                NetworkGameManager.Instance.RpcHidePointGiveUI();
                NetworkGameManager.Instance.RpcShowToast("Nem osztál ki időben pontokat!", false, 1f, GamePhase.Piramis);

                DOVirtual.DelayedCall(1f, () =>
                {
                    NetworkGameManager.Instance.RpcNextPiramisPlayer(2f);
                });
            }
            else if (IsSingle)
            {
                // SINGLEPLAYER: Eredeti logika
                HidePointGive();
                gameDisplay.ShowToast(toast_FeedbackMessage, "Nem osztál ki időben pontokat!", false, 1f, GamePhase.Piramis);
                DOVirtual.DelayedCall(1f, () =>
                {
                    NextPiramisPlayer(2f);
                });
            }
        }
    }

    #endregion

    #endregion


    #region Piramis Management

    private void FillPiramisWithCards()
    {
        string contentOfPiramis = "Filling Piramis with cards:\n";
        for (int row = 1; row <= 5; row++) // Row_1 - Row_5
        {
            Transform rowTransform = piramisGroup.transform.Find($"Row_{row}");
            if (rowTransform == null)
            {
                Debug.LogError($"Row_{row} not found in piramisGroup!");
                continue;
            }

            // Kártyák száma a sorban (mód szerint)
            int cardsInRow = GameVars.Instance.ReversedPyramidMode 
                ? row           // Fordított: Row_1=1, Row_2=2, ..., Row_5=5
                : (6 - row);    // Normál: Row_1=5, Row_2=4, ..., Row_5=1
            contentOfPiramis += $"\tRow_{row}: ";

            for (int cardIndex = 0; cardIndex < cardsInRow; cardIndex++)
            {
                if (cardIndex >= rowTransform.childCount)
                {
                    Debug.LogWarning($"Row_{row} has only {rowTransform.childCount} children, but expected {cardsInRow}!");
                    break;
                }

                Transform cardTransform = rowTransform.GetChild(cardIndex);
                CardManager cardManager = cardTransform.GetComponent<CardManager>();
                if (cardManager != null)
                {
                    cardManager.SetCard(deck.DrawCard());
                    cardManager.ShowCardBack();
                    contentOfPiramis += $"[{cardManager.GetCardData().GetCardBackType()} {cardManager.GetCardData().GetCardType()} {cardManager.GetCardData().GetCardValue()}] ";
                }
                else
                {
                    Debug.LogWarning($"CardManager component not found on card at Row_{row}, Index {cardIndex}");
                }
            }
            contentOfPiramis += "\n";
        }
        if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile(contentOfPiramis);
    }

    /// MULTIPLAYER: Piramis feltöltése a server által broadcastolt kártyákkal
    public void FillPyramidWithBroadcastedCards(PyramidCardData[] cards)
    {
        if (cards.Length != 15)
        {
            Debug.LogError($"Expected 15 pyramid cards, got {cards.Length}!");
            return;
        }

        string contentOfPiramis = "Filling Piramis with cards:\n";
        int cardIndex = 0;

        for (int row = 1; row <= 5; row++) // Row_1 - Row_5
        {
            Transform rowTransform = piramisGroup.transform.Find($"Row_{row}");
            if (rowTransform == null)
            {
                Debug.LogError($"Row_{row} not found in piramisGroup!");
                continue;
            }

            // Kártyák száma a sorban (mód szerint)
            int cardsInRow = GameVars.Instance.ReversedPyramidMode 
                ? row           // Fordított: Row_1=1, Row_2=2, ..., Row_5=5
                : (6 - row);    // Normál: Row_1=5, Row_2=4, ..., Row_5=1
            contentOfPiramis += $"\tRow_{row}: ";

            for (int cardInRow = 0; cardInRow < cardsInRow; cardInRow++)
            {
                if (cardInRow >= rowTransform.childCount)
                {
                    Debug.LogWarning($"Row_{row} has only {rowTransform.childCount} children, but expected {cardsInRow}!");
                    break;
                }

                Transform cardTransform = rowTransform.GetChild(cardInRow);
                CardManager cardManager = cardTransform.GetComponent<CardManager>();
                if (cardManager != null && cardIndex < cards.Length)
                {
                    PyramidCardData cardData = cards[cardIndex];
                    Card card = new Card((CardType)cardData.cardType, (CardBackType)cardData.cardBackType, (CardValue)cardData.cardValue);
                    
                    cardManager.SetCard(card);
                    cardManager.ShowCardBack();
                    contentOfPiramis += $"[{card.GetCardBackType()} {card.GetCardType()} {card.GetCardValue()}] ";
                    
                    cardIndex++;
                }
                else
                {
                    Debug.LogWarning($"CardManager component not found on card at Row_{row}, Index {cardInRow}");
                }
            }
            contentOfPiramis += "\n";
        }
        
        Debug.Log(contentOfPiramis);
        if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile(contentOfPiramis);
    }

    private void FillPiramisWithEmptyCards()
    {
        for (int row = 1; row <= 5; row++) // Row_1 - Row_5
        {
            Transform rowTransform = piramisGroup.transform.Find($"Row_{row}");
            if (rowTransform == null)
            {
                Debug.LogError($"Row_{row} not found in piramisGroup!");
                continue;
            }

            // Kártyák száma a sorban (mód szerint)
            int cardsInRow = GameVars.Instance.ReversedPyramidMode 
                ? row           // Fordított: Row_1=1, Row_2=2, ..., Row_5=5
                : (6 - row);    // Normál: Row_1=5, Row_2=4, ..., Row_5=1

            for (int cardIndex = 0; cardIndex < cardsInRow; cardIndex++)
            {
                if (cardIndex >= rowTransform.childCount)
                {
                    Debug.LogWarning($"Row_{row} has only {rowTransform.childCount} children, but expected {cardsInRow}!");
                    break;
                }

                Transform cardTransform = rowTransform.GetChild(cardIndex);
                CardManager cardManager = cardTransform.GetComponent<CardManager>();
                if (cardManager != null)
                {
                    cardManager.SetEmptyCard();
                }
                else
                {
                    Debug.LogWarning($"CardManager component not found on card at Row_{row}, Index {cardIndex}");
                }
            }
        }
    }

    // [Singleplayer + Szerver + Kliensek]
    public void FlipPyramidCard()
    {
        //HIBA ELLENŐRZÉS================================================================================

        // Row_X GameObject megkeresése
        Transform rowTransform = piramisGroup.transform.Find($"Row_{currentPiramisRow}");
        if (rowTransform == null)
        {
            Debug.LogError($"Row_{currentPiramisRow} nem található a piramisGroup-ban!");
            return;
        }

        // Card_Y GameObject megkeresése (0-indexed)
        if (currentPiramisCardIndex >= rowTransform.childCount)
        {
            Debug.LogError($"Card index {currentPiramisCardIndex} out of range! Row has {rowTransform.childCount} cards.");
            return;
        }

        //HIBA ELLENŐRZÉS VÉGE================================================================================


        Transform cardTransform = rowTransform.GetChild(currentPiramisCardIndex);
        currentPiramisCard = cardTransform.GetComponent<CardManager>();


        if (currentPiramisCard == null)
        {
            Debug.LogError($"CardManager nem található a {cardTransform.name}-n!");
            return;
        }

        if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GLOBAL] [GameManager] Flipping Piramis Card at Row \"{currentPiramisRow}\", Index \"{currentPiramisCardIndex}\", Card \"{currentPiramisCard.GetCardData().GetCardType()} {currentPiramisCard.GetCardData().GetCardValue()}\"");
        currentPiramisCard.AnimateCardFlip(1f);
    }
    
    // [Singleplayer ONLY]
    private void CheckIfPlayerCanDropCard()
    {
        CardValue piramisCardValue = currentPiramisCard.GetCardData().GetCardValue();
        Player currentPlayer = activePlayers[0];

        Debug.Log($"[GameManager] Checking if player \"{currentPlayer.GetPlayerName()}\" has matching card for Piramis card value: {piramisCardValue}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Checking if player \"{currentPlayer.GetPlayerName()}\" has matching card for Piramis card value: {piramisCardValue}");

        bool hasMatchingCard = false;
        foreach (var card in currentPlayer.GetPlayerCards())
        {
            if (card.GetCardValue() == piramisCardValue)
            {
                hasMatchingCard = true;
                break;
            }
        }

        if (hasMatchingCard)
        {
            DOVirtual.DelayedCall(1f, () =>
            {
                WaitForPlayerToDropCard();
            });
        }
        else
        {
            gameDisplay.ShowToast(toast_FeedbackMessage, "Nem tudsz kártyát letenni!", false, 1f, GamePhase.Piramis);
            
            DOVirtual.DelayedCall(1f, () =>
            {
                NextPiramisPlayer(2f);
            });
        }
    }

    // [Multiplayer ONLY - Szerver]
    public bool CheckIfMultiPlayerCanDropCard()
    {
        DebugDepth("CheckIfMultiPlayerCanDropCard()");

        // RECALL
        if (currentPiramisCard == null || currentPiramisCard.GetCardData() == null)
        {
            Debug.LogWarning($"[SV - GameManager] CheckIfMultiPlayerCanDropCard called but currentPiramisCard is null! Retrying in 0.5f...");
            if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] CheckIfMultiPlayerCanDropCard called but currentPiramisCard is null! Retrying in 0.5f...");
            
            DOVirtual.DelayedCall(0.5f, () =>
            {
                CheckIfMultiPlayerCanDropCard();
            });
            return false;
        }
        // RECALL

        CardValue piramisCardValue = currentPiramisCard.GetCardData().GetCardValue();
        Player currentPlayer = activePlayers[0];

        Debug.Log($"[GameManager] Checking if player \"{currentPlayer.GetPlayerName()}\" [ID:{currentPlayer.GetPlayerID()}] has matching card for Piramis card value: {piramisCardValue}");
        if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Checking if player \"{currentPlayer.GetPlayerName()}\" [ID:{currentPlayer.GetPlayerID()}] has matching card for Piramis card value: {piramisCardValue}");

        bool hasMatchingCard = false;
        foreach (var card in currentPlayer.GetPlayerCards())
        {
            if (card.GetCardValue() == piramisCardValue)
            {
                hasMatchingCard = true;
                break;
            }
        }

        if (hasMatchingCard)
        {
            Debug.Log($"[GameManager] Player \"{currentPlayer.GetPlayerName()}\" can drop a card. Broadcasting RpcWaitForPlayerToDropCard().");
            if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Player \"{currentPlayer.GetPlayerName()}\" can drop a card. Broadcasting RpcWaitForPlayerToDropCard().");
            
            DOVirtual.DelayedCall(1f, () =>
            {
                if (NetworkGameManager.Instance != null)
                {
                    NetworkGameManager.Instance.RpcWaitForPlayerToDropCard();
                }
            });
            
            return true;
        }
        else
        {
            Debug.Log($"[GameManager] Player \"{currentPlayer.GetPlayerName()}\" cannot drop a card. No matching card found.");
            if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Player \"{currentPlayer.GetPlayerName()}\" cannot drop a card. No matching card found.");
            
            string currentPlayerName = activePlayers[0].GetPlayerName();
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.RpcShowToast($"{currentPlayerName} nem tud kártyát letenni!", false, 1f, GamePhase.Piramis);
            }
            
            return false;
        }
    }

    // [Singleplayer ONLY]
    private void WaitForPlayerToDropCard()
    {
        cardDropTimer = 30f;
        waitingForCardDrop = true;
        gameDisplay.ShowTimer(timerGroup, 1f);
        timerText.text = "Kártya letevés: 30";

        gameDisplay.ShowToast(toast_FeedbackMessage, "Játsz ki azonos értékű kártyákat vagy hagyd ki a kört!", true, 2f, GamePhase.Piramis);

        placedCardsNumber = 0;
        AllowPlayerToDragCard(0, true);

        skipPyramidCardButton.gameObject.SetActive(true);
        confirmPyramidCardButton.gameObject.SetActive(false);

        TextMeshProUGUI buttonText = skipPyramidCardButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = $"Kártya\nkihagyása";
        }
        gameDisplay.ShowPiramisButtons(pyramidButtonsGroup, 0.5f);
        skipPyramidCardButton.GetComponent<CustomButtonForeground>().SetInteractiveState(true);
    }

    // [Multiplayer ONLY - Kliensek]
    public void WaitForMultiPlayerToDropCard()
    {
        if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GLOBAL] WaitForMultiPlayerToDropCard()");
        cardDropTimer = 30f;
        waitingForCardDrop = true;
        gameDisplay.ShowTimer(timerGroup, 1f);
        timerText.text = "Kártya letevés: 30";

        // Szerver: Letett kártyák száma 0-ra
        if (IsMPServer)
        {
            if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[SERVER] UpdatePlacedCardsNumber call in WaitForMultiPlayerToDropCard");
            NetworkGameManager.Instance.UpdatePlacedCardsNumber(0);
        }

        // Gombok és toast megjelenítése
        var localPlayer = Mirror.NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (localPlayer != null)
        {
            // Ez a local player van soron
            if (localPlayer.playerId == activePlayers[0].GetPlayerID())
            {
                Debug.Log($"\t\tYou are {localPlayer.playerName}! It's your turn to drop card!");
                if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"\t\tYou are {localPlayer.playerName}! It's your turn to drop card!");

                gameDisplay.ShowToast(toast_FeedbackMessage, "Játsz ki azonos értékű kártyákat vagy hagyd ki a kört!", true, 2f, GamePhase.Piramis);
                AllowPlayerToDragCard(0, true);

                skipPyramidCardButton.gameObject.SetActive(true);
                confirmPyramidCardButton.gameObject.SetActive(false);

                TextMeshProUGUI buttonText = skipPyramidCardButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = $"Kártya\nkihagyása";
                }
                gameDisplay.ShowPiramisButtons(pyramidButtonsGroup, 0.5f);
                skipPyramidCardButton.GetComponent<CustomButtonForeground>().SetInteractiveState(true);

                Debug.Log($"[GameManager] It's your turn to drop card, player \"{activePlayers[0].GetPlayerName()}\"");
                if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] It's your turn to drop card, player \"{activePlayers[0].GetPlayerName()}\"");
            }
            else
            {
                Debug.Log($"You are {localPlayer.playerName}! You are waiting!");
                if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"You are {localPlayer.playerName}! You are waiting!");

                string currentPlayerName = activePlayers[0].GetPlayerName();
                gameDisplay.ShowToast(toast_FeedbackMessage, $"{currentPlayerName} dob kártyát...", false, 2f, GamePhase.Piramis);
                
                Debug.Log($"[GameManager] Waiting for player \"{activePlayers[0].GetPlayerName()}\" to drop card");
                if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Waiting for player \"{activePlayers[0].GetPlayerName()}\" to drop card");
            }
        }
    }

    private void HandleCardDroppedToPiramis(int playerId, Card playerCard, int cardSlotIndex, CardManager droppedOnThisPiramisCard, int PiramisRowIndex)
    {
        Debug.Log($"[GameManager] HandleCardDroppedToPiramis - Player {playerId}, slot {cardSlotIndex}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] HandleCardDroppedToPiramis - Player {playerId}, slot {cardSlotIndex}");
        
        // MULTIPLAYER: Kliens küldi a szerverre (Command RPC), szerver feldolgozza
        if (IsMPClient && NetworkGameManager.Instance != null)
        {
            Debug.Log($"[Client] Sending card drop to server - Player {playerId}, slot {cardSlotIndex}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client] Sending card drop to server - Player {playerId}, slot {cardSlotIndex}");
            
            // Küldjük a szerverre feldolgozásra
            if (NetworkGameManager.Instance != null)
            {
                Debug.Log($"[Client] Calling CmdDropCardOnPiramis - Player {playerId}, slot {cardSlotIndex}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client] Calling CmdDropCardOnPiramis - Player {playerId}, slot {cardSlotIndex}");
                NetworkGameManager.Instance.CmdDropCardOnPiramis(playerId, cardSlotIndex);
            }
            return; // Kliens nem dolgozza fel lokálisan!
        }
        
        // SZERVER vagy SINGLEPLAYER feldolgozza:
        ProcessCardDropOnPiramis(playerId, playerCard, cardSlotIndex, droppedOnThisPiramisCard, PiramisRowIndex);
    }
    
    // [Singleplayer + Szerver feldolgozza a kártya lerakást]
    public void ProcessCardDropOnPiramis(int playerId, Card playerCard, int cardSlotIndex, CardManager droppedOnThisPiramisCard, int PiramisRowIndex)
    {
        // playerId = PlayerManager eredeti Initialize() ID-ja (0-9), NEM a jelenlegi tömb index!
        // Meg kell találni, hogy melyik PlayerManager-nek van ez a playerId-ja
        int playerManagerIndex = -1;
        for (int i = 0; i < playerManagers.Length; i++)
        {
            if (playerManagers[i] != null && playerManagers[i].GetPlayerId() == playerId)
            {
                playerManagerIndex = i;
                break;
            }
        }

        if (playerManagerIndex == -1)
        {
            Debug.LogError($"ProcessCardDropOnPiramis: PlayerManager with playerId {playerId} not found!");
            return;
        }

        if (!currentPiramisCard.CardFrontRenderer.gameObject.activeSelf)
        {
            
            
            // MULTIPLAYER: Broadcast card return
            if (IsMPServer && NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.RpcShowToast("Lefordított kártyára nem tehetsz le!", false, 2f, GamePhase.Piramis);
                NetworkGameManager.Instance.RpcReturnCardToPlayer(playerId, cardSlotIndex);
            }
            else if (IsSingle)
            {
                gameDisplay.ShowToast(toast_FeedbackMessage, "Lefordított kártyára nem tehetsz le!", false, 2f, GamePhase.Piramis);
                playerManagers[playerManagerIndex].OnCardReturnedToPlayer(cardSlotIndex);
            }
            return;
        }

        if (PiramisRowIndex != currentPiramisRow)
        {
            // MULTIPLAYER: Broadcast card return
            if (IsMPServer && NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.RpcShowToast("Rossz sorba tetted le a kártyát!", false, 2f, GamePhase.Piramis);
                NetworkGameManager.Instance.RpcReturnCardToPlayer(playerId, cardSlotIndex);
            }
            else if (IsSingle)
            {
                gameDisplay.ShowToast(toast_FeedbackMessage, "Rossz sorba tetted le a kártyát!", false, 2f, GamePhase.Piramis);
                playerManagers[playerManagerIndex].OnCardReturnedToPlayer(cardSlotIndex);
            }
            return;
        }

        if (droppedOnThisPiramisCard != currentPiramisCard)
        {
            // MULTIPLAYER: Broadcast card return
            if (IsMPServer && NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.RpcShowToast("Csak a jelenlegi piramis kártyára tehetsz le!", false, 2f, GamePhase.Piramis);
                NetworkGameManager.Instance.RpcReturnCardToPlayer(playerId, cardSlotIndex);
            }
            else if (IsSingle)
            {
                gameDisplay.ShowToast(toast_FeedbackMessage, "Csak a jelenlegi piramis kártyára tehetsz le!", false, 2f, GamePhase.Piramis);
                playerManagers[playerManagerIndex].OnCardReturnedToPlayer(cardSlotIndex);
            }
            return;
        }

        if (playerCard.GetCardValue() != currentPiramisCard.GetCardData().GetCardValue())
        {
            // MULTIPLAYER: Broadcast card return
            if (IsMPServer && NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.RpcShowToast("Hibás kártya! Azonos értékűt tehetsz csak le!", false, 2f, GamePhase.Piramis);
                NetworkGameManager.Instance.RpcReturnCardToPlayer(playerId, cardSlotIndex);
            }
            else if (IsSingle)
            {
                gameDisplay.ShowToast(toast_FeedbackMessage, "Hibás kártya! Azonos értékűt tehetsz csak le!", false, 2f, GamePhase.Piramis);
                playerManagers[playerManagerIndex].OnCardReturnedToPlayer(cardSlotIndex);
            }
            return;
        }

        placedCardsNumber++;

        Debug.Log($"[GameManager] Player {playerManagers[playerManagerIndex].GetPlayerName()} [ID:{playerId}] dropped \"{placedCardsNumber}\". matching card [{playerCard.GetCardType()} {playerCard.GetCardValue()}] from slot \"{cardSlotIndex}\" onto Piramis Row \"{currentPiramisRow}\""); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Player {playerManagers[playerManagerIndex].GetPlayerName()} [ID:{playerId}] dropped \"{placedCardsNumber}\". matching card [{playerCard.GetCardType()} {playerCard.GetCardValue()}] from slot \"{cardSlotIndex}\" onto Piramis Row \"{currentPiramisRow}\"");
        
        // MULTIPLAYER: Kártya elvétel broadcastolása minden kliensnek
        if (IsMPServer && NetworkGameManager.Instance != null)
        {
            //Debug.Log($"[Server]\tBroadcasting card removal - Player {playerId}, slot {cardSlotIndex}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tBroadcasting card removal - Player {playerId}, slot {cardSlotIndex}");
            
            NetworkGameManager.Instance.RpcRemoveCardFromPlayer(playerId, cardSlotIndex);
            
            // MULTIPLAYER: Letett kártyák száma és pontok broadcastolása
            totalPointsToGive = placedCardsNumber * currentPiramisRow;
            Debug.Log($"[GameManager] Player {playerManagers[playerManagerIndex].GetPlayerName()} will give: totalPointsToGive = (placedCardsNumber * currentPiramisRow)  |  \"{totalPointsToGive}\" =  {placedCardsNumber} * {currentPiramisRow}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Player {playerManagers[playerManagerIndex].GetPlayerName()} will give: totalPointsToGive = (placedCardsNumber * currentPiramisRow)  |  \"{totalPointsToGive}\" =  {placedCardsNumber} * {currentPiramisRow}");
            NetworkGameManager.Instance.RpcUpdatePiramisCardCount(placedCardsNumber, totalPointsToGive);
        }
        else if (IsSingle)
        {
            // SINGLEPLAYER: Direkt lokális elvétel
            activePlayers[playerManagerIndex].ChangeCardToEmptyCardAtIndex(cardSlotIndex);
            playerManagers[playerManagerIndex].ChangeCardToEmptyCard(cardSlotIndex, true);
            
            // SINGLEPLAYER: Pontok számítása
            totalPointsToGive = placedCardsNumber * currentPiramisRow;
        }
        
        AllowPlayerToDragCard(playerManagerIndex, true);

        // Singleplayer UI frissítés (multiplayer-ben az RPC végzi)
        if (IsSingle)
        {
            TextMeshProUGUI buttonText = confirmPyramidCardButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = $"Kiosztás\n({totalPointsToGive} pont)";
            }

            skipPyramidCardButton.gameObject.SetActive(false);
            confirmPyramidCardButton.gameObject.SetActive(true);
            gameDisplay.ShowPiramisButtons(pyramidButtonsGroup, 0.5f);
            confirmPyramidCardButton.GetComponent<CustomButtonForeground>().SetInteractiveState(true);

            gameDisplay.ShowToast(toast_FeedbackMessage, $"Kártya letéve! ({placedCardsNumber} db)", false, 1.5f, GamePhase.Piramis);
        }
        else if (IsMPServer && NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.RpcShowConfirmPiramisButton();
        }
    }
    
    // Nincs kód referencia, Inspectorban van beállítva.
    public void OnSkipPyramidCardButtonClicked()
    {
        // MULTIPLAYER: Kliens küldi a skip parancsot a szervernek
        // [ Mirror.NetworkClient.isConnected => Host vagy Client ]
        if (IsHostOrClients)
        {
            var localPlayer = Mirror.NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
            if (localPlayer != null)
            {
                Debug.Log("[GameManager] OnSkipPyramidCardButtonClicked - Sending skip to server"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] OnSkipPyramidCardButtonClicked - Sending skip to server");
                
                localPlayer.CmdSkipPyramidCard();
                
                // UI elrejtése lokálisan
                skipPyramidCardButton.GetComponent<CustomButtonForeground>().SetInteractiveState(false);
                AllowPlayerToDragCard(0, false);
                waitingForCardDrop = false;
                gameDisplay.HideTimer(timerGroup, 0.25f);
                gameDisplay.HidePiramisButtons(pyramidButtonsGroup, 0.5f);
                
                return;
            }
            else
            {
                Debug.LogError("[GameManager] OnSkipPyramidCardButtonClicked - localPlayer is null!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] OnSkipPyramidCardButtonClicked - localPlayer is null!");
                return;
            }
        }
        else if (IsSingle)
        {    
            // SINGLEPLAYER: Eredeti logika
            skipPyramidCardButton.GetComponent<CustomButtonForeground>().SetInteractiveState(false);
            AllowPlayerToDragCard(0, false);

            waitingForCardDrop = false;

            gameDisplay.HideTimer(timerGroup, 0.25f);
            gameDisplay.HidePiramisButtons(pyramidButtonsGroup, 0.5f);

            NextPiramisPlayer(2f);
        }
    }

    // Nincs kód referencia, Inspectorban van beállítva.
    public void OnStartPointGiveButtonClicked(string magicWord = "")
    {
        // MULTIPLAYER: Kliens küldi a parancsot a szervernek
        if (IsMPClient)
        {
            var localPlayer = Mirror.NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
            if (localPlayer != null)
            {
                Debug.Log("[GameManager] OnStartPointGiveButtonClicked - Client sending command to server"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] OnStartPointGiveButtonClicked - Client sending command to server");
                
                localPlayer.CmdConfirmPyramidCard(magicWord);
                
                // UI elrejtése lokálisan
                AllowPlayerToDragCard(0, false);
                skipPyramidCardButton.gameObject.SetActive(false);
                confirmPyramidCardButton.gameObject.SetActive(false);
                gameDisplay.HidePiramisButtons(pyramidButtonsGroup, 0.5f);
                confirmPyramidCardButton.GetComponent<CustomButtonForeground>().SetInteractiveState(false);
                gameDisplay.HideCurrentCard(tippCardGroup, 0.5f);
                
                return;
            }
            else
            {
                Debug.LogError("[GameManager] OnStartPointGiveButtonClicked - localPlayer is null!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] OnStartPointGiveButtonClicked - localPlayer is null!");
                return;
            }
        }
        
        // Timer leállítása (Host/Server vagy Singleplayer)
        if (IsMPServer && NetworkGameManager.Instance != null)
        {
            // Broadcast minden kliensnek: állítsd le a card drop timert!
            NetworkGameManager.Instance.UpdateWaitingForCardDrop(false);
            NetworkGameManager.Instance.UpdateCardDropTimer(30f);
            NetworkGameManager.Instance.RpcStopCardDropTimer();
        }
        else if (IsSingle)
        {
            waitingForCardDrop = false;
            gameDisplay.HideTimer(timerGroup, 0.25f);
        }

        // Drag letiltása
        AllowPlayerToDragCard(0, false);

        // Piramis Gombok Group elrejtése
        skipPyramidCardButton.gameObject.SetActive(false);
        confirmPyramidCardButton.gameObject.SetActive(false);
        gameDisplay.HidePiramisButtons(pyramidButtonsGroup, 0.5f);
        confirmPyramidCardButton.GetComponent<CustomButtonForeground>().SetInteractiveState(false);

        gameDisplay.HideCurrentCard(tippCardGroup, 0.5f);
        
        DOVirtual.DelayedCall(0.5f, () =>
        {
            ShowPointGiving(magicWord == "piramis");
        });
    }

    private void OnCardDropTimeout()
    {
        // MULTIPLAYER: Toast broadcast
        if (IsHostOrClients)
        {
            string currentPlayerName = activePlayers[0].GetPlayerName();
            var localPlayer = Mirror.NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
            if (localPlayer != null && localPlayer.playerId == activePlayers[0].GetPlayerID())
            {
                gameDisplay.ShowToast(toast_FeedbackMessage, $"{currentPlayerName} tettél le kártyát időben!", false, 1f, GamePhase.Piramis);
            }
            else
            {
                gameDisplay.ShowToast(toast_FeedbackMessage, $"{currentPlayerName} nem tett le kártyát időben!", false, 1f, GamePhase.Piramis);
            }
        }
        else if (IsSingle)
        {
            // Singleplayer
            gameDisplay.ShowToast(toast_FeedbackMessage, "Nem tettél le kártyát időben!", false, 1f, GamePhase.Piramis);
        }
        
        waitingForCardDrop = false;
        gameDisplay.HideTimer(timerGroup, 0.25f);

        // Drag letiltása
        AllowPlayerToDragCard(0, false);

        // MULTIPLAYER: Gombok elrejtése broadcast
        if (IsMPServer && NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.RpcHidePiramisButtons();
            NetworkGameManager.Instance.RpcHideTimer();
        }
        else if (IsSingle)
        {
            // Singleplayer
            pyramidButtonsGroup.gameObject.SetActive(false);
        }

        DOVirtual.DelayedCall(1f, () =>
        {
            // MULTIPLAYER: NextPiramisPlayer broadcast
            if (IsMPServer && NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.RpcNextPiramisPlayer(2f);
            }
            else if (IsSingle)
            {
                NextPiramisPlayer(2f);
            }
        });
    }

    // Ezt tiszta Singleplayer. Többjátékos módban soha nem fut le!
    public void NextPiramisPlayer(float delay = 0f)
    {
        RotatePlayers();

        DOVirtual.DelayedCall(delay, () =>
        {
            currentPlayerIndex++;
            
            if (currentPlayerIndex >= activePlayers.Count)
            {
                currentPlayerIndex = 0;
                NextPiramisCard();
            }
            else
            {
                CheckIfPlayerCanDropCard();
            }
        });
    }

    // EZ NECCES MERT DELAY CALL VAN KLIENSEKNÉL!
    public void GM_Client_NextPiramisPlayer(float delay = 0f)
    {
        DebugDepth("GM_Client_NextPiramisPlayer");
        Debug.Log("[FromRpc] [GameManager] GM_Client_NextPiramisPlayer called"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[FromRpc] [GameManager] GM_Client_NextPiramisPlayer called");
        
        RotatePlayers();
        
        DOVirtual.DelayedCall(delay, () =>
        {
            currentPlayerIndex++;

            Debug.Log($"[GameManager] GM_Client_NextPiramisPlayer() before Server calling RpcNextPiramisPlayer/RpcNextPiramisCard, currentPiramisCardIndex: {currentPiramisCardIndex}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] GM_Client_NextPiramisPlayer() before Server calling RpcNextPiramisPlayer/RpcNextPiramisCard, currentPiramisCardIndex: {currentPiramisCardIndex}");

            // CSAK A SZERVER DÖNT A KÖVETKEZŐ LÉPÉSRŐL!
            if (IsMPServer && NetworkGameManager.Instance != null)
            {
                if (currentPlayerIndex >= activePlayers.Count)
                {
                    currentPlayerIndex = 0;             // TODO: BroadCast mindenkinek, hogy a currentPlayerIndex 0-ra állt vissza?
                    
                    Debug.Log("[SERVER] [GameManager] Broadcasting NextPiramisCard from Server"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[SERVER] [GameManager] Broadcasting NextPiramisCard from Server");
                    NetworkGameManager.Instance.RpcNextPiramisCard(0.25f);
                }
                else
                {
                    // Szerver ellenőrzi, hogy a játékos tud-e kártyát letenni
                    bool hasMatchingCard = CheckIfMultiPlayerCanDropCard();
                    
                    // Ha NINCS kártya, akkor 1 másodperc után lépünk a következő játékosra
                    if (!hasMatchingCard)
                    {
                        DOVirtual.DelayedCall(1f, () =>
                        {
                            Debug.Log("[SERVER] [GameManager] Broadcasting NextPiramisPlayer from Server"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[SERVER] [GameManager] Broadcasting NextPiramisPlayer from Server");
                            NetworkGameManager.Instance.RpcNextPiramisPlayer(2f);
                        });
                    }
                }
            }
        });
    }

    // Ez tiszta Singleplayer, Többjátékos módban soha nem fut le!
    private void NextPiramisCard()
    {
        currentPiramisCardIndex++;

        int cardsInRow = GameVars.Instance.ReversedPyramidMode ? currentPiramisRow : (6 - currentPiramisRow);

        if (currentPiramisCardIndex >= cardsInRow)
        {
            NextRound();
        }
        else
        {
            FlipPyramidCard();

            DOVirtual.DelayedCall(0.5f, () =>
            {
                CheckIfPlayerCanDropCard();
            });
        }
    }

    public void GM_Client_NextPiramisCard()
    {
        DebugDepth("GM_Client_NextPiramisCard");

        Debug.Log($"[FromRpc] [GameManager] GM_Client_NextPiramisCard() - Current currentPiramisCardIndex: {currentPiramisCardIndex}  |  Current local {_currentPiramisCardIndex}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc] [GameManager] GM_Client_NextPiramisCard() - Current currentPiramisCardIndex: {currentPiramisCardIndex}  |  Current local {_currentPiramisCardIndex}");
        
        if (IsMPClient && NetworkGameManager.Instance != null)
        {
            // KLIENS: Olvassuk a SyncVar értékét és állítsuk be lokálisan (a SyncVar frissítés később érkezik meg, mint az RPC hívás!)
            Debug.Log($"[FromRpc] [GameManager] GM_Client_NextPiramisCard() - Client reads GetPiramisCardIndex()"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc] [GameManager] GM_Client_NextPiramisCard() - Client reads GetPiramisCardIndex()");
            _currentPiramisCardIndex = NetworkGameManager.Instance.GetPiramisCardIndex();
            /*
            //Kliens lekérése
            var localPlayer = Mirror.NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
            if (localPlayer != null)
            {
                Debug.Log($"[Client] In GM_Client_NextPiramisCard() Player \"{localPlayer.playerName}\" sending CmdRegisterClientReady()"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client] In GM_Client_NextPiramisCard() Player \"{localPlayer.playerName}\" sending CmdRegisterClientReady()");
                //localPlayer.CmdRegisterClientReady();
            }
            */
        }

        Debug.Log($"[FromRpc] [GameManager] GM_Client_NextPiramisCard() - New currentPiramisCardIndex: {currentPiramisCardIndex}  |  New local {_currentPiramisCardIndex}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc] [GameManager] GM_Client_NextPiramisCard() - New currentPiramisCardIndex: {currentPiramisCardIndex}  |  New local {_currentPiramisCardIndex}");
        int cardsInRow = GameVars.Instance.ReversedPyramidMode ? currentPiramisRow : (6 - currentPiramisRow);

        if (IsMPServer && NetworkGameManager.Instance != null)
        {
            if (currentPiramisCardIndex >= cardsInRow)
            {
                Debug.Log($"[GameManager] GM_Client_NextPiramisCard() - [SERVER] calling RpcNextRound() while currentPiramisCardIndex: \"{currentPiramisCardIndex}\""); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] GM_Client_NextPiramisCard() - [SERVER] calling RpcNextRound() while currentPiramisCardIndex: \"{currentPiramisCardIndex}\"");
                // Multiplayer: Server broadcastolja a NextRound-ot
                if (IsMPServer && NetworkGameManager.Instance != null)
                {
                    //NetworkGameManager.Instance.WaitForAllClients();
                    NetworkGameManager.Instance.RpcNextRound();
                }
            }
            else
            {
                Debug.Log($"[GameManager] GM_Client_NextPiramisCard() - [SERVER] calling RpcFlipPyramidCard() while currentPiramisCardIndex: \"{currentPiramisCardIndex}\""); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] GM_Client_NextPiramisCard() - [SERVER] calling RpcFlipPyramidCard() while currentPiramisCardIndex: \"{currentPiramisCardIndex}\"");
                NetworkGameManager.Instance.RpcFlipPyramidCard();

                DOVirtual.DelayedCall(1f, () =>
                {
                    bool hasMatchingCard = CheckIfMultiPlayerCanDropCard();
                    
                    // Ha NINCS kártya, akkor 1 másodperc után lépünk a következő játékosra (Multiplayer)
                    if (!hasMatchingCard)
                    {
                        DOVirtual.DelayedCall(1f, () =>
                        {
                            NetworkGameManager.Instance.RpcNextPiramisPlayer(2f);
                        });
                    }
                });
            }
        }
    }

    /// Multiplayer: Timer elrejtése minden kliensnél
    public void GM_Client_HideTimer()
    {
        gameDisplay.HideTimer(timerGroup, 0.25f);
    }

    /// Multiplayer: Piramis gombok elrejtése minden kliensnél
    public void GM_Client_HidePiramisButtons()
    {
        gameDisplay.HidePiramisButtons(pyramidButtonsGroup, 0.5f);
    }

    /// Multiplayer: Confirm gomb megjelenítése a soron lévő játékosnak (RPC-ből hívva)
    public void GM_Client_ShowConfirmPiramisButton()
    {
        Debug.Log($"[Client]\t[GameManager]\tGM_Client_ShowConfirmPiramisButton"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Client]\t[GameManager]\tGM_Client_ShowConfirmPiramisButton");
        
        // Csak a soron lévő játékosnál jelenjen meg
        var localPlayer = Mirror.NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
        if (localPlayer != null && localPlayer.playerId == activePlayers[0].GetPlayerID())
        {
            skipPyramidCardButton.gameObject.SetActive(false);
            confirmPyramidCardButton.gameObject.SetActive(true);
            gameDisplay.ShowPiramisButtons(pyramidButtonsGroup, 0.5f);
            confirmPyramidCardButton.GetComponent<CustomButtonForeground>().SetInteractiveState(true);
        }
    }

    private void AllowPlayerToDragCard(int playerIndex, bool enabled)
    {
        if (playerIndex < 0 || playerIndex >= playerManagers.Length)
        {
            Debug.LogError($"AllowPlayerToDragCard: Invalid playerIndex {playerIndex}!");
            return;
        }

        if (playerManagers[playerIndex] == null)
        {
            Debug.LogError($"AllowPlayerToDragCard: playerManagers[{playerIndex}] is NULL!");
            return;
        }

        if (!playerManagers[playerIndex].gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"AllowPlayerToDragCard: playerManagers[{playerIndex}] is inactive! Skipping.");
            return;
        }

        playerManagers[playerIndex].SetInteractive(enabled);
        playerManagers[playerIndex].SetCardsDraggable(enabled);
    }

    private void GivePlayersPointsAfterPyramid()
    {
        for (int I = 0; I < activePlayers.Count; I++)
        {
            int cardsLeft = activePlayers[I].GetPlayerCards().Count;
            if (cardsLeft > 0)
            {
                activePlayers[I].IncreasePlayerScore(cardsLeft);
            }
        }
        
        if (IsMPServer && NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.RpcRefreshPlayerUI();
        }
        else if (IsSingle)
        {
            RefreshPlayerUI();
        }
    }

    #endregion

    #region Busz Management

    private void FillBuszWithCards()
    {
        deck.ResetDeck();

        for (int I = 0; I < buszCards.Length; I++)
        {
            buszCards[I].SetCard(deck.DrawCard());
        }
        // DEBUG !!!!!!!
        /*
        for (int I = 0; I < buszCards.Length; I++)
        {
            if
            (
                buszCards[I].GetCardData().GetCardBackType() == CardBackType.NONE ||
                buszCards[I].GetCardData().GetCardBackType() == CardBackType.NONE ||
                buszCards[I].GetCardData().GetCardValue() == CardValue.ZERO ||
                buszCards[I].GetComponent<CardManager>().CardFrontRenderer.sprite.name == "card_back"
            )
            {
                Debug.LogError($"Busz card at index {I} has invalid card data!\n" +
                               $"\tType: {buszCards[I].GetCardData().GetCardType()}\n" +
                               $"\tValue: {buszCards[I].GetCardData().GetCardValue()}\n" +
                               $"\tBackType: {buszCards[I].GetCardData().GetCardBackType()}\n" +
                               $"\tFrontRendererSourceName: {buszCards[I].GetComponent<CardManager>().CardFrontRenderer.sprite.name}");
            }
            Debug.Log($"Busz card at index {I} initialized:\n" +
                      $"\tType: {buszCards[I].GetCardData().GetCardType()}\n" +
                      $"\tValue: {buszCards[I].GetCardData().GetCardValue()}\n" +
                      $"\tBackType: {buszCards[I].GetCardData().GetCardBackType()}\n" +
                      $"\tFrontRendererSourceName: {buszCards[I].GetComponent<CardManager>().CardFrontRenderer.sprite.name}");
        }
        */
        // DEBUG !!!!!!!
    }

    // Ez tiszta Singleplayer, Többjátékos módban soha nem fut le!
    private void ProcessBuszTipp()
    {
        // Reveal the truth
        if (currentBuszCardIndex == 4 && !buszCards[currentBuszCardIndex].CardFrontRenderer.gameObject.activeSelf)
        {
            buszCards[currentBuszCardIndex].AnimateCardFlip(1f);
            DOVirtual.DelayedCall(0.5f, () =>
            {
                currentCard.AnimateCardFlip(1f);
            });
        }
        else
        {
            currentCard.AnimateCardFlip(1f);
        }

        TippValue tipp = activePlayers[0].GetTipp();

        Card currentCardData = currentCard.GetCardData();
        Card buszCardData = buszCards[currentBuszCardIndex].GetCardData();

        bool isTippCorrect = CheckBuszTipp(tipp, currentCardData, buszCardData);

        DOVirtual.DelayedCall(0.5f, () =>
        {
            if (isTippCorrect)
            {
                gameDisplay.ShowToast(toast_FeedbackMessage, "Helyes tipp!", false, 2f, GamePhase.Busz);
            }
            else
            {
                gameDisplay.ShowToast(toast_FeedbackMessage, "Hibás tipp!", false, 2f, GamePhase.Busz);
            }

            DOVirtual.DelayedCall(2f, () =>
            {
                float delay = 0f;
                if (currentBuszCardIndex == 4)
                {
                    buszCards[currentBuszCardIndex].AnimateCardFlip(1f, () => delay = 1f);
                }
                else
                    delay = 0f;

                DOVirtual.DelayedCall(delay, () =>
                {
                    buszCards[currentBuszCardIndex].SetCard(currentCardData);

                    if (currentBuszCardIndex == 4)
                    {
                        CheckDeckForBusz();
                        buszCards[currentBuszCardIndex].SetCard(deck.DrawCard());
                    }

                    // Minden eltűnik.
                    gameDisplay.HighlightBuszCard(buszCards, currentBuszCardIndex, false);
                    gameDisplay.HideBusz(buszGroup, buszCards, 0.25f, RefreshBuszUI);
                    gameDisplay.HideCurrentCard(tippCardGroup, 0.25f);
                });
            });

            DOVirtual.DelayedCall(2.5f, () =>
            {
                // Játékos sorsáról döntünk...
                if (isTippCorrect)
                {
                    DOVirtual.DelayedCall(0.5f, () =>
                    {
                        NextRound();
                    });
                }
                else
                {
                    // Rossz tipp! Növeljük a próbálkozások számát és a pontszámát.
                    int playerId = activePlayers[0].GetPlayerID();
                    if (!ContainsPlayerBuszAttempts(playerId))
                    {
                        SetPlayerBuszAttempts(playerId, 0);
                    }
                    SetPlayerBuszAttempts(playerId, GetPlayerBuszAttempts(playerId) + 1);
                    
                    // Pontok hozzáadása a busz pozíció alapján (1-6)
                    int pointsToAdd = currentBuszCardIndex + 1;
                    activePlayers[0].IncreasePlayerScore(pointsToAdd);
                    
                    // Fog-e tudni próbálkozni még?
                    // Ellenőrzés: Játékos elérte-e a max próbálkozást?
                    int attempts = GetPlayerBuszAttempts(playerId);

                    if (attempts >= maxBuszAttempts)
                    {
                        gameDisplay.ShowToast(toast_FeedbackMessage, $"{activePlayers[0].GetPlayerName()} kiesett (10 próba)!", false, 2f, GamePhase.Busz);

                        playersOnBusz.Remove(playerId);

                        // Jelöljük, hogy túllépte a max próbálkozást
                        activePlayers[0].SetExitStatus(PlayerExitStatus.FAILED);

                        DOVirtual.DelayedCall(2.5f, () =>
                        {
                            // Elrejtjük az ÖSSZES játékost
                            gameDisplay.HidePlayers(10, playersGroup, 0.5f, () =>
                            {
                                // RemovePlayer mindent kezel: activePlayers törlés, PlayerManager inaktiválás
                                RemovePlayer(playerId);
                                
                                NextBuszPlayer(shouldRotate: false); // Kiesett, nem kell rotálni.
                            });
                        });
                    }
                    else
                    {
                        NextBuszPlayer(shouldRotate: true); // Még játékban van.
                    }
                }
            });
        });
    }

    private bool CheckBuszTipp(TippValue tipp, Card currentCardData, Card buszCardData)
    {
        int currentCardValue = (int)currentCardData.GetCardValue();
        int buszValue = (int)buszCardData.GetCardValue();
        
        if (tipp == TippValue.ALATTA) return buszValue > currentCardValue;
        if (tipp == TippValue.UGYANAZ) return buszValue == currentCardValue;
        if (tipp == TippValue.FELETTE) return buszValue < currentCardValue;
        
        return false;
    }

    private void PlayerCompletedBusz()
    {
        gameDisplay.ShowToast(toast_FeedbackMessage, $"{activePlayers[0].GetPlayerName()} leszállt a buszról!", false, 2f, GamePhase.Busz);

        int playerId = activePlayers[0].GetPlayerID();
        playersOnBusz.Remove(playerId);

        // Jelöljük, hogy sikeresen teljesítette a buszt
        activePlayers[0].SetExitStatus(PlayerExitStatus.COMPLETED);

        // Először elrejtjük az ÖSSZES játékost (még a régi számban)
        gameDisplay.HidePlayers(10, playersGroup, 0.5f, () =>
        {
            DOVirtual.DelayedCall(0.5f, () =>
            {
                // Most eltávolítjuk a játékost
                RemovePlayer(playerId);

                // Ellenőrzés: van-e még játékos a buszon?
                if (activePlayers.Count == 0 || playersOnBusz.Count == 0)
                {
                    DOVirtual.DelayedCall(1f, () =>
                    {
                        EndGame();
                    });
                }
                else
                {
                    NextBuszPlayer(shouldRotate: false); // Nem rotálunk, mert leszállt.
                }
            });
        });
    }

    // Ez tiszta Singleplayer, Többjátékos módban soha nem fut le!
    private void NextBuszPlayer(bool shouldRotate = true)
    {
        if (activePlayers.Count == 0 || playersOnBusz.Count == 0)
        {
            NextRound();
        }
        else
        {
            currentBuszCardIndex = 0;
            gameDisplay.HideCurrentCard(tippCardGroup, 0.5f, () =>
            {
                if (shouldRotate)
                {
                    RotatePlayers();
                    DOVirtual.DelayedCall(2f, () => StartBusz());
                }
                else
                {
                    // Nem rotálunk, mert az előző játékos kiesett/leszállt
                    // RefreshPlayerUI után ShowPlayers (kártyák NÉLKÜL, mert Busz fázis)
                    RefreshPlayerUI();
                    gameDisplay.ShowPlayers(activePlayers.Count, playersGroup, 1f, () =>
                    {
                        DOVirtual.DelayedCall(1f, () => StartBusz());
                    });
                }
            });
        }
    }

    private void OnBuszTimerExpired()
    {
        // MULTIPLAYER: Csak a szerver generál random tippet
        if (IsMPServer)
        {
            // Szerver logika
            int randomIndex = Random.Range(0, 3);
            TippValue randomTipp = randomIndex switch
            {
                0 => TippValue.ALATTA,
                1 => TippValue.UGYANAZ,
                _ => TippValue.FELETTE
            };
            
            int currentPlayerId = activePlayers[0].GetPlayerID();
            
            Debug.Log($"[Server]\tBusz timer expired for player {currentPlayerId}, random tipp: {randomTipp}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tBusz timer expired for player {currentPlayerId}, random tipp: {randomTipp}");

            // Toast broadcast minden kliensnek
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.RpcShowBuszTimerExpired(currentPlayerId);
            }

            // Tipp feldolgozás (multiplayer flow)
            DOVirtual.DelayedCall(1f, () =>
            {
                GM_Server_ProcessBuszTipp(currentPlayerId, randomTipp);     // [Csak Szerver hívja!]
            });
        }
        else if (IsSingle)         // [Se nem Szerver, se nem Kliens => Singleplayer]
        {
            // SINGLEPLAYER: Eredeti logika
            gameDisplay.ShowToast(toast_FeedbackMessage, "Lejárt az idő! Véletlenszerű tipp!", false, 2f, GamePhase.Busz);
            
            int randomIndex = Random.Range(0, 3);
            TippValue randomTipp = randomIndex switch
            {
                0 => TippValue.ALATTA,
                1 => TippValue.UGYANAZ,
                _ => TippValue.FELETTE
            };
            
            activePlayers[0].SetTipp(randomTipp);
            waitingForBuszTipp = false;
            gameDisplay.HideTimer(timerGroup, 0.25f);
            DisplayTippGroup(TippType.NONE);
            gameDisplay.HideBuszButtons(buszButtonsGroup, 0.25f);
            
            DOVirtual.DelayedCall(2.5f, () =>
            {
                ProcessBuszTipp();
            });
        }
    }
    
    /// Client-side: Busz timer lejárt toast megjelenítése (RPC-ből hívva)
    public void ShowBuszTimerExpired()
    {
        if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[Client]\tShowBuszTimerExpired() called on clients.");

        gameDisplay.ShowToast(toast_FeedbackMessage, "Lejárt az idő! Véletlenszerű tipp!", false, 1f, GamePhase.Busz);
        waitingForBuszTipp = false;
        gameDisplay.HideTimer(timerGroup, 0.25f);
        DisplayTippGroup(TippType.NONE);
        gameDisplay.HideBuszButtons(buszButtonsGroup, 0.25f);
    }

    public void OnGiveUpBuszClicked()
    {
        // Multiplayer esetén küldjük el a szervernek
        if (IsHostOrClients)
        {
            var networkPlayer = Mirror.NetworkClient.localPlayer?.GetComponent<NetworkPlayer>();
            if (networkPlayer != null)
            {
                Debug.Log($"[GameManager] Sending Give Up Busz to server"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[GameManager] Sending Give Up Busz to server");

                networkPlayer.GiveUpBusz();
                
                return; // Szerver fogja feldolgozni és visszaküldeni az eredményt
            }
        }

        // Singleplayer logika (eredeti)
        waitingForBuszTipp = false;
        gameDisplay.HideTimer(timerGroup, 0.25f);
        DisplayTippGroup(TippType.NONE);
        gameDisplay.HighlightBuszCard(buszCards, currentBuszCardIndex, false);
        gameDisplay.HideBusz(buszGroup, buszCards, 0.25f);
        gameDisplay.HideCurrentCard(tippCardGroup, 0.25f);
        gameDisplay.HideBuszButtons(buszButtonsGroup, 0.25f);

        int playerId = activePlayers[0].GetPlayerID();
        gameDisplay.ShowToast(toast_FeedbackMessage, $"{activePlayers[0].GetPlayerName()} feladta a buszt!", false, 2f, GamePhase.Busz);

        // Jelöljük, hogy feladta a buszt
        activePlayers[0].SetExitStatus(PlayerExitStatus.GAVE_UP);

        DOVirtual.DelayedCall(2f, () =>
        {
            playersOnBusz.Remove(playerId);
            
            // Elrejtjük az ÖSSZES játékost
            gameDisplay.HidePlayers(10, playersGroup, 0.5f, () =>
            {
                // RemovePlayer mindent kezel: activePlayers törlés, PlayerManager inaktiválás
                RemovePlayer(playerId);

                if (activePlayers.Count == 0 || playersOnBusz.Count == 0)
                {
                    DOVirtual.DelayedCall(1f, () =>
                    {
                        EndGame();
                    });
                }
                else
                {
                    NextBuszPlayer(shouldRotate: false); // NEM rotálunk, mert feladta
                }
            });
        });
    }

    private void CheckDeckForBusz()
    {
        if (deck.CardsRemaining() == 0)
        {
            gameDisplay.ShowToast(toast_FeedbackMessage, "Elfogyott a kártya a pakliból! Újrakeverés!", false, 2f, GamePhase.Busz);
            deck.ResetDeck();
            if (currentBuszCardIndex != 0)
            {
                for (int I = 0; I < currentBuszCardIndex; I++)
                {
                    Debug.Log($"Drawing back card to busz: {buszCards[I].GetCardData().GetCardBackType()} {buszCards[I].GetCardData().GetCardType()} {buszCards[I].GetCardData().GetCardValue()}");
                    buszCards[I].SetCard(deck.DrawSpecificCard(buszCards[I].GetCardData()));
                }
                for (int I = currentBuszCardIndex; I < buszCards.Length; I++)
                {
                    buszCards[I].SetCard(deck.DrawCard());
                }
            }
        }
    }
    
    private void RefreshBuszUI()
    {
        for (int I = 0; I < currentBuszCardIndex + 1; I++)
        {
            if (I == buszCards.Length -2 )
            {
                buszCards[I].ShowCardBack();
            }
            else
            {
                buszCards[I].ShowCardFront();
            }
        }
    }

    /// Multiplayer: Server feldolgozza a Busz tippet
    public void GM_Server_ProcessBuszTipp(int playerId, TippValue tippValue)
    {
        Debug.Log($"[Server]\tGM_Server_ProcessBuszTipp - Player {playerId}, tipp: {tippValue}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tGM_Server_ProcessBuszTipp - Player {playerId}, tipp: {tippValue}");
        
        //HIBA ELLENŐRZÉS ====================================================================================
        Player player = activePlayers.FirstOrDefault(p => p.GetPlayerID() == playerId);
        if (player == null)
        {
            Debug.LogError($"[Server]\tPlayer {playerId} not found!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"<ERROR!> [Server]\tPlayer {playerId} not found!");
            return;
        }

        // Ellenőrizzük a currentBuszCardIndex értékét
        if (currentBuszCardIndex < 0 || currentBuszCardIndex >= buszCards.Length)
        {
            Debug.LogError($"[Server]\tcurrentBuszCardIndex out of range: {currentBuszCardIndex} (valid: 0-{buszCards.Length - 1})"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"<ERROR!> [Server]\tcurrentBuszCardIndex out of range: {currentBuszCardIndex} (valid: 0-{buszCards.Length - 1})");
            return;
        }
        //HIBA ELLENŐRZÉS VÉGE ===============================================================================

        player.SetTipp(tippValue);

        /*
        // Timer leállítása
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.RpcStopBuszTimer();
        }*/

        // Kártya adatok
        Card currentCardData = currentCard.GetCardData();
        Card buszCardData = buszCards[currentBuszCardIndex].GetCardData();

        bool isTippCorrect = CheckBuszTipp(tippValue, currentCardData, buszCardData);

        // Broadcast eredmény
        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.RpcShowBuszTippResult
            (
                playerId, 
                isTippCorrect, 
                (int)currentCardData.GetCardType(), 
                (int)currentCardData.GetCardValue(), 
                (int)currentCardData.GetCardBackType()
            );
        }

        // Feldolgozás 3 másodperc múlva
        DOVirtual.DelayedCall(3f, () =>
        {
            // Busz kártya frissítése.
            if (NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.RpcUpdateBuszCard(currentBuszCardIndex, (int)currentCardData.GetCardType(), (int)currentCardData.GetCardValue(), (int)currentCardData.GetCardBackType());
            }
            if (currentBuszCardIndex == 4 && NetworkGameManager.Instance != null)
            {
                NetworkGameManager.Instance.ServerDrawAndBroadcastBuszCard(4);
            }

            if (isTippCorrect)
            {
                // Helyes tipp - következő kártya vagy teljesítette
                currentBuszCardIndex++;
                
                if (currentBuszCardIndex >= 6)
                {
                    // Teljesítette a buszt (mind a 6 kártya helyes volt)!
                    if (NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.RpcPlayerCompletedBusz(playerId, player.GetPlayerName());
                    }
                }
                else
                {
                    // Még van hátra kártya - új kártya húzása és broadcast
                    if (NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.ServerDrawAndBroadcastCurrentCard();
                    }
                    
                    // Következő kör indítása (ugyanaz a játékos, következő busz kártya, NO rotate)
                    DOVirtual.DelayedCall(0.5f, () =>
                    {
                        if (NetworkGameManager.Instance != null)
                        {
                            // currentBuszCardIndex már növelve van
                            NetworkGameManager.Instance.RpcNextBuszPlayer(shouldRotate: false);
                        }
                    });
                }
            }
            else
            {
                // Rossz tipp!
                if (!ContainsPlayerBuszAttempts(playerId))
                {
                    SetPlayerBuszAttempts(playerId, 0);
                }
                SetPlayerBuszAttempts(playerId, GetPlayerBuszAttempts(playerId) + 1);
                
                int pointsToAdd = currentBuszCardIndex + 1;
                player.IncreasePlayerScore(pointsToAdd);
                
                if (NetworkGameManager.Instance != null)
                {
                    NetworkGameManager.Instance.RpcUpdatePlayerScore(playerId, player.GetPlayerScore());
                }

                int attempts = GetPlayerBuszAttempts(playerId);

                if (attempts >= maxBuszAttempts)
                {
                    // Kiesett!
                    if (NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.RpcPlayerFailedBusz(playerId, player.GetPlayerName());
                    }
                }
                else
                {
                    // Következő játékos (ugyanaz, újrapróbálkozás)
                    if (NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.RpcNextBuszPlayer(shouldRotate: true);
                    }
                }
            }
        });
    }

    /// Multiplayer: Client-side Busz tipp eredmény megjelenítése
    public void GM_Client_ShowBuszTippResult(int playerId, bool isCorrect, int cardType, int cardValue, int cardBackType)
    {
        // UI MINDENKINEK!
        waitingForTipp = false;
        waitingForBuszTipp = false;
        gameDisplay.HideTimer(timerGroup, 0.25f);
        DisplayTippGroup(TippType.NONE);

        Debug.Log($"[FromRpc] [GameManager] GM_Client_ShowBuszTippResult - Player {playerId}, correct: {isCorrect}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc] [GameManager] GM_Client_ShowBuszTippResult - Player {playerId}, correct: {isCorrect}");
        
        // Kártya felfordítás animáció
        if (currentBuszCardIndex == 4 && !buszCards[currentBuszCardIndex].CardFrontRenderer.gameObject.activeSelf)
        {
            buszCards[currentBuszCardIndex].AnimateCardFlip(1f);
            DOVirtual.DelayedCall(0.5f, () =>
            {
                currentCard.AnimateCardFlip(1f);
            });
        }
        else
        {
            currentCard.AnimateCardFlip(1f);
        }

        DOVirtual.DelayedCall(0.5f, () =>
        {
            if (isCorrect)
            {
                gameDisplay.ShowToast(toast_FeedbackMessage, "Helyes tipp!", false, 2f, GamePhase.Busz);
            }
            else
            {
                gameDisplay.ShowToast(toast_FeedbackMessage, "Hibás tipp!", false, 2f, GamePhase.Busz);
            }

            DOVirtual.DelayedCall(2f, () =>
            {
                float delay = 0f;
                if (currentBuszCardIndex == 4)
                {
                    buszCards[currentBuszCardIndex].AnimateCardFlip(1f, () => delay = 1f);
                }
                else
                    delay = 0f;

                DOVirtual.DelayedCall(delay, () =>
                {
                    //Card currentCardData = new Card((CardType)cardType, (CardBackType)cardBackType, (CardValue)cardValue);
                    //buszCards[currentBuszCardIndex].SetCard(currentCardData);

                    // Az 5. kártya (index 4) frissítését a szerver kezeli és RpcUpdateBuszCard-on keresztül broadcastolja
                    // Ez a metódus csak RPC handler, soha nem fut singleplayer esetén

                    // Minden eltűnik
                    gameDisplay.HighlightBuszCard(buszCards, currentBuszCardIndex, false);
                    gameDisplay.HideBusz(buszGroup, buszCards, 0.25f, RefreshBuszUI);
                    gameDisplay.HideCurrentCard(tippCardGroup, 0.25f);
                });
            });
        });
    }

    /// Multiplayer: Client-side következő Busz játékos
    public void GM_Client_NextBuszPlayer(bool shouldRotate)
    {
        Debug.Log($"[FromRpc] [GameManager] GM_Client_NextBuszPlayer - shouldRotate: {shouldRotate}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc] [GameManager] GM_Client_NextBuszPlayer - shouldRotate: {shouldRotate}");
        
        if (activePlayers.Count == 0 || playersOnBusz.Count == 0)
        {
            DOVirtual.DelayedCall(0.5f, () =>
            {
                if (IsMPServer && NetworkGameManager.Instance != null)
                {
                    NetworkGameManager.Instance.RpcNextRound();
                }
            });
        }
        else
        {
            // Ha shouldRotate == false ÉS currentBuszCardIndex != 0 ÉS currentBuszCardIndex < 6, 
            // akkor NEM reseteljük (helyes tipp, következő kártya ugyanaz a játékos)
            // Ha currentBuszCardIndex >= 6, akkor az előző játékos teljesítette a buszt, nullázni kell!
            if (shouldRotate || currentBuszCardIndex == 0 || currentBuszCardIndex >= 6)
            {
                currentBuszCardIndex = 0;
            }
            
            gameDisplay.HideCurrentCard(tippCardGroup, 0.5f, () =>
            {
                if (shouldRotate)
                {
                    // MULTIPLAYER: RPC broadcast a játékosok rotálására
                    if (IsMPServer && NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.RpcRotatePlayers();
                    }
                    /*
                    else if (IsSingle)
                    {
                        RotatePlayers();
                    }
                    */
                    
                    DOVirtual.DelayedCall(2f, () =>
                    {
                        if (IsMPServer && NetworkGameManager.Instance != null)
                        {
                            // Új játékos - új kártya húzása
                            NetworkGameManager.Instance.ServerDrawAndBroadcastCurrentCard();
                            NetworkGameManager.Instance.RpcStartBusz();
                        }
                        /*
                        else if (IsSingle)
                        {
                            StartBusz();
                        }
                        */
                    });
                }
                else
                {
                    RefreshPlayerUI();
                    gameDisplay.ShowPlayers(activePlayers.Count, playersGroup, 1f, () =>
                    {
                        DOVirtual.DelayedCall(1f, () =>
                        {
                            // Ugyanaz a játékos - kártya már húzva a ServerDrawAndBroadcastCurrentCard()-ban (helyes tipp esetén)
                            // vagy új játékos indítás (feladás/kiesés után)
                            if (IsMPServer && NetworkGameManager.Instance != null)
                            {
                                NetworkGameManager.Instance.RpcStartBusz();
                            }
                            /*
                            else if (IsSingle)
                            {
                                StartBusz();
                            }
                            */
                        });
                    });
                }
            });
        }
    }

    /// Multiplayer: Server feldolgozza a Give Up Busz kérést
    public void GM_Server_ProcessGiveUpBusz(int playerId)
    {
        Debug.Log($"[Server]\tGM_Server_ProcessGiveUpBusz - Player {playerId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[Server]\tGM_Server_ProcessGiveUpBusz - Player {playerId}");
        
        Player player = activePlayers.FirstOrDefault(p => p.GetPlayerID() == playerId);
        if (player == null)
        {
            Debug.LogError($"[Server]\tPlayer {playerId} not found!"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"<ERROR!> [Server]\tPlayer {playerId} not found!");
            return;
        }

        player.SetExitStatus(PlayerExitStatus.GAVE_UP);
        playersOnBusz.Remove(playerId);

        if (NetworkGameManager.Instance != null)
        {
            NetworkGameManager.Instance.RpcStopBuszTimer();
            NetworkGameManager.Instance.RpcPlayerGaveUpBusz(playerId, player.GetPlayerName());
        }
    }

    /// Multiplayer: Client-side játékos feladta a buszt
    public void GM_Client_PlayerGaveUpBusz(int playerId, string playerName)
    {
        Debug.Log($"[FromRpc] [GameManager] GM_Client_PlayerGaveUpBusz - Player {playerId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc] [GameManager] GM_Client_PlayerGaveUpBusz - Player {playerId}");
        
        gameDisplay.ShowToast(toast_FeedbackMessage, $"{playerName} feladta a buszt!", false, 2f, GamePhase.Busz);

        DOVirtual.DelayedCall(2f, () =>
        {
            gameDisplay.HideTimer(timerGroup, 0.25f);
            DisplayTippGroup(TippType.NONE);
            gameDisplay.HighlightBuszCard(buszCards, currentBuszCardIndex, false);
            gameDisplay.HideBusz(buszGroup, buszCards, 0.25f);
            gameDisplay.HideCurrentCard(tippCardGroup, 0.25f);
            gameDisplay.HideBuszButtons(buszButtonsGroup, 0.25f);

            playersOnBusz.Remove(playerId);
            
            gameDisplay.HidePlayers(10, playersGroup, 0.5f, () =>
            {
                RemovePlayer(playerId);

                if (activePlayers.Count == 0 || playersOnBusz.Count == 0)
                {
                    DOVirtual.DelayedCall(1f, () =>
                    {
                        if (IsMPServer && NetworkGameManager.Instance != null)
                        {
                            // Vége a játéknak
                            EndGame();
                        }
                    });
                }
                else
                {
                    if (IsMPServer && NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.RpcNextBuszPlayer(shouldRotate: false);
                    }
                }
            });
        });
    }

    /// Multiplayer: Client-side játékos sikeresen teljesítette a buszt
    public void GM_Client_PlayerCompletedBusz(int playerId, string playerName)
    {
        Debug.Log($"[FromRpc] [GameManager] GM_Client_PlayerCompletedBusz - Player {playerId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc] [GameManager] GM_Client_PlayerCompletedBusz - Player {playerId}");
        
        gameDisplay.ShowToast(toast_FeedbackMessage, $"{playerName} leszállt a buszról!", false, 2f, GamePhase.Busz);

        Player player = activePlayers.FirstOrDefault(p => p.GetPlayerID() == playerId);
        if (player != null)
        {
            player.SetExitStatus(PlayerExitStatus.COMPLETED);
        }

        playersOnBusz.Remove(playerId);

        gameDisplay.HidePlayers(10, playersGroup, 0.5f, () =>
        {
            DOVirtual.DelayedCall(0.5f, () =>
            {
                RemovePlayer(playerId);

                if (activePlayers.Count == 0 || playersOnBusz.Count == 0)
                {
                    DOVirtual.DelayedCall(1f, () =>
                    {
                        if (IsMPServer && NetworkGameManager.Instance != null)
                        {
                            EndGame();
                        }
                    });
                }
                else
                {
                    if (IsMPServer && NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.RpcNextBuszPlayer(shouldRotate: false);
                    }
                }
            });
        });
    }

    /// Multiplayer: Client-side játékos kiesett (túl sok próbálkozás)
    public void GM_Client_PlayerFailedBusz(int playerId, string playerName)
    {
        Debug.Log($"[FromRpc] [GameManager] GM_Client_PlayerFailedBusz - Player {playerId}"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[FromRpc] [GameManager] GM_Client_PlayerFailedBusz - Player {playerId}");
        
        gameDisplay.ShowToast(toast_FeedbackMessage, $"{playerName} kiesett (10 próba)!", false, 2f, GamePhase.Busz);

        Player player = activePlayers.FirstOrDefault(p => p.GetPlayerID() == playerId);
        if (player != null)
        {
            player.SetExitStatus(PlayerExitStatus.FAILED);
        }

        playersOnBusz.Remove(playerId);

        DOVirtual.DelayedCall(2.5f, () =>
        {
            gameDisplay.HidePlayers(10, playersGroup, 0.5f, () =>
            {
                RemovePlayer(playerId);
                
                if (activePlayers.Count == 0 || playersOnBusz.Count == 0)
                {
                    DOVirtual.DelayedCall(1f, () =>
                    {
                        if (IsMPServer && NetworkGameManager.Instance != null)
                        {
                            EndGame();
                        }
                    });
                }
                else
                {
                    if (IsMPServer && NetworkGameManager.Instance != null)
                    {
                        NetworkGameManager.Instance.RpcNextBuszPlayer(shouldRotate: false);
                    }
                }
            });
        });
    }

    /// Multiplayer: Client-side Busz timer leállítása
    public void StopBuszTimerMultiplayer()
    {
        Debug.Log("[GameManager] StopBuszTimerMultiplayer - stopping timer"); if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile("[GameManager] StopBuszTimerMultiplayer - stopping timer");
        
        waitingForBuszTipp = false;
        currentTimer = 15f;
        
        gameDisplay.HideTimer(timerGroup, 0.25f);
        DisplayTippGroup(TippType.NONE);
    }

    #endregion

    #region DEBUG!

    public void AllClientsReadyToNextRound()
    {
        DOVirtual.DelayedCall(1f, () =>
        {
            NetworkGameManager.Instance.RpcNextRound();
        });
    }


    private void FillPlayersWithEmptyCards()
    {
        foreach (var player in activePlayers)
        {
            int cardCount = player.GetPlayerCards().Count;
            for (int I = 0; I < cardCount; I++)
            {
                player.ChangeCardToEmptyCardAtIndex(I);
            }
        }
    }

    private void FillPlayersWithCards()
    {
        foreach (var player in activePlayers)
        {
            for (int I = 0; I < 5; I++)
            {
                if (deck.CardsRemaining() > 0)
                {
                    player.AddCardToPlayer(deck.DrawCard());
                }
            }
        }
    }

    private void DebugDepth(string where)
    {
    #if UNITY_EDITOR
        int depth = System.Environment.StackTrace.Split('\n').Length;
        string spaces = "";
        for (int i = 0; i < depth; i++)
        {
            spaces += " ";
        }
        Debug.Log($"[DEPTH]{spaces}{where} = \"{depth}\"");
        if (debugger != null && debugger.gameObject.activeInHierarchy) debugger.AddTextToDebugFile($"[DEPTH]{spaces}{where} = \"{depth}\"");
    #endif
    }


    #endregion
}