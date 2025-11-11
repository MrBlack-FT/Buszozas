using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Nobi.UiRoundedCorners;

public class GameManager : MonoBehaviour
{
    #region Változók

    [Header("Debugger")]
    [SerializeField] private Debugger debugger;

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
    private bool isGamePaused = false;
    private List<Player> activePlayers = new List<Player>();
    private List<Player> allPlayers = new List<Player>();
    private Deck deck;
    private float currentTimer;
    private TextMeshProUGUI timerText;
    private GamePhase currentPhase;
    private TippType currentTippType;
    private CardManager currentCard;
    private int currentRound;
    private int currentPlayerIndex;
    private bool waitingForTipp = false;

    [Header("Pont osztás állapot")]
    private int[] pointsToGive = new int[10];
    private int totalPointsToGive = 0;
    private bool isGivingPoints = false;
    private float pointGiveTimer = 15f;

    [Header("Piramis állapot")]
    private int currentPiramisRow = 1;          // 1-5
    private int currentPiramisCardIndex = 0;    // Hányadik kártya a sorban
    private CardManager currentPiramisCard;     // Aktuálisan felfordított piramis kártya
    private int placedCardsNumber = 0;          // Játékos hány kártyát tett le
    private float cardDropTimer = 30f;
    private bool waitingForCardDrop = false;

    [Header("Busz állapot")]
    private Dictionary<int, int> playerBuszAttempts = new Dictionary<int, int>();   // playerId -> próbálkozások száma
    private List<int> playersOnBusz = new List<int>();                              // Még buszon lévő játékosok ID-i
    private int currentBuszCardIndex = 0;                                           // Jelenleg melyik busz kártyánál tartunk (0-5)
    private int maxBuszAttempts = 10;
    private bool waitingForBuszTipp = false;

    #endregion


    #region Unity metódusok

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

