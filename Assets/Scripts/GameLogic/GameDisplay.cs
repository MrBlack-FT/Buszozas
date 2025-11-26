using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

public class GameDisplay : MonoBehaviour
{
    # region Unity metódusok

    private Debugger debugger;

    void Awake()
    {
        debugger = Resources.FindObjectsOfTypeAll<Debugger>()[0];
    }

    void Update()
    {
        if (debugger != null && debugger.gameObject.activeSelf)
        {
            //debugger.UpdatePersistentLog("isPlayersTransitioning", debugger.ColoredString(isPlayersTransitioning ? "TRUE" : "FALSE", isPlayersTransitioning ? Color.green : Color.red));
        }
    }

    #endregion

    #region Start Buttons 
    public void ShowStartButtons(GameObject startButtonsGroup, float duration = 0.5f, System.Action onComplete = null)
    {
        Button[] buttons = startButtonsGroup.GetComponentsInChildren<Button>(true);

        startButtonsGroup.SetActive(true);

        foreach (var button in buttons)
        {
            button.GetComponent<CustomButtonForeground>().SetInteractiveState(false);
        }

        CanvasGroup canvasGroup = startButtonsGroup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("Start Buttons Group does not have a CanvasGroup component. Adding one...");
            canvasGroup = startButtonsGroup.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, duration).OnComplete(() =>
        {
            foreach (var button in buttons)
            {
                button.GetComponent<CustomButtonForeground>().SetInteractiveState(true);
            }
        });
    }

    public void HideStartButtons(GameObject startButtonsGroup, float duration = 0.5f, System.Action onComplete = null)
    {
        CanvasGroup canvasGroup = startButtonsGroup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("Start Buttons Group does not have a CanvasGroup component. Adding one...");
            startButtonsGroup.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        canvasGroup.DOFade(0f, duration).OnComplete(() =>
        {
            startButtonsGroup.SetActive(false);
            onComplete?.Invoke();
        });
    }

    #endregion

    #region End Buttons Display

    public void ShowEndButtons(GameObject endButtonsGroup, float duration = 0.5f, System.Action onComplete = null)
    {
        endButtonsGroup.SetActive(true);
        CanvasGroup canvasGroup = endButtonsGroup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("End Buttons Group does not have a CanvasGroup component. Adding one...");
            canvasGroup = endButtonsGroup.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, duration).OnComplete(() =>
        {
            onComplete?.Invoke();
        });
    }

    public void HideEndButtons(GameObject endButtonsGroup, float duration = 0.5f, System.Action onComplete = null)
    {
        CanvasGroup canvasGroup = endButtonsGroup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("End Buttons Group does not have a CanvasGroup component. Adding one...");
            endButtonsGroup.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        canvasGroup.DOFade(0f, duration).OnComplete(() =>
        {
            endButtonsGroup.SetActive(false);
            onComplete?.Invoke();
        });
    }

    #endregion

    #region Players Display

    private bool isPlayersTransitioning = false;

    public void ShowPlayers(int activePlayerCount, GameObject playersGroup, float duration = 1f, System.Action onComplete = null)
    {
        if (isPlayersTransitioning) return;
        isPlayersTransitioning = true;

        Transform[] players = new Transform[playersGroup.transform.childCount];
        
        for (int i = 0; i < playersGroup.transform.childCount; i++)
        {
            players[i] = playersGroup.transform.GetChild(i);
        }

        int completedCount = 0;

        for (int i = 0; i < activePlayerCount; i++)                     // Csak activePlayerCount-ig megy
        {
            Transform player = players[i];
            int playerIndex = i;                                        // Játékos index (Player1 = index 0)
            
            Vector3 offset = GetPlayerDirectionOffset(playerIndex);     // Irány meghatározása
            Vector3 originalPosition = player.localPosition;            // Eredeti pozíció mentése

            player.localPosition = originalPosition + offset;           // Kezdő pozíció beállítása (offset-elt pozíció)

            // CanvasGroup kezelés
            CanvasGroup canvasGroup = player.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            Sequence playerSequence = DOTween.Sequence();
            playerSequence.Append(player.DOLocalMove(originalPosition, duration).SetEase(Ease.OutCubic));
            
            if (canvasGroup != null)
            {
                playerSequence.Join(canvasGroup.DOFade(1f, duration));
            }

            // Cards SpriteRenderer-ek fade in (Cards gyermek alatt)
            Transform cardsTransform = player.Find("Cards");
            if (cardsTransform != null)
            {
                SpriteRenderer[] sprites = cardsTransform.GetComponentsInChildren<SpriteRenderer>();
                foreach (var sprite in sprites)
                {
                    Color color = sprite.color;
                    color.a = 0f;
                    sprite.color = color;
                    sprite.DOFade(1f, duration).SetEase(Ease.OutCubic);
                }
            }

            playerSequence.OnComplete(() =>
            {
                completedCount++;
                if (completedCount >= activePlayerCount)
                {
                    onComplete?.Invoke();
                    isPlayersTransitioning = false;
                }
            });
        }

        if (activePlayerCount == 0)
        {
            isPlayersTransitioning = false;
            onComplete?.Invoke();
        }
    }

    public void HidePlayers(int activePlayerCount, GameObject playersGroup, float duration = 1f, System.Action onComplete = null)
    {
        if (isPlayersTransitioning) return;
        isPlayersTransitioning = true;
        Transform[] players = new Transform[playersGroup.transform.childCount];
        
        for (int i = 0; i < playersGroup.transform.childCount; i++)
        {
            players[i] = playersGroup.transform.GetChild(i);
        }

        int completedCount = 0;

        for (int i = 0; i < activePlayerCount; i++) // Csak activePlayerCount-ig megy
        {
            Transform player = players[i];

            int playerIndex = i;
            Vector3 offset = GetPlayerDirectionOffset(playerIndex);
            Vector3 originalPosition = player.localPosition; // Eredeti pozíció mentése

            // CanvasGroup kezelés
            CanvasGroup canvasGroup = player.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = player.gameObject.AddComponent<CanvasGroup>();
            }

            Sequence playerSequence = DOTween.Sequence();
            playerSequence.Append(canvasGroup.DOFade(0f, duration));
            playerSequence.Join(player.DOLocalMove(originalPosition + offset, duration).SetEase(Ease.InCubic));

            // Cards SpriteRenderer-ek fade out
            Transform cardsTransform = player.Find("Cards");
            if (cardsTransform != null)
            {
                SpriteRenderer[] sprites = cardsTransform.GetComponentsInChildren<SpriteRenderer>();
                foreach (var sprite in sprites)
                {
                    sprite.DOFade(0f, duration).SetEase(Ease.InCubic);
                }
            }

            playerSequence.OnComplete(() =>
            {
                player.localPosition = originalPosition; // Eredeti pozíció visszaállítása
                completedCount++;
                if (completedCount >= activePlayerCount)
                {
                    onComplete?.Invoke();
                    isPlayersTransitioning = false;
                }
            });
        }

        if (activePlayerCount == 0)
        {
            isPlayersTransitioning = false;
            onComplete?.Invoke();
        }
    }

    private Vector3 GetPlayerDirectionOffset(int playerIndex)
    {
        if (playerIndex == 0)
        {
            return new Vector3(0, -100, 0);
        }
        // Player2-5 (index 1-4): balról
        else if (playerIndex >= 1 && playerIndex <= 4)
        {
            return new Vector3(-100, 0, 0);
        }
        // Player6 (index 5): felülről
        else if (playerIndex == 5)
        {
            return new Vector3(0, 100, 0);
        }
        // Player7-10 (index 6-9): jobbról
        else if (playerIndex >= 6 && playerIndex <= 9)
        {
            return new Vector3(100, 0, 0);
        }

        // Default: alulról
        return  Vector3.zero;
    }

    #endregion

    #region Timer Display
    public void ShowTimer(GameObject timerGroup, float duration = 0.5f, System.Action onComplete = null)
    {
        timerGroup.GetComponent<Image>().color = Color.blue;
        timerGroup.SetActive(true);
        CanvasGroup canvasGroup = timerGroup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("Timer Group does not have a CanvasGroup component. Adding one...");
            canvasGroup = timerGroup.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, duration).SetEase(Ease.OutCubic);
        DOVirtual.DelayedCall(duration, () => onComplete?.Invoke());
    }

    public void HideTimer(GameObject timerGroup, float duration = 0.5f, System.Action onComplete = null)
    {
        CanvasGroup canvasGroup = timerGroup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("Timer Group does not have a CanvasGroup component. Adding one...");
            timerGroup.SetActive(false);
            timerGroup.GetComponent<Image>().color = Color.blue;
            onComplete?.Invoke();
            return;
        }

        canvasGroup.DOFade(0f, duration).SetEase(Ease.InCubic).OnComplete(() =>
        {
            timerGroup.SetActive(false);
            timerGroup.GetComponent<Image>().color = Color.blue;
            onComplete?.Invoke();
        });
    }

    #endregion

    #region Current Card Display

    public void ShowCurrentCard(GameObject currentCardGroup, float duration = 0.5f, System.Action onComplete = null)
    {
        currentCardGroup.SetActive(true);

        SpriteRenderer[] sprites = currentCardGroup.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sprite in sprites)
        {
            Color color = sprite.color;
            color.a = 0f;
            sprite.color = color;
            sprite.DOFade(1f, duration)/*.SetEase(Ease.OutCubic)*/;
        }

        CanvasGroup canvasGroup = currentCardGroup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("Current Card Group does not have a CanvasGroup component. Adding one...");
            canvasGroup = currentCardGroup.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, duration)/*.SetEase(Ease.OutCubic)*/;
        DOVirtual.DelayedCall(duration, () => onComplete?.Invoke());
    }

    public void HideCurrentCard(GameObject currentCardGroup, float duration = 0.5f, System.Action onComplete = null)
    {
        SpriteRenderer[] sprites = currentCardGroup.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sprite in sprites)
        {
            sprite.DOFade(0f, duration)/*.SetEase(Ease.InCubic)*/;
        }

        CanvasGroup canvasGroup = currentCardGroup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("Current Card Group does not have a CanvasGroup component. Adding one...");
            currentCardGroup.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        canvasGroup.DOFade(0f, duration)/*.SetEase(Ease.InCubic)*/.OnComplete(() =>
        {
            currentCardGroup.SetActive(false);
            onComplete?.Invoke();
        });
    }

    public void HighlightBuszCard(CardManager[] buszCards, int cardIndex, bool highlight)
    {
        if (cardIndex < 0 || cardIndex >= buszCards.Length) return;

        buszCards[cardIndex].GetComponent<Image>().color = highlight ? new Color(0.5f, 0f, 1f, 1f) : new Color(0f, 0f, 0f, 0f);
    }

    #endregion

        #region Piramis Display

    public void ShowPiramis(GameObject piramisGroup, float duration = 1f, System.Action onComplete = null)
    {
        piramisGroup.SetActive(true);

        /*
        // CanvasGroup fade (ha van UI Image)
        CanvasGroup canvasGroup = piramisGroup.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, duration).SetEase(Ease.OutCubic);
        }
        */

        // SpriteRenderer-ek fade
        SpriteRenderer[] sprites = piramisGroup.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sprite in sprites)
        {
            Color color = sprite.color;
            color.a = 0f;
            sprite.color = color;
            sprite.DOFade(1f, duration).SetEase(Ease.OutCubic);
        }
        DOVirtual.DelayedCall(duration, () => onComplete?.Invoke());
    }

    public void HidePiramis(GameObject piramisGroup, float duration = 1f, System.Action onComplete = null)
    {
        /*
        // CanvasGroup fade
        CanvasGroup canvasGroup = piramisGroup.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, duration).SetEase(Ease.InCubic);
        }
        */

        // SpriteRenderer-ek fade
        SpriteRenderer[] sprites = piramisGroup.GetComponentsInChildren<SpriteRenderer>();
        int fadeCount = sprites.Length;

        if (fadeCount == 0)
        {
            piramisGroup.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        foreach (var sprite in sprites)
        {
            sprite.DOFade(0f, duration)/*.SetEase(Ease.InCubic)*/.OnComplete(() =>
            {
                fadeCount--;
                if (fadeCount <= 0)
                {
                    piramisGroup.SetActive(false);
                    onComplete?.Invoke();
                }
            });
        }
    }

    #endregion

    #region Busz Display

    public void ShowBusz(GameObject buszGroup, CardManager[] buszCards, float duration = 1f, System.Action onComplete = null)
    {
        // ELŐSZÖR: Minden kártya hátlapját aktívvá tesszük (reset a kezdőállapotba)
        foreach (var card in buszCards)
        {
            card.ShowCardBack();
        }

        buszGroup.SetActive(true);

        /*
        // CanvasGroup fade (ha van UI Image)
        CanvasGroup canvasGroup = buszGroup.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, duration).SetEase(Ease.OutCubic);
        }
        */

        // SpriteRenderer-ek fade
        SpriteRenderer[] sprites = buszGroup.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sprite in sprites)
        {
            Color color = sprite.color;
            color.a = 0f;
            sprite.color = color;
            sprite.DOFade(1f, duration).SetEase(Ease.OutCubic);
        }

        
        // Kártyák felfordítása
        int delayIndex = 0;
        for (int I = 0; I < buszCards.Length; I++)
        {
            if (I != buszCards.Length - 2) // az utolsó előtti kimarad
            {
                int index = I;
                float delay = delayIndex * 0.25f;

                DOVirtual.DelayedCall(delay, () => buszCards[index].AnimateCardFlip(0.5f));

                delayIndex++; // csak akkor növeljük, ha tényleg animáltunk
            }
        }
        
        DOVirtual.DelayedCall(duration, () => onComplete?.Invoke());
    }

    public void HideBusz(GameObject buszGroup, CardManager[] buszCards, float duration = 1f, System.Action onComplete = null)
    {
        /*
        // CanvasGroup fade
        CanvasGroup canvasGroup = buszGroup.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, duration).SetEase(Ease.InCubic);
        }
        */

        /*
        //Kártyák lefordítása
        int delayIndex = 0;
        for (int I = 0; I < buszCards.Length; I++)
        {
            if (I != buszCards.Length - 2)
            {
                int index = I;
                float delay = delayIndex * 0.25f;

                DOVirtual.DelayedCall(delay, () => buszCards[index].AnimateCardFlip(0.5f));

                delayIndex++;
            }
        }
        */

        // SpriteRenderer-ek fade
        SpriteRenderer[] sprites = buszGroup.GetComponentsInChildren<SpriteRenderer>();
        int fadeCount = sprites.Length;

        if (fadeCount == 0)
        {
            buszGroup.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        foreach (var sprite in sprites)
        {
            sprite.DOFade(0f, duration)/*.SetEase(Ease.InCubic)*/.OnComplete(() =>
            {
                fadeCount--;
                if (fadeCount <= 0)
                {
                    buszGroup.SetActive(false);
                    onComplete?.Invoke();
                }
            });
        }
    }

    #endregion

    #region Tipp Groups Display

    public void ShowTippGroups(GameObject tippGroupsGroup, float duration = 0.5f)
    {
        tippGroupsGroup.SetActive(true);

        CanvasGroup canvasGroup = tippGroupsGroup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("Tipp Groups Group does not have a CanvasGroup component. Adding one...");
            canvasGroup = tippGroupsGroup.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, duration).SetEase(Ease.OutCubic);
    }

    public void HideTippGroups(GameObject tippGroupsGroup, float duration = 0.5f)
    {
        CanvasGroup canvasGroup = tippGroupsGroup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("Tipp Groups Group does not have a CanvasGroup component. Adding one...");
            tippGroupsGroup.SetActive(false);
            return;
        }

        canvasGroup.DOFade(0f, duration).SetEase(Ease.InCubic).OnComplete(() =>
        {
            tippGroupsGroup.SetActive(false);
        });
    }

    #endregion

    #region Piramis Buttons Display

    public void ShowPiramisButtons(GameObject buttonGroup, float duration = 0.5f)
    {
        buttonGroup.SetActive(true);

        CanvasGroup canvasGroup = buttonGroup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("Piramis Button Group does not have a CanvasGroup component. Adding one...");
            canvasGroup = buttonGroup.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, duration).SetEase(Ease.OutCubic);
    }

    public void HidePiramisButtons(GameObject buttonGroup, float duration = 0.5f, System.Action onComplete = null)
    {
        CanvasGroup canvasGroup = buttonGroup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("Piramis Button Group does not have a CanvasGroup component. Adding one...");
            buttonGroup.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        canvasGroup.DOFade(0f, duration).SetEase(Ease.InCubic).OnComplete(() =>
        {
            buttonGroup.SetActive(false);
            onComplete?.Invoke();
        });
    }

    #endregion

    #region Busz Buttons Display

    public void ShowBuszButtons(GameObject buttonGroup, float duration = 0.5f)
    {
        buttonGroup.SetActive(true);

        CanvasGroup canvasGroup = buttonGroup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("Busz Button Group does not have a CanvasGroup component. Adding one...");
            canvasGroup = buttonGroup.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, duration).SetEase(Ease.OutCubic);
    }

    public void HideBuszButtons(GameObject buttonGroup, float duration = 0.5f, System.Action onComplete = null)
    {
        CanvasGroup canvasGroup = buttonGroup.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("Busz Button Group does not have a CanvasGroup component. Adding one...");
            buttonGroup.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        canvasGroup.DOFade(0f, duration).SetEase(Ease.InCubic).OnComplete(() =>
        {
            buttonGroup.SetActive(false);
            onComplete?.Invoke();
        });
    }

    #endregion

    #region UI Display

    public void ShowToast(GameObject toastObject, string message, bool isLongMessage, float duration = 2f, GamePhase gamePhase = default)
    {
        if (toastObject == null) return;

        // GamePhase alapján beállítjuk a toast Y pozícióját
        RectTransform toastRect = toastObject.GetComponent<RectTransform>();
        switch (gamePhase)
        {
            case GamePhase.Tipp:
                toastRect.anchoredPosition = new Vector2(0, 430);
                toastRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600);
                break;
            case GamePhase.Piramis:
                int xPos = isLongMessage ? -150 : 0;
                toastRect.anchoredPosition = new Vector2(xPos, 180);
                toastRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 850);
                break;
            case GamePhase.Busz:
                toastRect.anchoredPosition = new Vector2(0, 310);
                toastRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600);
                break;
            case GamePhase.JatekVege:
                toastRect.anchorMin = new Vector2(0.5f, 0.5f);
                toastRect.anchorMax = new Vector2(0.5f, 0.5f);
                toastRect.pivot = new Vector2(0.5f, 0.5f);
                toastRect.anchoredPosition =  new Vector2(0, 50);
                toastRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 800);
                toastRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 800);
                break;
            default:
                Debug.Log("DEFAULT CASE IN SHOWTOAST!");
                toastRect.anchoredPosition = new Vector2(0, 0);
                toastRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 600);
                break;
        }

        TextMeshProUGUI toastText = toastObject.GetComponentInChildren<TextMeshProUGUI>();
        if (toastText != null)
        {
            toastText.text = message;
        }

        toastObject.SetActive(true);

        // Fade in
        CanvasGroup canvasGroup = toastObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = toastObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.25f);

        if (duration < 0f)
            return; // Ha negatív az idő, akkor nem Fade out-olunk és nem deaktiváljuk.
        
        // Fade out után duration idővel
        DOVirtual.DelayedCall(duration, () =>
        {
            canvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
            {
                toastObject.SetActive(false);
            });
        });
    }

    public void ShowPanel(GameObject panel, float duration = 0.5f)
    {
        panel.SetActive(true);

        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = panel.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, duration).SetEase(Ease.OutCubic);
    }

    public void HidePanel(GameObject panel, float duration = 0.5f, System.Action onComplete = null)
    {
        CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            panel.SetActive(false);
            onComplete?.Invoke();
            return;
        }

        canvasGroup.DOFade(0f, duration).SetEase(Ease.InCubic).OnComplete(() =>
        {
            panel.SetActive(false);
            onComplete?.Invoke();
        });
    }

    #endregion
}