using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class GameVarsUI : MonoBehaviour
{
    #region UI Referenciák
    [Header("SinglePlayer UI")]
    [SerializeField] private TMP_InputField[] playerNameInputFieldsSinglePlayer;
    [SerializeField] private Slider numberOfPlayersSliderSinglePlayer;
    [SerializeField] private Toggle revPyramidModeToggleSinglePlayer;

    [Header("MultiPlayer UI")]
    [SerializeField] private TMP_InputField busNameInputFieldMultiPlayer;
    [SerializeField] private Slider numberOfPlayersSliderMultiPlayer;
    [SerializeField] private Toggle revPyramidModeToggleMultiPlayer;

    [Header("Warning")]
    [SerializeField] private TextMeshProUGUI warningText;
    #endregion

    void Start()
    {
        if (GameVars.Instance != null)
        {
            InitializeUI();
        }
        else
        {
            Debug.LogWarning("GameVars.Instance is NULL! GameVarsUI cannot initialize.");
        }
    }

    public void InitializeUI()
    {
        LoadDataFromGameVars();
        RegisterListeners();
    }

    private void LoadDataFromGameVars()
    {
        if (GameVars.Instance == null) return;

        if (playerNameInputFieldsSinglePlayer != null)
        {
            for (int i = 0; i < playerNameInputFieldsSinglePlayer.Length; i++)
            {
                if (i < GameVars.Instance.NumberOfPlayersInGame)
                {
                    playerNameInputFieldsSinglePlayer[i].text = GameVars.Instance.GetPlayerName(i);
                }
            }
        }

        if (numberOfPlayersSliderSinglePlayer != null)
        {
            numberOfPlayersSliderSinglePlayer.value = GameVars.Instance.NumberOfPlayersInGame;
        }
        if (revPyramidModeToggleSinglePlayer != null)
        {
            revPyramidModeToggleSinglePlayer.isOn = GameVars.Instance.ReversedPyramidMode;
        }


        if (busNameInputFieldMultiPlayer != null)
        {
            busNameInputFieldMultiPlayer.text = GameVars.Instance.BusName;
        }
        if (numberOfPlayersSliderMultiPlayer != null)
        {
            numberOfPlayersSliderMultiPlayer.value = GameVars.Instance.NumberOfPlayersInGame;
        }
        if (revPyramidModeToggleMultiPlayer != null)
        {
            revPyramidModeToggleMultiPlayer.isOn = GameVars.Instance.ReversedPyramidMode;
        }
        
    }

    private void RegisterListeners()
    {
        if (GameVars.Instance == null) return;

        if (playerNameInputFieldsSinglePlayer != null)
        {
            for (int i = 0; i < playerNameInputFieldsSinglePlayer.Length; i++)
            {
                int index = i;
                playerNameInputFieldsSinglePlayer[i].onValueChanged.AddListener((value) =>
                {
                    // InputField érték változása esetén beállítja a GameVars-ban a megfelelő játékos nevét
                    if (index < GameVars.Instance.NumberOfPlayersInGame)
                    {
                        GameVars.Instance.SetPlayerName(index, value);
                    }
                });
            }
        }

        if (busNameInputFieldMultiPlayer != null)
        {
            busNameInputFieldMultiPlayer.onValueChanged.AddListener((value) =>
            {
                // Busz név változása esetén beállítja a GameVars-ban a busz nevét
                GameVars.Instance.BusName = value;
            });
        }

        if (numberOfPlayersSliderSinglePlayer != null)
        {
            numberOfPlayersSliderSinglePlayer.onValueChanged.AddListener((value) =>
            {
                GameVars.Instance.SetNumberOfPlayers((int)value);   // Beállítja a GameVars-ban a játékosok számát
                SyncSlidersValues();                                // Szinkronizálja a másik slider értékét
                UpdatePlayerNameFields();                           // Frissíti az InputField-eket
            });
        }

        if (numberOfPlayersSliderMultiPlayer != null)
        {
            numberOfPlayersSliderMultiPlayer.onValueChanged.AddListener((value) =>
            {
                GameVars.Instance.SetNumberOfPlayers((int)value);   // Beállítja a GameVars-ban a játékosok számát
                SyncSlidersValues();                                // Szinkronizálja a másik slider értékét
                UpdatePlayerNameFields();                           // Frissíti az InputField-eket
            });
        }

        if (revPyramidModeToggleSinglePlayer != null)
        {
            revPyramidModeToggleSinglePlayer.onValueChanged.AddListener((value) =>
            {
                GameVars.Instance.ReversedPyramidMode = value;      // Beállítja a GameVars-ban a fordított piramis módot
                SyncToggles();                                      // Szinkronizálja a másik toggle állapotát
            });
        }

        if (revPyramidModeToggleMultiPlayer != null)
        {
            revPyramidModeToggleMultiPlayer.onValueChanged.AddListener((value) =>
            {
                GameVars.Instance.ReversedPyramidMode = value;      // Beállítja a GameVars-ban a fordított piramis módot 
                SyncToggles();                                      // Szinkronizálja a másik toggle állapotát
            });
        }
    }

    private void UpdatePlayerNameFields()
    {
        if (GameVars.Instance == null || playerNameInputFieldsSinglePlayer == null) return;

        int players = GameVars.Instance.NumberOfPlayersInGame;

        for (int i = 0; i < playerNameInputFieldsSinglePlayer.Length; i++)
        {
            if (i < players)
            {
                playerNameInputFieldsSinglePlayer[i].text = GameVars.Instance.GetPlayerName(i);
            }
        }
    }

    public void SyncToggles()
    {
        if (GameVars.Instance == null) return;

        bool reversed = GameVars.Instance.ReversedPyramidMode;

        if (revPyramidModeToggleSinglePlayer != null)
        {
            revPyramidModeToggleSinglePlayer.isOn = reversed;
        }

        if (revPyramidModeToggleMultiPlayer != null)
        {
            revPyramidModeToggleMultiPlayer.isOn = reversed;
        }
    }

    public void SyncSlidersValues()
    {
        if (GameVars.Instance == null) return;

        int players = GameVars.Instance.NumberOfPlayersInGame;

        if (numberOfPlayersSliderSinglePlayer != null)
        {
            numberOfPlayersSliderSinglePlayer.value = players;
        }

        if (numberOfPlayersSliderMultiPlayer != null)
        {
            numberOfPlayersSliderMultiPlayer.value = players;
        }
    }

    public void ShowWarning(string message)
    {
        if (warningText != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowWarningCoroutine(message));
        }
    }

    private IEnumerator ShowWarningCoroutine(string message)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);
        yield return new WaitForSeconds(3f);
        warningText.gameObject.SetActive(false);
    }
}