        gameDisplay.ShowStartButtons(startButtonsGroup, 1f);
        gameDisplay.ShowPlayers(activePlayers.Count, playersGroup, 1f);
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
    }


    void Update()
    {
        // Szünet menü
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ChangeIsGamePaused();
        }

        if (isGamePaused) return;


        // Tipp timer kezelés
        if (waitingForTipp && currentTimer > 0)
        {
            currentTimer -= Time.deltaTime;
            timerText.text = "Választási idő:\n" + currentTimer.ToString("F2");

            if (currentTimer <= 5f)
            {
                timerGroup.GetComponent<Image>().color = Color.red;
            }

            if (currentTimer <= 0)
            {
                OnTimerExpired();
            }
        }
        
        // Pont osztás timer kezelés
        if (isGivingPoints && pointGiveTimer > 0)
        {
            pointGiveTimer -= Time.deltaTime;
            timerText.text = "Pont kiosztása:\n" + pointGiveTimer.ToString("F2");

            if (pointGiveTimer <= 5f)
            {
                timerGroup.GetComponent<Image>().color = Color.red;
            }

            if (pointGiveTimer <= 0)
            {
                OnPointGiveTimeout();
            }
        }

        // Kártya letevés timer kezelés (Piramis)
        if (waitingForCardDrop && cardDropTimer > 0)
        {
            cardDropTimer -= Time.deltaTime;
            timerText.text = "Kártya lerakása:\n" + cardDropTimer.ToString("F2");

            if (cardDropTimer <= 5f)
            {
                timerGroup.GetComponent<Image>().color = Color.red;
            }

            if (cardDropTimer <= 0)
            {
                OnCardDropTimeout();
            }
        }

        // Busz tipp timer kezelés
        if (waitingForBuszTipp && currentTimer > 0)
        {
            currentTimer -= Time.deltaTime;
            timerText.text = "Választási idő:\n" + currentTimer.ToString("F2");

            if (currentTimer <= 5f)
            {
                timerGroup.GetComponent<Image>().color = Color.red;
            }

            if (currentTimer <= 0)
            {
                OnBuszTimerExpired();
            }
        }

        /*
        // DEBUG BEMENET
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

        if (Input.GetKeyDown(KeyCode.L))
        {
            string status = "";

            foreach (var player in activePlayers)
            {
                status += player.GetPlayerStatus() + "\n";
            }
            status += "--------------------\n";
            foreach (var player in playerBuszAttempts)
            {
                status += $"Player {player.Key} Attempts: {player.Value}\n";
            }
            Debug.Log(status);
        }

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
        }

        //timerDO += Time.deltaTime;
        //timerDOText.text = "Timer: " + timerDO.ToString("F5");
        debugger.UpdatePersistentLog("currentPhase", currentPhase.ToString());
        debugger.UpdatePersistentLog("currentRound", currentRound.ToString());
        debugger.UpdatePersistentLog("currentPiramisRow", currentPiramisRow.ToString());
        debugger.UpdatePersistentLog("currentPiramisCardIndex", currentPiramisCardIndex.ToString());
        debugger.UpdatePersistentLog("currentPlayerIndex", currentPlayerIndex.ToString());
        debugger.UpdatePersistentLog("totalPointsToGive", totalPointsToGive.ToString());
        debugger.UpdatePersistentLog("=====", "=====");
        debugger.UpdatePersistentLog("currentBuszCardIndex", currentBuszCardIndex.ToString());
        debugger.UpdatePersistentLog("currentRound", currentRound.ToString());
        debugger.UpdatePersistentLog("timer", currentTimer.ToString());
        string buszWaitForTipStr = waitingForBuszTipp ? debugger.ColoredString("TRUE", Color.green) : debugger.ColoredString("FALSE", Color.red);
        debugger.UpdatePersistentLog("waitingForBuszTipp", buszWaitForTipStr);
        */
    }

    #endregion

    #region Game State Machine

    private void InitializeGame()
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

    public void StartTippKor()
    {
        currentTippType = GetTippTypeForRound(currentRound);

        // Jelenleg húzott kártya hátuljának megjelenítése
        currentCard.SetCard(deck.DrawCard());
        currentCard.ShowCardBack();
        gameDisplay.ShowCurrentCard(tippCardGroup, 1f, () =>
        {
            // Megfelelő tipp gombok megjelenítése
            DisplayTippGroup(currentTippType);

            // Timer indítása
            StartTimer();
        });
    }

    private void StartPiramis()
    {
        FlipPyramidCard();

        DOVirtual.DelayedCall(0.5f, () =>
        {
            CheckIfPlayerCanDropCard();
        });
    }

    private void StartBusz()
    {
        // Játékos tracking inicializálás (ha még nincs benne a dictionary-ben)
        if (!playerBuszAttempts.ContainsKey(activePlayers[0].GetPlayerID()))
        {
            playerBuszAttempts[activePlayers[0].GetPlayerID()] = 0;
        }

        CheckDeckForBusz();

        currentCard.SetCard(deck.DrawCard());
        gameDisplay.ShowBusz(buszGroup, buszCards, 2f, () =>
        {
            gameDisplay.HighlightBuszCard(buszCards, currentBuszCardIndex, true);
            currentCard.ShowCardBack();
            gameDisplay.ShowCurrentCard(tippCardGroup, 1f, () =>
            {
                DisplayTippGroup(TippType.AlattaVagyFelette);

                StartTimer();
                gameDisplay.ShowTimer(timerGroup, 0.5f);
                
                int attempts = playerBuszAttempts.ContainsKey(activePlayers[0].GetPlayerID())? playerBuszAttempts[activePlayers[0].GetPlayerID()] : 0;

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
            });
        });
    }

    private void NextRound()
    {
        switch (currentPhase)
        {
            case GamePhase.Tipp:
                if (currentRound < 5)
                {
                    currentRound++;
                    StartTippKor();
                }
                else
                {
                    gameDisplay.HideCurrentCard(tippCardGroup, 1f);

                    currentRound = 0;
                    currentPiramisRow = 1;
                    currentPiramisCardIndex = 0;
                    currentPlayerIndex = 0;

                    currentCard.SetEmptyCard();
                    currentPhase = GamePhase.Piramis;
                    FillPiramisWithCards();

                    gameDisplay.ShowToast(toast_FeedbackMessage, "Tipp kör vége!", false, 2f);
                    DOVirtual.DelayedCall(3f, () =>
                    {
                        gameDisplay.ShowToast(toast_FeedbackMessage, "Kezdődjön a piramis!", false, 2f);
                        DOVirtual.DelayedCall(3f, () =>
                        {
                            gameDisplay.ShowPiramis(piramisGroup, 2f, () =>
                            {
                                StartPiramis();
                            });
                        });
                    });
                }
                break;

            case GamePhase.Piramis:
                if (currentPiramisRow < 5)
                {
                    currentPiramisRow++;
                    currentPiramisCardIndex = 0; // TODO DEBUG !!!!!!!!!!!!!!!!!!!!!!!
                    StartPiramis();
                }
                else
                {
                    GivePlayersPointsAfterPyramid();
                    gameDisplay.ShowToast(toast_FeedbackMessage, "Büntetés pontok kiosztva a piramis után!", true, 2f, GamePhase.Piramis);
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

                    tippCardGroup.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 170);

                    playersOnBusz.Clear();
                    foreach (var player in activePlayers)
                    {
                        playersOnBusz.Add(player.GetPlayerID());
                    }

                    FillBuszWithCards();

                        DOVirtual.DelayedCall(1f, () =>
                        {
                            for (int I = 0; I < activePlayers.Count; I++)
                            {
                                playerManagers[I].HideCardsGroup();
                            }

                            gameDisplay.ShowToast(toast_FeedbackMessage, "Piramisnak vége!", false, 2f, GamePhase.Tipp);
                            DOVirtual.DelayedCall(3f, () =>
                            {
                                string buszNev = GameVars.Instance.BusName;
                                gameDisplay.ShowToast(toast_FeedbackMessage, $"Felszállás a {buszNev} járatra!", false, 2f, GamePhase.Tipp);

                                DOVirtual.DelayedCall(3f, () =>
                                {
                                    gameDisplay.ShowPlayers(activePlayers.Count, playersGroup, 1f);
                                    StartBusz();
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
                        DOVirtual.DelayedCall(0.25f, () =>
                        {
                            StartBusz();
                        });
                    }
                    else
                    {
                        PlayerCompletedBusz();
                        currentBuszCardIndex = 0;
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
        gameDisplay.HideStartButtons(startButtonsGroup, 0.5f, InitializeGame);
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

    private void RefreshPlayerUI()
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

    private void FillPiramisWithCards()
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
                    cardManager.SetCard(deck.DrawCard());
                    cardManager.ShowCardBack();
                }
                else
                {
                    Debug.LogWarning($"CardManager component not found on card at Row_{row}, Index {cardIndex}");
                }
            }
        }
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

    #endregion

    #region Timer Management
    private void StartTimer()
    {
        currentTimer = GetTimerForPhase(currentPhase);

        timerText.text = "Választási idő:\n" + currentTimer.ToString("F2");
        gameDisplay.ShowTimer(timerGroup, 0.5f);

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

    private void OnTimerExpired()
    {
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

    private float GetTimerForPhase(GamePhase phase)
    {
        return phase switch
        {
            GamePhase.Tipp => 10f,
            GamePhase.Piramis => 20f,
            GamePhase.Busz => 10f,
            _ => 10f
        };
    }

    #endregion

    #region Tipp Management

    private void OnTippButtonClicked(TippValue tippValue)
    {
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

    // TODO -> EZEKET TÖRÖLNI!
    // DEBUG!
    //private float timerDO = 0f;
    [SerializeField] private TextMeshProUGUI timerDOText;
    // DEBUG!

    private void TippContinue(float delay = 0f)
    {
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
        if (isPiramis)
            totalPointsToGive = placedCardsNumber * currentPiramisRow;
        else
            totalPointsToGive = currentRound;
        
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

    private void OnPointGiveTimeout()
    {
        confirmPointGiveButton.GetComponent<CustomButtonForeground>().SetInteractiveState(false);
        HidePointGive();

        if (currentPhase == GamePhase.Tipp)
        {
            gameDisplay.ShowToast(toast_FeedbackMessage, "Nem osztottál ki időben pontokat!", false, 1f, GamePhase.Tipp);

            DOVirtual.DelayedCall(1f, () =>
            {
                TippContinue(2f);
            });
        }
        else if (currentPhase == GamePhase.Piramis)
        {
            gameDisplay.ShowToast(toast_FeedbackMessage, "Nem osztottál ki időben pontokat!", false, 1f, GamePhase.Piramis);
            DOVirtual.DelayedCall(1f, () =>
            {
                NextPiramisPlayer(2f);
            });
        }
    }

    #endregion

    #endregion

    #region Piramis Management

    private void FlipPyramidCard()
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

        currentPiramisCard.AnimateCardFlip(1f);
    }
    
    private void CheckIfPlayerCanDropCard()
    {
        CardValue piramisCardValue = currentPiramisCard.GetCardData().GetCardValue();
        Player currentPlayer = activePlayers[0]; // Mindig az aktuális játékost ellenőrizzük.

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

    private void WaitForPlayerToDropCard()
    {
        gameDisplay.ShowToast(toast_FeedbackMessage, "Játsz ki azonos értékű kártyákat vagy hagyd ki a kört!", true, 2f, GamePhase.Piramis);

        placedCardsNumber = 0;
        
        cardDropTimer = 30f;
        waitingForCardDrop = true;
        gameDisplay.ShowTimer(timerGroup, 1f);
        timerText.text = "Kártya letevés: 30";

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

    private void HandleCardDroppedToPiramis(int playerId, Card playerCard, int cardSlotIndex, CardManager droppedOnThisPiramisCard, int PiramisRowIndex)
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
            Debug.LogError($"HandleCardDroppedToPiramis: PlayerManager with playerId {playerId} not found!");
            return;
        }

        if (!currentPiramisCard.CardFrontRenderer.gameObject.activeSelf)
        {
            gameDisplay.ShowToast(toast_FeedbackMessage, "Lefordított kártyára nem tehetsz le!", false, 2f, GamePhase.Piramis);
            playerManagers[playerManagerIndex].OnCardReturnedToPlayer(cardSlotIndex);
            return;
        }

        if (PiramisRowIndex != currentPiramisRow)
        {
            gameDisplay.ShowToast(toast_FeedbackMessage, "Rossz sorba tetted le a kártyát!", false, 2f, GamePhase.Piramis);
            playerManagers[playerManagerIndex].OnCardReturnedToPlayer(cardSlotIndex);
            return;
        }

        if (droppedOnThisPiramisCard != currentPiramisCard)
        {
            gameDisplay.ShowToast(toast_FeedbackMessage, "Csak a jelenlegi piramis kártyára tehetsz le!", false, 2f, GamePhase.Piramis);
            playerManagers[playerManagerIndex].OnCardReturnedToPlayer(cardSlotIndex);
            return;
        }

        if (playerCard.GetCardValue() != currentPiramisCard.GetCardData().GetCardValue())
        {
            gameDisplay.ShowToast(toast_FeedbackMessage, "Hibás kártya! Azonos értékűt tehetsz csak le!", false, 2f, GamePhase.Piramis);
            playerManagers[playerManagerIndex].OnCardReturnedToPlayer(cardSlotIndex);
            return;
        }

        placedCardsNumber++;
        
        activePlayers[playerManagerIndex].ChangeCardToEmptyCardAtIndex(cardSlotIndex);

        playerManagers[playerManagerIndex].ChangeCardToEmptyCard(cardSlotIndex, true);
        
        AllowPlayerToDragCard(playerManagerIndex, true);

        //int pointsToDistribute = placedCardsNumber * currentPiramisRow; // multiplier = currentPiramisRow
        totalPointsToGive = placedCardsNumber * currentPiramisRow; // multiplier = currentPiramisRow

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
    
    public void OnSkipPyramidCardButtonClicked()
    {
        skipPyramidCardButton.GetComponent<CustomButtonForeground>().SetInteractiveState(false);
        AllowPlayerToDragCard(0, false);

        waitingForCardDrop = false;

        gameDisplay.HideTimer(timerGroup, 0.25f);
        gameDisplay.HidePiramisButtons(pyramidButtonsGroup, 0.5f);

        NextPiramisPlayer(2f);
    }

    public void OnStartPointGiveButtonClicked(string magicWord = "")
    {
        // Timer leállítása
        waitingForCardDrop = false;
        gameDisplay.HideTimer(timerGroup, 0.25f);

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
        gameDisplay.ShowToast(toast_FeedbackMessage, "Nem tettél le kártyát időben!", false, 1f, GamePhase.Piramis);
        
        waitingForCardDrop = false;
        gameDisplay.HideTimer(timerGroup, 0.25f);

        // Drag letiltása
        AllowPlayerToDragCard(0, false);

        // Start Kiosztás Group elrejtése
        pyramidButtonsGroup.gameObject.SetActive(false);

        DOVirtual.DelayedCall(1f, () =>
        {
            NextPiramisPlayer(2f);
        });
    }

    private void NextPiramisPlayer(float delay = 0f)
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
        
        RefreshPlayerUI();
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
                    if (currentBuszCardIndex == 4)
                    {
                        CheckDeckForBusz();
                        buszCards[currentBuszCardIndex].SetCard(deck.DrawCard());
                    }

                    // Minden eltűnik.
                    gameDisplay.HighlightBuszCard(buszCards, currentBuszCardIndex, false);
                    gameDisplay.HideBusz(buszGroup, buszCards, 0.25f, RefreshBuszUI);
                    gameDisplay.HideCurrentCard(tippCardGroup, 0.25f);

                    buszCards[currentBuszCardIndex].SetCard(currentCardData);
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
                    if (!playerBuszAttempts.ContainsKey(playerId))
                    {
                        playerBuszAttempts[playerId] = 0;
                    }
                    playerBuszAttempts[playerId]++;
                    
                    // Pontok hozzáadása a busz pozíció alapján (1-6)
                    int pointsToAdd = currentBuszCardIndex + 1;
                    activePlayers[0].IncreasePlayerScore(pointsToAdd);
                    
                    // Fog-e tudni próbálkozni még?
                    // Ellenőrzés: Játékos elérte-e a max próbálkozást?
                    int attempts = playerBuszAttempts[playerId];

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

    public void OnGiveUpBuszClicked()
    {
        // Játékos feladja a buszt
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

    #endregion

    #region DEBUG!

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

    #endregion
}