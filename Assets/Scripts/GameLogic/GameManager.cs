using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    #region Változók

    [Header("Debugger")]
    [SerializeField] private Debugger debugger;

    [Header("Játékosok")]
    [SerializeField] private PlayerManager[] playerManagers = new PlayerManager[10];
    private List<Player> activePlayers = new List<Player>();

    [Space(10)]
    [Header("Játék események")]
    [SerializeField] private GameEvents gameEvents;

    [Header("UI referenciák")]
    [SerializeField] private GameObject startButtons;
    [SerializeField] private GameObject timerGroup;
    [SerializeField] private GameObject feedbackMessageText;
    private TextMeshProUGUI timerText;

    [Header("Tipp gombok csoportjai")]
    [SerializeField] private GameObject redOrBlackGroup;
    [SerializeField] private GameObject belowOrAboveGroup;
    [SerializeField] private GameObject betweenOrApartGroup;
    [SerializeField] private GameObject exactColorGroup;
    [SerializeField] private GameObject exactNumberGroup;

    [Header("Piramis")]
    [SerializeField] private GameObject piramisGroup;

    [Header("Jelenleg húzott kártya")]
    [SerializeField] private GameObject tippCardGroup;
    private CardManager currentCard;

    [Header("Pont osztás UI")]
    [SerializeField] private GameObject pointGiveGroup;
    [SerializeField] private Button confirmPointGiveButton;

    [Header("Piramis UI")]
    [SerializeField] private GameObject startGivePointGroup;

    [Header("Játék állapot")]
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
    private int letettKartyakSzama = 0;         // Játékos hány kártyát tett le
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
            Debug.Log("GameManager: GameVars instance not found, loading prefab...");
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

        startButtons.SetActive(true);
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
                OnTimeExpired();
            }
        }
        
        // Pont osztás timer kezelés
        if (isGivingPoints && pointGiveTimer > 0)
        {
            pointGiveTimer -= Time.deltaTime;
            timerText.text = "Pont kiosztása: " + pointGiveTimer.ToString("F2");

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
            timerText.text = "Kártya lerakása: " + cardDropTimer.ToString("F2");

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

    }

    #endregion

    #region Game State Machine

    private void InitializeGame()
    {
        /*
        currentPhase = GamePhase.Tipp;
        currentRound = 1;
        currentPlayerIndex = 0;

        StartTippKor();
        */

        currentPhase = GamePhase.Piramis;
        currentRound = 1;
        currentPlayerIndex = 0;
        FillPlayersWithCards();
        FillPiramisWithCards();
        piramisGroup.SetActive(true);
        RefreshPlayerUI();
        StartPiramis();

    }

    private void StartTippKor()
    {
        currentTippType = GetTippTypeForRound(currentRound);

        // Jelenleg húzott kártya hátuljának megjelenítése
        currentCard.SetCard(deck.DrawCard());
        currentCard.ShowCardBack();
        tippCardGroup.SetActive(true);
        
        // Megfelelő tipp gombok megjelenítése
        DisplayTippGroup(currentTippType);

        // Timer indítása
        StartTimer();
    }

    private void StartPiramis()
    {
        Debug.Log($"Starting Piramis! Current Round: {currentRound}");
        currentPiramisRow = 1;
        currentPiramisCardIndex = 0;
        currentPlayerIndex = 0;
        
        piramisGroup.SetActive(true);
        
        FlipPyramidCard();
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
                    tippCardGroup.SetActive(false);
                    currentCard.SetEmptyCard();
                    currentPhase = GamePhase.Piramis;
                    StartPiramis();
                }
                break;

            case GamePhase.Piramis:
                if (currentRound < 5)
                {
                    currentRound++;
                    StartPiramis();
                }
                else
                {
                    piramisGroup.SetActive(false);
                    FillPiramisWithEmptyCards();
                    currentPhase = GamePhase.Busz;
                    StartBusz();
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





    #region Setup

    public void StartGame()
    {
        startButtons.SetActive(false);
        InitializeGame();
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

    public void RotatePlayers()
    {
        if (activePlayers.Count <= 1) return;

        Player firstPlayer = activePlayers[0];
        activePlayers.RemoveAt(0);
        activePlayers.Add(firstPlayer);

        RefreshPlayerUI();
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
        redOrBlackGroup.SetActive(false);
        belowOrAboveGroup.SetActive(false);
        betweenOrApartGroup.SetActive(false);
        exactColorGroup.SetActive(false);
        exactNumberGroup.SetActive(false);

        switch (tippType)
        {
            case TippType.PirosVagyFekete:
                redOrBlackGroup.SetActive(true);
                break;
            case TippType.AlattaVagyFelette:
                belowOrAboveGroup.SetActive(true);
                break;
            case TippType.KozteVagySzet:
                betweenOrApartGroup.SetActive(true);
                break;
            case TippType.PontosTipus:
                exactColorGroup.SetActive(true);
                break;
            case TippType.PontosSzam:
                exactNumberGroup.SetActive(true);
                break;
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

        timerGroup.SetActive(true);
        timerText.text = "Választási idő:\n" + currentTimer.ToString("F2");

        waitingForTipp = true;
    }

    private void OnTimeExpired()
    {
        timerGroup.SetActive(false);
        timerGroup.GetComponent<Image>().color = Color.blue;

        Debug.LogWarning($"Time expired! Generating random tipp for {currentTippType}");

        TippValue randomTipp = GetRandomTippForType(currentTippType);
        activePlayers[0].SetTipp(randomTipp);
        waitingForTipp = false;
        
        DisplayTippGroup(TippType.NONE);
        
        ProcessTipp();
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
        timerGroup.SetActive(false);
        timerGroup.GetComponent<Image>().color = Color.blue;
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
            // 2 másodperc várakozás, majd pont osztás
            DOVirtual.DelayedCall(2f, () =>
            {
                RefreshPlayerUI();
                ShowPointGiving();
            });
        }
        else
        {
            activePlayers[0].IncreasePlayerScore(currentRound);

            TippContinue(2f);
        }
    }

    private void TippContinue(float delay)
    {
        DOVirtual.DelayedCall(delay, () =>
        {
            RefreshPlayerUI();
            RotatePlayers();
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
    private void ShowPointGiving()
    {
        totalPointsToGive = currentRound;
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
        timerGroup.SetActive(true);
        pointGiveTimer = 15f;
        isGivingPoints = true;
        
        UpdatePointGiveUI();
    }

    private void HidePointGive()
    {
        isGivingPoints = false;
        pointGiveGroup.SetActive(false);
        timerGroup.SetActive(false);

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

    private void UpdatePointGiveUI()
    {
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
        
        confirmPointGiveButton.interactable = currentTotal == totalPointsToGive;
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
            TippContinue(1f);
        }
        else if (currentPhase == GamePhase.Piramis)
        {
            DOVirtual.DelayedCall(1f, () =>
            {
                NextPiramisPlayer();
            });
        }
    }

    private void OnPointGiveTimeout()
    {
        HidePointGive();
        
        if (currentPhase == GamePhase.Tipp)
        {
            TippContinue(1f);
        }
        else if (currentPhase == GamePhase.Piramis)
        {
            DOVirtual.DelayedCall(1f, () =>
            {
                NextPiramisPlayer();
            });
        }
    }

    #endregion

    #endregion

    #region Piramis Management

    private void FlipPyramidCard()
    {
        // Kártyák száma az aktuális sorban
        int cardsInRow = GameVars.Instance.ReversedPyramidMode ? currentPiramisRow : (6 - currentPiramisRow);;
        
        Debug.Log($"Felfordítás: Row_{currentPiramisRow}, Card {currentPiramisCardIndex + 1}/{cardsInRow}");
        
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
        
        // Kártya felfordítás animáció
        currentPiramisCard.AnimateCardFlip(1f);
        
        // 1s várakozás, majd játékosok kártyaletételének megkezdése
        DOVirtual.DelayedCall(1f, () =>
        {
            WaitForPlayerToDropCard();
        });
    }

    private void WaitForPlayerToDropCard()
    {
        currentPlayerIndex = 0;
        letettKartyakSzama = 0;
        
        // Timer indítása (30s)
        cardDropTimer = 30f;
        waitingForCardDrop = true;
        timerGroup.SetActive(true);
        timerGroup.GetComponent<Image>().color = Color.blue;
        timerText.text = "Kártya letevés: 30s";
        
        // Toast üzenet: "Játékos neve: Tedd le a kártyád!"
        ShowToast($"{activePlayers[currentPlayerIndex].GetPlayerName()}: Tedd le azonos értékű kártyát!", 3f);
        
        // Játékos kártyáinak drag engedélyezése
        EnablePlayerCardDrag(currentPlayerIndex, true);
        
        // Start Kiosztás Group elrejtése
        startGivePointGroup.gameObject.SetActive(false);
    }

    private void HandleCardDroppedToPiramis(int playerId, Card playerCard, int cardSlotIndex)
    {
        // Validálás: Azonos érték?
        if (playerCard.GetCardValue() != currentPiramisCard.GetCardData().GetCardValue())
        {
            ShowToast("Hibás kártya! Azonos értékűt tehetsz csak le!", 2f);
            return;
        }
        
        // Kártya eltávolítása a játékostól
        activePlayers[currentPlayerIndex].RemoveCardFromPlayerAtIndex(cardSlotIndex);
        letettKartyakSzama++;
        
        RefreshPlayerUI();
        
        // Start Kiosztás gomb megjelenítése
        int multiplier = currentPiramisRow;
        int pointsToDistribute = letettKartyakSzama * multiplier;
        
        TextMeshProUGUI buttonText = startGivePointGroup.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = $"Kiosztás\n({pointsToDistribute} pont)";
        }
        
        startGivePointGroup.gameObject.SetActive(true);
        
        ShowToast($"Kártya letéve! ({letettKartyakSzama} db)", 1.5f);
    }

    public void OnStartTippButtonClicked()
    {
        // Timer leállítása
        waitingForCardDrop = false;
        timerGroup.SetActive(false);
        timerGroup.GetComponent<Image>().color = Color.blue;
        
        // Drag letiltása
        EnablePlayerCardDrag(currentPlayerIndex, false);

        // Start Kiosztás Group elrejtése
        startGivePointGroup.gameObject.SetActive(false);
        
        // Pont osztás indítása
        int multiplier = currentPiramisRow;
        totalPointsToGive = letettKartyakSzama * multiplier;
        
        ShowPointGiving();
    }

    private void OnCardDropTimeout()
    {
        Debug.Log($"{activePlayers[currentPlayerIndex].GetPlayerName()} nem tett le kártyát (timeout)");
        
        waitingForCardDrop = false;
        timerGroup.SetActive(false);
        timerGroup.GetComponent<Image>().color = Color.blue;

        // Drag letiltása
        EnablePlayerCardDrag(currentPlayerIndex, false);

        // Start Kiosztás Group elrejtése
        startGivePointGroup.gameObject.SetActive(false);

        // Következő játékos
        NextPiramisPlayer();
    }

    private void NextPiramisPlayer()
    {
        currentPlayerIndex++;
        
        if (currentPlayerIndex >= activePlayers.Count)
        {
            // Minden játékos végzett ezzel a kártyával
            CheckIfAnyoneHadCard();
        }
        else
        {
            // Következő játékos jön
            WaitForPlayerToDropCard();
        }
    }

    private void CheckIfAnyoneHadCard()
    {
        if (letettKartyakSzama == 0)
        {
            ShowToast("Senkinek sem volt azonos kártyája!", 2f);
            
            DOVirtual.DelayedCall(2f, () =>
            {
                NextPiramisCard();
            });
        }
        else
        {
            // Valaki letett kártyát, de nem osztotta ki a pontokat
            NextPiramisCard();
        }
    }

    private void NextPiramisCard()
    {
        currentPiramisCardIndex++;

        int cardsInRow = GameVars.Instance.ReversedPyramidMode ? currentPiramisRow : (6 - currentPiramisRow);

        if (currentPiramisCardIndex >= cardsInRow)
        {
            // Sor véget ért
            NextPiramisRow();
        }
        else
        {
            // Következő kártya ugyanabban a sorban
            FlipPyramidCard();
        }
    }

    private void NextPiramisRow()
    {
        currentPiramisRow++;
        
        if (currentPiramisRow > 5)
        {
            // Piramis véget ért
            piramisGroup.SetActive(false);
            NextRound(); // → Busz
        }
        else
        {
            // Következő sor
            currentPiramisCardIndex = 0;
            FlipPyramidCard();
        }
    }

    private void EnablePlayerCardDrag(int playerIndex, bool enabled)
    {
        // TODO: PlayerManager-ben kell implementálni
        // playerManagers[playerIndex].SetCardsDraggable(enabled);
        playerManagers[playerIndex].SetInteractive(enabled);
        playerManagers[playerIndex].SetCardsDraggable(enabled);
        Debug.Log($"Player {playerIndex} drag enabled: {enabled}");
    }

    private void ShowToast(string message, float duration)
    {
        if (feedbackMessageText == null)
        {
            Debug.LogWarning("Toast Text nincs beállítva!");
            return;
        }

        feedbackMessageText.GetComponent<TextMeshProUGUI>().text = message;
        feedbackMessageText.SetActive(true);
        feedbackMessageText.GetComponent<CanvasGroup>().DOFade(1, 0.5f).SetEase(Ease.OutCubic);

        DOVirtual.DelayedCall(duration, () =>
        {
            feedbackMessageText.GetComponent<CanvasGroup>().DOFade(0, 0.5f).SetEase(Ease.OutCubic).OnComplete(() =>
            {
                feedbackMessageText.SetActive(false);
            });
        });
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