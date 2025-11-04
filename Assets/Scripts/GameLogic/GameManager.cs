using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Linq;
using Nobi.UiRoundedCorners;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    #region Változók

    [Header("Debugger")]
    [SerializeField] private Debugger debugger;

    [Header("Megjelenítés")]
    [SerializeField] private GameDisplay gameDisplay;

    [Header("Panelek")]
    [SerializeField] private Canvas PauseMenuCanvas;
    [SerializeField] private GameObject PauseMenuPanel;
    [SerializeField] private GameObject PauseOptionsPanel;
    [SerializeField] private GameObject PauseExitPanel;

    [Header("Játékosok")]
    [SerializeField] private GameObject playersGroup;
    [SerializeField] private PlayerManager[] playerManagers = new PlayerManager[10];
    private List<Player> activePlayers = new List<Player>();

    [Space(10)]
    [Header("Játék események")]
    [SerializeField] private GameEvents gameEvents;

    [Header("UI referenciák")]
    [SerializeField] private GameObject startButtons;
    [SerializeField] private GameObject timerGroup;
    [SerializeField] private GameObject toast_FeedbackMessage;
    private TextMeshProUGUI timerText;

    [Header("Tipp gombok csoportjai")]
    [SerializeField] private GameObject tippGroupsGroup;
    [SerializeField] private GameObject redOrBlackGroup;
    [SerializeField] private GameObject belowOrAboveGroup;
    [SerializeField] private GameObject betweenOrApartGroup;
    [SerializeField] private GameObject exactColorGroup;
    [SerializeField] private GameObject exactNumberGroup;

    [Header("Piramis")]
    [SerializeField] private GameObject piramisGroup;

    [Header("Jelenleg húzott kártya")]
    [SerializeField] private GameObject tippCardTitle;
    [SerializeField] private GameObject tippCardGroup;
    private CardManager currentCard;

    [Header("Pont osztás UI")]
    [SerializeField] private GameObject pointGiveGroup;
    [SerializeField] private Button confirmPointGiveButton;

    [Header("Piramis UI")]
    [SerializeField] private GameObject pyramidButtonsGroup;
    [SerializeField] private Button skipPyramidCardButton;
    [SerializeField] private Button confirmPyramidCardButton;

    [Header("Játék állapot")]
    private bool isGamePaused = false;
    private GamePhase currentPhase;
    private TippType currentTippType;
    private int currentRound;
    private bool waitingForTipp = false;
    private float currentTimer;
    private int currentPlayerIndex;         // Jelenleg hányadik játékos van soron.

    [Header("Pont osztás állapot")]
    private int[] pointsToGive = new int[10];
    private int totalPointsToGive = 0;
    private bool isGivingPoints = false;
    private float pointGiveTimer = 15f;

    [Header("Piramis állapot")]
    private int currentPiramisRow = 1;          // 1-5
    private int currentPiramisCardIndex = 0;    // Hányadik kártya a sorban
    private CardManager currentPiramisCard;     // Aktuálisan felfordított piramis kártya
    private int placedCardsNumber = 0;         // Játékos hány kártyát tett le
    private bool waitingForCardDrop = false;
    private float cardDropTimer = 30f;

    [Header("Játék adatok")]
    private Deck deck;

    #endregion


    #region Unity metódusok

    void Awake()
    {
        // EZ CSAK TESZT CÉLJÁBÓL VAN! EZT KÉSŐBB KOMMENTELNI KELL!
        
        
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
            GameVars.Instance.ReversedPyramidMode = false;
        }
        

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        // GameEvents inicializálása
        gameEvents = GetComponent<GameEvents>();
        if (gameEvents == null)
        {
            gameEvents = gameObject.AddComponent<GameEvents>();
        }

        gameDisplay.ShowStartButtons(startButtons, 1f);
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

        for (int I = 0; I < playerManagers.Length; I++)
        {
            if (I < GameVars.Instance.NumberOfPlayersInGame)
            {
                playerManagers[I].gameObject.SetActive(true);
                playerManagers[I].Initialize(I);
                activePlayers.Add(new Player(I, GameVars.Instance.GetPlayerName(I)));
                
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
            Debug.Log("!!!!!GameManager: Setting up Reversed Pyramid Mode.");
            SetupReversedPyramid();
        }
    }


    void Update()
    {
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
            Debug.Log(status);
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            currentCard.SetEmptyCard();
            FillPlayersWithCards();
            FillPiramisWithCards();
            DisplayTippGroup(TippType.NONE);
            RefreshPlayerUI();
            StartPiramis();
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            gameDisplay.HidePlayers(10, playersGroup, 0.5f);
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            gameDisplay.ShowPlayers(10, playersGroup, 0.5f);
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            Debug.Log(debugDOmessage);
        }

        timerDO += Time.deltaTime;
        timerDOText.text = "Timer: " + timerDO.ToString("F5");
        debugger.UpdatePersistentLog("currentPhase", currentPhase.ToString());
        debugger.UpdatePersistentLog("currentRound", currentRound.ToString());
        debugger.UpdatePersistentLog("currentPiramisRow", currentPiramisRow.ToString());
        debugger.UpdatePersistentLog("currentPiramisCardIndex", currentPiramisCardIndex.ToString());
        debugger.UpdatePersistentLog("currentPlayerIndex", currentPlayerIndex.ToString());
        debugger.UpdatePersistentLog("totalPointsToGive", totalPointsToGive.ToString());
    }

    #endregion

    #region Game State Machine

    private void InitializeGame()
    {
        /*
        currentPhase = GamePhase.Tipp;
        currentRound = 1;
        currentPlayerIndex = 0;

        gameDisplay.ShowToast(toast_FeedbackMessage, "Kezdődjön a tipp kör!", 2f, GamePhase.Tipp);

        DOVirtual.DelayedCall(3f, () =>
        {
            StartTippKor();
        });
        */
        

        
        currentPhase = GamePhase.Tipp;
        currentRound = 6;
        currentPlayerIndex = 0;
        FillPlayersWithCards();
        RefreshPlayerUI();
        NextRound();
        
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
        debugDOmessage += $"StartPiramis() -> ";
        Debug.Log($"Starting Piramis! Current Round: {currentRound}");

        //TODO DEBUG - Ez ki lett kommentelve
        // Nem a StartPiramis hívódik meg, nincs benne a körbe járásban!
        
        //currentPiramisRow = 1;
        //currentPiramisCardIndex = 0;
        //currentPlayerIndex = 0;

        // Piramis megjelenítése és (első) kártya felfordítása
        //gameDisplay.ShowPiramis(piramisGroup, 2f, () =>
        //{
            Debug.Log("Piramis displayed, flipping first card.");
            FlipPyramidCard();


            // Ez pedig a FlipPyramidCard-ban van alapból!
            // Viszont ha a StartPiramis a körbe járásba kerül, akkor viszont a gameDisplay.ShowPiramis-t nem szabad meghívni
            // mert folyamatosan fog megjelenni minden egyes sor után!
            // Szóval FlipPyramidCard callback és utána CheckIfPlayerCanDropCard?
            DOVirtual.DelayedCall(0.5f, () =>
            {
                //Debug.Log($"0.5 seconds delay over after flipping card. Calling CheckIfPlayerCanDropCard();");
                CheckIfPlayerCanDropCard();
            });
        //});
    }

    private void StartBusz()
    {
        Debug.Log("Starting Bus");
        currentRound = 1;
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
                    currentRound = 0;

                    gameDisplay.HideCurrentCard(tippCardGroup, 1f);

                    currentCard.SetEmptyCard();
                    currentPhase = GamePhase.Piramis;
                    FillPiramisWithCards();

                    gameDisplay.ShowToast(toast_FeedbackMessage, "Tipp kör vége!", 2f);
                    DOVirtual.DelayedCall(3f, () =>
                    {
                        gameDisplay.ShowToast(toast_FeedbackMessage, "Kezdődjön a piramis!", 2f);
                        DOVirtual.DelayedCall(3f, () =>
                        {
                            currentPiramisRow = 1;
                            currentPiramisCardIndex = 0;
                            currentPlayerIndex = 0;
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
                    currentRound = 0;

                    gameDisplay.HidePiramis(piramisGroup, 1f);
                    gameDisplay.HidePlayers(activePlayers.Count, playersGroup, 1f);

                    FillPiramisWithEmptyCards();
                    currentPhase = GamePhase.Busz;
                    //FillBuszWithCards();      // TODO!

                    DOVirtual.DelayedCall(1f, () =>
                    {
                        gameDisplay.ShowToast(toast_FeedbackMessage, "Piramisnak vége!", 2f, GamePhase.Tipp);
                        DOVirtual.DelayedCall(3f, () =>
                        {
                            // GameVars-ból a busz nevének lekérése
                            string buszNev = GameVars.Instance.BusName;
                            gameDisplay.ShowToast(toast_FeedbackMessage, $"Felszállás a {buszNev} járatra!", 2f, GamePhase.Tipp);
                            DOVirtual.DelayedCall(3f, () =>
                            {
                                StartBusz();
                            });
                        });
                    });
                }
                break;

            case GamePhase.Busz:
                if (currentRound < 6)
                {
                    currentRound++;
                    StartBusz();
                }
                else
                {
                    currentPhase = GamePhase.JatekVege;
                    EndGame();
                }
                break;
        }
    }

    private void EndGame()
    {
        string EndGameMessage = "A Busznak vége!\nEredmények:\n";
        //EndGameMessage += playerManager.FinalScore();
        Debug.Log(EndGameMessage);
    }

    #endregion

    #region Metódusok

    public void ChangeIsGamePaused()
    {
        if (isGamePaused)
        {
            PauseMenuCanvas.gameObject.SetActive(false);
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
        gameDisplay.HideStartButtons(startButtons, 0.5f, InitializeGame);
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

            DOVirtual.DelayedCall(delay, () => gameDisplay.ShowPlayers(activePlayers.Count, playersGroup, 1f));
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
        for (int I = 0; I < activePlayers.Count; I++)
        {
            playerManagers[I].SetPlayerData(activePlayers[I]);
            //playerManagers[I].SetInteractive(I == 0);
        }
    }

    public void RemovePlayer(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= activePlayers.Count)
        {
            Debug.LogWarning($"Invalid player index: {playerIndex}");
            return;
        }
        activePlayers[playerIndex].ClearPlayerCards();
        activePlayers.RemoveAt(playerIndex);
        RefreshPlayerUI();

        for (int I = activePlayers.Count; I < playerManagers.Length; I++)
        {
            playerManagers[I].gameObject.SetActive(false);
        }

        if (playerIndex == 0)
        {
            Debug.LogWarning("Host disconnected! Returning to menu...");
            // TODO: SceneManager.LoadScene("MainMenu");
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

        waitingForTipp = true;
    }

    private void OnTimerExpired()
    {
        gameDisplay.ShowToast(toast_FeedbackMessage, "Lejárt az idő! Véletlenszerű tippet kapsz!", 2f, currentPhase);
        TippValue randomTipp = GetRandomTippForType(currentTippType);

        activePlayers[0].SetTipp(randomTipp);
        waitingForTipp = false;
        gameDisplay.HideTimer(timerGroup, 0.25f);
        DisplayTippGroup(TippType.NONE);

        DOVirtual.DelayedCall(2f, () =>
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
        gameDisplay.HideTimer(timerGroup, 0.25f);
        DisplayTippGroup(TippType.NONE);
        ProcessTipp();
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
                gameDisplay.ShowToast(toast_FeedbackMessage, "Helyes tipp!", 1.5f, GamePhase.Tipp);
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
                gameDisplay.ShowToast(toast_FeedbackMessage, "Helytelen tipp!", 1.5f, GamePhase.Tipp);
            });
            DOVirtual.DelayedCall(2f, () =>
            {
                gameDisplay.HideCurrentCard(tippCardGroup, 0.5f, () => TippContinue(2f));
            });
        }
    }

    // TODO -> EZEKET TÖRÖLNI!
    // DEBUG!
    private float timerDO = 0f;
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

    string debugDOmessage = "";

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
        //totalPointsToGive = currentRound;
        totalPointsToGive = currentPiramisRow;
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
            gameDisplay.ShowToast(toast_FeedbackMessage, "Pontok kiosztva!", 2f, GamePhase.Tipp);
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
            gameDisplay.ShowToast(toast_FeedbackMessage, "Nem osztottál ki időben pontokat!", 2f, GamePhase.Tipp);

            DOVirtual.DelayedCall(2f, () =>
            {
                TippContinue(2f);
            });
        }
        else if (currentPhase == GamePhase.Piramis)
        {
            gameDisplay.ShowToast(toast_FeedbackMessage, "Nem osztottál ki időben pontokat!", 2f, GamePhase.Piramis);
            DOVirtual.DelayedCall(2f, () =>
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
        //Debug.Log($"Felfordítás: Row_{currentPiramisRow}, Card {currentPiramisCardIndex + 1}/{cardsInRow}");

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

        Transform cardTransform = rowTransform.GetChild(currentPiramisCardIndex);
        currentPiramisCard = cardTransform.GetComponent<CardManager>();

        if (currentPiramisCard == null)
        {
            Debug.LogError($"CardManager nem található a {cardTransform.name}-n!");
            return;
        }

        //HIBA ELLENŐRZÉS VÉGE================================================================================

        debugDOmessage += "FlipPyramidCard() -> ";

        timerDO = 0f;
        currentPiramisCard.AnimateCardFlip(1f);     // NEKI IS VAN onComplete!

        /*
        DOVirtual.DelayedCall(0.5f, () =>
        {
            //Debug.Log($"0.5 seconds delay over after flipping card. Calling CheckIfPlayerCanDropCard();");
            CheckIfPlayerCanDropCard();
        });
        */
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
                debugDOmessage += "CheckIfPlayerCanDropCard() = [YES] => ";
                WaitForPlayerToDropCard();
            });
        }
        else
        {
            gameDisplay.ShowToast(toast_FeedbackMessage, "Nem tudsz kártyát letenni!", 2f, GamePhase.Piramis);
            DOVirtual.DelayedCall(2f, () =>
            {
                debugDOmessage += "CheckIfPlayerCanDropCard() = [NO ] => ";
                NextPiramisPlayer(2f);
            });
        }
    }

    private void WaitForPlayerToDropCard()
    {
        debugDOmessage += "WaitForPlayerToDropCard() -> ";
        gameDisplay.ShowToast(toast_FeedbackMessage, "Játsz ki azonos értékű kártyákat vagy hagyd ki a kört!", 2f, GamePhase.Piramis);

        placedCardsNumber = 0;
        
        cardDropTimer = 30f;
        waitingForCardDrop = true;
        gameDisplay.ShowTimer(timerGroup, 1f);
        timerText.text = "Kártya letevés: 30";

        /*
        // DEBUG
        string debugTxt = "activePlayers: ";
        foreach (var player in activePlayers)
        {
            debugTxt += player.GetPlayerName() + " | ";
        }
        debugTxt += "\n";
        for (int I = 0; I < playerManagers.Length; I++)
        {
            debugTxt += playerManagers[I] + " | ";
        }
        Debug.Log($"Allow drag for player: \"{activePlayers[0].GetPlayerName()}\" \n " +
                  $"currentPlayerIndex: {currentPlayerIndex}\n" +
                  $"{debugTxt}");
        // DEBUG
        */
        
        //playerManagers[0].SaveCardsOriginalTransforms();
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

        Debug.Log($"\"{activePlayers[0].GetPlayerName()}\" letett egy kártyát a piramisra: |{playerCard.GetCardType()} {playerCard.GetCardValue()}|");
        Debug.Log($"HandleCardDroppedToPiramis -> playerId: \"{playerId}\", playerManagerIndex: \"{playerManagerIndex}\"");

        if (!currentPiramisCard.CardFrontRenderer.gameObject.activeSelf)
        {
            gameDisplay.ShowToast(toast_FeedbackMessage, "Lefordított kártyára nem tehetsz le!", 2f, GamePhase.Piramis);
            playerManagers[playerManagerIndex].OnCardReturnedToPlayer(cardSlotIndex);
            return;
        }

        if (PiramisRowIndex != currentPiramisRow)
        {
            gameDisplay.ShowToast(toast_FeedbackMessage, "Rossz sorba tetted le a kártyát!", 2f, GamePhase.Piramis);
            Debug.Log($"PiramisRowIndex: {PiramisRowIndex}, currentPiramisRow: {currentPiramisRow}");
            playerManagers[playerManagerIndex].OnCardReturnedToPlayer(cardSlotIndex);
            return;
        }

        if (droppedOnThisPiramisCard != currentPiramisCard)
        {
            gameDisplay.ShowToast(toast_FeedbackMessage, "Csak a jelenlegi piramis kártyára tehetsz le!", 2f, GamePhase.Piramis);
            playerManagers[playerManagerIndex].OnCardReturnedToPlayer(cardSlotIndex);
            return;
        }

        if (playerCard.GetCardValue() != currentPiramisCard.GetCardData().GetCardValue())
        {
            gameDisplay.ShowToast(toast_FeedbackMessage, "Hibás kártya! Azonos értékűt tehetsz csak le!", 2f, GamePhase.Piramis);
            playerManagers[playerManagerIndex].OnCardReturnedToPlayer(cardSlotIndex);
            return;
        }

        //waitingForCardDrop = false;
        //timerGroup.SetActive(false);
        placedCardsNumber++;
        
        // FONTOS: El kell távolítani a kártyát a Player objektumból is!
        // activePlayers[playerManagerIndex] = az a Player, aki ledobta a kártyát
        //activePlayers[playerManagerIndex].RemoveCardFromPlayerAtIndex(cardSlotIndex);
        activePlayers[playerManagerIndex].ChangeCardToEmptyCardAtIndex(cardSlotIndex);

        //playerManagers[playerId].RemoveCard(cardSlotIndex);
        //playerManagers[playerId].AddCard(new Card(CardType.NONE, CardBackType.BLUE, CardValue.ZERO), cardSlotIndex);

        //Debug.Log($"DROPPED CARD CURRENT RECTTRANSFORM: {playerManagers[playerId].GetComponent<RectTransform>().rect}");

        playerManagers[playerManagerIndex].ChangeCardToEmptyCard(cardSlotIndex, true);
        
        AllowPlayerToDragCard(playerManagerIndex, true);

        //RefreshPlayerUI();

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

        gameDisplay.ShowToast(toast_FeedbackMessage, $"Kártya letéve! ({placedCardsNumber} db)", 1.5f, GamePhase.Piramis);
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
        
        // Pont osztás indítása
        totalPointsToGive = placedCardsNumber * currentPiramisRow; // multiplier = currentPiramisRow

        gameDisplay.HideCurrentCard(tippCardGroup, 0.5f);
        
        DOVirtual.DelayedCall(0.5f, () =>
        {
            ShowPointGiving(magicWord == "piramis");
        });
    }

    private void OnCardDropTimeout()
    {
        gameDisplay.ShowToast(toast_FeedbackMessage, "Nem tettél le kártyát időben!", 2f, GamePhase.Piramis);
        //Debug.Log($"{activePlayers[currentPlayerIndex].GetPlayerName()} nem tett le kártyát (timeout)");
        
        waitingForCardDrop = false;
        gameDisplay.HideTimer(timerGroup, 0.25f);

        // Drag letiltása
        AllowPlayerToDragCard(0, false);

        // Start Kiosztás Group elrejtése
        pyramidButtonsGroup.gameObject.SetActive(false);

        debugDOmessage += "OnCardDropTimeout() -> ";
        // Következő játékos
        NextPiramisPlayer(2f);
    }

    private void NextPiramisPlayer(float delay = 0f)
    {
        RotatePlayers();
        DOVirtual.DelayedCall(delay, () =>
        {
            currentPlayerIndex++;
            
            if (currentPlayerIndex >= activePlayers.Count)
            {
                debugDOmessage += $"NextPiramisPlayer() = [NO ] => ";
                currentPlayerIndex = 0;
                NextPiramisCard();
            }
            else
            {
                // Következő játékos jön
                //WaitForPlayerToDropCard();
                debugDOmessage += $"NextPiramisPlayer() = [YES] => ";
                CheckIfPlayerCanDropCard();
            }
        });
    }

    /*
    private void CheckIfAnyoneHadCard()
    {
        if (placedCardsNumber == 0)
        {
            ShowToast("Senkinek sem volt azonos kártyája!", 2f);

            DOVirtual.DelayedCall(2f, () =>
            {
                NextPiramisCard();
            });
        }
        else
        {
            NextPiramisCard();
        }
    }
    */

    private void NextPiramisCard()
    {
        currentPiramisCardIndex++;

        int cardsInRow = GameVars.Instance.ReversedPyramidMode ? currentPiramisRow : (6 - currentPiramisRow);

        if (currentPiramisCardIndex >= cardsInRow)
        {
            // Sor véget ért
            //NextPiramisRow();
            debugDOmessage += "NextPiramisCard() = [NO ] => ";
            NextRound();
        }
        else
        {
            // Következő kártya ugyanabban a sorban
            debugDOmessage += "NextPiramisCard() = [YES] => ";
            //Mivel a Flip-ben már nincs CheckIfPlayerCanDropCard hívás, ezért a kártya felfordítása után nem történik semmi.
            FlipPyramidCard();
            //Check kell VAGY StartPiramis()...
            DOVirtual.DelayedCall(0.5f, () =>
            {
                CheckIfPlayerCanDropCard();
            });
        }
    }

    private void NextPiramisRow()
    {
        Debug.Log("currentRound: " + currentRound + "\t currentPiramisRow: " + currentPiramisRow + "\t currentPiramisCardIndex: " + currentPiramisCardIndex);

        currentPiramisRow++;
        
        if (currentPiramisRow > 5)
        {
            // Piramis véget ért
            piramisGroup.SetActive(false);
            NextRound();
        }
        else
        {
            // Következő sor
            currentPiramisCardIndex = 0;
            //TODO DEBUG EZT AZONNAL VISSZARAKNI HA VALAMI NEM JÓ!
            //MÁRMINT LEHET EZ SE JÓ DE LEGALÁBB EZZEL VÉGIG MEGY A PIRAMISON!
            FlipPyramidCard();
            //NextRound();

            //Amikor elkezdünk húzni egy kártyát, akkor a helyére csinálunk egy üres kártyát.
            //Rossz helyre tett kártya esetén vissza rakjuk a húzott káryát, illetve töröljük a helyéről az üreset.
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
        /*
        Debug.Log($"AllowPlayerToDragCard -> playerIndex: \"{playerIndex}\", enabled: \"{enabled}\", playerName: \"{playerManagers[playerIndex].GetPlayerName()}\"");
        
        // Debug ciklus - összes PlayerManager állapota
        string dbgtxtpl = "PlayerManagers állapota:\n";
        for (int I = 0; I < playerManagers.Length; I++)
        {
            if (playerManagers[I] != null && playerManagers[I].gameObject.activeInHierarchy)
            {
                dbgtxtpl += $"  [{I}] Active - Name: {playerManagers[I].GetPlayerName()}\n";
            }
            else if (playerManagers[I] != null)
            {
                dbgtxtpl += $"  [{I}] Inactive\n";
            }
            else
            {
                dbgtxtpl += $"  [{I}] NULL\n";
            }
        }
        Debug.Log(dbgtxtpl);
        */
    }

    #endregion

    #region DEBUG!

    private void FillPlayersWithCards()
    {
        foreach (var player in activePlayers)
        {
            for (int i = 0; i < 5; i++)
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